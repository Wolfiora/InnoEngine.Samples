# 继续制作

1. 在 Visual Novel 工作区打开 `Assets/Game/Story/Chapters` 下的原生章节。
2. 修改剧情文字、选择、条件或连线。不要把导出的阅读稿当成运行脚本。
3. `Variables.ivnvariables` 管理四个共享变量；不要在各章重复声明同名变量。
4. 角色设计器编辑姿势和表情。图集源区域是六列两行，新增表情时同时检查两个姿势。
5. 舞台与时间线设计器编辑背景、镜头、粒子及演出。已有片段通过资源键绑定。
6. 界面设计器编辑 `Game/UI/Screens`；主题在 `NeonNight.ivntheme`。
7. 原生资源整理目录包含分类及标签；授权目录与文字署名文件同时维护。
8. 修改对白后，从插件配音管理导出新稿，再生成对应台词 ID 的 WAV 并审核关联。

`Tools/Bake-Voice.ps1` 使用 Windows PowerShell 与系统 WinRT 语音。它按台词 ID 生成文件，已有文件默认保留；修改已有台词时应先明确替换该句录音，再重新绑定。

`Tools/Prepare-Audio.ps1` 是离线合成工具，只生成 WAV，不进入 Player。

`Tools/Authoring` 必须显式选定 `NovelEngineAssemblies` 和正式编译输出 `NovelScriptOutput`。它可以 inspect、导出阅读稿及校验两路线；seed 仅用于首次创建，已经交付的原生图不依赖重新运行 seed。工具通过公共宿主接口显式准备媒体和原生引用，避免把冷启动的未准备对象当成有效内容。audit 只报告原生引用身份和引擎目录路径；polish 用于本案例的姿势与资源标签整理。

普通存档严格匹配内容和代码构建。修改剧情或插件后，旧手动槽可能被正确拒绝；玩家设置和鉴赏使用独立配置机制。
