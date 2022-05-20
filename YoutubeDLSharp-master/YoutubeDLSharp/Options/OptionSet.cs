using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace YoutubeDLSharp.Options
{
    /// <summary>
    /// Represents a set of options for youtube-dl.
    /// </summary>
    public partial class OptionSet : ICloneable
    {
        private static readonly OptionComparer Comparer = new OptionComparer();
        
        /// <summary>
        /// The default option set (if no options are explicitly set).
        /// </summary>
        public static readonly OptionSet Default = new OptionSet();

        /// <summary>
        /// Writes all options to a config file with the specified path.
        /// </summary>
        public void WriteConfigFile(string path)
        {
            File.WriteAllLines(path, GetOptionFlags());
        }

        public override string ToString() => " " + String.Join(" ", GetOptionFlags());

        /// <summary>
        /// Returns an enumerable of all option flags.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetOptionFlags()
        {
            return GetKnownOptions()
                .Concat(CustomOptions)
                .Select(opt => opt.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value));
        }

        internal IEnumerable<IOption> GetKnownOptions()
        {
            return this.GetType()
                .GetRuntimeFields()
                .Where(p => p.FieldType.IsGenericType && p.FieldType.GetGenericTypeDefinition() == typeof(Option<>))
                .Select(p => p.GetValue(this)).Cast<IOption>();
        }

        /// <summary>
        /// Creates a clone of this option set and overrides all options with non-default values set in the given option set.
        /// </summary>
        /// <param name="overrideOptions">All non-default option values of this option set will be copied to the cloned option set.</param>
        /// <returns>A cloned option set with all specified options overriden.</returns>
        public OptionSet OverrideOptions(OptionSet overrideOptions)
        {
            var cloned = (OptionSet) Clone();
            cloned.CustomOptions = cloned.CustomOptions
                .Concat(overrideOptions.CustomOptions)
                .Distinct(Comparer)
                .ToArray();

            IEnumerable<FieldInfo> overrideFields = overrideOptions.GetType().GetRuntimeFields()
                .Where(p => p.FieldType.IsGenericType && p.FieldType.GetGenericTypeDefinition() == typeof(Option<>));
            
            foreach (var field in overrideFields)
            {
                var fieldValue = (IOption)field.GetValue(overrideOptions);
                if (fieldValue.IsSet)
                {
                    cloned.GetType()
                        .GetField(field.Name, BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(cloned, fieldValue);
                }
            }
            
            
            return cloned;
        }

        /// <summary>
        /// Creates an option set from an array of command-line option strings.
        /// </summary>
        /// <param name="lines">An array containing one command-line option string per item.</param>
        /// <returns>The parsed OptionSet.</returns>
        public static OptionSet FromString(IEnumerable<string> lines)
        {
            var optSet = new OptionSet();
            
            var customOptions = GetOptions(lines, optSet.GetKnownOptions())
                .Where(option => option.IsCustom)
                .ToArray();
            optSet.CustomOptions = customOptions;
            
            return optSet;
        }

        private static IEnumerable<IOption> GetOptions(IEnumerable<string> lines, IEnumerable<IOption> options)
        {
            IEnumerable<IOption> knownOptions = options.ToList();

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                
                // skip comments
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] segments = line.Split(' ');
                string flag = segments[0];

                IOption knownOption = knownOptions.FirstOrDefault(o => o.OptionStrings.Contains(flag));
                IOption customOption = segments.Length > 1 
                    ? (IOption) new Option<string>(isCustom: true, flag) 
                    : (IOption) new Option<bool>(isCustom: true, flag);

                var option = knownOption ?? customOption;

                option.SetFromString(line);
                yield return option;
            }
        }

        /// <summary>
        /// Loads an option set from a youtube-dl config file.
        /// </summary>
        /// <param name="path">The path to the config file.</param>
        /// <returns>The loaded OptionSet.</returns>
        public static OptionSet LoadConfigFile(string path)
        {
            return FromString(File.ReadAllLines(path));
        }

        public object Clone()
        {
            return OptionSet.FromString(this.GetOptionFlags());
        }
    }
}
