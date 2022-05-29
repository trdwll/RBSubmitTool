using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using CommandLine;

namespace RBSubmitTool
{
    internal class Program
    {
        static void StartProcess(string command, string args, string workingdir)
        {
            string path = FindInPath(command);
            if (path == null)
            {
                Console.WriteLine($"[!] Unable to find the {command}. Are you sure the program is in the PATH?");
                return;
            }

            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(path)
            {
               // WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingdir,
                Arguments = $@"{args}",
                UseShellExecute = true, // if true we don't get back our error
                //RedirectStandardError = true
            };
            p.Start();
            p.WaitForExit();
            Console.WriteLine("[!] Finished");

            //int exitCode = p.ExitCode;

            //if (exitCode != 0)
            //{
            //    Environment.Exit(-1);
            //}

            // TODO: If error then exit and print error
        }

        // https://stackoverflow.com/questions/22210758/equivalent-of-where-command-prompt-command-in-c-sharp
        static string FindInPath(string filename)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var directories = path.Split(';');

            foreach (var dir in directories)
            {
                var fullpath = Path.Combine(dir, filename);
                if (File.Exists(fullpath)) return fullpath;
            }

            // filename does not exist in path
            return null;
        }

        internal class Options
        {
            [Option("cl", Required = true, HelpText = "The changelist number that is going to be submitted.")]
            public int Changelist { get; set; }

            [Option("client", Required = true, HelpText = "The client that will be submitting this review. (the perforce client)")]
            public string P4Client { get; set; }

            [Option("root", Required = true, HelpText = "The root of the workspace.")]
            public string P4Root { get; set; }

            [Option("target-group", Required = true, HelpText = "What group(s) will be reviewing this?")]
            public string TargetGroup { get; set; }
        }

        static async Task Main(string[] args)
        {
            // verify the rbt, diff, and perforce are installed
            if (FindInPath("rbt") == null)
            {
                Console.WriteLine("[!] RBTools is not installed or found.");
                return;
            }

            if (FindInPath("diff.exe") == null)
            {
                Console.WriteLine("[!] GNUDiff is not installed or found.");
                return;
            }

            if (FindInPath("p4.exe") == null)
            {
                Console.WriteLine("[!] Perforce is not installed or found.");
                return;
            }

            // Parse command args
            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync<Options>(async o =>
            {
                Console.WriteLine($"[+] Posting a review for {o.Changelist}");

                // TODO: Support -X/--exclude when they actually add support for rbt post
                // a hacky workaround would be rbt diff then rbt --diff-filename file.diff
                string Command = $"post -o --target-groups {o.TargetGroup} --p4-client {o.P4Client} {o.Changelist}";

                Console.WriteLine($"[+] Issuing command \"rbt {Command}\".");

                StartProcess("rbt", Command, o.P4Root);
            });
        }
    }
}
