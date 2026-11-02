using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inno.Assets;
using Inno.Assets.Pipeline;
using Inno.Audio;
using Inno.Core.Identity;
using Inno.Core.Graphs;
using Inno.Core.Serialization;
using Inno.Extensibility.Types;
using Inno.Rendering;
using Inno.Rendering.Assets;
using Inno.Scene;
using Inno.Text;
using Inno.VisualNovel.Authoring.AssetAuditing.Licenses;
using Inno.VisualNovel.Authoring.AssetOrganization;
using Inno.VisualNovel.Authoring.Templates;
using Inno.VisualNovel.Authoring.Preview;
using Inno.VisualNovel.Compiler;
using Inno.VisualNovel.Integration.Hosting;
using Inno.VisualNovel.Model.Audio;
using Inno.VisualNovel.Model.Characters;
using Inno.VisualNovel.Model.Collections;
using Inno.VisualNovel.Model.Interface;
using Inno.VisualNovel.Model.Interface.Themes;
using Inno.VisualNovel.Model.Localization;
using Inno.VisualNovel.Model.Performance;
using Inno.VisualNovel.Model.Project;
using Inno.VisualNovel.Model.Stages;
using Inno.VisualNovel.Model.Story;
using Inno.VisualNovel.Model.Story.Nodes.Reading;
using Inno.VisualNovel.Model.Story.Nodes.Stage;
using Inno.VisualNovel.Model.Story.Nodes.Interface;
using Inno.VisualNovel.Narrative.Execution;
using Inno.VisualNovel.Player.Sessions;
using Inno.VisualNovel.Model.Values;

namespace Story.Tools.Authoring;

