
# 《霓虹零时：最后一盏灯》视觉小说 Demo 实施计划

> 原计划归档。用户已于 2026-11-02 明确授权执行；正文中的“本轮为计划阶段”描述保留原始语境，不限制此次实施。实际交付与偏差见 Acceptance.md、KnownLimitations.md，实际完整文件树见 FileTree.txt。额度保留线为 5%，不是 10%。

## 1. 交付目标与实施边界

在 `C:\Dev\GameEngineDev\InnoEngine.Samples\Story` 制作一个完整、整洁、可继续编辑的视觉小说示例。

已确定的规格：

| 项目 | 决定 |
|---|---|
| 游戏类型 | 都市奇幻、魔法战斗、轻量分支视觉小说 |
| 时长 | 单次正常游玩约 15–20 分钟 |
| 内容 | 三章、三位主要角色、三个主要选择、两个完整结局 |
| 美术 | 二次元都市奇幻；深蓝城市、青色术式、暖金黎明 |
| 配音 | 全部角色对白中文合成配音；旁白不配音 |
| 必须交付 | 编辑器原生 Play 中完整游玩，真实画面、输入、音频、持久化可用 |
| 独立程序 | 现有 SDK 与 Support Pack 匹配时附加交付，不作为本次硬门槛 |
| 持久化 | 存档、设置、已读、鉴赏解锁能跨 Play 重启保存 |
| 插件修改 | 允许修复缺陷、补充通用能力；禁止加入故事专用业务 |
| 引擎修改 | 禁止修改 `InnoEngine` 的源码、配置、构建产物或缓存 |
| 优先级 | 项目整洁度 = 分层复用设计 = 完整度 > 内容深度 > 测试数量 |

本任务明确指定 Story 为目标，因此写入边界按以下方式执行：

- 案例内容、制作工具、素材、案例报告写入 Story。
- 插件通用改动、插件验证输出及状态文档留在 VisualNovel 仓库。
- InnoEngine 只读；使用明确选定的既有构建。
- 不改其他示例，不依赖 Canvas、Rendering2D 或其他功能插件。
- 本轮为计划阶段，不创建项目文件或生成正式素材。

**完成标准是能从标题开始，正常读完任一结局，返回标题，停止并重新进入 Play 后继续读取存档和鉴赏记录。**

---

## 2. 当前能力、架构与必要补齐

### 2.1 已查明的实际基础

Story 目前为空。VisualNovel 已包含：

- 原生游戏定义、章节图、共享变量、角色、舞台、音频提示和演出片段。
- 18 个系统页面角色，以及主题和 UI 组件复用。
- 对白配音、角色独立音量、历史语音重播。
- 姿势、表情、部位差分、呼吸和动画。
- 时间线、粒子、转场、屏幕效果、镜头及音量轨道。
- 存档服务、玩家配置、全局进度、收藏和隔离回想。
- 原生资源分类标签与许可记录。
- 正式脚本编译、插件导出和干净消费工程验证工具。

需要区分两个现状：

1. 功能代码与逻辑验证已经存在，不等于当前场景下的实机表现已经验收。
2. 原生场景中的 `NovelPlayerComponent` 尚未自动获得持久化服务；现有 `EditorNovelStateCodec` 用于编辑器预览，不能直接宣称正式游戏存档已经接通。

本次将首先解决第二项，并在真实 Play 中验证第一项。

### 2.2 内容与代码分层

```text
引擎既有构建，只读
    │
    ├── Scene / GameObject / GameBehavior 生命周期
    ├── Assets / Serialization / Identity / Events
    ├── Graph / Editor History
    └── Rendering / Audio / Storage
            │
            ▼
InnoEngine.VisualNovel 插件
    ├── Runtime：剧情、舞台、音频、UI、存档、鉴赏
    ├── Integration：原生组件与引擎适配
    └── Editor：创作工具、Play 持久化接线
            │
            ▼
Story 案例
    ├── 原生章节图和变量
    ├── 页面、主题、角色、舞台、演出资源
    ├── 图片、配音、音乐、音效
    └── 离线制作及启动工具
```

明确的实现规则：

- 游戏入口保持 `Scene → GameObject → NovelPlayerComponent`。
- Story 默认不增加运行时 C# 脚本，也不创建空的运行时程序集。
- 三章剧情使用 `.ivnstory`；章节图是唯一控制流权威。
- 阅读稿、配音表、分镜表属于制作资料或导出结果，不成为第二套运行脚本。
- 战斗由剧情节点、时间线、粒子、镜头、音频和选择分支构成。
- 不新增战斗框架、剧情解释器、资源数据库、事件总线或保存格式。
- 修改共享 UI、舞台和动作资源即可改变多个使用位置，避免复制业务逻辑。
- 所有正式资源经原生导入和保存生成身份及 `.imeta`，不手写原生二进制格式。

### 2.3 Play 持久化接线

这是第一阶段必须完成的通用插件功能。

已在引擎源码中发现公开的：

- `IEditorPlayMode.ListenStateChanges`。
- `IEditorSceneWorkspace.scenes`，Play 时提供隔离后的运行场景。
- Editor scope 的 `SerializationRegistry`。
- Runtime scope 的 `IApplicationStorage` 和 `Storage`。
- 插件已有 `NovelPlayerComponent.ConfigurePersistence(storage, codec)`。

实施方案：

1. 在插件 Editor 层新增 `NovelPlayPersistenceModule`。
2. 通过引擎依赖注入获取 Play 状态、场景工作区及当前代序列化服务。
3. 使用引擎事件订阅生命周期监听进入 Play。
4. 进入 Playing 后，找到当前运行场景中的 VN 组件，在首次会话创建前注入服务。
5. 复用现有 `EditorNovelStateCodec`，不创建另一套编解码器。
6. 若没有可直接取得的正式存储实例，增加一个薄的 `IApplicationStorage` 适配器，转发到公开 `Storage` API；它不实现文件格式或独立文件存储。
7. 所有存储操作在正确的运行上下文发起；异步完成仍通过现有控制器在 owner 线程提交。
8. Play 停止、脚本代际退休时取消工作、释放订阅，禁止旧 codec 或旧组件被迟到回调使用。
9. 显式配置过持久化的组件保留原配置，自动接线不能覆盖。
10. 不在编辑场景上创建游戏会话；不依赖 VN 面板是否打开。

