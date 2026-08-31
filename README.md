# LostRelic 失落遗物

3D 第一人称地牢探索 ARPG 垂直切片。**Unity 2021.3.45f1c1（中国版 LTS）+ URP + XLua**。

C# 只做框架、玩法全部在 Lua、配置全部走 JSON —— 23 个 `.cs`（3117 行）、20 个 Lua 模块（3424 行）、
5 份 JSON（262 行）。一条从进入到终局可完整走通的流程：探索遗迹、找回 4 只猫头鹰雕像、
与老向导对话、与守卫战斗。

> **仓库不含美术素材**（约 384 MB，来自 Asset Store 及第三方素材包，多数授权禁止二次分发）。
> clone 后打开场景会看到洋红色缺失材质，但**代码、prefab 组件配置、动画状态机、场景编排全部完整**，
> 读代码不受影响。详见[文末](#运行与素材)。

本文讲四件事：**游戏框架**、**性能优化**、**AI 实现历程与思路**、**AI 使用经验、流程与注意事项**。

---

## 一、游戏框架

设计目标只有一条：**改玩法、改数值、改流程都不碰 C#、不重编译。**

### 三层分工

```
        C# 框架层（Assets/_Project/Scripts）          Lua 玩法层（Assets/_Project/Lua）
                                                     ── 可热更 ──────────────────────
  GameBootstrap      自动挂载 / 驱动 Tick    ──────►  main.lua            装配与生命周期
  XLuaManager        LuaEnv + 自定义 loader            player_ctrl         移动 / 视角 / 攻击
  ResService         Addressables 按路径取址           enemy_ctrl          敌人状态机
  DataService        JSON 读取 + 静态缓存              quest_ctrl          任务推进
  EventCenter    ◄──────── 事件双向 ────────►         event_bus           事件总线
  UIManager          面板查找与显隐                    dialog_ctrl         逐字对话
  AudioService       BGM / SFX                         inventory_*         背包 MVC
  InputService       输入聚合                          player_attr_*       属性 MVC
  ComponentFactory   运行时组件装配                    ui_ctrl             面板调度
  EnemyAlertZone     警戒范围触发                      camera_ctrl / audio_ctrl
  Interactable / RelicGuard                            enemy_hp_view / json_util
```

C# 层**不含任何玩法规则**。它提供的是能力：实例化、加载资源、读 JSON、发事件、运行时装组件。
规则在 Lua，数据在 JSON。

### 整个生命周期只有 3 个 C# 调用点

`GameBootstrap` 是 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`，不需要手工放进场景 ——
进 Play 时它自己建出来（`GameBootstrap.cs:12-24`），然后：

| C# 侧 | → Lua 侧 |
|---|---|
| `Start()` → `XLuaManager.Initialize("main")` | `main = require 'main'; main.start()` |
| `Update()` → `XLuaManager.Tick()` | 取全局函数 `on_update`，`Call(Time.deltaTime)` |
| `OnDestroy()` → `Dispose()` | 全局 `on_shutdown()`，再清 JSON 缓存与事件表 |

`main.start()`（`main.lua.txt:60`）载入 5 份 JSON、按数组遍历生成敌人与雕像、依次 `init` 13 个模块，
最后把 `on_update` / `on_shutdown` 挂到 Lua 全局上。**C# 从此不再参与调度** ——
`main.update(dt)` 自己决定这一帧谁 tick、面板打开时哪些逻辑该让路（`main.lua.txt:178-215`）。

### 事件双向

`event_bus.publish` 先派发给 Lua 订阅者，再镜像一份到 C# 的 `EventCenter.Publish`
（`event_bus.lua.txt:26-40`）。所以同一个事件两侧都收得到，而 Lua 侧不需要知道 C# 有没有人在听 ——
UI、音频、HP 条都是靠这条总线解耦的，没有任何模块之间的直接引用。

### 双通道资源加载

`XLuaManager` 和 `DataService` 用同一套策略：**编辑器下直读磁盘源文件，运行时回落 Addressables。**

```csharp
// XLuaManager.cs:26-58，自定义 AddLoader
if (Application.isEditor) {
    var filePath = Path.Combine(Application.dataPath, "_Project", "Lua", module + ".lua.txt");
    if (File.Exists(filePath)) text = File.ReadAllText(filePath);
}
if (string.IsNullOrEmpty(text)) text = ResService.LoadText(address);
```

`DataService.cs:13` 的 `LoadJson` 对 JSON 做同样的事。**改一行 Lua 或一个数值，进 Play 立即生效，
不必重建 Addressables**；而打包路径完全没变，仍然走 Addressables 取址。

配置改完若没生效，是静态缓存没失效 —— 调 `DataService.ClearCache()`（`GameBootstrap.OnDestroy` 已自动调用）。

### 数据驱动：逐字段合并

`Assets/_Project/Data/` 下 5 份配置，敌人与雕像都是遍历数组生成：

| 文件 | 内容 |
|---|---|
| `spawn_config.json` | 出生点、玩家属性、敌人属性、警戒/追击/巡逻半径 |
| `quest_config.json` | 主线任务与完成条件 |
| `dialog_config.json` | 对话文本与 `onComplete` 动作 |
| `item_config.json` | 道具定义与堆叠规则 |
| `audio_config.json` | BGM / SFX 映射 |

敌人的 15 项数值走**逐字段合并**：JSON 里写了的键覆盖组件（`0` 也算写了），没写的键保留
Inspector 上手调的值。玩家的 5 项（`player.stats`）规则一致，兜底值集中在 `player_attr.lua` 的 `DEFAULTS`。

**这层合并必须放在 Lua，不能放 C#** —— 原因见下面第三章的失效案例 5。注册日志行尾会写明这组数值
来自 JSON 还是 Inspector，省掉「改了没生效」的来回排查。

---

## 二、性能优化

### 静态合批：batches 613 → 254（−59%）

给场景中 **492 个静态物体**打上 `StaticEditorFlags` 组合值 **78**
（Batching + Occluder + Occludee + Navigation）后实测：

| 指标 | 前 | 后 |
|---|---|---|
| **batches** | 613 | **254**（−59%） |
| setPassCalls | 不变 | 不变 |
| triangles | 不变 | 不变 |

`setPassCalls` 与三角形数不变、只有 batches 下降，说明收益确实来自**静态合批的顶点合并**，
而不是剔除或材质切换减少 —— 这两项如果起作用，动的会是另外两个指标。

**一个容易漏掉的开关**：给物体打标记只做了一半。`ProjectSettings.asset` 里每个构建目标各有一份
`m_StaticBatching`，**Standalone 默认是 `0`** —— 标记打满了但这个开关没翻，合批在构建里根本不生效
（编辑器 Play 下反而看得到效果，所以很容易误判成已经做完了）。提交 `40e1b41` 就是补这一步。

### 遮挡剔除：烤出来是空的（负面结论）

**Bake Occlusion Culling 对这个场景无效**，记在这里是因为它比正面结论更说明问题：

这套地牢 tileset 是**壁厚约 0.2 m 的空心壳**。Umbra 的烘焙是体素化的，只把**被完全填满**的格子
标记为不透明；薄壳撑不满任何一个体素，于是没有任何遮挡体，烘焙数据为空 —— 全程不报错、
不警告，只是白烤一遍。

要让遮挡剔除生效，得另外手工摆放**实心的 occluder 代理体**（不能从现有网格的 AABB 自动生成，
那会把走廊也堵上）。这属于后续工作，仓库里还没有。

### URP 下的取舍

- **SRP Batcher** 6 个质量档全部开启（`m_UseSRPBatcher: 1`）。已生效的前提下，
  再开 GPU Instancing **没有额外收益** —— 走 SRP Batcher 的物体不再走实例化路径，两者不叠加。
- **阴影距离与级联读的是 Pipeline Asset**，不是 `QualitySettings`。改后者不生效，
  这一条排查掉之前浪费过时间。

---

## 三、AI 实现历程与思路

这个项目是**和 AI（Claude）协作完成的**，不回避这一点 —— 怎么用、以及知道它在哪里不可靠，
本身就是能力的一部分。下面写的是真实的协作方式和它的失效边界。

### 阶段一：先定架构，再让 AI 写玩法

C# / Lua / JSON 三层分工最初不是为了热更新，而是为了**压低「验证一次」的成本**。

AI 迭代里最贵的不是生成一次要多久，是**验证一次要多久**。走 C# 的话每次改动 = 编译 +
domain reload + 重进 Play，几十秒；走 Lua + JSON 且编辑器直读磁盘的话 = 存盘 + 进 Play。
这个差值决定了一个下午能试 5 次还是 50 次。所以架构上刻意把**所有会被反复调整的东西**
（AI 状态机、数值、对话、任务）全部推到不需要编译的那一侧。

### 阶段二：接入 Unity MCP，把「人肉转述」换成 AI 直接观测

早期的循环是：AI 写代码 → 人进 Play → 人把 Console 复制回来 → AI 猜。
瓶颈在中间那个人 —— 他是个有损信道，会漏掉他自己觉得不重要的报错。

接入 **Unity MCP** 后，AI 可以直接读 Console、进出 Play、查询场景物体与坐标、
截图、在编辑器里执行 C#、跑菜单项。「改动 → 进 Play → 读日志 → 再改」这个环由 AI 自己闭合，
人只做取舍判断。

### 阶段三：从「看日志」到「注入探针实测」

做「敌人受击击退」时，验证方式从看日志升级成了**注入 Lua 探针逐帧采样**：反射进
`XLuaManager._env`（它是 `private static`），monkey-patch `enemy_ctrl.update`，在帧内取样。

**这里有个坑值得单独写**：一次 MCP 往返会把它落在的那一帧撑长 —— 实测 `dt = 0.333 s`，
而正常是 0.006–0.04 s。所以**不能用两次 MCP 调用去读「前」和「后」的状态**：那样读到的
不是击退位移，是敌人又走回来之后的位置。正确做法是把动作排进队列、由注入的 wrapper 在 N 帧后
触发，采样也全部在帧内完成。

这么做才拿到了确定的数字：

| 配置击退距离 | 净位移 | 累计路程 | 到峰值耗时 |
|---|---|---|---|
| 1.00 m | 1.000 | 1.000 | 0.197 s |
| 0.60 m | 0.600 | 0.600 | 0.239 s |
| 0（免疫） | 0.000 | 0.000 | — |

净位移 == 累计路程 == 沿「远离玩家」轴的投影，说明零偏折、零导航网格裁剪。
同一套探针还量出了这个功能的**平衡代价** —— 击退会打断守卫已挥出的那一刀，
固定贴身、9 秒窗口下：

| 玩家点击间隔 | 守卫挥砍次数 | 实际掉血次数 |
|---|---|---|
| 不打（基线） | 6 | 5 |
| 1.00 s（正常节奏） | 6 | 4 |
| 0.45 s（连点） | 6 | **0** |

三种条件挥砍数都是 6，说明打断只削伤害、不影响出手频率。这类结论靠「感觉手感对了」是拿不到的。

### AI 生成的代码在哪里失效：5 个真实案例

这 5 个的共同点是**引擎全程不报错**。代码能编译、能跑、读起来也对，错在引擎行为的边界上 ——
只能靠观察现象反推机制。这也正是为什么上面那个验证环比生成环重要。

**1. 巡逻状态死锁：`pathPending` 永不落下**

现象：守卫停在 `WalkFWD` 动画里不动，能持续好几分钟，Console 零报错。

机制：巡逻点是在出生点周围随机取偏移的，很容易落到墙的另一侧。这种目标 `NavMeshAgent` 会回
`PathPartial`，agent 朝墙走、路径请求一直挂着，`pathPending` 不落下。而到达判定要求
`not agent.pathPending`，于是永远判不出到达，状态机卡死。

三道防线：**下发前校验可达**（`ComponentFactory.cs:66` `SampleReachablePoint()`，要求
`NavMeshPathStatus.PathComplete`）、**目标点去重**（只在目标移动超过 0.15 m 时才重新
`SetDestination`，每帧无条件下发同一个目标本身就会让请求永远在飞）、
**状态超时兜底**（`PATROL_TIMEOUT = 6.0`，从机制上排除同类死锁）。

**2. 生成位置被 Agent 拽回原点**

prefab 出厂就带着**启用状态**的 `NavMeshAgent`，实例化那一刻它就绑到离 prefab 原点最近的导航网格上。
之后再赋 `transform.position` **不会移动 agent**，而 `updatePosition = true` 会在下一帧把身体拉回
那个原点岛。解法是 `agent:Warp(spawn_center)` 重置 agent 的模拟位置。

后来做击退时复用了同一条结论：位移走 `agent:Move(offset)` —— 不写 `transform.position`（会被拽回）、
不用 `Warp`（瞬移没有过程）、更不能写 `agent.velocity`（会被 agent 自己的操舵覆盖）。
`Move` 顺带把身体约束在导航网格上，所以击退**结构上不可能**把敌人推进墙里，
连带排除了案例 1 那类死锁。

**3. 挥砍次数比伤害次数多**

`TurtleShell` 的 `Attack01` 片段被标记为 looping，裸调 `Play()` 会每 0.83 s 重挥一次，
而攻击间隔是 1.6 s —— 看起来砍两下只掉一次血。解法是读出片段真实长度
（`ComponentFactory.GetClipLength`），按 `ATTACK_HIT_FRACTION = 0.4` 在挥砍中段结算伤害，
并显式跟踪 `swing_left`，让一次挥砍严格对应一次伤害。

**4. Animator 文件每次打开工程都变脏**

现象：`DogControl.controller` 每次重新加载工程都出现在 `git status` 里，diff 一百多行，
读起来像是过渡条件被人改过。

机制：装配脚本是 `[InitializeOnLoad]`，每次加载都跑一遍，而它先 `RemoveTransitions()` 再
`AddTransition()`。**删除再新增会重新分配 fileID**，于是 YAML 里 18 个 transition 的锚点全变、
块的排列顺序也变 —— 文件字节不同而语义完全一致。逐行 diff 之所以像「改了条件」，
是 git 把两个互不相干的块对齐到了一起：**这种 diff 不能直接读**，得按状态名和条件做语义比对。

解法是让装配**幂等**：把期望的 13 条过渡构造成数据、和资产里现有的逐条比对，一致就在 `SetDirty`
之前直接返回。

**5. `or` 吞掉策划刻意填的 `0`**

`EnemyAlertZone.Attach(..., config.attack or 5, ...)` 这种写法把**「策划没填」和「策划填了 0」
压成了同一个数**，到 C# 侧再也分不开 —— 一个刻意配的 `0`（比如「这个敌人免疫击退」）
会被静默换成默认值。

这条直接决定了架构：**逐字段合并层必须放在 Lua**（用 `~= nil` 判断），C# 侧只做一次性兜底。
同样的坑在玩家数值上也踩过一次：`M.base_speed = attrs.speed or 3.6` 会把配置的 `0` 换回 3.6。

---

## 四、AI 使用经验、流程与注意事项

只写怎么做。一半是这个项目踩出来的，一半来自公开的最佳实践（METR 的对照实验、Claude Code
官方文档、Simon Willison 的用法总结等）。

### 开工前

- **分档决定交不交给它。** 重复性高 / 有明确规范 / 我一眼能验证 → 全交；我不熟的领域 → 交，
  但每行都看；性能热路径、平台语义、涉及安全 → 自己写，并且自己实测。
- **别给一句话需求。** 让它先访谈我，把边界、异常、取舍问出来，产出一份规格。
- **换一个干净会话再开始实现。** 讨论阶段被否掉的方案不要带进去。
- **规矩写进规则文件**（`CLAUDE.md` 这类），逐行问「删掉它会不会让它犯错」，不会就删。写太长它会
  忽略一半。
- **必须每次都发生的事写成 hook 或脚本**（跑 lint、禁改某目录）。规则文件是建议，hook 才是必然。

### 每个切片五步

> 一次只做一个切片，验证通过再推进下一块。

| 步骤 | 做法 | 完成判据 |
|---|---|---|
| 1 定边界 | 说清这次只改什么 | 判据必须可观测，能写成一句贴得出来的话 |
| 2 核实机制 | 让它先读相关调用链、把约束说出来，再动手 | 约束落成文字，不是口头确认 |
| 3 只改一处 | —— | diff 里没有第二处改动 |
| 4 自己验证 | 进 Play / 跑测试；涉及量级就取数 | 有数字或有日志行，不是「感觉对了」 |
| 5 结论写回 | 提交信息写机制约束 + 实测数字 | `git log` 里搜得到，下轮不必重新发现 |

能用一句话描述 diff 的改动，跳过 1、2 直接改 —— 规划本身有成本。

### 让它能自己验证（最省时间的一步）

给它一个能自己跑的判据，它才会自己收敛；没有判据，你本人就是那个判据。

| 判据形态 | 这个项目里的做法 |
|---|---|
| 改完立即生效 | Lua + JSON 走编辑器直读磁盘，省掉编译 + domain reload |
| 它自己读运行时状态 | 接入 Unity MCP：自己读 Console、进出 Play、查场景物体 |
| 拿到数字而不是感觉 | 注入 Lua 探针逐帧采样 |
| 让程序自报读到了什么 | 敌人注册日志行尾标明数值来自 JSON 还是 Inspector；键名拼错追加 `!! unrecognised key(s) ignored` |

换个技术栈的等价物：测试、构建退出码、linter、截图比对、脚本比 fixture。

### 审查

- **换一个只看 diff 的干净会话审**，别让写代码的那个会话给自己打分。
- **限定「只报影响正确性和明确需求的」。** 让它找问题它一定找得出来，照着全改就是过度设计。
- **专门看它「顺手」改了什么** —— 那部分往往没在任何一次对话里被提起过。
- **查它是复制粘贴还是抽了函数。** 它默认倾向复制，「要不要抽一层」这个决定得自己做。
- **同一个问题纠正两次还不对：清空重开**，把学到的写进新 prompt，不要接着纠。

### 交付前必查

| 查什么 | 为什么 |
|---|---|
| 见它真跑起来了吗 | 「能编译」「没报错」「它说已完成」都不算验证通过 |
| 依赖名真的存在吗 | 它会编包名，而且已经有人抢注这些名字做供应链攻击 |
| API 是这个版本的吗 | 训练有截止日期。优先选老稳定版本，资料密度高、出错概率低 |
| `or` / `??` 有没有吞掉刻意填的 `0` | 「没填」和「填了 0」被压成同一个值（案例 5） |
| 平台的隐含语义碰到了吗 | 组件的隐含状态 / 资产的元数据 / 序列化身份，这三类代码里看不出来（案例 2、3、4） |
| 结论写回提交信息了吗 | 不写，下一轮重新发现同一件事 |

### 两条测量陷阱

- **不能跨工具调用测量。** 一次 MCP 往返会把它落在的那一帧撑到 `dt = 0.333 s`（正常 0.006–0.04 s），
  两次调用读到的不是同一时刻的状态。要测准就在被测系统内部测。
- **`git diff` 不一定能逐行读。** fileID 重分配会让 git 把互不相干的 YAML 块对齐到一起，逐行看
  像是逻辑被改过（案例 4）。这种只能按语义比对。

---

## 运行与素材

需要 **Unity 2021.3.45f1c1**（其他 2021.3.x 大概也行，未验证）。

1. 打开工程等待导入，首次导入会自动创建 `ProjectLua`、`ProjectData`、`UserAssets` 三个 Addressables 本地组。
2. 打开 `Assets/Scenes/SampleScene.unity`，按 Play。`GameBootstrap` 会自动挂载，无需手工放入场景。

操作：`WASD` 移动 / 鼠标转视角 / `Shift` 疾跑 / `Space` 跳跃 / `E` 拾取与对话 / `Tab` 背包。

**关于缺失的美术素材**：模型、贴图、音频（约 384 MB）未纳入版本控制，仓库因此只有 23 MB。
保留下来的是全部文本资产 —— 23 个 `.cs`、20 个 Lua 模块、5 份 JSON、219 个 prefab（含组件配置）、
4 个 Animator 控制器与 10 个动画片段、26 个材质、25 个场景，以及 `ProjectSettings/`、
Addressables 分组、URP 管线资产。各素材包的原始说明与授权文件已随仓库保留，
可据此回到来源重新获取、放回同名目录，例如 `Assets/Project-Assets/Ground/Modular Dungeon Tiles/README.txt`。

`ProjectSettings/EditorSettings.asset` 中 `m_SerializationMode: 2`（Force Text），
保证 `.unity` / `.prefab` 的改动在 git 里是可读的文本 diff。Lua 用 `.lua.txt` 后缀，
是为了让 Unity 直接按 `TextAsset` 导入、纳入 Addressables。

更细的运行说明、数值调整入口和已知编辑器噪音见 [`Assets/_Project/README.md`](Assets/_Project/README.md)，
设计文档见 [`Docs/策划文档.md`](Docs/策划文档.md)。

---

代码与配置为本人所写（与 AI 协作，过程见第三章）。`Assets/Project-Assets/` 下的第三方素材版权归各自作者，
其原始授权文件已随目录保留。