internal sealed class NativeProject(
    string root,
    AssetPipeline assets,
    IdentityAllocator identities,
    TypeCatalog types,
    SerializationRegistry serialization,
    StoryNodeDataCodec codec
)
{
    private static readonly string[] m_emotions = ["neutral", "smile", "surprised", "angry", "sad", "determined"];
    private static readonly string[] m_chapters = ["Ch01_LastTrain", "Ch02_BrokenCircuit", "Ch03_LastLight"];
    private readonly List<AssetObject> m_authored = [];
    private const string GamePath = "Game/NeonZero.ivnproject";

    internal void Create()
    {
        if (File.Exists(Path.Combine(root, "Assets", GamePath)))
            throw new InvalidOperationException("The native game already exists. Edit its graph and resources; the seed never overwrites an authored game.");
        var font = BuiltIn<FontAsset>("BuiltIn/Fonts/NotoSansCJKsc-Regular.otf");
        var material = Save("Game/Presentation/Materials/NovelSurface.imaterial", new MaterialAsset { shader = BuiltIn<ShaderAsset>("BuiltIn/Shaders/Graphs/NovelSurface.ishader") });
        var composition = Save("Game/Presentation/Materials/NovelComposition.imaterial", new MaterialAsset { shader = BuiltIn<ShaderAsset>("BuiltIn/Shaders/Graphs/NovelPostProcess.ishader") });
        var stages = new List<StageAsset>();
        foreach (string id in new[] { "StreetRain", "MetroPlatform", "LampAlley", "LampWorkshop", "ClockTower", "ZeroCore", "RooftopDawn", "StreetDawn", "RainLamp", "ResonantDawn", "CityLights", "MorningReunion" })
        {
            bool cg = id is "RainLamp" or "ResonantDawn" or "CityLights" or "MorningReunion";
            var image = Load<TextureAsset>("Art/" + (cg ? "CG/" : "Backgrounds/") + id + ".png");
            stages.Add(Save("Game/Stages/" + id + ".ivnstage", new StageAsset { resourceId = "nz.stage." + id, title = id, textures = [image],
                layers = [new() { id = "background", name = "背景", texture = 0, width = 1920, height = 1080, parallax = .85 }] }));
        }
        var characters = new[] { Character("lin", "林澈", 0xFFBE75FF), Character("xu", "许岚", 0x7CE8E5FF), Character("shen", "沈砚", 0xF19B99FF) };
        var music = new List<AudioCueAsset>();
        foreach (var item in new[] { ("city", "Cipher", "城市的另一面"), ("hidden", "HeartOfNowhere", "灯火与记忆"), ("conflict", "ClashDefiant", "回路交锋"), ("battle", "VolatileReaction", "零时决战") })
            music.Add(Save("Game/Audio/Cues/Music/" + item.Item2 + ".ivnaudio", new AudioCueAsset { resourceId = "nz.music." + item.Item1,
                title = item.Item3, author = "Kevin MacLeod · incompetech.com", channel = NovelAudioChannel.Bgm, loop = true, volume = .55,
                fadeIn = .9, fadeOut = .9, clip = Load<AudioClipAsset>("Audio/Music/" + item.Item2 + ".mp3") }));
        var dawn = AudioCueCopy(BuiltIn<AudioCueAsset>("BuiltIn/Audio/Music/Dawn.ivnaudio")); dawn.resourceId = "nz.music.dawn"; dawn.title = "天亮以后";
        music.Add(Save("Game/Audio/Cues/Music/Dawn.ivnaudio", dawn));
        var audio = music.ToList();
        foreach (string name in new[] { "Rain", "MetroHum", "RooftopWind" })
            audio.Add(Save("Game/Audio/Cues/Ambience/" + name + ".ivnaudio", new AudioCueAsset { resourceId = "nz.ambience." + name,
                title = name, channel = NovelAudioChannel.Ambience, loop = true, volume = .35, fadeIn = .8, fadeOut = .8,
                clip = Load<AudioClipAsset>("Audio/Ambience/" + name + ".wav") }));
        foreach (string name in new[] { "Footsteps", "PowerOut", "LampIgnite", "SpellCharge", "SpellSlash", "Shield", "Impact", "GlassCrack", "CircuitConnect", "ClockBell", "SealBreak", "DawnChime" })
            audio.Add(Save("Game/Audio/Cues/Sfx/" + name + ".ivnaudio", new AudioCueAsset { resourceId = "nz.sfx." + name,
                title = name, channel = NovelAudioChannel.Sfx, volume = .55, clip = Load<AudioClipAsset>("Audio/Sfx/" + name + ".wav") }));
        var clips = Clips();
        var particles = new[] { Particle("Rain", "Rain"), Particle("LampSparks", "Fireflies"), Particle("CircuitFlow", "LightPoints"), Particle("NightFragments", "Dust") };
        var variables = Save("Game/Story/Variables.ivnvariables", new VariableTableAsset { variables = [
            new() { id = "trust", name = "共鸣", initialValue = NovelValue.Integer(0), group = "主线", description = "共同承担术式的准备程度。" },
            new() { id = "stationChoice", name = "站台选择", initialValue = NovelValue.String("") },
            new() { id = "workshopChoice", name = "修灯铺选择", initialValue = NovelValue.String("") },
            new() { id = "ending", name = "本次结局", initialValue = NovelValue.String("") }] });
        var voices = new List<string> { "chapter\ttextId\tcharacter\ttext" };
        var chapters = StorySeed.Create(codec, voices);
        for (int i = 0; i < chapters.Length; i++)
        {
            chapters[i].variableTable = variables;
            chapters[i] = Save("Game/Story/Chapters/" + m_chapters[i] + ".ivnstory", chapters[i]);
        }
        string exports = Path.Combine(root, "artifacts", "Exports", "Voice"); Directory.CreateDirectory(exports);
        File.WriteAllLines(Path.Combine(exports, "VoiceScript.tsv"), voices, new UTF8Encoding(true));
        var theme = ShellThemeTemplates.Create("Cinema", font); theme.resourceId = "nz.theme.night"; theme.title = "霓虹夜色";
        foreach (var color in theme.colors)
            if (color.key == "accent") color.value = 0x75DAD7FF;
        foreach (var binding in theme.screens)
        {
            ScreenAsset screen = binding.screen!;
            if (binding.role == "title") DecorateTitle(screen);
            if (binding.role == "credits") foreach (var element in Walk(screen.root))
                if (element.id == "credits") element.text = "霓虹零时：最后一盏灯\nInnoEngine.VisualNovel 原生资源示例\n角色对白：Windows 中文合成语音\n图片：AI 生成原创都市奇幻插画\n音乐：Kevin MacLeod / InnoEngine.VisualNovel\n外部音乐按 CC BY 4.0 署名，详见项目 Licenses。\n感谢游玩。灯会坏，天会亮。";
            binding.screen = Save("Game/UI/Screens/" + binding.role + ".ivnui", screen);
        }
        theme = Save("Game/UI/NeonNight.ivntheme", theme);
        var collections = Save("Game/Collections/NeonZero.ivncollection", Collections(music));
        var game = new NovelProjectAsset { gameId = "inno.samples.story.neonzero", title = "霓虹零时：最后一盏灯", defaultLanguage = "zh-CN",
            width = 1920, height = 1080, font = font, material = material, effectsMaterial = composition, chapters = chapters, startChapter = chapters[0].chapterId,
            characters = characters, stages = stages.ToArray(), initialStage = "nz.stage.StreetRain", audio = audio.ToArray(), titleAudio = music[0].clip,
            performances = clips, particles = particles, collections = collections, themes = [theme], defaultTheme = theme.resourceId, screens = theme.screens,
            manualSaveSlots = 20, automaticSaveSlots = 3, autoSaveOnChoices = true };
        game.localization = Save("Game/Localization/Chinese.ivnlocale", StarterLocalizationTemplate.Create(game));
        game = Save(GamePath, game);
        using (var world = new SceneWorld(identities, types))
        using (world.EnterScope())
        {
            var scene = new GameScene("霓虹零时"); var owner = scene.CreateObject("Visual Novel Player");
            owner.AddComponent<NovelPlayerComponent>().project = game;
            Save("Game/Scenes/Main.iscene", SceneAsset.Capture(scene, serialization, assets));
            scene.DestroyObject(owner);
        }
        Save("Authoring/Catalog/StoryResources.ivnlabels", new ResourceLabelCatalogAsset { title = "霓虹零时资源", entries = m_authored.Select(item => new ResourceLabelEntry
            { asset = item, group = item.assetPath.localPath.Split('/')[1], tags = ["霓虹零时", Path.GetExtension(item.assetPath.localPath).TrimStart('.')], description = item.assetPath.localPath }).ToArray() });
        Console.WriteLine("Native Story game created. Voice lines: " + (voices.Count - 1));
    }

    private CharacterAsset Character(
        string id,
        string name,
        uint color
    ) {
        var atlas = Load<TextureAsset>("Art/Characters/" + id + "/Atlas.png");
        var poses = new List<CharacterPose>();
        for (int row = 0; row < 2; row++) poses.Add(new() { id = row == 0 ? "idle" : "battle", name = row == 0 ? "日常" : "施术", parts =
            m_emotions.Select((emotion, column) => new CharacterPart { id = emotion, name = emotion, texture = 0, width = 520, height = 940,
                visible = column == 0, surface = new() { sourceX = column / 6.0, sourceY = row / 2.0, sourceWidth = 1 / 6.0, sourceHeight = .5 } }).ToArray() });
        return Save("Game/Characters/" + id + ".ivncharacter", new CharacterAsset { resourceId = "nz.character." + id, displayName = name, nameColor = color,
            defaultPose = "idle", textures = [atlas], poses = poses.ToArray(), breathing = new() { enabled = true, amplitude = .004, period = 4.5 },
            expressions = m_emotions.Select(emotion => new CharacterExpression { id = emotion, name = emotion,
                parts = m_emotions.Select(part => new CharacterPartOverride { partId = part, visibility = part == emotion ? 1 : 0 }).ToArray() }).ToArray() });
    }

    private PerformanceClipAsset[] Clips()
    {
        string[] names = ["EnterLeft", "EnterRight", "SpeakerEmphasis", "CameraPush", "RainOpening", "LampReveal", "PlatformClash", "ResonancePractice", "TowerBreak", "FinalDefense", "FinalCounter", "SealCollapse", "DawnReveal"];
        return names.Select((name, index) => {
            bool shake = name is "PlatformClash" or "TowerBreak" or "FinalDefense" or "FinalCounter" or "SealCollapse";
            double duration = shake ? 1.2 : name is "DawnReveal" or "RainOpening" ? 3 : 1.8;
            var clip = new PerformanceClipAsset { resourceId = "nz.clip." + name, displayName = name, duration = duration, essential = false,
                tracks = [new() { target = "camera", property = shake ? PerformanceProperty.ShakeX : PerformanceProperty.X, keys = [
                    new() { time = 0, value = 0, interpolation = PerformanceInterpolation.Smooth },
                    new() { time = duration * .25, value = shake ? 8 + index : -15, interpolation = PerformanceInterpolation.Smooth },
                    new() { time = duration * .65, value = shake ? -6 - index : 12, interpolation = PerformanceInterpolation.Smooth },
                    new() { time = duration, value = 0 }] }] };
            return Save("Game/Presentation/Clips/" + name + ".ivnclip", clip);
        }).ToArray();
    }

    private VisualEffectAsset Particle(
        string name,
        string builtIn
    ) {
        var effect = VisualEffectCopy.Copy(BuiltIn<VisualEffectAsset>("BuiltIn/Particles/" + builtIn + ".ivneffect"));
        effect.resourceId = "nz.particle." + name;
        return Save("Game/Presentation/Particles/" + name + ".ivneffect", effect);
    }

    private CollectionAsset Collections(List<AudioCueAsset> music)
    {
        var entries = new List<CollectionEntry>();
        foreach (var item in new[] { ("rain", "雨中灯印", "RainLamp"), ("resonance", "共鸣破晓", "ResonantDawn"), ("city", "万家灯火", "CityLights"), ("morning", "黎明仍至", "MorningReunion") })
            entries.Add(new() { id = "cg." + item.Item1, kind = CollectionKind.Cg, title = item.Item2, image = Load<TextureAsset>("Art/CG/" + item.Item3 + ".png") });
        foreach (var cue in music) entries.Add(new() { id = "music." + cue.resourceId, kind = CollectionKind.Music, title = cue.title, author = cue.author,
            musicCueId = cue.resourceId, initiallyUnlocked = true });
        foreach (var item in new[] { ("lin", "林澈", "维修学徒。相信所有断开的线路，都值得再接一次。"), ("xu", "许岚", "守灯人。守住别人的归途，也在寻找自己的明天。"), ("shen", "沈砚", "前电网工程师。曾想留住一分钟，后来学会认真道别。") })
            entries.Add(new() { id = "character." + item.Item1, kind = CollectionKind.Character, title = item.Item2, description = item.Item3 });
        foreach (var item in new[] { ("tide", "夜潮", "从时间缝隙中涌出的暗影，会模仿人的愿望。"), ("lamp", "灯印", "把记忆化成光的术式，也可以通过共鸣由多人分担。"), ("keeper", "守灯人", "维护城市封印的人。他们的灯，照着别人回家的路。"), ("circuit", "零时回路", "把电网化为术式的装置。只能重播记忆，不能归还生命。") })
            entries.Add(new() { id = "glossary." + item.Item1, kind = CollectionKind.Glossary, title = item.Item2, description = item.Item3 });
        foreach (var item in new[] { ("city", "万家灯火", "把灯交给整座城市。"), ("morning", "黎明仍至", "记忆会散去，新的故事仍会开始。") })
            entries.Add(new() { id = "ending." + item.Item1, kind = CollectionKind.Ending, title = item.Item2, description = item.Item3 });
        entries.Add(new() { id = "replay.tower", kind = CollectionKind.Replay, title = "钟塔对峙", chapterId = "nz.ch02", initialValues = [new() { id = "trust", value = NovelValue.Integer(1) }] });
        entries.Add(new() { id = "replay.final", kind = CollectionKind.Replay, title = "最后的灯印", chapterId = "nz.ch03", initialValues = [new() { id = "trust", value = NovelValue.Integer(1) }] });
        entries.Add(new() { id = "replay.opening", kind = CollectionKind.Replay, title = "雨夜相遇", chapterId = "nz.ch01", initiallyUnlocked = true });
        return new() { entries = entries.ToArray() };
    }

    private void DecorateTitle(ScreenAsset screen)
    {
        foreach (var element in Walk(screen.root))
        {
            if (element.id == "title") element.text = "霓虹零时";
            if (element.id == "subtitle") element.text = "最后一盏灯 · THE LAST LIGHT";
            if (element.id == "eyebrow") element.text = "URBAN FANTASY / VISUAL NOVEL";
            if (element.id == "background")
            {
                element.kind = UiElementKind.Image; element.image = Load<TextureAsset>("Art/UI/TitleKeyArt.png"); element.imageFit = UiImageFit.Cover;
                element.styleClass = ""; element.style.background = 0; element.style.imageTint = 0xFFFFFFFF;
            }
            if (element.id == "title-composition") { element.layout.x = 130; element.layout.y = 120; element.layout.width = 780; element.layout.gap = 18; }
            if (element.id == "menu") { element.kind = UiElementKind.Vertical; element.layout.widthMode = UiSizeMode.Fixed; element.layout.width = 520; element.layout.gap = 10;
                foreach (var child in element.children) child.layout.minHeight = 64; }
            if (element.id == "title") { element.style.fontSize = 82; element.styleOverrides |= UiStyleProperty.FontSize; }
        }
        screen.root.children = screen.root.children.Where(item => item.id != "title-art").ToArray();
    }

    internal void Refine()
    {
        var game = Load<NovelProjectAsset>(GamePath);
        var theme = game.themes.Single();
        foreach (var binding in theme.screens)
        {
            if (binding.role == "title") DecorateTitle(binding.screen!);
            if (binding.role == "credits") foreach (var element in Walk(binding.screen!.root))
            {
                if (element.id == "credits") element.text = "霓虹零时：最后一盏灯\nInnoEngine.VisualNovel 原生资源示例\n角色对白：Microsoft Yaoyao / Huihui / Kangkang 中文合成语音\n图片：AI 生成原创都市奇幻插画\n音乐：Cipher / Heart of Nowhere / Clash Defiant / Volatile Reaction\nKevin MacLeod (incompetech.com)\nLicensed under Creative Commons: By Attribution 4.0 License\nhttps://creativecommons.org/licenses/by/4.0/\n内置音乐 Dawn：InnoEngine.VisualNovel\n感谢游玩。灯会坏，天会亮。";
                if (element.kind == UiElementKind.Button && element.action is "back" or "title") { element.action = "open"; element.argument = "title"; element.text = "返回标题"; }
            }
            binding.screen = Save(binding.screen!.assetPath.localPath, binding.screen);
        }
        var help = Save("Game/UI/Screens/help.ivnui", new ScreenAsset { title = "操作帮助", font = game.font, root = new()
        {
            id = "help-root", kind = UiElementKind.Panel, style = new() { background = 0x0C172BF7 },
            layout = new() { widthMode = UiSizeMode.Fill, heightMode = UiSizeMode.Fill }, children = [
                new() { id = "help-heading", kind = UiElementKind.Text, text = "操作帮助", style = new() { fontSize = 52 }, layout = new() { x = 120, y = 80, width = 1400, height = 90 } },
                new() { id = "help-text", kind = UiElementKind.Text, text = "左键 / Space / Enter：显示完整句子，再次输入推进\nCtrl 按住：快进已读内容；选择时停止\nEscape：暂停菜单\n历史：回看对白并重播语音\n自动：等待文字和配音结束后推进\n存档、设置及鉴赏记录会跨 Play 保存\n减少动态效果、高对比与字号可在设置中调整\n编辑器中结束游戏，请使用引擎停止 Play。", style = new() { fontSize = 30, lineSpacing = 1.5f }, layout = new() { x = 120, y = 220, width = 1650, height = 680 } },
                new() { id = "back", kind = UiElementKind.Button, text = "返回", action = "back", style = new() { background = 0x243B55FF, fontSize = 30 }, layout = new() { x = 120, y = 950, width = 260, height = 70 } }] }
        });
        var title = theme.screens.Single(item => item.role == "title").screen!;
        var menu = Walk(title.root).Single(item => item.id == "menu");
        if (!menu.children.Any(item => item.id == "help")) menu.children = [.. menu.children, new() { id = "help", kind = UiElementKind.Button,
            text = "操作帮助", action = "open", argument = "help", styleClass = "button", layout = new() { widthMode = UiSizeMode.Fill, heightMode = UiSizeMode.Content, minHeight = 64 } }];
        theme.screens.Single(item => item.role == "title").screen = Save(title.assetPath.localPath, title);
        if (!theme.screens.Any(item => item.role == "help")) theme.screens = [.. theme.screens, new() { role = "help", screen = help }];
        else theme.screens.Single(item => item.role == "help").screen = help;
        game.screens = theme.screens;
        game.themes = [Save(theme.assetPath.localPath, theme)];
        foreach (var actor in game.characters)
        {
            string id = actor.resourceId.Split('.').Last();
            string portraitPath = "Art/Characters/" + id + "/Portrait.png";
            if (File.Exists(Path.Combine(root, "Assets", portraitPath))) actor.portrait = Load<TextureAsset>(portraitPath);
            Save(actor.assetPath.localPath, actor);
        }
        foreach (string name in new[] { "dialogue", "choices", "toolbar", "confirmation", "text-speed", "button" })
        {
            var template = BuiltIn<ScreenAsset>("BuiltIn/Widgets/" + name + ".ivnui");
            Save("Game/UI/Components/" + name + ".ivnui", new ScreenAsset { title = name, font = template.font, width = template.width, height = template.height,
                root = template.root, textFonts = template.textFonts, textIcons = template.textIcons });
        }
        var licenses = new List<ResourceLicenseEntry>();
        foreach (var entry in assets.GetFileSystemEntries(includeDirectories: false).Where(item => item.source == AssetSourceId.project && item.assetPath.localPath.StartsWith("Art/", StringComparison.Ordinal)))
            if (entry.assetPath.localPath.EndsWith(".png", StringComparison.Ordinal)) licenses.Add(new() { asset = Inno.Assets.Assets.Load<TextureAsset>(entry.assetPath), author = "Story Demo · AI generated",
                source = "imagegen built-in; original fictional characters; prompts documented in Production/Art/GenerationPrompts.md", license = "Generated for this demo", redistributionConfirmed = true });
        foreach (var cue in game.audio.Where(item => item.channel == NovelAudioChannel.Bgm && item.resourceId != "nz.music.dawn"))
            licenses.Add(new() { asset = cue.clip, author = "Kevin MacLeod", source = "https://incompetech.com/music/royalty-free/", license = "CC BY 4.0", attributionRequired = true,
                redistributionConfirmed = true, notice = "Kevin MacLeod (incompetech.com), Creative Commons Attribution 4.0, https://creativecommons.org/licenses/by/4.0/", modifications = "None; original MP3 files." });
        Save("Authoring/Catalog/StoryLicenses.ivnlicenses", new ResourceLicenseCatalogAsset { title = "素材来源与许可", entries = licenses.ToArray() });
        var transition = VisualEffectCopy.Copy(BuiltIn<VisualEffectAsset>("BuiltIn/Transitions/dissolve.ivneffect"));
        transition.resourceId = "nz.transition.dissolve"; transition.transition.duration = .6;
        game.transitions = [Save("Game/Presentation/Transitions/Dissolve.ivneffect", transition)];
        for (int i = 0; i < m_chapters.Length; i++)
        {
            string local = "Game/Story/Chapters/" + m_chapters[i] + ".ivnstory";
            var source = EditorAssets.DecodeNative<StoryAsset>(File.ReadAllBytes(Path.Combine(root, "Assets", local)));
            foreach (var node in source.graph.document.nodes.ToArray())
            {
                if (node.definitionId == StoryNodeIds.stage && codec.Read<StageNode>(node) is { operation: StageOperation.SetStage } scenery)
                {
                    var edges = source.graph.document.edges.Where(edge => edge.output.nodeId == node.id || edge.input.nodeId == node.id).ToArray();
                    source.graph.document.RemoveNode(node.id);
                    var replacement = new GraphNodeRecord(node.id, StoryNodeIds.transition) { position = node.position };
                    codec.Write(replacement, new TransitionNode { operation = TransitionOperation.Begin, nextStage = scenery.resourceId,
                        resourceId = transition.resourceId, waitForCompletion = true });
                    source.graph.document.AddNode(replacement); foreach (var edge in edges) source.graph.document.AddEdge(edge);
                }
                if (node.definitionId == StoryNodeIds.end) InsertSystem(source, node, new() { operation = SystemOperation.OpenPage, argument = "credits" }, "credits-");
                if (node.definitionId == StoryNodeIds.call) InsertSystem(source, node, new() { operation = SystemOperation.AutoSave, argument = "" }, "autosave-");
            }
            if (i == 1 && !source.entries.Any(entry => entry.id == "tower"))
            {
                var target = source.graph.document.nodes.First(node => node.definitionId == StoryNodeIds.transition && codec.Read<TransitionNode>(node).nextStage == "nz.stage.ClockTower");
                var entry = new GraphNodeRecord(new("entry-tower"), StoryNodeIds.entry) { position = new(target.position.x - 340, target.position.y - 170) };
                source.graph.document.AddNode(entry); source.graph.document.AddEdge(new(new(Guid.NewGuid().ToString("N")), new(entry.id, new("next")), new(target.id, new("in"))));
                source.entries = [.. source.entries, new() { id = "tower", nodeId = entry.id.value }]; source.fragments[0].nodes = [.. source.fragments[0].nodes, entry.id.value];
            }
            game.chapters[i] = Save(local, source);
            if (i == 2)
            {
                foreach (var ending in new[] { ("CityLights", "结局一", new[] { "a", "a", "a" }), ("MorningReunion", "结局二", new[] { "b", "b", "b" }) })
                {
                    var target = source.graph.document.nodes.First(node => node.definitionId == StoryNodeIds.dialogue && codec.Read<DialogueNode>(node).text.StartsWith(ending.Item2, StringComparison.Ordinal));
                    Save("Authoring/Routes/Ending" + ending.Item1 + ".ivntest", new NovelPreviewCaseAsset { title = ending.Item2, mode = PreviewRunMode.Route,
                        chapterId = "nz.ch01", nodeId = target.id.value, choices = ending.Item3, maximumSeconds = 1800 });
                }
            }
        }
        var collection = game.collections!;
        collection.entries.Single(entry => entry.id == "replay.tower").entryId = "tower";
        game.collections = Save(collection.assetPath.localPath, collection);
        Save("Authoring/Routes/FinalBattleReplay.ivntest", new NovelPreviewCaseAsset { title = "最终战预览", mode = PreviewRunMode.Route,
            chapterId = "nz.ch03", nodeId = "final-choice", initialValues = [new() { id = "trust", value = NovelValue.Integer(1) }], maximumSeconds = 600 });
        Save(GamePath, game); Inspect();
    }

    private void InsertSystem(
        StoryAsset source,
        GraphNodeRecord target,
        SystemNode action,
        string prefix
    ) {
        var id = new GraphNodeId(prefix + target.id.value);
        if (source.graph.document.FindNode(id) is not null) return;
        var incoming = source.graph.document.edges.Where(edge => edge.input.nodeId == target.id).ToArray();
        var node = new GraphNodeRecord(id, StoryNodeIds.system) { position = new(target.position.x - 160, target.position.y + 100) };
        codec.Write(node, action); source.graph.document.AddNode(node);
        foreach (var edge in incoming)
        {
            source.graph.document.RemoveEdge(edge.id);
            source.graph.document.AddEdge(new(edge.id, edge.output, new(node.id, new("in"))));
        }
        source.graph.document.AddEdge(new(new(Guid.NewGuid().ToString("N")), new(node.id, new("next")), new(target.id, new("in"))));
        source.fragments[0].nodes = [.. source.fragments[0].nodes, node.id.value];
    }

    internal void Polish()
    {
        var game = Load<NovelProjectAsset>(GamePath);
        var theme = game.themes.Single();
        var reading = theme.screens.Single(binding => binding.role == "reading").screen!;
        var option = Walk(reading.root).Single(element => element.id == "option");
        option.binding = "item.text"; option.text = ""; option.children = [];
        option.tooltipBinding = "item.reason"; option.layout.minHeight = 96;
        theme.screens.Single(binding => binding.role == "reading").screen = Save(reading.assetPath.localPath, reading);
        foreach (var binding in theme.screens)
        {
            foreach (var element in Walk(binding.screen!.root))
                if (element.action == "page") element.action = "open";
            binding.screen = Save(binding.screen.assetPath.localPath, binding.screen);
        }
        game.themes = [Save(theme.assetPath.localPath, theme)]; game.screens = theme.screens;
        var locale = game.localization!;
        locale.languages = locale.languages.Where(language => language.id == "zh-CN").ToArray();
        foreach (var entry in locale.entries) entry.translations = entry.translations.Where(translation => translation.language == "zh-CN").ToArray();
        game.localization = Save(locale.assetPath.localPath, locale);
        foreach (var chapter in game.chapters)
        {
            var source = EditorAssets.DecodeNative<StoryAsset>(File.ReadAllBytes(Path.Combine(root, "Assets", chapter.assetPath.localPath)));
            foreach (var node in source.graph.document.nodes.Where(node => node.definitionId == StoryNodeIds.stage))
            {
                var actor = codec.Read<StageNode>(node);
                if (actor.operation != StageOperation.ShowCharacter) continue;
                var pending = new Queue<GraphNodeId>(); var visited = new HashSet<GraphNodeId>(); var scenery = new HashSet<string>();
                pending.Enqueue(node.id);
                while (pending.TryDequeue(out GraphNodeId cursor))
                {
                    if (!visited.Add(cursor)) continue;
                    var current = source.graph.document.FindNode(cursor)!;
                    if (current.definitionId == StoryNodeIds.transition)
                    {
                        scenery.Add(codec.Read<TransitionNode>(current).nextStage); continue;
                    }
                    foreach (var edge in source.graph.document.edges.Where(edge => edge.input.nodeId == cursor)) pending.Enqueue(edge.output.nodeId);
                }
                if (scenery.Count != 1) continue;
                actor.pose = scenery.Single() is "nz.stage.MetroPlatform" or "nz.stage.ClockTower" or "nz.stage.ZeroCore" ? "battle" : "idle";
                codec.Write(node, actor);
            }
            int index = Array.IndexOf(game.chapters, chapter);
            game.chapters[index] = Save(chapter.assetPath.localPath, source);
        }
        var catalog = Load<ResourceLabelCatalogAsset>("Authoring/Catalog/StoryResources.ivnlabels");
        var labels = catalog.entries.ToList();
        foreach (var entry in assets.GetFileSystemEntries(includeDirectories: false).Where(item => item.source == AssetSourceId.project))
        {
            string path = entry.assetPath.localPath;
            AssetObject? media = path.EndsWith(".png", StringComparison.Ordinal) ? Load<TextureAsset>(path)
                : path.EndsWith(".wav", StringComparison.Ordinal) || path.EndsWith(".mp3", StringComparison.Ordinal) ? Load<AudioClipAsset>(path) : null;
            if (media is null || labels.Any(label => label.asset?.assetPath == media.assetPath)) continue;
            string group = path.StartsWith("Art/Backgrounds/", StringComparison.Ordinal) ? "背景"
                : path.StartsWith("Art/Characters/", StringComparison.Ordinal) ? "角色立绘与头像"
                : path.StartsWith("Art/CG/", StringComparison.Ordinal) ? "CG"
                : path.StartsWith("Audio/Voice/", StringComparison.Ordinal) ? "中文配音"
                : path.StartsWith("Audio/Music/", StringComparison.Ordinal) ? "音乐"
                : path.StartsWith("Audio/Ambience/", StringComparison.Ordinal) ? "环境"
                : path.StartsWith("Audio/Sfx/", StringComparison.Ordinal) ? "音效" : "界面美术";
            labels.Add(new() { asset = media, group = group, tags = ["霓虹零时", group], description = path });
        }
        catalog.entries = labels.ToArray(); Save(catalog.assetPath.localPath, catalog); Save(GamePath, game); Inspect();
    }

    internal void BindVoices()
    {
        var game = Inno.Assets.Assets.Load<NovelProjectAsset>(AssetPath.Project(GamePath));
        var cues = game.audio.ToList();
        for (int i = 0; i < m_chapters.Length; i++)
        {
            var path = AssetPath.Project("Game/Story/Chapters/" + m_chapters[i] + ".ivnstory");
            var source = EditorAssets.DecodeNative<StoryAsset>(File.ReadAllBytes(Path.Combine(root, "Assets", path.localPath)));
            foreach (var node in source.graph.document.nodes.Where(node => node.definitionId == StoryNodeIds.dialogue))
            {
                var line = codec.Read<DialogueNode>(node);
                if (line.characterId.Length == 0) continue;
                string voice = "Audio/Voice/zh-CN/" + source.chapterId + "/" + line.textId + ".wav";
                var clip = Load<AudioClipAsset>(voice);
                var cue = Save("Game/Audio/Cues/Voice/" + source.chapterId + "/" + line.textId + ".ivnaudio", new AudioCueAsset
                    { resourceId = "nz.voice." + line.textId, title = line.textId, channel = NovelAudioChannel.Voice, clip = clip, volume = 1, loop = false });
                line.voiceCueId = cue.resourceId; codec.Write(node, line); cues.RemoveAll(existing => existing.resourceId == cue.resourceId); cues.Add(cue);
            }
            game.chapters[i] = Save(path.localPath, source);
        }
        game.audio = cues.ToArray(); Save(GamePath, game); Inspect();
    }

    internal void Inspect()
    {
        var game = Load<NovelProjectAsset>(GamePath);
        Console.WriteLine("Game: " + game.title + " / chapters=" + game.chapters.Length + " / pages=" + game.screens.Length + " / cues=" + game.audio.Length);
        foreach (var chapter in game.chapters) Console.WriteLine(chapter.chapterId + " native program=" + (chapter.program is not null));
        foreach (var route in new[] { (new[] { "a", "a", "a" }, "ending.city"), (new[] { "b", "b", "b" }, "ending.morning") })
        {
            using var session = new NovelSession(game, new RouteAudioOutput()); session.StartNew(); int choice = 0;
            for (int step = 0; step < 20000; step++)
            {
                if (session.story?.status == StoryStatus.Faulted) throw new InvalidOperationException(session.story.error);
                if (session.page == "credits" || session.story?.status == StoryStatus.Completed) break;
                session.Tick(.1);
                if (session.interactiveStory?.status == StoryStatus.Choice)
                {
                    if (choice >= route.Item1.Length || !session.Choose(route.Item1[choice++])) throw new InvalidOperationException("Route choice rejected.");
                }
                else session.Advance();
            }
            if (!session.collections.IsUnlocked(route.Item2) || choice != 3) throw new InvalidOperationException("Route did not reach its authored ending: " + route.Item2);
            Console.WriteLine("Native route passed: " + route.Item2 + " / choices=" + choice + " / history=" + session.historyCount);
        }
        string readingDirectory = Path.Combine(root, "Production", "Story"); Directory.CreateDirectory(readingDirectory);
        var reading = new List<string> { "霓虹零时：最后一盏灯", "此阅读稿由原生章节图导出，包含所有分支；章节图仍是运行时唯一控制流。", "" };
        foreach (string chapter in m_chapters)
        {
            var source = EditorAssets.DecodeNative<StoryAsset>(File.ReadAllBytes(Path.Combine(root, "Assets", "Game", "Story", "Chapters", chapter + ".ivnstory")));
            reading.Add(source.title);
            foreach (var line in source.graph.document.nodes.Where(node => node.definitionId == StoryNodeIds.dialogue).Select(node => codec.Read<DialogueNode>(node)).OrderBy(line => line.textId, StringComparer.Ordinal))
                reading.Add(line.textId + " | " + (line.characterId.Length == 0 ? "旁白" : game.characters.Single(actor => actor.resourceId == line.characterId).displayName) + "：" + line.text);
            reading.Add("");
        }
        File.WriteAllLines(Path.Combine(readingDirectory, "Reading.zh-CN.txt"), reading, new UTF8Encoding(true));
    }

    private T BuiltIn<T>(string local) where T : AssetObject
    {
        var entry = assets.GetFileSystemEntries(includeDirectories: false).First(item => item.source != AssetSourceId.project && item.assetPath.localPath == local);
        if (!assets.Import(entry.assetPath)) throw new InvalidOperationException("Plugin asset did not import: " + entry.assetPath);
        return Inno.Assets.Assets.Load<T>(entry.assetPath);
    }

    private T Load<T>(string local) where T : AssetObject
    {
        var path = AssetPath.Project(local);
        if (!assets.Import(path)) throw new InvalidOperationException("Required native asset failed to import: " + local);
        var result = Inno.Assets.Assets.Load<T>(path);
        if (result.isMissing) throw new InvalidOperationException("Required asset is Missing: " + local);
        return result;
    }

    private T Save<T>(
        string local,
        T source
    ) where T : AssetObject
    {
        var properties = assets.CaptureProperties(source);
        _ = serialization.Decode(properties.data.Span, reader => { reader.RestoreProperties(source); return 0; },
            AssetSerializationContext.Create(new CurrentSourceReferences(assets, types)));
        if (!EditorAssets.Save(AssetPath.Project(local), source)) throw new InvalidOperationException("Native save failed: " + local);
        var result = Inno.Assets.Assets.Load<T>(AssetPath.Project(local)); m_authored.Add(result); return result;
    }

    private static IEnumerable<UiElementDefinition> Walk(UiElementDefinition root)
    {
        yield return root;
        foreach (var child in root.children) foreach (var item in Walk(child)) yield return item;
    }

    private static AudioCueAsset AudioCueCopy(AudioCueAsset cue) => new() { resourceId = cue.resourceId, title = cue.title, author = cue.author, clip = cue.clip,
        channel = cue.channel, loop = cue.loop, volume = cue.volume, fadeIn = cue.fadeIn, fadeOut = cue.fadeOut };

    private sealed class CurrentSourceReferences(
        AssetPipeline pipeline,
        TypeCatalog catalog
    ) : IAssetReferenceResolver
    {
        /// <summary>Reacquires each source reference from the current native catalog before an offline save.</summary>
        public AssetObject Resolve(
            Guid persistentId,
            Guid stableTypeId,
            string lastKnownPath,
            Type expectedType,
            string propertyPath
        ) {
            if (!pipeline.TryGetInfo(persistentId, out var info) || info is null
                || !pipeline.TryLoad<AssetObject>(info.assetPath, out var value) || value is null
                || value.isMissing || !value.assetPath.isValid || !expectedType.IsInstanceOfType(value)
                || value.identity.persistentId != persistentId || catalog.GetTypeRef(value.GetType()).stableId != stableTypeId)
                throw new InvalidOperationException("The current source reference is unavailable: " + propertyPath + " -> " + persistentId);
            return value;
        }
    }
}
