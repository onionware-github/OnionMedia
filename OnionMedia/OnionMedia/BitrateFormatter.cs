/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using Windows.Globalization.NumberFormatting;

namespace OnionMedia
{
    public sealed class BitrateFormatter : INumberFormatter2, INumberParser
    {
        private BitrateFormatter() { }
        public static BitrateFormatter Instance { get; } = new BitrateFormatter();
        public string FormatDouble(double value) => value != 0 ? value.ToString() : string.Empty;

        public string FormatInt(long value) => value != 0 ? value.ToString() : string.Empty;

        public string FormatUInt(ulong value) => value != 0 ? value.ToString() : string.Empty;

        public double? ParseDouble(string text)
        {
            if (text == null) return 0;
            if (double.TryParse(text, out double value))
                return value;
            string input = text.ToLower();
            if (input.EndsWith('m') && double.TryParse(input.TrimEnd('m'), out double val))
                return val * 1000;
            return 0;
        }

        public long? ParseInt(string text)
        {
            if (text == null) return 0;
            if (long.TryParse(text, out long value))
                return value;
            string input = text.ToLower();
            if (input.EndsWith('m') && long.TryParse(input.TrimEnd('m'), out long val))
                return val * 1000;
            return 0;
        }

        public ulong? ParseUInt(string text)
        {
            if (text == null) return 0;
            if (ulong.TryParse(text, out ulong value))
                return value;
            string input = text.ToLower();
            if (input.EndsWith('m') && ulong.TryParse(input.TrimEnd('m'), out ulong val))
                return val * 1000;
            return 0;
        }
    }
}
