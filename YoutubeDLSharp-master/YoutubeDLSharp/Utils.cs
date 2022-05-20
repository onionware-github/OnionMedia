using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace YoutubeDLSharp
{
    /// <summary>
    /// Utility methods.
    /// </summary>
    public static class Utils
    {
        private static readonly Regex rgxTimestamp = new Regex("[0-9]+(?::[0-9]+)+", RegexOptions.Compiled);
        private static readonly Dictionary<char, string> accentChars
            = "ÂÃÄÀÁÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖŐØŒÙÚÛÜŰÝÞßàáâãäåæçèéêëìíîïðñòóôõöőøœùúûüűýþÿ"
                .Zip(new[] { "A","A","A","A","A","A","AE","C","E","E","E","E","I","I","I","I","D","N",
                    "O","O","O","O","O","O","O","OE","U","U","U","U","U","Y","P","ss",
                    "a","a","a","a","a","a","ae","c","e","e","e","e","i","i","i","i","o","n",
                    "o","o","o","o","o","o","o","oe","u","u","u","u","u","y","p","y"},
                    (c, s) => new { Key = c, Val = s }).ToDictionary(o => o.Key, o => o.Val);

        /// <summary>
        /// Sanitize a string to be a valid file name.
        /// Ported from:
        /// https://github.com/ytdl-org/youtube-dl/blob/33c1c7d80fd99024879a5f087b55b24374385e43/youtube_dl/utils.py#L2067
        /// </summary>
        /// <returns></returns>
        public static string Sanitize(string s, bool restricted = false)
        {
            rgxTimestamp.Replace(s, m => m.Groups[0].Value.Replace(':', '_'));
            var result = String.Join("", s.Select(c => sanitizeChar(c, restricted)));
            result = result.Replace("__", "_").Trim('_');
            if (restricted && result.StartsWith("-_"))
                result = result.Substring(2);
            if (result.StartsWith("-"))
                result = "_" + result.Substring(1);
            result = result.TrimStart('.');
            if (String.IsNullOrWhiteSpace(result))
                result = "_";
            return result;
        }

        private static string sanitizeChar(char c, bool restricted)
        {
            if (restricted && accentChars.ContainsKey(c))
                return accentChars[c];
            else if (c == '?' || c < 32 || c == 127)
                return "";
            else if (c == '"')
                return restricted ? "" : "\'";
            else if (c == ':')
                return restricted ? "_-" : " -";
            else if ("\\/|*<>".Contains(c))
                return "_";
            else if (restricted && "!&\'()[]{}$;`^,# ".Contains(c))
                return "_";
            else if (restricted && c > 127)
                return "_";
            else return c.ToString();
        }

        /// <summary>
        /// Returns the absolute path for the specified path string.
        /// Also searches the environment's PATH variable.
        /// </summary>
        /// <param name="fileName">The relative path string.</param>
        /// <returns>The absolute path or null if the file was not found.</returns>
        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var p in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(p, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
