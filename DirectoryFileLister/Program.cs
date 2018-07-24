using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;

namespace ProjectReferenceChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser
            {
                ShowUsageOnEmptyCommandline = false,
                AcceptSlash = true
            };

            //Optional
            var wait = new SwitchArgument(
                    'w', "wait",
                    "Wait for console input after output is completed", false)
            { Optional = true };
            parser.Arguments.Add(wait);

            var searchPath = new ValueArgument<string>(
                    'p', "searchPath",
                    "Directory path to search for and list files")
            { Optional = true };
            parser.Arguments.Add(searchPath);

            try
            {
                parser.ParseCommandLine(args);

                var dirSearchPath = string.IsNullOrEmpty(searchPath.Value) ? Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) : searchPath.Value;

                Console.Write("Scanning...");
                var results = new ConcurrentBag<string>();
                if (dirSearchPath != null)
                {
                    var d = new DirectoryInfo(dirSearchPath);
                    var files = d.GetFiles("*.*", SearchOption.AllDirectories);
                    Parallel.ForEach(files, file =>
                    {
                        var relativePath = file.FullName.Replace(dirSearchPath, "");

                        var assemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(file.FullName);
                        var assemblyFileInfo = new FileInfo(file.FullName);
                        var version = string.IsNullOrEmpty(assemblyFileVersionInfo.FileVersion)
                            ? "0.0.0.0"
                            : assemblyFileVersionInfo.FileVersion;
                        var lastModified = string.Format($"{assemblyFileInfo.LastWriteTime.ToShortDateString()} {assemblyFileInfo.LastWriteTime.ToShortTimeString()}");

                        results.Add($"{lastModified.PadRight(20)} {version.PadRight(20)} {relativePath} ");
                    });
                }

                Console.WriteLine("Done.");
                var counter = 0;
                foreach (var result in results)
                {
                    counter++;
                    Console.WriteLine($"{counter.ToString().PadRight(8)} {result}");
                }

                if (wait.Value)
                {
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }

                Environment.ExitCode = 0; //Success

            }
            catch (CommandLineException e)
            {
                Console.WriteLine("Unknown CommandLineException error: " + e.Message);
            }
        }
    }
}
