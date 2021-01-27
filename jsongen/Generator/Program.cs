using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using JsonWin32Generator;

internal class Program
{
    private static int Main()
    {
        string repoDir = JsonWin32Common.FindWin32JsonRepo();
        string apiDir = JsonWin32Common.GetAndVerifyWin32JsonApiDir(repoDir);
        CleanDir(apiDir);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Canceling...");
            cts.Cancel();
            e.Cancel = true;
        };
        try
        {
            var generateTimer = Stopwatch.StartNew();
            using var metadataFileStream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, "Windows.Win32.winmd"));
            using PEReader peReader = new PEReader(metadataFileStream);
            Console.WriteLine("OutputDirectory: {0}", apiDir);
            JsonGenerator.Generate(peReader.GetMetadataReader(), apiDir, cts.Token);
            Console.WriteLine("Generation time: {0}", generateTimer.Elapsed);
            return 0;
        }
        catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token)
        {
            Console.Error.WriteLine("Canceled.");
            return -1;
        }
    }

    private static void CleanDir(string dir)
    {
        if (Directory.Exists(dir))
        {
            foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                // hack to allow me to publish to a git repo
                if (file.Contains("\\.git\\"))
                {
                    continue;
                }

                File.Delete(file);
            }
        }
        else
        {
            Directory.CreateDirectory(dir);
        }
    }
}
