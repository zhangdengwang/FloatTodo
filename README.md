# FloatTodo

FloatTodo 是一个基于 C# + WPF + .NET 10 的桌面悬浮待办工具。默认显示桌宠悬浮入口，常用操作通过右键菜单完成，复杂功能可以打开完整主面板。

## 主要功能

1. 普通任务管理：新增、查看、完成待办任务。
2. 项目任务管理：创建项目，并为项目添加小任务。
3. 快截止提醒：桌宠状态和红点用于提示 24 小时内截止或已逾期任务。
4. 日常记录：支持喝水、休息眼睛、起身活动等快捷 +1。
5. 日常提醒：通过悬浮窗状态提示，不使用弹窗打扰。
6. AI 拆解：将项目描述拆解为候选任务，选择后加入现有待办。
7. 完整主面板：保留完整功能面板用于集中查看和课程答辩展示。

## 技术栈

- C#
- WPF
- .NET 10
- 本地 JSON 存储

## 项目结构说明

- `App.xaml.cs`：程序启动入口，负责启动桌宠悬浮窗、延迟创建完整主面板和统一退出逻辑。
- `MiniWidgetWindow`：桌宠悬浮入口，负责图片状态、红点角标、日常提醒文字和右键功能菜单。
- `MainWindow / ShellView`：完整功能面板，用于集中展示任务、日常记录、AI 设置等功能。
- `Models`：任务、项目进度、日常记录、AI 候选任务和 API 设置等数据模型。
- `Services`：业务逻辑和本地 JSON 存储，包括任务存储、日常记录存储、API 设置和 AI 调用。
- `QuickXXXWindow`：右键菜单打开的快捷功能窗口，例如快速新增任务、查看任务列表、AI 拆解等。
- `Assets/Pet`：桌宠三状态图片资源。
- `scripts`：发布打包脚本，用于生成 Windows x64 自包含发布包。

## 本地运行

```powershell
dotnet run --project src/FloatTodo.App/FloatTodo.App.csproj
```

## 如何打包 Windows 自包含版本

在项目根目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-win-x64.ps1
```

输出：

```text
dist/FloatTodo-win-x64/
dist/FloatTodo-win-x64.zip
```

将 `dist/FloatTodo-win-x64.zip` 发给其他 Windows x64 用户。对方解压后，双击 `FloatTodo.App.exe` 即可运行，不需要额外安装 .NET，也不需要打开终端。

发布包不会包含本地 `data/`、日志、`.env`、本地配置或密钥文件。程序首次运行时会自行创建需要的数据文件。
