# InnoEngine Samples

示例项目使用 .NET 10 和 C# 14。安装 `global.json` 声明的 .NET SDK 10.0.402，并将匹配的 InnoEngine 与本仓库放在同一父目录。Canvas 与 Rendering2D 通过项目中的插件包安装。IDE 使用支持 .NET 10 的 Rider 或 Visual Studio 2026。

在本仓库目录启动 FlappyBird：

```sh
dotnet run --project ../InnoEngine/entry/InnoEngine.Editor -- ./FlappyBird
```

打开 `Assets/FlappyBird/FlappyBird.iscene` 后点击 Play。玩法、资源说明与许可见 [FlappyBird 文档](FlappyBird/Assets/FlappyBird/README.md)。

游戏源码与场景位于 `FlappyBird/Assets`，Canvas 与 Rendering2D 通过 `FlappyBird/Plugins/*.iplugin` 安装。安装内容由 Editor 作为只读资产来源加载；插件源码修改后通过引擎的正式导出流程更新包。

Editor 自动生成 IDE 工程与 `Library` 缓存。在 IDE 中打开 `FlappyBird/InnoProject.sln`，使用与 Editor 相同的 C# 14 语言和逻辑脚本 API。生成工程、缓存和构建输出不需要提交。
