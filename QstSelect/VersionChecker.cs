using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QstSelect
{
    public static class VersionChecker
    {
        public enum VersionType : int
        {
            Old = 1,
            New = Old + 1,
            Invalid = int.MinValue,
            NotFound = Invalid + 1,
            Error = NotFound + 1
        }

        private const string CMDLINE = "e \"{0}\" game.aslx -so";
        private const string REGEX = "<asl[^>]+version\\s*=\\s*\"(\\d+)\"";

        public static VersionType GetVersion(string Filename)
        {
            try
            {
                using (var P = new Process())
                {
                    P.StartInfo.FileName = Config.Get("7z");
                    P.StartInfo.Arguments = string.Format(CMDLINE, Filename);
                    P.StartInfo.UseShellExecute = false;
                    P.StartInfo.RedirectStandardOutput = true;
                    if (P.Start())
                    {
                        var Output = P.StandardOutput.ReadToEnd();
                        if (Output.Length > 0)
                        {
                            var M = Regex.Match(Output, REGEX);
                            if(M.Success)
                            {
                                var V = int.Parse(M.Groups[1].Value);
                                return V >= 580 ? VersionType.New : VersionType.Old;
                            }
                            return VersionType.Invalid;
                        }
                        return File.Exists(Filename) ? VersionType.Invalid : VersionType.NotFound;
                    }
                    return VersionType.Error;
                }
            }
            catch
            {
                return VersionType.Error;
            }
        }
    }
}
