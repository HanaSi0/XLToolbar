# XLToolbar

从 XLChat 的旧实现中独立出来的 FF14 / Dalamud API 15 顶部快捷工具条。

全部 16 类工具条图标统一使用原创单色扁平图标，透明背景，无立体高光、渐变或厚重描边；图集内嵌在插件 DLL 中，顶部不读取游戏图标或 FontAwesome。DTR 入口使用统一图标。

工具条固定在游戏视口顶部，左侧入口均匀排列，固定信息靠右，空间不足时自动换行。支持游戏图标、中文文字、悬停提示，以及 DTR 入口的左右键和修饰键。

- 快捷面板：特效与召唤兽、部族声望、在线状态、职业套装、装备耐久与精炼度、货币、传送、主音量。
- 游戏入口：地图/区域、背包、任务。任务左键打开任务日志，右键打开任务情报；区域 ID、地图 ID 与天气编号放在地图入口的悬停提示中。
- 游戏菜单：独立窗口收录客户端全部主菜单入口，包括截图中的角色、技能、物品、肖像、名牌、兵装库、坐骑、宠物、装饰、搭档、青魔法书、魔兽图鉴、对战资料、金碟与成就等。支持图标、搜索和收藏；点击 ☆ 添加常用项，★ 取消，常用项显示在所有选项上方并保存。不可用的功能仍显示，恢复可用状态后即可点击。
- 插件入口：其他插件 DTR 信息；过滤重复的 FPS/帧率指标，只保留工具条自身的 FPS。
- 状态信息：FPS、本地时间与艾欧泽亚时间。
- 设置：工具条背景颜色和不透明度（0 完全透明，1 完全不透明），即时生效并保存；显示开关、图标大小、间距、上下留白、文字宽度、逐项显示与右侧固定、功能面板布局锁定。

输入 `/xltoolbar` 打开设置；输入 `/xltoolbar toggle` 切换工具条显示。游戏交互默认启用；可在设置中关闭。登录后显示工具条，退出登录时清理角色缓存。

XLToolbar 可与 XLChat 同时使用，不需要加载 XLChat。背包、任务日志等打开原生游戏窗口。它不替换原版聊天、热键栏、地图，也不隐藏或修改原版 UI。

顶部已移除插件收纳、指令收纳、锁定布局与游戏功能入口。功能面板的布局锁定仍可在设置中调整。

二级窗口统一使用高可读性深色背景，仅保留标题栏和右上角关闭按钮。特效和召唤兽大小直接点选；职业套装显示名称、职业、等级与装等并自动调整列数；装备耐久/精炼度与货币限额分列展示。游戏接口和原有操作保持不变。

## 编译

需要 .NET 10 SDK，以及**实际游戏客户端使用的 Dalamud API 15** 文件目录。国服测试应使用国服启动器提供的文件。

Windows PowerShell：

```powershell
./Build-Local.ps1 -DalamudPath '你的 Dalamud Hooks/dev 目录'
```

其他环境：

```bash
export DALAMUD_HOME=/path/to/dalamud
dotnet restore XLToolbar.csproj --locked-mode
dotnet build XLToolbar.csproj -c Release --no-restore
dotnet run --project tests/Layout/Layout.csproj -c Release
```

开发加载：在 Dalamud 的开发插件设置中添加 `bin/Release/XLToolbar.dll`，加载后输入 `/xltoolbar`。打包文件位于 `bin/Release/XLToolbar/latest.zip`。Release 发布后，可在 Dalamud 自定义插件仓库中添加：

```text
https://raw.githubusercontent.com/HanaSi0/XLToolbar/main/pluginmaster.json
```

## 验证范围

布局测试覆盖换行、右侧固定、无重叠、极窄视口、无效尺寸、随机组合，以及找不到游戏图标时的回退。完整插件可在 Linux 上交叉编译，但游戏按钮、国服游戏表和 DTR 点击需要在游戏中验证。

游戏内检查：登录并确认顶部显示；缩放或改变分辨率；调整显示项与固定项；打开地图/背包/任务；测试套装、传送、音量及特效；测试 DTR 左右键；禁用交互；退出并重新登录；卸载插件后确认命令和绘制订阅被移除。

代码来源见 [NOTICE.md](NOTICE.md)。
