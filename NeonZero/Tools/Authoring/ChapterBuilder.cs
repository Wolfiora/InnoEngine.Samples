using System;
using System.Collections.Generic;
using System.IO;
using Inno.Core.Graphs;
using Inno.Core.Serialization;
using Inno.VisualNovel.Authoring.Documents;
using Inno.VisualNovel.Compiler;
using Inno.VisualNovel.Model.Story;
using Inno.VisualNovel.Model.Story.Nodes.Audio;
using Inno.VisualNovel.Model.Story.Nodes.Flow;
using Inno.VisualNovel.Model.Story.Nodes.Reading;
using Inno.VisualNovel.Model.Story.Nodes.Stage;
using Inno.VisualNovel.Model.Story.Nodes.Variables;
using Inno.VisualNovel.Model.Performance;
using Inno.VisualNovel.Model.Values;

namespace Story.Tools.Authoring;

internal sealed class ChapterBuilder
{
    private readonly StoryNodeDataCodec m_codec;
    private readonly StoryDraftState m_draft;
    private readonly List<string> m_nodes = [];
    private readonly List<string> m_voiceRows;
    private string? m_tail;
    private int m_line;
    private bool m_cg;

    internal ChapterBuilder(
        StoryNodeDataCodec codec,
        string chapter,
        string title,
        List<string> voiceRows
    ) {
        m_codec = codec; m_voiceRows = voiceRows;
        m_draft = new() { chapterId = chapter, title = title };
        Add("entry", StoryNodeIds.entry, null); m_draft.entries = [new() { id = "start", nodeId = "entry" }];
    }

    internal string tail => m_tail!;
    internal void Continue(string node) => m_tail = node;
    internal void Entry(string id)
    {
        string node = "entry-" + id;
        m_tail = null; Add(node, StoryNodeIds.entry, null);
        m_draft.entries = [.. m_draft.entries, new() { id = id, nodeId = node }];
    }

    internal void Add(
        string id,
        string kind,
        ISerializable? settings
    ) {
        var node = new GraphNodeRecord(new(id), kind) { position = new((m_nodes.Count % 6) * 340, (m_nodes.Count / 6) * 210) };
        m_draft.graph.document.AddNode(node);
        if (settings is not null) Write(node, settings);
        if (m_tail is not null) Link(m_tail, id);
        m_tail = id; m_nodes.Add(id);
    }

    private void Write(
        GraphNodeRecord node,
        ISerializable settings
    ) {
        switch (settings)
        {
            case DialogueNode v: m_codec.Write(node, v); break;
            case StageNode v: m_codec.Write(node, v); break;
            case AudioNode v: m_codec.Write(node, v); break;
            case PerformanceNode v: m_codec.Write(node, v); break;
            case ChoiceNode v: m_codec.Write(node, v); break;
            case AssignmentNode v: m_codec.Write(node, v); break;
            case UnlockNode v: m_codec.Write(node, v); break;
            case CallNode v: m_codec.Write(node, v); break;
            default: throw new InvalidOperationException("Unsupported native seed node.");
        }
    }

    internal void Link(
        string from,
        string to,
        string port = "next"
    ) => m_draft.graph.document.AddEdge(new(new(Guid.NewGuid().ToString("N")), new(new(from), new(port)), new(new(to), new("in"))));

    internal void Say(
        string who,
        string text,
        string emotion = "neutral"
    ) {
        string id = m_draft.chapterId + "-" + (++m_line).ToString("D3");
        if (who.Length > 0 && !m_cg) Actor(who, emotion);
        Add(id, StoryNodeIds.dialogue, new DialogueNode { textId = id, text = text, characterId = who.Length == 0 ? "" : "nz.character." + who,
            charactersPerSecond = 32, punctuationPause = .12 });
        if (who.Length > 0) m_voiceRows.Add($"{m_draft.chapterId}\t{id}\t{who}\t{text}");
    }

    internal void Lines(string text)
    {
        foreach (string raw in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string line = raw.Trim(); int separator = line.IndexOf('|');
            if (separator < 0) throw new InvalidDataException("Seed dialogue must declare a native character identity.");
            string[] who = line[..separator].Split('/');
            Say(who[0] == "n" ? "" : who[0], line[(separator + 1)..], who.Length > 1 ? who[1] : "neutral");
        }
    }

