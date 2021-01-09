using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace QstSelect
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            while (!CheckConfig())
            {
                Console.Clear();
                ReportConfig();
                Console.WriteLine(@"
The components listed above are missing.
You need to install and configure them.
What do you want to do?

[B]rowse for missing files (recommended)
[E]dit configuration in your text editor (for experts)
[Q]uit
[B/E/Q]");
                switch (WaitForKey(ConsoleKey.B, ConsoleKey.E, ConsoleKey.Q))
                {
                    case ConsoleKey.B:
                        FixConfig();
                        break;
                    case ConsoleKey.E:
                        Process.Start(Config.Filename);
                        return;
                    default:
                        return;
                }
            }

            if (args.Length == 0 || args.Contains("/?"))
            {
                Console.WriteLine(@"QstSelect.exe <quest_file>
Runs the given game or savegame");
                DebugPause();
                return;
            }

            var UserFile = Path.GetFullPath(args[0]);
            if (!File.Exists(UserFile))
            {
                Console.Error.WriteLine("File not found: {0}", UserFile);
                DebugPause();
                return;
            }

            var Props = VersionChecker.GetGameProperties(UserFile);
            if (!Props.Valid)
            {
                Console.Error.WriteLine("This file could neither be identified as a game, nor as a save");
                DebugPause();
                return;
            }

            //Game is only a save, check if the real game still exists and offer fixes if it doesn't.
            if (Props.IsSavegame)
            {
                if (!File.Exists(Props.MainGame))
                {
                    //Check if the file exists in the same location as the save game.
                    var NewFile = Path.Combine(Path.GetDirectoryName(UserFile), Path.GetFileName(Props.MainGame));

                    if (File.Exists(NewFile))
                    {
                        Console.Error.WriteLine(@"The game this save is for no longer exists.
This can happen when you delete, rename or move the main game file.
The last known location was:
{0}

A game with the same name was found in the folder of this save file.
Fix the savegame to use the new file?
[Y]es   : Save the newly detected location and run the game.
[N]o    : Do not modify the save file and abort.
[B]rowse: Search for a matching game manually
[Y/N/B]", Props.MainGame);


                        switch (WaitForKey(ConsoleKey.Y, ConsoleKey.N, ConsoleKey.B))
                        {
                            case ConsoleKey.Y:
                                VersionChecker.SetGameRoot(Props.FileName, NewFile);
                                break;
                            case ConsoleKey.N:
                                return;
                            case ConsoleKey.B:
                                var Game = BrowseFile(
                                    "Select quest game file",
                                    Path.GetDirectoryName(UserFile),
                                    "Quest game files|*.quest");
                                if (string.IsNullOrEmpty(Game))
                                {
                                    return;
                                }
                                VersionChecker.SetGameRoot(Props.FileName, Game);
                                break;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(@"The game this save is for no longer exists.
This can happen when you delete, rename or move the main game file.
The last known location was:
{0}

Do you want to specify a new file?
[Y]es   : Show a dialog to browse to the appropriate game file
[N]o    : Do not modify the save file and abort.
[Y/N]", Props.MainGame);
                        switch (WaitForKey(ConsoleKey.Y, ConsoleKey.N))
                        {
                            case ConsoleKey.Y:
                                var Game = BrowseFile(
                                    "Select quest game file",
                                    Path.GetDirectoryName(UserFile),
                                    Path.GetFileName(Props.FileName) + "|" +
                                    Path.GetFileName(Props.FileName) + "|" +
                                    "Quest game files|*.quest");
                                if (string.IsNullOrEmpty(Game))
                                {
                                    return;
                                }
                                VersionChecker.SetGameRoot(Props.FileName, Game);
                                break;
                            case ConsoleKey.N:
                                return;
                        }
                    }
                }
            }

            switch (Props.Version)
            {
                case VersionChecker.VersionType.New:
                    Console.WriteLine("Launching game in new quest version");
                    Launch(Config.Get("Quest.New"), UserFile);
                    break;
                case VersionChecker.VersionType.Old:
                    Console.WriteLine("Launching game in old quest version");
                    Launch(Config.Get("Quest.Old"), UserFile);
                    break;
                default:
                    Console.WriteLine("This is neither a quest game file nor a save file");
                    break;
            }
            DebugPause();
        }

        private static void ReportConfig()
        {
            if (!File.Exists(Config.Get("7z")))
            {
                Console.WriteLine("7-zip executable not found");
            }
            if (!File.Exists(Config.Get("Quest.Old")))
            {
                Console.WriteLine("Quest 5.7 or older not found");
            }
            if (!File.Exists(Config.Get("Quest.New")))
            {
                Console.WriteLine("Quest 5.8 or later not found");
            }
        }

        /// <summary>
        /// Shows a standard file browse dialog
        /// </summary>
        /// <param name="Title">Dialog title</param>
        /// <param name="InitialDirectory">Initial dialog directory</param>
        /// <param name="Filter">File selection filter</param>
        /// <returns></returns>
        private static string BrowseFile(string Title = null, string InitialDirectory = null, string Filter = null)
        {
            using (var OFD = new OpenFileDialog())
            {
                OFD.Title = string.IsNullOrEmpty(Title) ? Console.Title : Title;
                if (!string.IsNullOrEmpty(InitialDirectory) && Directory.Exists(InitialDirectory))
                {
                    OFD.InitialDirectory = InitialDirectory;
                }
                if (string.IsNullOrEmpty(Filter))
                {
                    Filter = "All files|*.*";
                }
                OFD.Filter = Filter;
                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    return OFD.FileName;
                }
                return null;
            }
        }

        private static ConsoleKey WaitForKey(params ConsoleKey[] Keys)
        {
            if (Keys == null || Keys.Length == 0)
            {
                throw new ArgumentException("Expecting at least one key");
            }
            while (true)
            {
                var K = Console.ReadKey(true).Key;
                if (Keys.Contains(K))
                {
                    return K;
                }
                Console.Beep();
            }
        }

        private static void Launch(string exe, string game)
        {
#if DEBUG
            Console.WriteLine("Start: {0} \"{1}\"", exe, game);
#endif
            Process.Start(new ProcessStartInfo()
            {
                FileName = exe,
                Arguments = $"\"{game}\"",
                WorkingDirectory = Path.GetDirectoryName(exe)
            }).Dispose();
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

            if (!File.Exists(Config.Get("7z")))
            {
                //Try to automatically detect 7-zip locations first (in descending order of preference)
                var paths = new string[]
                {
                    //64 bit 7-zip
                    GetRegString(@"HKEY_LOCAL_MACHINE\SOFTWARE\7-Zip","Path64","")+@"\7z.exe",
                    GetRegString(@"HKEY_LOCAL_MACHINE\SOFTWARE\7-Zip","Path","")+@"\7z.exe",
                    //32 bit 7-zip
                    GetRegString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\7-Zip","Path64","")+@"\7z.exe",
                    GetRegString(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\7-Zip","Path","")+@"\7z.exe",
                    //Default program locations
                    Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\7-zip\7z.exe"),
                    Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\7-zip\7z.exe"),
                    //Current directory (this would support portable setups)
                    Path.Combine(Environment.CurrentDirectory, "7z.exe")
                };
                var p = paths.FirstOrDefault(File.Exists);

                if (p != null)
                {
                    //New 7z found
                    Config.Set("7z", p);
                }
                else if (Config.Get("7z") == null)
                {
                    //7z not found and not yet configured
                    Config.Set("7z", "7z.exe");
                    valid = false;
                }
                else
                {
                    //7z not found but configured
                    valid = false;
                }
            }
            if (!(valid &= File.Exists(Config.Get("Quest.Old"))))
            {
                if (Config.Get("Quest.Old") == null)
                {
                    Config.Set("Quest.Old", @"C:\Path\To\Old\Quest.exe");
                }
            }
            if (!(valid &= File.Exists(Config.Get("Quest.New"))))
            {
                if (Config.Get("Quest.New") == null)
                {
                    Config.Set("Quest.New", @"C:\Path\To\Old\Quest.exe");
                }
            }
            return valid;
        }

        private static void FixConfig()
        {
            if (CheckConfig())
            {
                return;
            }
            while (!File.Exists(Config.Get("7z")))
            {
                Console.Clear();
                Console.WriteLine(@"7-zip could not be found
Please go to https://7-zip.org and download and install the latest version.

[O]pen URL
[T]ry again
[B]rowse manually
[A]bort
[T/B/A]");
                switch (WaitForKey(ConsoleKey.O, ConsoleKey.T, ConsoleKey.B, ConsoleKey.A))
                {
                    case ConsoleKey.O:
                        Process.Start("https://7-zip.org");
                        break;
                    case ConsoleKey.T:
                        //Setting this to null will force auto detection
                        Config.Set("7z", null);
                        CheckConfig();
                        break;
                    case ConsoleKey.B:
                        var zip = BrowseFile(
                            "7-Zip executable",
                            Environment.ExpandEnvironmentVariables("%ProgramFiles%"),
                            "7-zip executable|7z.exe");
                        if (!string.IsNullOrEmpty(zip))
                        {
                            Config.Set("7z", zip);
                        }
                        break;
                    default:
                        return;
                }
            }

            while (!File.Exists(Config.Get("Quest.Old")))
            {
                Console.Clear();
                Console.WriteLine(@"Old Quest version not found.
If you did not install it yet, please do so.
The Quest executable can't be auto detected
because new and old version share the same name.
You have to browse for it manually.

[D]ownload installer
[B]rowse for quest
[A]bort
[T/B/A]");
                switch (WaitForKey(ConsoleKey.D, ConsoleKey.B, ConsoleKey.A))
                {
                    case ConsoleKey.D:
                        Process.Start("https://github.com/textadventures/quest/releases/download/5.7.2/quest572.exe");
                        break;
                    case ConsoleKey.B:
                        var q = BrowseFile(
                            "Old Quest executable",
                            Get32BitProgDir(),
                            "Old Quest executable|quest.exe");
                        if (!string.IsNullOrEmpty(q))
                        {
                            Config.Set("Quest.Old", q);
                        }
                        break;
                    default:
                        return;
                }
            }

            while (!File.Exists(Config.Get("Quest.New")))
            {
                Console.Clear();
                Console.WriteLine(@"New Quest version not found.
If you did not install it yet, please do so.
The Quest executable can't be auto detected
because new and old version share the same name.
You have to browse for it manually.

CAUTION!
The new installer uses the same default path as the old installer.
It is important that you change the installation path when offered,
otherwise you will overwrite the old installation.

[D]ownload installer
[B]rowse for quest
[A]bort
[T/B/A]");
                switch (WaitForKey(ConsoleKey.D, ConsoleKey.B, ConsoleKey.A))
                {
                    case ConsoleKey.D:
                        Process.Start("https://github.com/textadventures/quest/releases/download/5.8.0/quest580.exe");
                        break;
                    case ConsoleKey.B:
                        var q = BrowseFile(
                            "New Quest executable",
                            Get32BitProgDir(),
                            "New Quest executable|quest.exe");
                        if (!string.IsNullOrEmpty(q))
                        {
                            Config.Set("Quest.New", q);
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        private static string Get32BitProgDir()
        {
            var p = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (string.IsNullOrEmpty(p))
            {
                p = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
            return p;
        }

        private static string GetRegString(string KeyName, string ValueName = null, string Default = null)
        {
            return (string)Microsoft.Win32.Registry.GetValue(KeyName, ValueName, Default);
        }
    }
}
