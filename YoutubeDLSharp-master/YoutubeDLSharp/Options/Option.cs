using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace YoutubeDLSharp.Options
{
    /// <summary>
    /// Represents one youtube-dl option.
    /// </summary>
    /// <typeparam name="T">The type of the option.</typeparam>
    public class Option<T> : IOption
    {
        private T value;

        /// <summary>
        /// The default string representation of the option flag.
        /// </summary>
        public string DefaultOptionString => OptionStrings.Last();

        /// <summary>
        /// An array of all possible string representations of the option flag.
        /// </summary>
        public string[] OptionStrings { get; }

        /// <summary>
        /// True if the option flag is set; false otherwise.
        /// </summary>
        public bool IsSet { get; private set; }

        /// <summary>
        /// The option value.
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                
                this.IsSet = !object.Equals(value, default(T));
                this.value = value;
            }
        }
        
        /// <summary>
        /// True if this option is custom.
        /// </summary>
        public bool IsCustom { get; }

        /// <summary>
        /// Creates a new instance of class Option.
        /// </summary>
        public Option(params string[] optionStrings)
        {
            OptionStrings = optionStrings;
            IsSet = false;
        }

        public Option(bool isCustom, params string[] optionStrings)
        {
            OptionStrings = optionStrings;
            IsSet = false;
            IsCustom = isCustom;
        }

        /// <summary>
        /// Sets the option value from a given string representation.
        /// </summary>
        /// <param name="s">The string (including the option flag).</param>
        public void SetFromString(string s)
        {
            string[] split = s.Split(' ');
            string stringValue = s.Substring(split[0].Length).Trim().Trim('"');
            if (!OptionStrings.Contains(split[0]))
                throw new ArgumentException("Given string does not match required format.");
            if (Value is bool)
            {
                Value = (T)(object)OptionStrings.Contains(s);
            }
            else if (Value is Enum)
            {
                string titleCase = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stringValue);
                Value = (T)Enum.Parse(typeof(T), titleCase);
            }
            else if (Value is DateTime)
            {
                Value = (T)(object)DateTime.ParseExact(stringValue, "yyyyMMdd", null);
            }
            else
            {
                TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
                Value = (T)conv.ConvertFrom(stringValue);
            }
        }

        public override string ToString()
        {
            if (!IsSet) return String.Empty;
            string val;
            if (Value is bool)
                val = String.Empty;
            else if (Value is Enum)
                val = $" \"{Value.ToString().ToLower()}\"";
            else if (Value is DateTime dateTime)
                val = $" {dateTime.ToString("yyyyMMdd")}";
            else if (Value is string)
                val = $" \"{Value}\"";
            else val = " " + Value;
            return DefaultOptionString + val;
        }
    }
}
