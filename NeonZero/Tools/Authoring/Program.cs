using System;
using System.IO;
using System.Linq;
using Inno.Adapter.Modules.DotNet;
using Inno.Adapter.Serialization.DotNet;
using Inno.Assets;
using Inno.Assets.Pipeline;
using Inno.Audio.Assets;
using Inno.Core.Diagnostics;
using Inno.Core.Identity;
using Inno.Core.IO;
using Inno.Core.Logging;
using Inno.Core.Serialization;
using Inno.Core.Settings;
using Inno.Extensibility.Modules;
using Inno.Extensibility.Types;
using Inno.Plugins.Authoring;
using Inno.VisualNovel.Authoring.AssetImporting;
using Inno.VisualNovel.Compiler;
using Inno.VisualNovel.Model.Story;

namespace Story.Tools.Authoring;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2) throw new ArgumentException("Supply the Story root and seed, voices, refine, polish, settings, manuscript or inspect.");
            string root = Path.GetFullPath(args[0]);
            if (!File.Exists(Path.Combine(root, "Documentation", "ImplementationPlan.md")))
                throw new InvalidOperationException("The explicit target must be the Story project.");
            var identities = new IdentityAllocator();
            using var identityScope = identities.EnterScope();
            var sceneImporter = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, "Inno.Scene.Assets.dll"));
            using var modules = new ModuleHost(new ModuleHostOptions { catalogSource = new DotNetAssemblyCatalogSource(typeof(Program).Assembly,
                typeof(AudioClipImporter).Assembly, typeof(Inno.Text.Assets.FontImporter).Assembly,
                typeof(Inno.Rendering.Assets.Authoring.ShaderGraphSourceStore).Assembly, sceneImporter) });
            _ = modules.Register("story.native-runtime", [typeof(StoryAsset).Assembly, typeof(Inno.VisualNovel.Narrative.Execution.StoryMachine).Assembly,
                typeof(Inno.VisualNovel.Stage.Timeline.PerformancePlayer).Assembly, typeof(Inno.VisualNovel.UI.Documents.UiDocumentInstance).Assembly,
                typeof(Inno.VisualNovel.Player.Sessions.NovelSession).Assembly, typeof(Inno.VisualNovel.Integration.Hosting.NovelPlayerComponent).Assembly,
                typeof(Inno.VisualNovel.Rendering.Frames.NovelFrameComposer).Assembly]);
            _ = modules.Register("story.native-authoring", [typeof(StoryImporter).Assembly, typeof(StoryCompiler).Assembly]);
            using var types = new TypeCatalog(modules, new ReflectionTypeCatalogSource());
            using var serialization = new SerializationRegistry(types, new ReflectionSerializationMetadataSource());
            if (args[1] == "settings")
            {
                using var settings = new ProjectSettingsStore(new FileByteDocumentStore(Path.Combine(root, "Settings.Project.inno")), types, serialization,
                    new ProjectId("inno.project"), SerializationContext.empty);
                settings.SetProjectOverride(ProjectIdentitySettings.settingId,
                    new ProjectIdentitySettings { projectId = "inno.samples.neonzero" }, []);
                Console.WriteLine("Native project namespace: inno.samples.neonzero");
                return 0;
            }
            using var logs = new LogRouter();
            var diagnostics = new DiagnosticHub();
            diagnostics.RegisterSink(new ConsoleDiagnostics());
            var pluginSources = new PluginSourceService(serialization, Path.Combine(root, "Plugins"), Path.Combine(root, "Library"));
            var scan = pluginSources.Scan();
            var options = AssetPipelineOptions.Create(Path.Combine(root, "Assets"), Path.Combine(root, "Library", "Authoring"));
            using var assets = new AssetPipeline(modules, types, serialization, identities, diagnostics, logs,
                options with { enableFileSystemWatcher = false, deferUnavailableExtensions = true,
                    sourceMounts = [.. options.sourceMounts!, .. PluginSourceService.GetActivatableMounts(scan)] });
            using var scope = AssetExecutionContext.EnterScope(assets);
            if (args[1] == "audit")
            {
                var raw = EditorAssets.DecodeNative<Inno.VisualNovel.Model.Project.NovelProjectAsset>(
                    File.ReadAllBytes(Path.Combine(root, "Assets", "Game", "NeonZero.ivnproject")));
                foreach (var dependency in assets.CaptureProperties(raw).dependencies)
                {
                    string path = assets.TryGetInfo(dependency.persistentId, out var info) ? info!.assetPath.ToString() : "<uncataloged>";
                    Console.WriteLine(dependency.persistentId + " | " + dependency.lastKnownPath + " | " + path);
                }
                return 0;
            }
            foreach (var entry in assets.GetFileSystemEntries(includeDirectories: false).Where(entry => args[1] != "manuscript" &&
                Path.GetExtension(entry.assetPath.localPath).ToLowerInvariant() is ".png" or ".wav" or ".mp3" or ".otf"))
                Prepare(entry.assetPath);
            if (args[1] != "seed" && args[1] != "manuscript")
                foreach (var entry in assets.GetFileSystemEntries(includeDirectories: false)
                    .Where(entry => entry.source == AssetSourceId.project && entry.assetPath.localPath.StartsWith("Game/", StringComparison.Ordinal)
                        && !entry.assetPath.localPath.StartsWith("Game/Scenes/", StringComparison.Ordinal)
                        && entry.assetPath.localPath != "Game/NeonZero.ivnproject")
                    .OrderBy(entry => entry.assetPath.localPath.Contains("/Story/Chapters/", StringComparison.Ordinal) ? 1 : 0))
                    Prepare(entry.assetPath);
            var native = new EditorStorySerialization(serialization);
            var codec = new StoryNodeDataCodec(native);
            var authoring = new NativeProject(root, assets, identities, types, serialization, codec);
            switch (args[1])
            {
                case "seed": authoring.Create(); break;
                case "voices": authoring.BindVoices(); break;
                case "inspect": authoring.Inspect(); break;
                case "refine": authoring.Refine(); break;
                case "polish": authoring.Polish(); break;
                case "manuscript":
                    var rows = new System.Collections.Generic.List<string> { "chapter\ttextId\tcharacter\ttext" };
                    _ = StorySeed.Create(codec, rows);
                    string directory = Path.Combine(root, "artifacts", "Exports", "Voice"); Directory.CreateDirectory(directory);
                    File.WriteAllLines(Path.Combine(directory, "VoiceScript.tsv"), rows, new System.Text.UTF8Encoding(true));
                    Console.WriteLine("Exported native seed voice rows: " + (rows.Count - 1));
                    break;
                default: throw new ArgumentException("Unknown operation.");
            }
            return 0;

            void Prepare(AssetPath path)
            {
                var value = Inno.Assets.Assets.Load<AssetObject>(path);
                if (!value.isMissing && value.assetPath.isValid) return;
                if (!assets.Import(path)) throw new InvalidOperationException("Native dependency preparation failed: " + path);
                value = Inno.Assets.Assets.Load<AssetObject>(path);
                if (value.isMissing) throw new InvalidOperationException("Native dependency remains Missing: " + path);
            }
        }
        catch (Exception failure) { Console.Error.WriteLine(failure); return 1; }
    }

    private sealed class ConsoleDiagnostics : IDiagnosticSink
    {
        /// <inheritdoc />
        public void Replace(DiagnosticReport report)
        {
            foreach (var item in report.diagnostics) Console.WriteLine(item.message);
        }
        /// <inheritdoc />
        public void Clear(DiagnosticSource source) { }
    }
}
