# EarlyWarp

EarlyWarp 是一个《戴森球计划》BepInEx 5 模组。研究「引力波折射」后，星际物流运输塔会立即出现原版样式的空间翘曲器专用栏位，星际物流运输船也可以实际消耗翘曲器进行曲速运输，不再要求完成「运输船引擎4」升级。

## 功能

- 研究科技 `1704`「引力波折射」后，启用 `GameHistoryData.logisticShipWarpDrive`。
- 已有星际物流运输塔的专用翘曲器栏位容量自动补齐至原版 50。
- 新建星际物流运输塔使用原版 `StationComponent.Init` 初始化专用栏位。
- 安装模组前已研究「引力波折射」的存档会在加载时幂等补齐曲速状态和已有塔栏位。
- 「运输船引擎4」仍保留运输机、配送机和运输船速度效果，但不再负责启用曲速。
- 不修改飞船速度、不添加自定义 UI、不复制栏位、不逐帧轮询。

## 原版逻辑

数据基于游戏 `0.10.35.29151` 的 `TechProtoSet` 核对：

| 科技 | ID | 原版解锁函数 |
| --- | --- | --- |
| 引力波折射 | 1704 | 无 |
| 运输船引擎4 | 3404 | `15=0.3`、`34=0.15`、`16=0.5`、`17=1.0` |

函数 `17` 会设置 `logisticShipWarpDrive = true`，并将已有物流塔的 `warperMaxCount` 设置为 50。EarlyWarp 只把该函数从 `3404` 移到 `1704`，保留其余速度函数。

运输塔界面、新建星际塔初始化、物流调度和运输船曲速循环都读取同一个曲速状态及塔内专用翘曲器计数，因此不需要额外的 UI 补丁。

## 安装

只需要 BepInEx 5。将 `EarlyWarp.dll` 放入游戏的 `BepInEx/plugins` 文件夹即可。

## 构建

本项目使用 .NET Framework 4.7.2 和 SDK-style 项目。

```powershell
dotnet build .\EarlyWarp.csproj -c Release
```

首次构建前，将 `DefaultPath.props.example` 复制为 `DefaultPath.props`，并按本机环境填写游戏 Managed 文件夹与 r2modman 配置档的 BepInEx 文件夹。`DefaultPath.props` 仅用于本机构建，不应提交到 GitHub。

Release 构建会生成：

```text
release/plugins/EarlyWarp.dll
```

如需同时安装到当前 r2modman 配置档：

```powershell
dotnet build .\EarlyWarp.csproj -c Release -p:InstallToProfile=true
```

生成 Thunderstore ZIP 时，需要另行指定包含 `manifest.json`、`README.md` 和 `icon.png` 的本地元数据目录：

```powershell
.\make_package.ps1 -PackageSource <本地元数据目录>
```

ZIP 会在项目上一级目录生成，文件名为 `sinthome-EarlyWarp-<版本>.zip`。

## 项目结构

```text
.gitignore                   本地与构建产物忽略规则
CHANGELOG.md                 版本记录
DefaultPath.props.example    本机构建路径模板
EarlyWarp.csproj             项目文件
EarlyWarp.sln                Visual Studio 解决方案
LICENSE                      MIT License
README.md                    项目说明
make_package.ps1             从指定元数据目录生成 Thunderstore ZIP
src/                         插件源码
```

## 许可

本项目采用 MIT License，详见 `LICENSE`。
