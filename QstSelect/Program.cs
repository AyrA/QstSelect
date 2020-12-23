using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace QstSelect
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!CheckConfig())
            {
                DebugPause();
                return;
            }

            if (args.Length == 0 || args.Contains("/?"))
            {
                Console.WriteLine("QstSelect <quest_file>");
                DebugPause();
                return;
            }
            switch (VersionChecker.GetVersion(args[0]))
            {
                case VersionChecker.VersionType.NotFound:
                    Console.Error.WriteLine("The specified file could not be found");
                    break;
                case VersionChecker.VersionType.Invalid:
                    Console.Error.WriteLine("This is not a valid quest game file");
                    break;
                case VersionChecker.VersionType.New:
                    Launch(Config.Get("Quest.New"), args[0]);
                    break;
                case VersionChecker.VersionType.Old:
                    Launch(Config.Get("Quest.Old"), args[0]);
                    break;
                default:
                    break;
            }
            DebugPause();
        }

        private static void Launch(string exe, string game)
        {
            Process.Start(exe, $"\"{game}\"").Dispose();
        }

        private static void DebugPause()
        {
#if DEBUG
            Console.Error.WriteLine("#END");
            Console.ReadKey(true);
#endif      
        }

        private static bool CheckConfig()
        {
            var valid = true;

            if (!(valid &= File.Exists(Config.Get("7z"))))
            {
                if (Config.Get("7z") == null)
                {
                    Config.Set("7z", "7z.exe");
                }
                Console.WriteLine("7-zip not found. Please configure");
            }
            if (!(valid &= File.Exists(Config.Get("Quest.Old"))))
            {
                if (Config.Get("Quest.Old") == null)
                {
                    Config.Set("Quest.Old", "quest.exe");
                }
                Console.WriteLine("Quest 5.8 or later not found. Please configure in Quest.Old");
            }
            if (!(valid &= File.Exists(Config.Get("Quest.New"))))
            {
                if (Config.Get("Quest.New") == null)
                {
                    Config.Set("Quest.New", "quest.exe");
                }
                Console.WriteLine("Quest 5.8 or later not found. Please configure in Quest.New");
            }
            return valid;
        }
    }
}