当前 Windows Editor 实现把持久数据放在：

```text
Story/Library/PersistentData/inno.editor.play/Storage/
```

保存身份继续由原有 `gameId` 和存档服务管理。该目录保留跨 Play、跨 Editor 重启的数据，不提交 Git，也不被普通构建清理操作删除。

**接口变更控制：**

- 引擎公开 API：零修改。
- Story 运行时 API：零新增。
- 优先复用 `ConfigurePersistence` 原签名。
- 如确实需要查询“组件是否已配置服务”，只增加只读状态查询，不开放任意替换运行中会话的接口。
- 新 Editor 模块放入现有 Editor 程序集；薄存储适配器放入现有 Integration 程序集。
- Runtime 不引用 Authoring、Compiler 或 Editor。

该路径尚未正式编译和运行验证。若选定的既有构建未导出相应接口，记录为阻塞；不得通过反射、实现 DLL 引用、内存假存档或修改引擎绕过。

---

## 3. 故事、角色与分支设计

### 3.1 故事概念

游戏名：**《霓虹零时：最后一盏灯》**

雨夜，城市的所有电子钟停在 23:59。

维修学徒林澈在末班地铁站发现，停电后的线路仍流动着青色光芒。守灯人许岚告诉她：城市电网之下，还存在一张封锁“夜潮”的魔法回路。

前工程师沈砚正准备在中央钟塔启动“零时仪式”，把城市永远留在灾难发生前的一分钟。林澈与许岚必须在最后一次钟声前抵达塔顶，阻止夜潮，并决定封印的代价由谁承担。

故事结构强调清楚的目标、即时冲突和明确收束；不展开复杂世界历史。

### 3.2 三位主要角色

| 角色 | 定位与性格 | 视觉特征 | 表现要求 |
|---|---|---|---|
| 林澈 | 女性，22 岁，城市维修学徒；直率、行动快，能看见魔法线路 | 深色短发、橙色工具包、短夹克 | 日常与施术两姿势；由惊讶到坚定的成长 |
| 许岚 | 女性，24 岁，守灯人；克制、有责任感，习惯独自承担 | 银灰长发、深蓝外套、青色灯印 | 日常与战斗两姿势；结局分别表现释然与失去术式后的平静 |
| 沈砚 | 男性，32 岁，前电网工程师；试图挽回事故，对城市并非毫无感情 | 黑发、长外套、红金回路、破损怀表 | 对峙与释放术式两姿势；从执着到承认现实 |

其他居民和夜潮怪物通过旁白、剪影或效果表现，不再增加需要独立声线的主要角色。

每位角色提供：

- 1 个头像。
- 2 个姿势。
- 6 个表情：平静、微笑、惊讶、愤怒、悲伤、坚定。
- 眨眼差分及轻微呼吸。
- 稳定角色 ID、姿势 ID、表情 ID。
- 独立姓名颜色与语音音量。

“不同标签”同时落实为：

1. 剧情可用的姿势与表情标签。
2. 创作资源的分类和检索标签。
3. 图节点的场景、战斗、CG、分支等作者备注。

资源标签不承担运行时逻辑。

### 3.3 三章逐场景安排

| 章节／场景 | 剧情与文案任务 | 主要演出及功能 |
|---|---|---|
| 第一章 A：雨夜末班车 | 林澈维修站台设备；广播突然中断；电子钟全部停在 23:59 | 雨夜街景、站台环境音、打字对白、第一次语音 |
| 第一章 B：灯印出现 | 夜潮沿线路侵入，许岚出场救下林澈；说明眼前危险 | 角色入场、首次表情变化、青色粒子、CG「雨中灯印」 |
| 第一章 C：站台交战 | 玩家选择先护送乘客，或切断怪物附着的线路 | 第一个选择、两段不同动作、变量写入、分支汇合 |
| 第一章 D：留下的人 | 林澈决定同行；二人看到钟塔的红色光环 | 短暂安静、镜头抬升、章节收束、自动保存 |
| 第二章 A：修灯铺 | 两人在旧修灯铺准备术式；解释封印会消耗守灯人的记忆 | 暖色室内、较慢对话、角色资料和词条解锁 |
| 第二章 B：共鸣练习 | 玩家选择分担许岚的术式，或独自调查回路 | 第二个选择；信任值变化；不同对白与表情 |
| 第二章 C：塔顶对峙 | 沈砚说明仪式目的；林澈指出被冻结的城市无法继续生活 | 三人同台、发言强调、压迫镜头、音乐过渡 |
| 第二章 D：封印破裂 | 沈砚启动仪式，夜潮反噬；许岚受伤，林澈抓住主回路 | 首轮战斗、红青效果对撞、音效、短黑场 |
| 第三章 A：零时核心 | 林澈与许岚进入核心；城市灯光逐区熄灭 | 核心舞台、背景视差、低频环境音 |
| 第三章 B：最后的术式 | 两人合力抵挡夜潮；沈砚发现仪式不能挽回过去 | 多阶段时间线、短句配音、战斗推进 |
| 第三章 C：最终选择 | 选择共同分担封印，或让许岚执行完整封印 | 第三个选择、条件分支、选择前自动保存 |
| 第三章 D-A：万家灯火 | 三人分别接通回路，城市亮起；沈砚关闭仪式，接受现实 | 合击 CG、金色光流、黎明 CG、温暖尾声 |
| 第三章 D-B：黎明仍至 | 许岚完成封印，失去魔法及相关记忆；林澈保留两人的约定 | 同一高潮的不同收束、独立结局 CG、安静尾声 |
| 两结局共同收尾 | 展示结局名、制作名单、解锁提示，回到标题 | 结局记录、鉴赏、回想入口 |

