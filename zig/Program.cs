// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Win32.CodeGen
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Metadata;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using System.Threading;
    using Microsoft.CodeAnalysis.CSharp;

    internal class Program
    {
        private static void Main()
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Canceling...");
                cts.Cancel();
                e.Cancel = true;
            };
            try
            {
                string output_dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "output");
                CleanDir(output_dir);
                var generate_stopwatch = Stopwatch.StartNew();
                using var metadata_stream = File.OpenRead(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location!)!, "Windows.Win32.winmd"));
                using PEReader pe_reader = new PEReader(metadata_stream);
                Console.WriteLine("output file: {0}", output_dir);
                ZigWin32.ZigGenerator.Generate(pe_reader.GetMetadataReader(), output_dir, cts.Token);
                Console.WriteLine("Generation time: {0}", generate_stopwatch.Elapsed);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token)
            {
                Console.Error.WriteLine("Canceled.");
            }
        }

        private static void CleanDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
