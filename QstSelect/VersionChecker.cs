using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace QstSelect
{
    public static class VersionChecker
    {
        /// <summary>
        /// Game properties
        /// </summary>
        public struct GameProperties
        {
            /// <summary>
            /// true, if this file is likely a valid savegame or main game file
            /// </summary>
            /// <remarks>If this is false, the other properties are meaningless</remarks>
            public bool Valid;
            /// <summary>
            /// true, if the supplied file is a savegame
            /// </summary>
            public bool IsSavegame;
            /// <summary>
            /// Detected quest version
            /// </summary>
            public VersionType Version;
            /// <summary>
            /// The main game file
            /// </summary>
            /// <remarks>
            /// If <see cref="IsSavegame"/> is false,
            /// this will be identical to <see cref="FileName"/>
            /// </remarks>
            public string MainGame;
            /// <summary>
            /// The supplied game/savegame file
            /// </summary>
            public string FileName;
        }

        public enum VersionType : int
        {
            /// <summary>
            /// Old quest version
            /// </summary>
            Old = 1,
            /// <summary>
            /// New quest version
            /// </summary>
            New = Old + 1,
            /// <summary>
            /// Not a quest game or savegame
            /// </summary>
            Invalid = int.MinValue,
            /// <summary>
            /// General error when determining quest type
            /// </summary>
            Error = Invalid + 1
        }

        public enum FileType : int
        {
            /// <summary>
            /// Unknown/Invalid file type
            /// </summary>
            Unknown = 0,
            /// <summary>
            /// Main quest game
            /// </summary>
            Game = 1,
            /// <summary>
            /// Savegame only
            /// </summary>
            Save = 2
        }

        /// <summary>
        /// Command line to invoke 7-zip
        /// </summary>
        private const string CMDLINE = "e \"{0}\" game.aslx -so";

        public static GameProperties GetGameProperties(string Filename)
        {
            var FT = GetFileType(Filename);
            var ret = new GameProperties()
            {
                Valid = FT != FileType.Unknown,
                IsSavegame = FT == FileType.Save,
                Version = FT != FileType.Unknown ? GetVersion(Filename) : VersionType.Invalid,
                FileName = Filename
            };
            if (ret.Valid)
            {
                ret.MainGame = ret.IsSavegame ? GetSaveRoot(Filename) : Filename;
            }

            return ret;
        }

        private static FileType GetFileType(string Filename)
        {
            //These are all known sequences for zip compressed files
            var ZipSequences = new byte[][]
            {
                new byte[]{0x50, 0x4B, 0x03, 0x04},
                new byte[]{0x50, 0x4B, 0x05, 0x06},
                new byte[]{0x50, 0x4B, 0x07, 0x08}
            };
            var Data = File.ReadAllBytes(Filename).ToList();

            if (Data.Count >= 4)
            {
                var Header = Data.Take(4).ToArray();
                //Games are Zip compressed
                foreach (var seq in ZipSequences)
                {
                    if (Header.SequenceEqual(seq))
                    {
                        return FileType.Game;
                    }
                }
            }

            //Saves are XML
            try
            {
                XmlDocument Doc = new XmlDocument();
                Doc.LoadXml(Encoding.UTF8.GetString(Data.ToArray(), 0, Data.Count));
            }
            catch
            {
                return FileType.Unknown;
            }
            return FileType.Save;
        }

        public static string GetSaveRoot(string Savegame)
        {
            return LoadXml(Savegame).DocumentElement.Attributes["original"].Value;
        }

        public static void SetGameRoot(string Savegame, string Game, string SavegameNew = null)
        {
            if (SavegameNew == null)
            {
                SavegameNew = Savegame;
            }
            var Doc = LoadXml(Savegame);
            Doc.DocumentElement.Attributes["original"].Value = Game;
            SaveXml(Doc, SavegameNew);
        }

        private static VersionType GetVersion(string Filename)
        {
            string FileContent = null;
            if (Filename == null)
            {
                return VersionType.Invalid;
            }
            switch (GetFileType(Filename))
            {
                case FileType.Save:
                    return
                        int.Parse(LoadXml(Filename).DocumentElement.Attributes["version"].Value) < 580 ?
                        VersionType.Old :
                        VersionType.New;
                case FileType.Unknown:
                    return VersionType.Invalid;
            }

            //At this point it must be a game file.
            //It's neither invalid nor a savegame

            try
            {
                FileContent = ExtractGameFile(Filename);
                if (string.IsNullOrEmpty(FileContent))
                {
                    throw new Exception("Unable to extract the file");
                }
            }
            catch
            {
                //Problems launching the executable.
                return VersionType.Error;
            }

            try
            {
                var Doc = new XmlDocument();
                int v = int.MinValue;
                Doc.LoadXml(FileContent);
                if (int.TryParse(Doc.DocumentElement.Attributes["version"].Value, out v) && v > 0)
                {
                    return v < 580 ? VersionType.Old : VersionType.New;
                }
                throw new FormatException("Version not a valid integer");
            }
            catch
            {
                return VersionType.Invalid;
            }
        }

        /// <summary>
        /// Extracts the XML game file from a compressed game
        /// </summary>
        /// <param name="Filename">File name</param>
        /// <returns>XML data, or null on error</returns>
        private static string ExtractGameFile(string Filename)
        {
            using (var P = new Process())
            {
                P.StartInfo.FileName = Config.Get("7z");
                P.StartInfo.Arguments = string.Format(CMDLINE, Filename);
                P.StartInfo.UseShellExecute = false;
                P.StartInfo.RedirectStandardOutput = true;
                if (P.Start())
                {
                    return P.StandardOutput.ReadToEnd();
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Loads a file as XML
        /// </summary>
        /// <param name="Filename">XML file</param>
        /// <returns>XML document</returns>
        private static XmlDocument LoadXml(string Filename)
        {
            XmlDocument Doc = new XmlDocument();
            Doc.LoadXml(File.ReadAllText(Filename));
            return Doc;
        }

        /// <summary>
        /// Saves an XML document to a file
        /// </summary>
        /// <param name="Doc">XML document</param>
        /// <param name="Filename">File name</param>
        private static void SaveXml(XmlDocument Doc, string Filename)
        {
            using (var SW = new StringWriter())
            {
                using (XmlTextWriter XTW = new XmlTextWriter(SW))
                {
                    XTW.Indentation = 1;
                    XTW.IndentChar = '\t';
                    XTW.Formatting = Formatting.Indented;
                    Doc.WriteTo(XTW);
                    XTW.Flush();
                }
                //Writing the document indented is necessary because Quest crashes otherwise.
                //The problem with this is that the .NET XML writer will now split empty nodes.
                //"<test></test>" becomes "<test>\r\n\t</test>"
                //(there may be more tabs to match the indentation level)
                //XML is whitespace sensitive if no style document says otherwise.
                //Empty elements with the open and close tag on different lines are no longer empty.
                //
                //The weird regex is because all tags that make quest crash have an attribute.
                //There's no reason to replace the others.
                //Note: Proper (but slower) regex would look like this: <([^\s>]+)(?:\s[^>]+)?>\s+</\1>
                var Result = Regex.Replace(SW.ToString(), "\">\\r\\n\\t+<", "\"><");
                var Pos = Result.IndexOf("><");
                //Undo the very first replacement, which is wrong
                //This fix is purely for aethetic purposes and the game would run without it.
                Result = Result.Substring(0, Pos) + ">\r\n\t" + Result.Substring(Pos + 1);
                File.WriteAllText(Filename, Result);
            }
        }
    }
}