### 3.4 分支规则

只保留少量有明确用途的游戏变量：

| 变量 | 类型／初值 | 用途 |
|---|---|---|
| `trust` | Integer，0 | 共同施术的准备程度，范围 0–2 |
| `stationChoice` | String，空 | 记录第一章路线，控制后续一句呼应对白 |
| `workshopChoice` | String，空 | 记录第二章路线及回想初值 |
| `ending` | String，空 | 本次路线结局标识 |

选择规则固定：

- 第一章“先护送乘客”：`trust + 1`；“切断附着线路”：不加信任，战斗动作不同。
- 第二章“让我分担术式”：`trust + 1`；“我去找另一条回路”：不加信任，提供另一段世界说明。
- 第三章：
  - `trust >= 1` 时可以选择“把灯交给整座城市”进入结局 A。
  - “完成守灯人的封印”始终可用，进入结局 B。
  - 条件不足时，共同分担选项显示明确原因，不能点击。

两条路线都救下城市。结局 B 有自己的尾声和人物结果，不以突然死亡或“未完待续”充当结局。

鉴赏解锁使用现有 Collection 机制，不另建 `cgUnlocked` 等平行变量。

### 3.5 文案制作规格

- 全量文本约 4,000–5,000 汉字，含分支、旁白和两段尾声。
- 单路线控制在约 3,000–4,000 汉字。
- 角色对白预计 160–190 条，以短句为主。
- 每句都有稳定 `textId`；文案修改不无故重建身份。
- 主线固定姓名，不加入命名输入，避免额外输入法流程。
- 主要阅读使用 ADV。
- 章节开场使用 Overlay；沈砚的旧记录使用一次短 Letter 展示。
- 不为展示功能而强行使用所有阅读布局。
- 最终以正常阅读和等待配音的实际时长校正文案，不单靠字数估算。

---

## 4. 游戏壳、页面与交互功能

### 4.1 页面清单

保留插件的 18 个系统角色，并增加一个帮助页面。

| 页面 | 必须实现的内容 |
|---|---|
| Loading | 读取玩家数据；加载中反馈；失败后重试或明确选择默认配置 |
| Title | 游戏标题、封面、开始、继续、读档、设置、鉴赏、帮助、制作名单 |
| Reading | 角色名、头像、正文、推进提示、选项、底部工具栏 |
| Pause | 继续阅读、存档、读档、设置、历史、回到标题 |
| Settings | 音频、阅读、显示辅助、快捷键四组设置 |
| History | 已读对白、角色名称、语音重播；不改变当前剧情位置 |
| Save | 手动／自动／快速槽分类、缩略图、章节、时间、覆盖确认 |
| Load | 同样的槽位信息；读取确认、无效存档原因、失败保持当前会话 |
| Confirm | 覆盖、删除、读取、回标题等明确确认 |
| Extras | CG、音乐、回想、结局、词条、人物分类入口 |
| Gallery | CG 缩略图、锁定占位、打开详情 |
| Music | 曲目列表、播放／暂停、切歌、循环、离开恢复原 BGM |
| Replays | 三个关键场景回想；退出后恢复原页面和音乐 |
| Endings | 两个结局的解锁状态与摘要 |
| Glossary | 四个短词条：夜潮、灯印、零时回路、守灯人 |
| Characters | 三位角色资料，随剧情解锁补充信息 |
| CollectionDetail | CG 查看、缩放／平移／重置，以及其他收藏详情 |
| Credits | 制作信息、合成配音说明、素材作者和许可署名 |
| Help | 鼠标与键盘操作、自动／快进／回滚说明 |

标题中的“继续”行为固定为：存在可读取的最新存档时读取该槽，否则禁用并说明原因。实现时复用现有槽位选择逻辑；若现有动作未覆盖该语义，在插件内补通用动作。

Editor Play 中不放一个会退出整个编辑器的“退出游戏”按钮。帮助页明确说明通过引擎停止 Play；未来独立 Player 才显示退出程序动作。

### 4.2 阅读交互

- 左键／Space／Enter：先显示完整句子，再推进。
- 自动阅读：正文显示完毕、语音结束及自动等待满足后推进。
- 快进：默认只快进已读内容；遇选择停止。
- Ctrl 按住快进；采用现有快捷键映射。
- 隐藏 UI：可欣赏完整舞台和 CG；再次输入恢复。
- 历史记录：可重播原角色语音，应用当前音量设置。
- 回滚：恢复剧情、舞台及演出状态，不撤销已获得的全局收藏。
- 暂停、设置或确认弹窗不让下层收到推进输入。
- 战斗段落仍允许暂停；不能因播放不可跳过的装饰效果卡住阅读。

### 4.3 设置与默认值

| 设置 | 默认值与行为 |
|---|---|
| 总音量 | 80% |
| BGM | 55% |
| 环境音 | 45% |
| 音效 | 70% |
| 语音 | 100% |
| UI 音效 | 50% |
| 每角色语音 | 全启用，100%；提供音量与静音 |
| 文字速度 | 1.0 倍 |
| 自动等待 | 1.5 秒；仍等待语音结束 |
| 推进停止语音 | 开启 |
| 仅快进已读 | 开启 |
| 字号 | 100%；提供 100%、125%、150% |
| 减少动态效果 | 关闭，可开启 |
| 允许闪光 | 默认关闭；普通模式也不使用高频闪烁 |
| 高对比 | 关闭，可开启 |
| 快捷键 | 使用插件默认映射，支持现有改键功能 |

设置立即生效，通过原生玩家配置保存。读档和回滚不把玩家音量、字号恢复成旧值。

只提供简体中文，不做没有实际译文的语言切换控件。

### 4.4 存档规格

