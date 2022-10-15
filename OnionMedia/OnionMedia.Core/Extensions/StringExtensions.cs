/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using OnionMedia.Core.Services;

namespace OnionMedia.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Removes forbidden characters to use the string as a filename.
        /// </summary>
        /// <param name="input">The string to trim.</param>
        /// <returns>The trimmed filename string.</returns>
        public static string TrimToFilename(this string input, int maxLength)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            char[] forbiddenChars = @"<>:/\|?""*".ToCharArray();
            return string.Concat(input.Trim().Where(c => !forbiddenChars.Contains(c))).ShortString(maxLength);
        }

        /// <summary>
        /// An extensionmethod for string.IsNullOrEmpty().
        /// </summary>
        /// <param name="input">The string to check</param>
        /// <returns>Indicates whetever the specified string is null or an empty string("").</returns>
        public static bool IsNullOrEmpty(this string input) => string.IsNullOrEmpty(input);

        /// <summary>
        /// An extensionmethod for string.IsNullOrWhiteSpace().
        /// </summary>
        /// <param name="input">The string to check</param>
        /// <returns>Indicates whetever the specified string is null, empty or contains only whitespaces.</returns>
        public static bool IsNullOrWhiteSpace(this string input) => string.IsNullOrWhiteSpace(input);

        public static string ShortString(this string input, int maxLength)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Length <= maxLength)
                return input;

            int maxStringLen = maxLength - 3;
            if (maxStringLen < 0)
                maxStringLen = 0;

            string range = input[0..maxStringLen];
            return range + "...";
        }

        public static string GetLocalized(this string resourceName, string resourceSection = null)
        {
            return resourceService.GetLocalized(resourceName, resourceSection);
        }

        private static readonly IStringResourceService resourceService = IoC.Default.GetService<IStringResourceService>() ?? throw new ArgumentNullException();
    }
}
