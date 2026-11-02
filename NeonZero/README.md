# 霓虹零时：最后一盏灯

InnoEngine.VisualNovel 的都市魔法短篇示例。雨夜末班车、守灯人、停在 23:59 的城市，以及两个通往黎明的结局。

## 启动

需要 Windows x64、.NET SDK 10.0.402，以及与插件正式 API 匹配的现有 InnoEngine Editor 构建。

```powershell
.\Tools\Open-Story.ps1 -EngineEditorDirectory '你的现有 Editor 构建目录'
```

当前开发工作区默认使用相邻 VisualNovel 仓库的 `artifacts/EngineEditorSnapshot`。启动工具只组合现有公开宿主，不构建或修改引擎。所有启动输出位于本工程 `artifacts/EditorHost`。

1. 等待插件脚本编译完成。
2. 在 File 面板打开 `Assets/Game/Scenes/Main.iscene`。
3. 点击引擎 Play，进入 Game 视图。
4. 从标题选择“开始游戏”。可关闭 Scene、Hierarchy 等面板扩大 Game 区域。

## 内容

- 三章原生剧情图、三个主要选择、两个完整结局。
- 三位角色；各两种姿势、六种表情、独立头像和呼吸表现。
- 八张背景、四张剧情 CG、一张标题封面。
- 127 条角色对白中文配音；旁白无配音。
- 五首音乐、三段环境循环、十二个剧情音效。
- 标题、阅读、暂停、设置、历史、存读档、鉴赏、回想、制作名单及帮助。
- 20 个手动槽、3 个自动槽和1个快速槽，使用插件原生存档机制。

角色、剧情、页面、舞台与演出均为可编辑原生资源。工程没有故事专用运行时 C#，也不依赖 Canvas 或 Rendering2D。

## 编辑与操作

操作见 `Documentation/Controls.md`；继续创作见 `Documentation/AuthoringGuide.md`；实际验证结果见 `Documentation/Acceptance.md`；交付清单见 `Documentation/Delivery.md`。

玩家数据位于 `Library/PersistentData/inno.editor.play/Storage`。停止 Play 不应删除该目录；清理普通构建输出也不要把它当成临时缓存。

外部音乐署名见 `Licenses/MusicAttribution.txt`。AI 图片、系统合成配音与内置资源说明见 `Documentation/AssetCredits.md`。