- 20 个手动槽。
- 3 个轮换自动槽。
- 1 个快速槽。
- 章节切换及主要选择前自动保存，复用已有自动保存控制器。
- 存档显示实际缩略图，不使用统一假图。
- 内容或代码指纹不匹配时，说明不兼容并保留当前游戏。
- 保存错误可重试；不能显示成功后实际没有落盘。
- 回标题后保留玩家配置和全局进度。
- 停止 Play 后再次进入，槽位和解锁状态仍存在。

---

## 5. 美术、音频与演出制作规格

### 5.1 统一美术规范

设计画布为 1920×1080，16:9。

- 背景与 CG：至少 1920×1080，优先高分辨率原图后使用原生缩放。
- 角色：透明背景 PNG，统一画布、脚底基准与头部定位。
- 色彩：深蓝底色、青色友方术式、红金敌方术式、暖金结局。
- 角色差分保持发型、服装、配饰、光源和脸部结构一致。
- 使用 `imagegen` 生成母版，再以母版制作姿势与表情变体。
- 先生成少量代表素材验证风格，再扩展整套。
- 背景不给出重要文字；标牌、章节名、游戏标题通过 UI 排版。
- 所有图片最终保存到项目，运行时不访问生成服务或远程 URL。

立绘采用身体、表情层、眨眼层的可组合资源。两种身体姿势保持可复用脸部的锚点；如某姿势确需不同脸部角度，制作对应真实差分，不靠错误拉伸解决。

### 5.2 固定图像清单

**8 张背景：**

1. 雨夜商业街。
2. 停电地铁站台。
3. 青色术式小巷。
4. 旧修灯铺室内。
5. 钟塔屋顶。
6. 零时核心空间。
7. 黎明屋顶。
8. 黎明街道。

**4 张剧情 CG：**

1. 雨中灯印：许岚挡在林澈身前。
2. 共鸣破晓：林澈与许岚连接城市回路。
3. 万家灯火：三人在黎明中见到城市复苏。
4. 黎明仍至：失去术式的许岚与林澈重新相遇。

**额外素材：**

- 1 张标题封面。
- 三位角色各 2 个身体姿势、6 个表情、2 个眨眼差分、1 个头像。
- 夜潮剪影、灯印、线路光流等效果精灵。
- UI 面板边框、分隔线和按钮装饰。
- 常用图标、字体优先复用插件已有正式资源。

### 5.3 音频方案

**配音：**

- 离线生成并随项目提供 WAV；游戏运行时不调用 TTS。
- 已在系统注册表看到慧慧、康康、瑶瑶中文声音，第一阶段验证实际枚举和合成能力。
- 默认角色声线：林澈用瑶瑶，许岚用慧慧，沈砚用康康。
- 每位角色固定声线和基础语速；战斗短句、停顿、重音逐句调整。
- 不用简单变调替代另一位角色的身份。
- 不配旁白；角色对白、回想和结局对白全部有实际文件。
- 以稳定台词 ID 命名，使用插件配音管理批量关联。
- 配音失败、零时长、错句、漏句都阻止该章节进入完成状态。
- 合成质量不达标时先修文本发音与停顿，不静默把该句降为无配音。

**5 首音乐用途：**

| 用途 | 采用素材 |
|---|---|
| 标题／都市日常 | Kevin MacLeod《Cipher》 |
| 修灯铺／仪式真相 | Kevin MacLeod《Heart of Nowhere》 |
| 站台与钟塔交战 | Kevin MacLeod《Clash Defiant》 |
| 最终战 | Kevin MacLeod《Volatile Reaction》 |
| 黎明与制作名单 | 插件内置《Dawn》 |

