# 实际验收记录

2026-11-02 完成本次 Editor 原生 Play 案例交付。选用 VisualNovel 仓库内既有 EngineBuildSnapshot-20261031-0611 与 EngineEditorSnapshot；未编译或写入相邻 InnoEngine。

| 项目 | 实际证据 |
|---|---|
| 原生内容 | 三章、三角色、12 个舞台、19 页、147 个音频提示；127 条对白 WAV 已绑定 |
| 双路线冷导入 | 最终资源与插件包重新加载后，两条 Native NovelSession 路线各通过三个选择，历史分别 134 / 137 条；artifacts/DeliveryInspection.txt |
| 正式编译 | 正式 Scripting API 编译、十程序集检查 0 错误；插件 artifacts/Reports/Story-SourceDependencyCompile.txt |
| 行为基线 | 246 项完整基线通过；Story-AsyncBehavior.txt |
| 后续定向检查 | 输入/音频/设置等 29 项、捕获 4 项、布局/选择等 14 项、原生冷缓存/闭包等 4 项通过，均 0 失败 |
| D3D11 全流程 | 第一结局在 EditorFinalAcceptance 运行阶段实际走完；最终包在 EditorDeliveryAcceptance 阶段走完第二结局及制作名单，返回标题正常 |
| 原生键盘 | Space 推进、S 快进、Esc 暂停已操作；使用正式 Input.snapshot 与视口鼠标合并，无全局钩子 |
| 画面与演出 | 实际显示标题、中文文本、头像、日常/施术姿势与表情、八背景、战斗镜头/粒子、转场与剧情 CG；未进行逐像素或所有预设排列验收 |
| 音频 | 原生播放与快进全路线未再触发音频准备退休错误；音乐鉴赏可选择曲目；合成配音文件齐备，不宣称真人配音或主观听感评分 |
| 设置持久化 | 文字速度与已读快进策略关闭并重开 Editor 后仍保留；输入、音量、角色开关等独立偏好另有行为契约 |
| 真实缩略图 | D3D11 实际生成 PNG，快速槽与手动槽落盘；存档页显示当时的画面、时间和正文 |
| 自动存档 | 三个自动槽实际轮转写入磁盘，包含三个选择及章节边界 |
| 跨重启读档 | 正常停止 Play、关闭并重开 Editor，再进 Play；手动槽缩略图仍在，确认读取后恢复到第一章雨夜正文与舞台；artifacts/EditorRestartAcceptance.txt |
| 全局进度 | 重开并读取较早存档后，“万家灯火”和“黎明仍至”两个结局记录仍保留 |
| 鉴赏与回想 | 四 CG 列表、大图查看、音乐选择、回想启动与退出均实际操作；未逐项操作所有缩放、搜索和音量组合 |
| 退场 | 两轮编辑器均正常停止 Play、正常关闭，日志具有 Dispose end.，未出现本轮运行错误 |

## 证据位置

插件 artifacts/Reports 下的 Story-FinalPlayableBehavior.txt、Story-CaptureBehavior.txt、Story-DeliveryBehavior.txt、Story-SourceDependencyBehavior.txt 对应上述定向检查。Story-DiffCheck.txt 对应最终差异空白检查，退出码 0。

案例 artifacts/EditorDeliveryAcceptance.txt 记录最终包的 D3D11 启动与正常退出，时间为 04:35–04:43；artifacts/EditorRestartAcceptance.txt 为重启恢复轮，时间为 04:44–04:50。GUI 操作与画面由本次 Computer Use 会话实际观察，运行日志不伪造点击或逐帧截图记录。

首次源资源尚在准备时可出现瞬时目标未就绪的诊断；不将它们等同于流程中断，也不把逻辑路线检查等同于真实设备播放。早期发现的作用域、纹理续用、异步存储、音频退休、空截图上传和无效页面动作已修正并由后续实机流程复验。

## 交付数据与边界

仅将本次验收新建的 Story 玩家数据归档到 artifacts/AcceptanceData/20261102-StoryPlay；包含偏好、全局进度与三类槽位。正常关闭 Editor 后才移动，未删除其他工程或用户数据。案例下一次 Play 使用默认设置与未通关状态；日后的 Library/PersistentData 是玩家数据，请保留。

未交付独立 Windows 程序；127 条对白与原计划 160–190 条不同，正常时长未人工计时。其余边界见 KnownLimitations.md。实机验收后仅补齐 Authoring 依赖相对路径的挂载归属，Player、Integration、Rendering 代码未再修改。更新包经正式编译、上述定向检查、另一个包引用归属检查（Story-PackageSourceBehavior.txt，1 项通过）和案例双路线原生重读验证，未重复整套 GUI。

当前结论限定于本案例及选定既有 Editor 构建，不代表整个插件 v1 或所有设备后端已验收。