    internal void Stage(string id)
    {
        m_cg = id is "RainLamp" or "ResonantDawn" or "CityLights" or "MorningReunion";
        Add("stage-" + m_nodes.Count, StoryNodeIds.stage, new StageNode { operation = StageOperation.SetStage, resourceId = "nz.stage." + id });
        foreach (string actor in new[] { "lin", "xu", "shen" })
            Add("hide-" + actor + "-" + m_nodes.Count, StoryNodeIds.stage, new StageNode { operation = StageOperation.HideCharacter, target = actor });
        Add("clear-particles-" + m_nodes.Count, StoryNodeIds.stage, new StageNode { operation = StageOperation.RemoveParticles, target = "atmosphere" });
        if (!m_cg)
            Add("particles-" + m_nodes.Count, StoryNodeIds.stage, new StageNode { operation = StageOperation.StartParticles,
                target = "atmosphere", resourceId = "nz.particle." + (id is "StreetRain" or "MetroPlatform" or "LampAlley" ? "Rain" : id == "ZeroCore" ? "NightFragments" : "LampSparks"), seed = 19 });
        if (id == "LampWorkshop" || m_cg)
            Add("quiet-" + m_nodes.Count, StoryNodeIds.audio, new AudioNode { operation = AudioOperation.Stop, slot = "ambience" });
        else
            Add("ambience-" + m_nodes.Count, StoryNodeIds.audio, new AudioNode { operation = AudioOperation.Play, slot = "ambience",
                cueId = "nz.ambience." + (id == "MetroPlatform" || id == "ZeroCore" ? "MetroHum" : id is "ClockTower" or "RooftopDawn" or "StreetDawn" ? "RooftopWind" : "Rain") });
    }

    internal void Actor(
        string id,
        string emotion = "neutral",
        string pose = "idle"
    ) => Add("actor-" + m_nodes.Count, StoryNodeIds.stage, new StageNode { operation = StageOperation.ShowCharacter, resourceId = "nz.character." + id,
        target = id, pose = pose, expression = emotion, transform = new() { x = id == "lin" ? 160 : id == "xu" ? 720 : 1260, y = 80 } });

    internal void Music(string id) => Add("music-" + m_nodes.Count, StoryNodeIds.audio, new AudioNode { operation = AudioOperation.Play, cueId = "nz.music." + id, slot = "bgm" });
    internal void Unlock(string id) => Add("unlock-" + m_nodes.Count, StoryNodeIds.unlock, new UnlockNode { entryId = id });
    internal void Clip(string id)
    {
        Add("clip-" + m_nodes.Count, StoryNodeIds.performance, new PerformanceNode { operation = PerformanceOperation.Play,
            clipId = "nz.clip." + id, instanceId = "sequence", bindings = [new() { slot = "camera", target = "camera" }] });
        string? sound = id switch { "LampReveal" => "LampIgnite", "PlatformClash" => "SpellSlash", "TowerBreak" => "GlassCrack",
            "FinalDefense" => "Shield", "FinalCounter" => "Impact", "SealCollapse" => "SealBreak", "DawnReveal" => "DawnChime", _ => null };
        if (sound is not null) Add("sfx-" + m_nodes.Count, StoryNodeIds.audio, new AudioNode { operation = AudioOperation.Play, slot = "sfx", cueId = "nz.sfx." + sound });
    }
    internal void Assign(
        string variable,
        NovelValue value
    ) => Add("set-" + m_nodes.Count, StoryNodeIds.assign, new AssignmentNode { variableId = variable, value = new() { literal = value } });
    internal void Trust() => Add("trust-" + m_nodes.Count, StoryNodeIds.assign, new AssignmentNode { variableId = "trust", value = new()
        { operation = ExpressionOperation.Add, operands = [new() { operation = ExpressionOperation.Variable, variableId = "trust" }, new() { literal = NovelValue.Integer(1) }] } });

    internal void Choice(
        string id,
        string prompt,
        string first,
        string second,
        bool needsTrust = false
    ) {
        Add(id, StoryNodeIds.choice, new ChoiceNode { prompt = prompt, options = [new() { id = "a", text = first,
            enabled = needsTrust ? new() { operation = ExpressionOperation.GreaterOrEqual, operands = [new() { operation = ExpressionOperation.Variable, variableId = "trust" }, new() { literal = NovelValue.Integer(1) }] } : new() { literal = NovelValue.Boolean(true) },
            disabledReason = needsTrust ? "尚未建立共同承担术式的共鸣。" : "" }, new() { id = "b", text = second }] });
        m_tail = null;
    }

    internal void Branch(
        string choice,
        string option,
        Action content
    ) {
        m_tail = null; int index = m_nodes.Count; content(); Link(choice, m_nodes[index], option);
    }

    internal void Merge(
        string first,
        string second
    ) {
        m_tail = null;
        string id = "merge-" + m_nodes.Count;
        Add(id, StoryNodeIds.stage, new StageNode { operation = StageOperation.SetCamera });
        Link(first, id); Link(second, id);
    }

    internal void End(string id)
    {
        Add(id, StoryNodeIds.end, null); m_tail = null;
    }

    internal StoryAsset Finish(string? next = null)
    {
        if (next is not null) Add("next-chapter", StoryNodeIds.call, new CallNode { chapterId = next, jump = true });
        else Add("end", StoryNodeIds.end, null);
        m_draft.fragments = [new() { id = "chapter", name = m_draft.title, nodes = m_nodes.ToArray() }];
        return m_draft.ToAsset();
    }
}