前三首战斗／氛围候选及其出处均有官方曲目记录；下载时按对应官方许可记录署名与编辑信息，不将“可免费下载”视为无需署名。[官方曲目目录](https://incompetech.com/music/royalty-free/full_list.html)、[Heart of Nowhere](https://incompetech.com/music/royalty-free/index.html?Search=Search&isrc=USUAN1400045)、[Clash Defiant](https://remote.incompetech.com/music/royalty-free/index.html?Search=Search&isrc=USUAN1600003)、[Volatile Reaction](https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1400039)。

- 使用原生音频提示配置循环、音量和淡变。
- 普通 BGM 切换淡出／淡入约 0.8–1.2 秒。
- 对白期间通过现有音量能力控制背景响度，确保配音清楚。
- 音乐鉴赏退出后恢复原曲目及播放状态。

**环境音与音效：**

- 3 个循环环境：雨声、站台低鸣、高处风声。
- 12 个剧情音效：脚步、断电、灯印启动、术式蓄力、挥击、护盾、冲击、玻璃裂纹、回路接通、钟声、破封、黎明提示。
- UI 点击、确认、取消优先复用插件内置资源。
- 非语言音效可离线合成或使用许可明确的素材；不增加运行时音频生成器。
- 统一检查突兀截断、爆音、静音尾段、循环接缝和相对音量。

### 5.4 演出资源

制作 13 个可复用片段：

| 片段 | 目的 |
|---|---|
| EnterLeft / EnterRight | 角色平滑入场 |
| SpeakerEmphasis | 当前发言人轻微强调 |
| CameraPush | 对峙及重要台词缓慢推镜 |
| RainOpening | 雨夜开场 |
| LampReveal | 第一次灯印显现 |
| PlatformClash | 第一章短战斗 |
| ResonancePractice | 修灯铺共鸣练习 |
| TowerBreak | 钟塔回路破裂 |
| FinalDefense | 最终战防御阶段 |
| FinalCounter | 最终反击阶段 |
| SealCollapse | 封印收束 |
| DawnReveal | 黎明与结局 CG 展示 |

制作 4 个粒子预设：

- Rain。
- LampSparks。
- CircuitFlow。
- NightFragments。

转场与屏幕效果从插件内置资源复制可编辑副本，保留本作实际使用的淡变、暗幕、短时震动、轻微色调变化等；不把整个内置库复制进 Story。

**高潮固定节拍：**

1. 约 2 秒：音乐转入最终战，核心光环显现。
2. 约 3 秒：许岚防御，盾面粒子与轻微镜头冲击。
3. 一句对白：林澈发现主回路，等待玩家正常推进。
4. 约 3 秒：镜头沿光流推进，沈砚的回路开始破裂。
5. 最终选择：停止装饰性演出对玩家操作的干扰。
6. 约 4 秒：对应路线的术式释放、CG 出现和音乐高潮。
7. 约 2 秒：粒子消退、屏幕恢复、转入尾声对白。

动作片段和对白共同组织节奏，不播放一整段无法交互的长动画。减少动态效果开启时仍保留剧情必需的状态变化，但移除震动和大幅运动。

---

## 6. 按实施顺序拆解的工作步骤

### 阶段 A：环境与真实可运行基础

1. 记录 VisualNovel、Samples 当前 Git 状态，保留已有用户修改。
2. 确认 Story 为空目录，并建立本项目专用忽略规则。
3. 选择一套已存在的 Editor、Build host 和脚本编译依赖，记录路径、版本和指纹。
4. 把会产生写入的工具运行目录与缓存放入 Story 或插件仓库。
5. 必要时复制完整、匹配的运行依赖到 Story 的忽略目录；禁止混搭不同构建的单个 DLL。
6. 建立极薄的工具启动器，调用既有公开 Editor 组合入口，避免引擎工程 `dotnet run` 触发兄弟仓库构建。
7. 从当前插件源码导出 `.iplugin`，不能直接沿用落后于源码的旧发行包。
8. 安装到 Story 的 `Plugins`，走正式插件发现和激活流程。
9. 创建原生项目身份、启动场景和 VN 游戏定义。
10. 在 Game 视图验证中文文字、一个角色图像、一段音乐和一条真实配音。

**阶段完成条件：** 真实 Play 能显示、输入和出声；没有生产实现 DLL 旁路引用。

### 阶段 B：持久化闭环

11. 增加 Editor Play 持久化模块及必要的薄存储适配。
12. 验证服务注入早于 `EnsureSession`，不因打开面板或渲染顺序变化失效。
13. 接通现有存档页、玩家配置加载页和全局进度服务。
14. 保存一条已读对白、一个手动槽、一个音量值和一个 CG 解锁。
15. 停止 Play，再进入，逐项确认恢复。
16. 关闭 Editor 再打开工程，确认磁盘数据仍可读取。
17. 验证损坏／不匹配存档不会覆盖当前会话。
18. 将实际结果写入插件状态文档。

**阶段完成条件：** 真正落盘并可恢复。未达成前，不把“存档功能完整”写入案例说明。

### 阶段 C：美术样板与完整游戏壳

19. 完成一页美术规范及角色造型参考。
20. 生成雨夜背景、许岚基础立绘和标题封面样板。
21. 实机检查透明边缘、缩放、中文字体与深色界面对比。
22. 从 Cinema 风格建立本作单一主题，清理未使用的其他主题引用。
23. 制作 18 个系统页面和帮助页。
24. 提取对话框、选择面板、工具栏、确认框、存档卡片和音量行等共享组件。
25. 接好标题、暂停、设置、返回、确认及鉴赏页面导航。
26. 检查鼠标、键盘焦点及 1280×720 下的布局。
27. 接入实际音频默认值和持久设置。

**阶段完成条件：** 任意页面均有正确返回路径，没有装饰性假按钮或死入口。

### 阶段 D：全文与章节图

28. 完成三章及两结局全文；先写完收尾，再继续扩展细节。
29. 为对白分配稳定 ID，标注角色、场景、情绪和配音停顿。
30. 建立共享变量库。
31. 使用正式导入及创作功能创建三个章节图。
32. 接入三个选择、条件、变量赋值和分支汇合。
33. 建立结局和回想公开入口。
34. 给每个场景入口明确设置背景、角色、音乐与必要变量前提。
35. 将重复演出通过片段调用复用。
36. 导出阅读稿校对角色称谓、术语、时间和因果关系。
37. 从标题正常走完两结局，确认所有分支都能结束。

**阶段完成条件：** 不依赖最终美术，也已是一段完整、无断路的故事；开发素材不得留到最终交付。

### 阶段 E：最终图片与资源整理

38. 按统一参考完成全部背景、角色差分、CG 和封面。
39. 统一角色画布、锚点、尺寸与表情覆盖关系。
40. 在角色设计器建立姿势、表情、头像及轻微动画。
41. 建立八个舞台及其背景、前景、粒子和角色站位。
42. 完成每个角色的独白、双人、三人构图检查。
43. 将资源加入原生分类目录，填写章节、角色、情绪、用途标签。
44. 将生成信息、外部来源和许可加入原生许可目录及中文素材说明。
45. 删除未引用的试做图，制作中间稿移至忽略的工作目录。

**阶段完成条件：** 每个剧情引用都有正式素材；无默认教学角色、错误表情或随机混合画风。

### 阶段 F：配音与音频

46. 从最终章节导出配音稿，避免手工维护另一份对白。
47. 实际枚举三个系统声线，先合成每人三条代表句。
48. 检查普通对白、疑问句和战斗短句的可懂度。
49. 批量生成全部对白 WAV，按台词 ID 落盘。
50. 修复多音字、专有名词、过长停顿及切分错误。
51. 通过配音管理创建 Voice 提示，关联章节及游戏资源库。
52. 导入音乐、环境音和音效，配置分类、循环与淡变。
53. 逐场景试听，调整音乐与对白的相对响度。
54. 验证自动阅读等待配音、推进停止语音及历史重播。
55. 导出最终配音覆盖清单。

**阶段完成条件：** 角色对白配音覆盖率 100%，文件内容与当前文本一致。

### 阶段 G：演出与高潮

56. 完成 13 个时间线片段和 4 个粒子预设。
57. 第一章接入出场、表情切换、初次灯印和站台战斗。
58. 第二章接入共鸣练习、三人对峙和回路破裂。
59. 第三章按固定节拍完成防御、选择、反击和黎明。
60. 为并行演出明确唯一交互焦点，装饰轨道不能抢占选择。
61. 确认同一属性的轨道冲突、结束姿态和取消行为正确。
62. 检查暂停、回滚、读档后没有残留粒子、错位镜头和重复播放声音。
63. 调整减少动态效果和禁止闪光时的完整表现。

**阶段完成条件：** 高潮具有清晰的动作、声音和画面变化，且不破坏阅读和恢复流程。

### 阶段 H：鉴赏、回想与收尾

64. 配置 4 个 CG 条目、5 个音乐条目、3 个回想条目。
65. 配置 2 个结局、4 个词条及 3 个角色资料。
66. 在对应剧情节点解锁，锁定内容不泄露图片和结局。
67. 为回想设置明确入口和变量初值。
68. 验证回想不污染主进度，也不覆盖主游戏存档。
69. 完成制作名单、素材署名、操作帮助及结局返回标题流程。
70. 检查“继续游戏”与最新存档一致。

### 阶段 I：必要验收与交付

71. 执行正式脚本编译和现有程序集架构检查。
72. 对插件通用改动运行必要行为测试，不新增庞大测试项目。
73. 完整人工游玩结局 A 和 B，记录真实时长。
74. 完成一次持久化、音频、设置、鉴赏和回想集中检查。
75. 修复发现的问题，只针对修改部分复验。
76. 导出最终插件包并再次用 Story 消费验证。
77. 清理无用资源、生成工程、临时材料及绝对路径依赖。
78. 完成 README、创作说明、素材说明和实际验收记录。
79. 若匹配的独立构建环境可用，再执行 Windows 打包；失败不伪装成独立程序已交付。

---

## 7. 完整目标文件结构

以下为最终业务文件结构。批量语音使用 `{textId}` 表示由原生台词身份展开的实际文件；它不是占位素材。图像差分的花括号表示明确列出的文件集合。

所有正式 `Assets` 资源及需要身份的目录，其 `.imeta` 由引擎生成并随源提交；为避免把文件树重复一倍，不逐项列出。

```text
C:\Dev\GameEngineDev\InnoEngine.Samples\Story\
├── .gitignore
├── README.md
├── Settings.Project.inno
├── Plugins/
│   └── InnoEngine.VisualNovel.iplugin
│
├── Assets/
│   ├── Game/
│   │   ├── NeonZero.ivnproject
│   │   ├── Scenes/
│   │   │   └── Main.iscene
│   │   │
│   │   ├── Story/
│   │   │   ├── Variables.ivnvariables
│   │   │   └── Chapters/
│   │   │       ├── Ch01_LastTrain.ivnstory
│   │   │       ├── Ch02_BrokenCircuit.ivnstory
│   │   │       └── Ch03_LastLight.ivnstory
│   │   │
│   │   ├── Characters/
│   │   │   ├── LinChe.ivncharacter
│   │   │   ├── XuLan.ivncharacter
│   │   │   └── ShenYan.ivncharacter
│   │   │
│   │   ├── Stages/
│   │   │   ├── StreetRain.ivnstage
│   │   │   ├── MetroPlatform.ivnstage
│   │   │   ├── LampAlley.ivnstage
│   │   │   ├── LampWorkshop.ivnstage
│   │   │   ├── ClockTower.ivnstage
│   │   │   ├── ZeroCore.ivnstage
│   │   │   ├── RooftopDawn.ivnstage
│   │   │   └── StreetDawn.ivnstage
│   │   │
│   │   ├── Presentation/
│   │   │   ├── Materials/
│   │   │   │   ├── NovelSurface.imaterial
│   │   │   │   └── NovelComposition.imaterial
│   │   │   ├── Clips/
│   │   │   │   ├── Common/
│   │   │   │   │   ├── EnterLeft.ivnclip
│   │   │   │   │   ├── EnterRight.ivnclip
│   │   │   │   │   ├── SpeakerEmphasis.ivnclip
│   │   │   │   │   └── CameraPush.ivnclip
│   │   │   │   ├── Chapter01/
│   │   │   │   │   ├── RainOpening.ivnclip
│   │   │   │   │   ├── LampReveal.ivnclip
│   │   │   │   │   └── PlatformClash.ivnclip
│   │   │   │   ├── Chapter02/
│   │   │   │   │   ├── ResonancePractice.ivnclip
│   │   │   │   │   └── TowerBreak.ivnclip
│   │   │   │   └── Chapter03/
│   │   │   │       ├── FinalDefense.ivnclip
│   │   │   │       ├── FinalCounter.ivnclip
│   │   │   │       ├── SealCollapse.ivnclip
│   │   │   │       └── DawnReveal.ivnclip
│   │   │   └── Particles/
│   │   │       ├── Rain.ivneffect
│   │   │       ├── LampSparks.ivneffect
│   │   │       ├── CircuitFlow.ivneffect
│   │   │       └── NightFragments.ivneffect
│   │   │
│   │   ├── UI/
│   │   │   ├── NeonNight.ivntheme
│   │   │   ├── Components/
│   │   │   │   ├── DialogueBox.ivnui
│   │   │   │   ├── ChoicePanel.ivnui
│   │   │   │   ├── ReadingToolbar.ivnui
│   │   │   │   ├── ConfirmDialog.ivnui
│   │   │   │   ├── SaveSlotCard.ivnui
│   │   │   │   └── VolumeRow.ivnui
│   │   │   └── Screens/
│   │   │       ├── Loading.ivnui
│   │   │       ├── Title.ivnui
│   │   │       ├── Reading.ivnui
│   │   │       ├── Pause.ivnui
│   │   │       ├── Settings.ivnui
│   │   │       ├── History.ivnui
│   │   │       ├── Save.ivnui
│   │   │       ├── Load.ivnui
│   │   │       ├── Confirm.ivnui
│   │   │       ├── Extras.ivnui
│   │   │       ├── Gallery.ivnui
│   │   │       ├── Music.ivnui
│   │   │       ├── Replays.ivnui
│   │   │       ├── Endings.ivnui
│   │   │       ├── Glossary.ivnui
│   │   │       ├── Characters.ivnui
│   │   │       ├── CollectionDetail.ivnui
│   │   │       ├── Credits.ivnui
│   │   │       └── Help.ivnui
│   │   │
│   │   ├── Audio/
│   │   │   └── Cues/
│   │   │       ├── Music/
│   │   │       │   ├── CityCipher.ivnaudio
│   │   │       │   ├── HiddenCircuit.ivnaudio
│   │   │       │   ├── TowerConflict.ivnaudio
│   │   │       │   ├── ZeroHourBattle.ivnaudio
│   │   │       │   └── Dawn.ivnaudio
│   │   │       ├── Ambience/
│   │   │       │   ├── Rain.ivnaudio
│   │   │       │   ├── MetroHum.ivnaudio
│   │   │       │   └── RooftopWind.ivnaudio
│   │   │       ├── Sfx/
│   │   │       │   ├── Footsteps.ivnaudio
│   │   │       │   ├── PowerOut.ivnaudio
│   │   │       │   ├── LampIgnite.ivnaudio
│   │   │       │   ├── SpellCharge.ivnaudio
│   │   │       │   ├── SpellSlash.ivnaudio
│   │   │       │   ├── Shield.ivnaudio
│   │   │       │   ├── Impact.ivnaudio
│   │   │       │   ├── GlassCrack.ivnaudio
│   │   │       │   ├── CircuitConnect.ivnaudio
│   │   │       │   ├── ClockBell.ivnaudio
│   │   │       │   ├── SealBreak.ivnaudio
│   │   │       │   └── DawnChime.ivnaudio
│   │   │       └── Voice/
│   │   │           ├── Ch01/{textId}.ivnaudio
│   │   │           ├── Ch02/{textId}.ivnaudio
│   │   │           └── Ch03/{textId}.ivnaudio
│   │   │
│   │   ├── Collections/
│   │   │   └── NeonZero.ivncollection
│   │   └── Localization/
│   │       └── Chinese.ivnlocale
│   │
│   ├── Art/
│   │   ├── Backgrounds/
│   │   │   ├── StreetRain.png
│   │   │   ├── MetroPlatform.png
│   │   │   ├── LampAlley.png
│   │   │   ├── LampWorkshop.png
│   │   │   ├── ClockTower.png
│   │   │   ├── ZeroCore.png
│   │   │   ├── RooftopDawn.png
│   │   │   └── StreetDawn.png
│   │   ├── Characters/
│   │   │   ├── LinChe/
│   │   │   │   ├── BodyIdle.png
│   │   │   │   ├── BodyCasting.png
│   │   │   │   ├── Face{Neutral,Smile,Surprised,Angry,Sad,Determined}.png
│   │   │   │   ├── BlinkHalf.png
│   │   │   │   ├── BlinkClosed.png
│   │   │   │   └── Portrait.png
│   │   │   ├── XuLan/
│   │   │   │   ├── BodyIdle.png
│   │   │   │   ├── BodyBattle.png
│   │   │   │   ├── Face{Neutral,Smile,Surprised,Angry,Sad,Determined}.png
│   │   │   │   ├── BlinkHalf.png
│   │   │   │   ├── BlinkClosed.png
│   │   │   │   └── Portrait.png
│   │   │   └── ShenYan/
│   │   │       ├── BodyIdle.png
│   │   │       ├── BodyCasting.png
│   │   │       ├── Face{Neutral,Smile,Surprised,Angry,Sad,Determined}.png
│   │   │       ├── BlinkHalf.png
│   │   │       ├── BlinkClosed.png
│   │   │       └── Portrait.png
│   │   ├── CG/
│   │   │   ├── RainLamp.png
│   │   │   ├── ResonantDawn.png
│   │   │   ├── CityLights.png
│   │   │   └── MorningReunion.png
│   │   ├── Effects/
│   │   │   ├── NightSilhouette.png
│   │   │   ├── LampSigil.png
│   │   │   ├── CircuitStreak.png
│   │   │   ├── RainDrop.png
│   │   │   └── LightShard.png
│   │   └── UI/
│   │       ├── TitleKeyArt.png
│   │       ├── PanelFrame.png
│   │       ├── ButtonFrame.png
│   │       └── Divider.png
│   │
│   ├── Audio/
│   │   ├── Music/
│   │   │   ├── Cipher.wav
│   │   │   ├── HeartOfNowhere.wav
│   │   │   ├── ClashDefiant.wav
│   │   │   └── VolatileReaction.wav
│   │   ├── Ambience/
│   │   │   ├── Rain.wav
│   │   │   ├── MetroHum.wav
│   │   │   └── RooftopWind.wav
│   │   ├── Sfx/
│   │   │   ├── Footsteps.wav
│   │   │   ├── PowerOut.wav
│   │   │   ├── LampIgnite.wav
│   │   │   ├── SpellCharge.wav
│   │   │   ├── SpellSlash.wav
│   │   │   ├── Shield.wav
│   │   │   ├── Impact.wav
│   │   │   ├── GlassCrack.wav
│   │   │   ├── CircuitConnect.wav
│   │   │   ├── ClockBell.wav
│   │   │   ├── SealBreak.wav
│   │   │   └── DawnChime.wav
│   │   └── Voice/
│   │       └── zh-CN/
│   │           ├── Ch01/{textId}.wav
│   │           ├── Ch02/{textId}.wav
│   │           └── Ch03/{textId}.wav
│   │
│   └── Authoring/
│       ├── Catalog/
│       │   ├── StoryResources.ivnlabels
│       │   └── StoryLicenses.ivnlicenses
│       └── Routes/
│           ├── EndingCityLights.ivntest
│           ├── EndingMorningReunion.ivntest
│           └── FinalBattleReplay.ivntest
│
├── Production/
│   ├── Art/
│   │   ├── ArtDirection.md
│   │   ├── CharacterReferences.md
│   │   └── GenerationPrompts.md
│   ├── Story/
│   │   ├── Synopsis.md
│   │   ├── SceneBeats.md
│   │   └── BranchMap.md
│   ├── Voice/
│   │   ├── VoiceCasting.tsv
│   │   └── Pronunciation.tsv
│   └── Audio/
│       └── MixNotes.md
│
├── Tools/
│   ├── Open-Story.ps1
│   ├── Import-Plugin.ps1
│   ├── Bake-Voice.ps1
│   ├── Prepare-Audio.ps1
│   └── EditorHost/
│       ├── Story.EditorHost.csproj
│       └── Program.cs
│
├── Documentation/
│   ├── ImplementationPlan.md
│   ├── Architecture.md
│   ├── AuthoringGuide.md
│   ├── Controls.md
│   ├── AssetCredits.md
│   ├── Acceptance.md
│   └── KnownLimitations.md
│
├── Licenses/
│   ├── MusicAttribution.txt
│   ├── FontNotices.txt
│   ├── PluginNotices.txt
│   └── GeneratedAssets.txt
│
├── Library/                         # 引擎生成，忽略
│   └── PersistentData/             # 跨 Play 保存，勿当普通缓存清理
│
├── artifacts/                       # 制作与验证输出，忽略
│   ├── EngineHost/
│   ├── Tooling/
│   ├── MediaWork/
│   ├── Exports/
│   │   ├── Reading/
│   │   └── Voice/
│   ├── Reports/
│   └── Screenshots/
│
└── Builds/                          # 仅独立发布实际成功时产生，忽略
    └── Windows/
```

说明：

- 《Dawn》音频、内置字体、公共 UI 音效及 Shader 使用插件正式资产引用，不复制无必要的二进制。
- `Chinese.ivnlocale` 用于稳定文字身份和现有本地化机制，不代表交付第二种语言。
- `Production` 不被 Player 读取。
- `.ivntest` 只保留三条有实际制作价值的路线，不建立额外测试框架。
- IDE 的 `.sln`、`.csproj` 等由引擎生成，不作为手工维护的游戏项目入口。
- `Assets/Code` 初始不创建；任何后续必要脚本必须有实际职责，不能预留空层。

插件侧的预计改动边界：

```text
C:\Dev\GameEngineDev\InnoEngine.VisualNovel\
├── Assets/Code/
│   ├── Editor/
│   │   └── Persistence/
│   │       └── NovelPlayPersistenceModule.editor.cs       # 新增
│   ├── Integration/
│   │   ├── Hosting/
│   │   │   └── NovelPlayerComponent.cs                    # 必要的小范围接线改动
│   │   └── Services/
│   │       └── InnoNovelStorage.cs                       # 仅在需要薄适配时新增
│   └── Authoring/Preview/
│       └── EditorNovelStateCodec.editor.cs               # 复用现有实现
├── Tests/Contracts/
│   └── 现有契约入口及持久化相关用例                       # 就地扩充
├── Documentation/
│   ├── Specifications/FeatureMatrix.md
│   └── ImplementationStatus.md
└── artifacts/
    ├── Distribution/
    └── Reports/
```

其他插件修改只针对实际发现的通用缺陷，不预先新增一批空服务或故事专用扩展。

---

## 8. 必要验证、交付内容与阻塞处理

### 8.1 验证保持集中

| 场景 | 验证内容 |
|---|---|
| 正式消费 | 当前源码导出的插件能被 Story 正式激活，程序集边界通过 |
| 路线 A | 标题到万家灯火结局，再返回标题 |
| 路线 B | 条件不足及主动选择封印都能到第二结局 |
| 声音 | 全对白有配音；BGM、环境音、音效分类音量真实生效 |
| 保存恢复 | 手动／自动／快速保存；停止 Play、重开 Editor 后读取 |
| 状态恢复 | 战斗中读档和回滚，角色、镜头、粒子、语音没有错乱 |
| 玩家配置 | 音量、字号和辅助设置持久化，读档不覆盖偏好 |
| 鉴赏回想 | 锁定、解锁、音乐恢复、独立回想与主进度隔离 |
| UI | 1920×1080、1280×720、150% 字号；鼠标与键盘可完成操作 |
| 工程边界 | 无引擎修改，无其他插件依赖，无生产二进制引用绕过 |

正式编译和必要行为测试必须执行，但不以增加测试条数替代完整游玩。图像截图也不能代替音频试听。

### 8.2 最终交付

1. 可直接打开并 Play 的 Story 工程。
2. 当前源码对应的 VisualNovel 插件包。
3. 完整三章文本、两结局与全对白语音。
4. 主菜单、设置、存读档、历史、鉴赏、回想和帮助。
5. 最终角色差分、背景、CG、音乐、音效及演出资源。
6. 原生可编辑资源和明确的文件组织。
7. 中文启动、编辑、素材及验收文档。
8. 插件状态文档中与本次改动对应的真实证据。
9. 独立 EXE 仅在实际构建并启动验收成功后附加。

### 8.3 不允许降格冒充完成的情况

以下情况必须标为未完成并说明具体原因：

- 只有脚本编译通过，没有实际 Play。
- 存档只保存在内存，停止 Play 后丢失。
- 有配音表但没有对应可播放文件。
- 有 CG 按钮但没有实际图片和解锁流程。
- 两个结局只更换标题，没有独立收束内容。
- 最终战只有文字，没有实际演出。
- 把编辑器预览当成原生场景 Play。
- 将 SDK／Support Pack 不匹配描述成已经完成独立发布。

实施中遇到引擎公开接口缺口时，继续完成不受阻的内容，并保留未达成的硬性条件；不修改引擎，也不降低已确定的持久化和完整可玩标准。

