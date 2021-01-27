using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;

internal class Program
{
    private static int Main()
    {
        string repo_dir = JsonWin32Common.FindWin32JsonRepo();
        string api_dir = JsonWin32Common.GetAndVerifyWin32JsonApiDir(repo_dir);
        Console.WriteLine("TODO: implement the validator");
        return 1;
    }
}
