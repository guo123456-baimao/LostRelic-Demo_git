# LostRelic 失落遗物

3D 第一人称地牢探索 ARPG 垂直切片。**Unity 2021.3.45f1c1（中国版 LTS）+ URP + XLua**，
C# 只做框架、玩法全部在 Lua，配置全部走 JSON。

探索遗迹、找回 4 只猫头鹰雕像、与老向导对话、与守卫战斗，是一条从进入到终局可完整走通的流程。

---

## ⚠️ 先看这里：仓库不含美术素材

模型、贴图、音频（约 **384 MB**）来自 Asset Store 及第三方素材包，多数授权禁止二次分发，
因此**未纳入版本控制**。仓库只有 23 MB。

**保留了什么**（都是文本 YAML / 源码，可直接阅读）：

| 内容 | 数量 |
|---|---|
| C# 框架层 + 编辑器工具 | 24 个 `.cs` |
| Lua 玩法逻辑 | 23 个模块 |
| JSON 配置 | 5 个 |
| prefab（含组件配置） | 219 个 |
| Animator 控制器 / 动画片段 | 4 / 10 |
| 材质 / 场景 | 24 / 25 |
| `ProjectSettings/`、Addressables 分组、URP 管线资产 | 全部 |

**后果**：克隆后打开 `SampleScene` 会看到洋红色缺失材质 —— 场景里对贴图和模型的
GUID 引用指向不存在的文件。**组件配置、动画状态机、场景编排、全部逻辑代码都是完整的**，
读代码和读 prefab 不受影响。

**想跑起来**：各素材包的原始说明与授权文件已随仓库保留，可据此回到来源重新获取，
放回同名目录即可，例如：

- `Assets/Assets/Ground/Modular Dungeon Tiles/README.txt`
- `Assets/Assets/Old Guide/Old Guide/License.txt`

---

## 架构

C# 提供能力，Lua 决定行为。C# 层不含任何玩法规则，改数值、改 AI、改流程都不用碰 C#、不用重编译。

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

### 双通道资源加载

`XLuaManager` 和 `DataService` 用同一套策略：**编辑器下直读磁盘源文件，运行时回落 Addressables**。

```csharp
// XLuaManager.cs:26-49，自定义 AddLoader
if (Application.isEditor) {
    var filePath = Path.Combine(Application.dataPath, "_Project", "Lua", module + ".lua.txt");
    if (File.Exists(filePath)) text = File.ReadAllText(filePath);
}
if (string.IsNullOrEmpty(text)) text = ResService.LoadText(address);
```

`DataService.cs:13-41` 的 `LoadJson` 对 JSON 做同样的事。这样改一行 Lua 或一个数值，进 Play 立即生效，
不必重建 Addressables；而打包路径完全没变，仍然走 Addressables 取址。

配置改完若没生效，是静态缓存没失效 —— 调 `DataService.ClearCache()`（`GameBootstrap.OnDestroy` 里已自动调用）。

### 数据驱动

`Assets/_Project/Data/` 下 5 份配置，`main.lua` 启动时全部载入，敌人与雕像都是遍历数组生成：

| 文件 | 内容 |
|---|---|
| `spawn_config.json` | 出生点、敌人属性、警戒/追击/巡逻半径 |
| `quest_config.json` | 主线任务与完成条件 |
| `dialog_config.json` | 对话文本与 `onComplete` 动作 |
| `item_config.json` | 道具定义与堆叠规则 |
| `audio_config.json` | BGM / SFX 映射 |

---

## 几段真实的排查记录

这几个问题的共同点是**引擎不报错**，只能靠观察现象反推机制，所以留在这里。

### 1. 巡逻状态死锁：`pathPending` 永不落下

现象：深处的守卫停在 `WalkFWD` 动画里不动，能持续好几分钟，Console 全程零报错。

机制：巡逻点是在出生点周围随机取偏移的，很容易落到墙的另一侧。这种目标 `NavMeshAgent`
会回 `PathPartial`，agent 朝墙走、路径请求一直挂着，`pathPending` 不落下。而到达判定
（`enemy_ctrl.lua:111`）要求 `not agent.pathPending`，于是永远判不出到达，状态机卡死。

三道防线：

1. **下发前校验可达** —— `ComponentFactory.cs:66` `SampleReachablePoint()`：
   `NavMesh.SamplePosition` 吸附到导航网格，再用 `NavMesh.CalculatePath` 要求
   `NavMeshPathStatus.PathComplete`，不满足就换候选点；重试耗尽则返回原地
   （调用方读作"立即到达"，退回 idle 几秒后再试）。
