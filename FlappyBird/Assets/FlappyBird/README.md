# Flappy Bird

在 Inno Editor 中打开项目资产 `Assets/FlappyBird/FlappyBird.iscene`，点击顶部 Play。Game 面板运行的就是该场景；Player 也以同一个场景为启动场景。点击 **PLAY** 或按 **Space** / **Up Arrow** 开始；游戏中按键或点击拍翅；死亡后点击 **TRY AGAIN** 或按 **Space** / **Enter** 重开。

项目的场景、prefab、脚本、美术、音频和 UI 源文件均位于 `Assets/FlappyBird`。`Plugins/*.iplugin` 是 Rendering2D 与 Canvas 的引擎扩展安装包，不是游戏场景的来源。Inspector 中组件的 `Source: InnoPlugin/Runtime` 指组件类型的程序集来源。Play 时场景暂时不能保存；退出 Play 后，`Assets` 中的场景仍可编辑。

在 Rider 中打开项目根目录的 `InnoProject.sln`。它只包含 `Inno.GameScripts.csproj` 和 `Inno.EditorScripts.csproj`；两者都是 Editor 自动生成的索引工程，源码全部来自 `Assets`。这三个生成文件与 `Library`、`Builds` 都不需要提交到 Git。

## Structure

- `FlappyBird.iscene` stores the camera, Canvas, backgrounds, ground, bird, one positioned pipe prefab preview, and lights. Named empty objects group the hierarchy. The Canvas references `UI/FlappyBird.rml` directly. Every authored object is visible and editable before Play.
- `Prefabs/Pipe Pair.iprefab` contains the upper and lower sprite objects and their `FlappyBirdPipe` behavior. New obstacles are instantiated from this prefab and destroyed after leaving the screen, keeping the live scene bounded.
- `Gameplay/FlappyBirdController.cs` is a `GameBehavior` on the Canvas object. It owns game state, flight and collision rules, scoring, audio cues, prefab spawning, and Canvas button events. Scene and audio references are serialized in the Inspector.
- `Gameplay/FlappyBirdBird.cs` animates three serialized sprite references and moves the bird light. `Gameplay/FlappyBirdPipe.cs` controls one prefab instance.
- `UI/FlappyBird.rml` contains the start, score, and death screens.

The original sprites and sounds were selected from [FLwolfy/Flappy-Bird-Plus](https://github.com/FLwolfy/Flappy-Bird-Plus), licensed under the MIT license in `ASSET_LICENSE.txt`. The UI fonts retain their license in `UI/LICENSE.txt`.
