# 交付说明

《霓虹零时：最后一盏灯》为 InnoEngine.VisualNovel 原生可编辑案例。游戏入口为 Assets/Game/Scenes/Main.iscene，运行内容根为 Assets/Game/NeonZero.ivnproject。

## 内容清单

| 分类 | 实际内容 |
|---|---|
| 故事 | 三章、三次主要选择、两个完整结局；全部分支阅读稿见 Production/Story/Reading.zh-CN.txt |
| 角色 | 林澈、许岚、沈砚；每人两姿势、六表情、独立头像、呼吸表现 |
| 画面 | 八背景、四 CG、标题插图；共十九张 PNG（含角色图集和头像） |
| 音频 | 127 条中文角色配音、五首音乐、三环境循环、十二音效；共 147 个原生音频提示 |
| 界面 | 十九页，覆盖标题、阅读、暂停、设置、存读档、历史、鉴赏、回想、资料、帮助及制作名单 |
| 演出 | 十二个舞台、十三个可编辑演出片段、四个粒子预设；转场、镜头震动、施术姿势和 CG 在剧情中使用 |
| 持久化 | 二十手动槽、三自动轮转槽、一快速槽；原生设置、已读及收藏进度 |
| 创作 | 原生章节图、主题与页面、角色与舞台、分类标签目录；离线生成和配音工具 |

## 交付边界

没有修改相邻 InnoEngine。Story 没有专用运行时脚本，继续使用原生 Scene → GameObject → NovelPlayerComponent 入口；不依赖 Canvas、Rendering2D，不引入平行序列化或资源系统。

此次为修复真实案例流程，在 VisualNovel 插件中修正了帧内文字和输入准备、纹理缓存续用、音频准备生命周期、原生 Play 持久化、异步上下文、缩略图目标初始化及少量 UI 显示问题。离线导入与保存继续使用正式资源契约。

## 使用与证据

启动步骤见 README.md；完整实施计划见 Documentation/ImplementationPlan.md；实际完整文件树见 Documentation/FileTree.txt。实机范围、编译与行为证据见 Documentation/Acceptance.md；偏差见 Documentation/KnownLimitations.md。

选择的现有构建为 VisualNovel/artifacts/EngineBuildSnapshot-20261031-0611（工具与正式 API）和 VisualNovel/artifacts/EngineEditorSnapshot（Editor）。启动工具只复制和组合既有公开宿主，不执行引擎构建。

配套插件包：Plugins/InnoEngine.VisualNovel.iplugin

SHA-256：EF627AB42AE5C29189C5202363C7E3421A81E8246A208AA8B297D3602602FAFF

临时报告、生成过程、运行日志与宿主组合位于 artifacts；玩家数据位于 Library/PersistentData。它们均不作为内容源纳入交付文件树。音乐与字体许可证位于 Licenses。
