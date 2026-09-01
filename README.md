# LostRelic 失落遗物

3D 第一人称地牢探索 ARPG 垂直切片。Unity 2021.3.45f1c1（中国版 LTS）+ URP + XLua。

代码规模 23 个 `.cs`（3117 行）、20 个 Lua 模块（3424 行）、5 份 JSON（262 行）。C# 仅提供框架能力，
玩法逻辑位于 Lua，数值与文本位于 JSON。含一条可完整走通的流程：探索遗迹、找回 4 只猫头鹰雕像、
与老向导对话、与守卫战斗。

> **仓库不含美术素材**（约 384 MB，来自 Asset Store 及第三方素材包，多数授权禁止二次分发）。
> clone 后打开场景会出现洋红色缺失材质，但代码、prefab 组件配置、动画状态机、场景编排均完整，
> 不影响代码阅读。详见[文末](#运行与素材)。

本文分四部分：游戏框架、性能优化、AI 实现历程与思路、AI 使用经验、流程与注意事项。

---

## 一、游戏框架

设计目标：玩法、数值、流程的修改均不涉及 C# 代码，不触发重新编译。

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

C# 层不含玩法规则，仅提供能力：实例化、资源加载、JSON 读取、事件派发、运行时组件装配。
规则位于 Lua，数据位于 JSON。

### 生命周期：3 个 C# 调用点

`GameBootstrap` 标记为 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`，无需手工放入场景，
进入 Play 时自行创建（`GameBootstrap.cs:12-24`）。

| C# 侧 | → Lua 侧 |
|---|---|
| `Start()` → `XLuaManager.Initialize("main")` | `main = require 'main'; main.start()` |
| `Update()` → `XLuaManager.Tick()` | 取全局函数 `on_update`，`Call(Time.deltaTime)` |
| `OnDestroy()` → `Dispose()` | 全局 `on_shutdown()`，再清 JSON 缓存与事件表 |

`main.start()`（`main.lua.txt:60`）载入 5 份 JSON，遍历数组生成敌人与雕像，依次 `init` 13 个模块，
并将 `on_update` / `on_shutdown` 注册到 Lua 全局。此后调度权在 Lua 侧：`main.update(dt)` 决定当帧
哪些模块 tick、面板打开时哪些逻辑挂起（`main.lua.txt:178-215`）。

### 事件双向

`event_bus.publish` 先派发 Lua 订阅者，再镜像一份至 C# 的 `EventCenter.Publish`
（`event_bus.lua.txt:26-40`），两侧均可收到同一事件，Lua 侧无需知晓 C# 是否存在订阅者。
UI、音频、HP 条经此总线解耦，模块之间无直接引用。

### 双通道资源加载

`XLuaManager` 与 `DataService` 采用同一策略：编辑器下直读磁盘源文件，运行时回落 Addressables。

```csharp
// XLuaManager.cs:26-58，自定义 AddLoader
if (Application.isEditor) {
    var filePath = Path.Combine(Application.dataPath, "_Project", "Lua", module + ".lua.txt");
    if (File.Exists(filePath)) text = File.ReadAllText(filePath);
}
if (string.IsNullOrEmpty(text)) text = ResService.LoadText(address);
```

`DataService.cs:13` 的 `LoadJson` 对 JSON 同理。修改 Lua 或数值后进入 Play 即生效，无需重建
Addressables；打包路径不变，仍经 Addressables 取址。

配置修改未生效的原因是静态缓存未失效，需调用 `DataService.ClearCache()`
（`GameBootstrap.OnDestroy` 已自动调用）。

### 数据驱动：逐字段合并

`Assets/_Project/Data/` 下 5 份配置，敌人与雕像均遍历数组生成：

| 文件 | 内容 |
|---|---|
| `spawn_config.json` | 出生点、玩家属性、敌人属性、警戒/追击/巡逻半径 |
| `quest_config.json` | 主线任务与完成条件 |
| `dialog_config.json` | 对话文本与 `onComplete` 动作 |
| `item_config.json` | 道具定义与堆叠规则 |
| `audio_config.json` | BGM / SFX 映射 |

敌人的 15 项数值按字段合并：JSON 中出现的键覆盖组件值（`0` 视为已填写），未出现的键保留 Inspector
上的手调值。玩家的 5 项（`player.stats`）规则一致，兜底值集中于 `player_attr.lua` 的 `DEFAULTS`。

合并层必须位于 Lua 而非 C#，原因见第三章案例 5。敌人注册日志的行尾标注该组数值的来源为 JSON
或 Inspector。

---

## 二、性能优化

### 静态合批：batches 613 → 254

对场景中 492 个静态物体设置 `StaticEditorFlags` 组合值 78
（Batching + Occluder + Occludee + Navigation），实测结果：

| 指标 | 前 | 后 |
|---|---|---|
| batches | 613 | 254（−59%） |
| setPassCalls | 不变 | 不变 |
| triangles | 不变 | 不变 |

收益来源为静态合批的顶点合并（`setPassCalls` 与三角形数不变，可排除剔除与材质切换减少两种可能）。

`ProjectSettings.asset` 中每个构建目标各有一份 `m_StaticBatching`，Standalone 默认值为 `0`。
仅设置物体标记而未开启该项时，合批在构建产物中不生效，而编辑器 Play 下仍可观察到效果。
提交 `40e1b41` 修正此项。

### 遮挡剔除：烘焙数据为空（负面结论）

Bake Occlusion Culling 对本场景无效。

该地牢 tileset 为壁厚约 0.2 m 的空心壳体。Umbra 的烘焙基于体素化，仅将被完全填充的体素标记为
不透明，薄壳无法填满任何体素，因此不产生遮挡体，烘焙数据为空。整个过程无报错、无警告。

启用遮挡剔除需另行摆放实心 occluder 代理体，且不能由现有网格的 AABB 自动生成（会阻塞走廊）。
此项属后续工作，仓库中尚未包含。

### URP 下的取舍

- SRP Batcher 在 6 个质量档全部启用（`m_UseSRPBatcher: 1`）。此前提下额外开启 GPU Instancing
  无收益：经 SRP Batcher 的物体不再走实例化路径，二者不叠加。
- 阴影距离与级联参数读取自 Pipeline Asset，而非 `QualitySettings`，修改后者无效。

---

## 三、AI 实现历程与思路

本项目与 AI（Claude）协作完成。以下记录协作方式的三个阶段，以及 AI 生成代码的实际失效边界。

### 阶段一：先定架构，再交付玩法实现

C# / Lua / JSON 的三层分工首要目的不是热更新，而是压低单次验证成本。

AI 迭代的瓶颈在验证环节，而非生成环节。经 C# 的改动需编译 + domain reload + 重进 Play，耗时数十秒；
经 Lua 与 JSON 且编辑器直读磁盘的改动仅需存盘 + 进 Play。因此架构上将全部需反复调整的内容
（敌人状态机、数值、对话、任务）置于无需编译的一侧。

### 阶段二：接入 Unity MCP

早期循环为：AI 生成代码 → 人工进入 Play → 人工回传 Console → AI 推断。中间环节是有损信道，
遗漏内容取决于转述者的主观判断。

接入 Unity MCP 后，AI 可直接读取 Console、进出 Play、查询场景物体与坐标、截图、在编辑器内执行
C# 与菜单项。「修改 → 进 Play → 读日志 → 再修改」的闭环由 AI 自行完成，人工只承担取舍判断。

### 阶段三：注入 Lua 探针实测

实现敌人受击击退时，验证方式由读日志改为注入 Lua 探针逐帧采样：反射获取 `XLuaManager._env`
（`private static`），monkey-patch `enemy_ctrl.update`，在帧内采样。

一处限制需注明：单次 MCP 往返会将其所在帧的 `dt` 撑至 0.333 s（正常范围 0.006–0.04 s），
因此不能以两次 MCP 调用分别读取动作前后的状态。做法是将动作排入队列，由注入的 wrapper 在 N 帧后
触发，采样全部在帧内完成。

实测数据：

| 配置击退距离 | 净位移 | 累计路程 | 到峰值耗时 |
|---|---|---|---|
| 1.00 m | 1.000 | 1.000 | 0.197 s |
| 0.60 m | 0.600 | 0.600 | 0.239 s |
| 0（免疫） | 0.000 | 0.000 | — |

净位移、累计路程与沿「远离玩家」轴的投影三者相等，即无偏折、无导航网格裁剪。

同一套探针同时量出该功能的平衡代价。击退会打断守卫已挥出的攻击，固定贴身、9 秒窗口下：

| 玩家点击间隔 | 守卫挥砍次数 | 实际掉血次数 |
|---|---|---|
| 不打（基线） | 6 | 5 |
| 1.00 s（正常节奏） | 6 | 4 |
| 0.45 s（连点） | 6 | 0 |

三种条件下挥砍次数均为 6，即打断只削减伤害、不影响出手频率。该结论无法由手感判断得出。

### AI 生成代码的 5 处失效

5 处的共同点是引擎全程无报错。代码可编译、可运行、静态阅读无误，错误位于引擎行为的边界上，
只能由现象反推机制。

**1. 巡逻状态死锁：`pathPending` 不落下**

现象：守卫停在 `WalkFWD` 动画内不动，可持续数分钟，Console 无报错。

机制：巡逻点在出生点周围随机取偏移，易落于墙体另一侧。此类目标 `NavMeshAgent` 返回 `PathPartial`，
agent 朝墙移动、路径请求持续挂起，`pathPending` 不落下；而到达判定要求 `not agent.pathPending`，
判定永不成立，状态机死锁。

处理：三道防线。下发前校验可达性（`ComponentFactory.cs:66` `SampleReachablePoint()`，要求
`NavMeshPathStatus.PathComplete`）；目标点去重，仅在目标移动超过 0.15 m 时重新 `SetDestination`
（每帧无条件下发同一目标本身即会使请求持续挂起）；状态超时兜底（`PATROL_TIMEOUT = 6.0`）。

**2. 生成位置被 Agent 拉回原点**

prefab 自带处于启用状态的 `NavMeshAgent`，实例化时即绑定至距 prefab 原点最近的导航网格。
此后赋值 `transform.position` 不移动 agent，而 `updatePosition = true` 会在下一帧将物体拉回原点
所在的网格岛。处理：以 `agent:Warp(spawn_center)` 重置 agent 的模拟位置。

击退功能复用同一结论：位移经 `agent:Move(offset)` 完成，不写 `transform.position`（会被拉回）、
不用 `Warp`（无过程）、不写 `agent.velocity`（会被 agent 自身的操舵覆盖）。`Move` 同时将物体约束
于导航网格上，因此击退在结构上不会将敌人推入墙体，并连带排除案例 1 一类的死锁。

**3. 挥砍次数多于伤害次数**

`TurtleShell` 的 `Attack01` 片段标记为 looping，直接调用 `Play()` 会每 0.83 s 重新挥砍一次，
而攻击间隔为 1.6 s，表现为挥砍两次仅掉血一次。处理：读取片段实际长度
（`ComponentFactory.GetClipLength`），按 `ATTACK_HIT_FRACTION = 0.4` 在挥砍中段结算伤害，
并显式跟踪 `swing_left`，使一次挥砍严格对应一次伤害。

**4. Animator 资产每次打开工程即变更**

现象：`DogControl.controller` 每次重新加载工程后出现在 `git status` 中，diff 逾百行，
形似过渡条件被修改。

机制：装配脚本标记为 `[InitializeOnLoad]`，每次加载执行一遍，且先 `RemoveTransitions()` 再
`AddTransition()`。删除后新增会重新分配 fileID，YAML 中 18 个 transition 的锚点全部变化、
块的排列顺序亦变化，文件字节不同而语义一致。逐行 diff 呈现为条件被修改，实际是 git 将两个互不相干
的块对齐所致，此类 diff 需按状态名与条件做语义比对。

处理：使装配幂等。将期望的 13 条过渡构造为数据，与资产中现有内容逐条比对，一致则在 `SetDirty`
之前返回。

**5. `or` 吞掉刻意填写的 `0`**

`EnemyAlertZone.Attach(..., config.attack or 5, ...)` 将「未填写」与「填写为 0」压为同一个值，
到 C# 侧无法区分，刻意配置的 `0`（如「该敌人免疫击退」）被静默替换为默认值。

此条决定了架构：逐字段合并层必须位于 Lua（以 `~= nil` 判断），C# 侧仅做一次性兜底。
玩家数值处存在同类问题：`M.base_speed = attrs.speed or 3.6` 会将配置的 `0` 替换为 3.6。

---

## 四、AI 使用经验、流程与注意事项

以下为通用做法，适用范围不限于本项目。一部分来自本项目实践，一部分来自公开的工程实践总结。

### 开工前

- 按可验证性分档决定交付范围。重复性高、有明确规范、可即时验证的部分全部交付；自身不熟悉的领域
  交付但逐行审阅；性能热路径、平台语义、安全相关的部分自行实现并自行实测。
- 需求先转为规格。由 AI 反问补全边界、异常与取舍，产出规格后再进入实现。
- 实现阶段另起干净会话，讨论阶段被否决的方案不带入上下文。
- 项目约定写入规则文件（如 `CLAUDE.md`）。逐条以「删除该条是否会导致出错」为标准取舍，
  篇幅过长会被部分忽略。
- 必须每次执行的检查写为 hook 或脚本。规则文件属建议，hook 属强制。

### 单个切片的五个步骤

单次只推进一个切片，验证通过后进入下一块。

| 步骤 | 做法 | 完成判据 |
|---|---|---|
| 1 定边界 | 明确本次修改范围 | 判据可观测，可表述为一句话 |
| 2 核实机制 | 先读相关调用链并陈述约束，再修改 | 约束以文字形式落地 |
| 3 单点修改 | — | diff 中无第二处改动 |
| 4 自行验证 | 进入 Play 或运行测试，涉及量级时取数 | 有数字或日志行为依据 |
| 5 结论回写 | 提交信息记录机制约束与实测数值 | `git log` 中可检索 |

diff 可由一句话描述的改动跳过步骤 1、2，规划本身有成本。

### 为 AI 提供可自行运行的判据

判据缺失时，验证环节由人承担；判据存在时，AI 可自行收敛。本项目的四种落地形态：

| 判据形态 | 本项目做法 |
|---|---|
| 修改即时生效 | Lua 与 JSON 经编辑器直读磁盘，省去编译与 domain reload |
| AI 自行读取运行时状态 | 接入 Unity MCP，读取 Console、进出 Play、查询场景物体 |
| 取得数值而非主观判断 | 注入 Lua 探针逐帧采样 |
| 程序自报读入的数据 | 敌人注册日志标注数值来源为 JSON 或 Inspector；键名拼写错误时追加 `!! unrecognised key(s) ignored` |

其他技术栈的等价物：单元测试、构建退出码、linter、截图比对、脚本比对 fixture。

### 审查

- 由另一个仅读取 diff 的干净会话执行审查，不由生成代码的会话自评。
- 限定审查范围为影响正确性与明确需求的问题。无限定时必然产出可修项，全部采纳将导致过度设计。
- 重点审查未在需求中提及的顺带改动。
- 检查实现方式为复制粘贴还是提取函数。AI 默认倾向复制，抽象层级的取舍由人决定。
- 同一问题纠正两次仍未解决时清空上下文重开，将已确认的信息写入新 prompt。

### 交付前检查项

| 检查项 | 说明 |
|---|---|
| 是否观察到实际运行 | 可编译、无报错、AI 声明完成均不构成验证通过 |
| 依赖名是否真实存在 | AI 会生成不存在的包名，且此类名称已被抢注用于供应链攻击 |
| API 是否属于当前版本 | 训练数据有截止日期。选用长期稳定版本时资料密度更高、出错概率更低 |
| `or` / `??` 是否吞掉刻意填写的 `0` | 「未填写」与「填写为 0」被压为同一个值（案例 5） |
| 是否触及平台的隐含语义 | 组件的隐含状态、资产的元数据、序列化身份三类无法从代码静态判断（案例 2、3、4） |
| 结论是否回写提交信息 | 未回写将导致同一问题被重复发现 |

### 两处测量陷阱

- 测量不能跨工具调用。单次 MCP 往返会将所在帧的 `dt` 撑至 0.333 s（正常 0.006–0.04 s），
  两次调用读取的并非同一时刻的状态。精确测量需在被测系统内部完成。
- `git diff` 不一定可逐行阅读。fileID 重分配会使 git 将互不相干的 YAML 块对齐，逐行阅读形似逻辑
  被修改（案例 4），此类 diff 只能按语义比对。

---

## 运行与素材

需要 Unity 2021.3.45f1c1，其他 2021.3.x 版本未验证。

1. 打开工程等待导入。首次导入会自动创建 `ProjectLua`、`ProjectData`、`UserAssets` 三个
   Addressables 本地组。
2. 打开 `Assets/Scenes/SampleScene.unity` 并进入 Play。`GameBootstrap` 自动挂载，无需手工放入场景。

操作：`WASD` 移动 / 鼠标转视角 / `Shift` 疾跑 / `Space` 跳跃 / `E` 拾取与对话 / `Tab` 背包。

关于缺失的美术素材：模型、贴图、音频约 384 MB 未纳入版本控制，仓库体积因此为 23 MB。
保留的是全部文本资产：23 个 `.cs`、20 个 Lua 模块、5 份 JSON、219 个 prefab（含组件配置）、
4 个 Animator 控制器与 10 个动画片段、26 个材质、25 个场景，以及 `ProjectSettings/`、
Addressables 分组、URP 管线资产。各素材包的原始说明与授权文件随仓库保留，可据此回到来源重新获取
并放回同名目录，例如 `Assets/Project-Assets/Ground/Modular Dungeon Tiles/README.txt`。

`ProjectSettings/EditorSettings.asset` 中 `m_SerializationMode: 2`（Force Text），
使 `.unity` 与 `.prefab` 的改动在 git 中呈现为可读的文本 diff。Lua 使用 `.lua.txt` 后缀，
以便 Unity 按 `TextAsset` 导入并纳入 Addressables。

更详细的运行说明、数值调整入口与已知编辑器噪音见
[`Assets/_Project/README.md`](Assets/_Project/README.md)，设计文档见
[`Docs/策划文档.md`](Docs/策划文档.md)。

---

代码与配置为本人所写（与 AI 协作，过程见第三章）。`Assets/Project-Assets/` 下的第三方素材版权归
各自作者，其原始授权文件已随目录保留。