2. **目标点去重** —— `enemy_ctrl.lua:101`：只在目标移动超过 0.15 m 时才重新
   `SetDestination`。每帧无条件下发同一个目标，本身就会让路径请求永远处于飞行中。
3. **状态超时兜底** —— `enemy_ctrl.lua:501`：`PATROL_TIMEOUT = 6.0`，巡逻超时无条件
   退回 idle，从机制上排除同类死锁。

### 2. 生成位置被 Agent 拽回原点

prefab 出厂就带着启用状态的 `NavMeshAgent`，实例化那一刻它就绑到离 prefab 原点最近的
导航网格上。之后再赋 `transform.position` **不会移动 agent**，而 `updatePosition = true`
会在下一帧把身体拉回那个原点岛。解法是用 `agent:Warp(spawn_center)`
（`enemy_ctrl.lua:366`）重置 agent 的模拟位置，而不是写 `transform.position`。

### 3. 挥砍次数比伤害次数多

`TurtleShell` 的 `Attack01` 片段被标记为 looping，裸调 `Play()` 会每 0.83 s 重挥一次，
而攻击间隔是 1.6 s —— 看起来砍两下只掉一次血。解法是读出片段真实长度
（`ComponentFactory.GetClipLength`），按 `ATTACK_HIT_FRACTION = 0.4` 在挥砍中段结算伤害，
并显式跟踪 `swing_left`，让一次挥砍严格对应一次伤害。间隙姿态优先用
`IdleBattle`，没有该片段的控制器回落 `IdleNormal`。

### 4. 渲染性能：静态合批有效，遮挡剔除无效

给场景中 492 个静态物体打上 `StaticEditorFlags`（Batching + Occluder + Occludee + Navigation，
组合值 78）后实测：

| 指标 | 前 | 后 |
|---|---|---|
| **batches** | 613 | **254**（−59%） |
| setPassCalls | 不变 | 不变 |
| triangles | 不变 | 不变 |

`setPassCalls` 与三角形数不变、只有 batches 下降，说明收益确实来自静态合批的顶点合并，
而非剔除或材质切换减少。

而 **Bake Occlusion Culling 烤出来是空的**：这套地牢 tileset 是壁厚约 0.2 m 的空心壳，
Umbra 只把体素完全填满的格子标记为不透明，薄壳撑不满任何体素，于是没有遮挡体，
烘焙数据为空。要让遮挡剔除生效，得另外手工摆放实心的 occluder 代理体
—— 这属于后续工作，仓库里还没有。

另外，URP 下 SRP Batcher 已生效，再开 GPU Instancing 没有额外收益；
阴影参数读的是 Pipeline Asset，不是 `QualitySettings`。

---

## 运行

需要 **Unity 2021.3.45f1c1**（其他 2021.3.x 大概也行，未验证）。素材缺失见上文。

1. 打开工程，等待导入。首次导入会自动创建 `ProjectLua`、`ProjectData`、`UserAssets`
   三个 Addressables 本地组。
2. 打开 `Assets/Scenes/SampleScene.unity`，按 Play。
   `GameBootstrap` 会通过 `[RuntimeInitializeOnLoadMethod]` 自动挂载，无需手工放入场景。

操作：`WASD` 移动 / 鼠标转视角 / `Shift` 疾跑 / `Space` 跳跃 / `E` 拾取与对话 / `Tab` 背包。

更细的运行说明和已知编辑器噪音见 [`Assets/_Project/README.md`](Assets/_Project/README.md)，
设计文档见 [`Docs/策划文档.md`](Docs/策划文档.md)。

---

## 目录

```
Assets/_Project/          本工程自有代码与配置
        Scripts/          C# 框架层
        Scripts/Editor/   Addressables 分组、场景构建、Animator 装配等编辑器工具
        Lua/              玩法逻辑（.lua.txt 以便 Unity 当 TextAsset 导入）
        Data/             JSON 配置
Assets/Scenes/            SampleScene（玩家、老向导、4 雕像、守卫均在场景中编排）
Assets/Settings/          URP 管线与质量资产
Assets/AddressableAssetsData/
Assets/Assets/            第三方素材目录（仅保留 prefab / Animator / 材质等文本资产）
Docs/策划文档.md
```

Lua 文件用 `.lua.txt` 后缀，是为了让 Unity 直接按 `TextAsset` 导入、纳入 Addressables。

`ProjectSettings/EditorSettings.asset` 中 `m_SerializationMode: 2`（Force Text），
保证 `.unity` / `.prefab` 的改动在 git 里是可读的文本 diff。

---

## 说明

代码与配置为本人所写。`Assets/Assets/` 下的第三方素材版权归各自作者，
其原始授权文件已随目录保留。
