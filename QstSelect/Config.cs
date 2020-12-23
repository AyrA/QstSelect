using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QstSelect
{
    public static class Config
    {
        public const string CONFIG_FILE = "config.txt";
        public const string BASE64 = "BASE64:";

        private static readonly string _file;

        static Config()
        {
            using (var P = Process.GetCurrentProcess())
            {
                _file = Path.Combine(Path.GetDirectoryName(P.MainModule.FileName), CONFIG_FILE);
            }
        }

        public static string Get(string Name, string Default = null)
        {
            foreach (var L in GetLines())
            {
                if (L.ToLower().StartsWith(Name.ToLower() + "="))
                {
                    return UnsafeVal(L.Substring(Name.Length + 2));
                }
            }
            return Default;
        }

        public static string Set(string Name, string Value)
        {
            var Lines = GetLines();
            bool added = false;
            for (var i = 0; i < Lines.Length; i++)
            {
                var L = Lines[i];
                if (L.ToLower().StartsWith(Name.ToLower() + "="))
                {
                    Lines[i] = Name + "=" + SafeVal(Value);
                    added = true;
                }
            }
            if(!added)
            {
                Lines = Lines.Concat(new string[] { Name + "=" + SafeVal(Value) }).ToArray();
            }
            SetLines(Lines);
            return Value;
        }

        private static string UnsafeVal(string Value)
        {
            if (Value.StartsWith(BASE64))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(Value.Substring(BASE64.Length)));
            }
            return Value;
        }

        private static string SafeVal(string Value)
        {
            if (Value.StartsWith(BASE64) || Value.Any(m => m < 0x20))
            {
                return BASE64 + Convert.ToBase64String(Encoding.UTF8.GetBytes(Value));
            }
            return Value;
        }

        private static void SetLines(IEnumerable<string> Lines)
        {
            File.WriteAllLines(_file, Lines);
        }

        private static string[] GetLines()
        {
            try
            {
                return File.ReadAllLines(_file);
            }
            catch (FileNotFoundException)
            {
                return new string[0];
            }
            catch (Exception ex)
            {
                throw new IOException("Unable to read the configuration file.", ex);
            }
        }
    }
}
