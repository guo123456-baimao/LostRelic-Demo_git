# 失落圣物：遗迹探险 Demo

本工程依据 `Docs/策划文档.md`（v0.4）实现，场景复用
`Assets/Scenes/SampleScene.unity`，游戏逻辑全部位于
`Assets/_Project/Lua`，C# 仅提供 xLua/Addressables/UI/音频/输入等框架能力。

## 运行方式

1. 打开工程后等待 Unity 导入 `Assets/_Project`，首次导入会自动创建
   `ProjectLua`、`ProjectData`、`UserAssets` 三个 Addressables 本地组。
2. 打开 `Assets/Scenes/SampleScene.unity`，直接按 Play。
   `GameBootstrap` 会在 SampleScene 中自动创建，并从 Addressables 加载
   `main.lua` 与全部配置。
   玩家 Animator 首次编译后会由编辑器脚本自动补上 `Speed` 参数和
   Idle/Walk/Run 过渡；如未生效可手动执行 `LostRelic/Setup Player Animator`。
3. 如需把 Bootstrap 显式保存进场景，可执行菜单
   `LostRelic/Build Demo Scene`。

玩家、老向导、4 只猫头鹰和遗迹守卫由 `LostRelic/Build Demo Scene` 直接
放入 SampleScene；运行时优先复用场景中的对象，仅当场景里缺失时才兜底生成。

第一人称视角由 Cinemachine Virtual Camera 驱动（`PlayerVirtualCamera` +
`CinemachinePOV`），依赖 `com.unity.cinemachine 2.10.3`。

## 操作

- WASD 移动，鼠标控制第一人称视角
- Shift 疾跑，Space 跳跃
- E 拾取/对话
- Tab 打开/关闭背包
- 对话中按 E 或左键逐字推进/跳过当前句

## 目录

- `Assets/_Project/Scripts`：C# 框架层（`GameBootstrap`、`XLuaManager`、
  `ResService`、`DataService`、`EventCenter`、`UIManager`、`AudioService`、
  `InputService`、`Interactable`、`EnemyAlertZone`）
- `Assets/_Project/Scripts/Editor`：Addressables 分组与 Demo 场景构建工具
- `Assets/_Project/Lua`：可热更玩法逻辑（玩家、相机、交互、背包、任务、
  对话、敌人警戒、音频、事件总线）
- `Assets/_Project/Data`：物品/任务/对话/音频/出生点 JSON 配置

## 当前占位与替换点

- v1 无战斗，敌人只触发 BGM 切换；后续可扩展追击/战斗。
- 终局使用简单 UI 面板，未提供终局特效。
- 4 只猫头鹰共用同一张图标 `Assets/Assets/Icons/#2 - Transparent Icons & Drop Shadow.png`。
- 出生点与警戒区坐标集中在 `Assets/_Project/Data/spawn_config.json`，可在
  场景内实测后直接修改。

## 已知提示

Unity 2021.3.45 中若打开了 Addressables 窗口，Addressables 1.19.19 可能会输出
`Missing built-in guistyle ToolbarSeach*` 编辑器 GUI 报错。这是插件编辑器窗口的
已知噪音，不影响 Demo 运行；关闭 Addressables 窗口即可不再输出。
