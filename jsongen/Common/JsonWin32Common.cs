using System;
using System.IO;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public static class JsonWin32Common
{
    public static string FindWin32JsonRepo()
    {
        string currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine("DEBUG: cwd is '{0}'", currentDir);
        while (true)
        {
            string repoDir = Path.Combine(currentDir, "win32json");
            Console.WriteLine("DEBUG: looking for win32json at '{0}'", repoDir);
            if (Directory.Exists(repoDir))
            {
                return repoDir;
            }

            string? nextDir = Path.GetDirectoryName(currentDir);
            if (nextDir == null || nextDir == currentDir)
            {
                Console.WriteLine("Error: failed to find the 'win32json' repository in any of the parent directories");
                System.Environment.Exit(1);
            }

            currentDir = nextDir;
        }
    }

    public static string GetAndVerifyWin32JsonApiDir(string repoDir)
    {
        string apiDir = Path.Combine(repoDir, "api");
        if (!Directory.Exists(apiDir))
        {
            Console.WriteLine("Error: the win32json repository '{0}' does not have an 'api' subdirectory", repoDir);
            System.Environment.Exit(1);
        }

        return apiDir;
    }
}
