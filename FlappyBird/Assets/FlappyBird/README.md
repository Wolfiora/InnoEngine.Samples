# Flappy Bird

在 Inno Editor 中打开项目资产 `Assets/FlappyBird/FlappyBird.iscene`，点击顶部 Play。Game 面板运行的就是该场景；Player 也以同一个场景为启动场景。点击 **PLAY** 或按 **Space** / **Up Arrow** 开始；游戏中按键或点击拍翅；死亡后点击 **TRY AGAIN** 或按 **Space** / **Enter** 重开。点击画面右上角的月亮或太阳，或按 **N**，切换昼夜。

项目的场景、prefab、脚本、美术、音频和 UI 源文件均位于 `Assets/FlappyBird`。`Plugins/*.iplugin` 是 Rendering2D 与 Canvas 的引擎扩展安装包，不是游戏场景的来源。Inspector 中组件的 `Source: InnoPlugin/Runtime` 指组件类型的程序集来源。Play 时可编辑独立的运行副本，但不能保存它；退出 Play 后所有临时修改消失，原场景保持原样。

在 Rider 中打开项目根目录的 `InnoProject.sln`。它只包含 `Inno.GameScripts.csproj` 和 `Inno.EditorScripts.csproj`；两者都是 Editor 自动生成的索引工程，源码全部来自 `Assets`。这三个生成文件与 `Library`、`Builds` 都不需要提交到 Git。

## Structure

- `FlappyBird.iscene` stores the camera, Canvas, backgrounds, ground, bird, one positioned pipe prefab preview, and lights. Named empty objects group the hierarchy. The Canvas references `UI/FlappyBird.rml` directly. The ready screen displays several live prefab pipes and animates the bird beside the start panel; starting a run takes over those pipes without replacing them.
- `Prefabs/Pipe Pair.iprefab` contains the upper and lower sprite objects and their `FlappyBirdPipe` behavior. New obstacles are instantiated from this prefab and destroyed after leaving the screen, keeping the live scene bounded.
- `Gameplay/FlappyBirdController.cs` is a `GameBehavior` on the Canvas object. It owns game state, flight and collision rules, scoring, audio cues, prefab spawning, and Canvas button events. Scene and audio references are serialized in the Inspector.
- `Gameplay/FlappyBirdBird.cs` animates three serialized sprite references and moves the bird light. `Gameplay/FlappyBirdPipe.cs` controls one prefab instance.
- `UI/FlappyBird.rml` contains the start, score, and death screens.

最高分通过脚本公开的 `InnoEngine.Storage.Storage` 保存为 UTF-8 十进制文本，键为 `flappy-bird/best-score.txt`。首次启动没有存档时按 0 分处理；每次刷新最高分都会立即原子写入，下一次启动读取。Editor Play 的存档位于项目的 `Library/PersistentData/inno.editor.play/Storage/flappy-bird/best-score.txt`；导出的 macOS Player 使用系统用户数据目录下的 `InnoEngine/flappybird/Storage/flappy-bird/best-score.txt`。两者是独立存档，因此 Editor 中的最佳分数不会自动带进导出游戏。

场景中的 `Day Sky/Sun shaft` 是一盏向右下方照射的 `Light2D` 聚光灯。日间天空、地面和水管接收 Style0；夜空的星光使用 Style1，水管不接收星光，所以星光只照亮夜空，不会盖过前景水管。昼夜切换和最高分存取都由 Canvas 物体上的 `FlappyBirdController` 管理。

`Main Camera` 的 `orthographicSize` 为 5.12，画面宽高比由实际 Game View / Player 渲染目标决定；本项目的 Player 设置为 1280×720（16:9）。Camera 的 `referenceResolution` 同为 1280×720，但它只用于启用 Pixel Perfect 时的参考分辨率，不是独立的 aspect ratio 开关。场景中的 Canvas 为 910×512 设计尺寸，在 16:9 画面内显示。

The original sprites and sounds were selected from [FLwolfy/Flappy-Bird-Plus](https://github.com/FLwolfy/Flappy-Bird-Plus), licensed under the MIT license in `ASSET_LICENSE.txt`. The UI fonts retain their license in `UI/LICENSE.txt`.
