using System;
using System.Threading;
using System.IO;
using Inno.Editor.WindowsX64;

namespace Story.Tools.EditorHost;

internal static class Program
{
    private static int Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, request) => { request.Cancel = true; cancellation.Cancel(); };
        if (args.Length > 0 && Environment.GetEnvironmentVariable("STORY_DIAGNOSTICS") == "1")
        {
            string directory = Path.Combine(Path.GetFullPath(args[0]), "artifacts"); Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "EditorExceptions.txt");
            object gate = new();
            AppDomain.CurrentDomain.FirstChanceException += (_, failure) =>
            {
                string stack = failure.Exception.StackTrace ?? "";
                if (!stack.Contains("Inno.VisualNovel", StringComparison.Ordinal) && !stack.Contains("Inno.Rendering", StringComparison.Ordinal)
                    && !stack.Contains("Inno.Text", StringComparison.Ordinal)) return;
                lock (gate) File.AppendAllText(path, DateTimeOffset.Now.ToString("O") + " " + failure.Exception + Environment.NewLine);
            };
        }
        return WindowsX64EditorComposition.Run(args, cancellation.Token);
    }
}
