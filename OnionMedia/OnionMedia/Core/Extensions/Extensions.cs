/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace OnionMedia.Core.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Removes forbidden characters to use the string as a filename.
        /// </summary>
        /// <param name="input">The string to trim.</param>
        /// <returns>The trimmed filename string.</returns>
        public static string TrimToFilename(this string input)
        {
            char[] forbiddenChars = @"<>:/\|?""*".ToCharArray();
            return string.Concat(input.Trim().Where(c => !forbiddenChars.Contains(c)));
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

        public static string GetDisplayName(this Enum enumtype)
        {
            var displayattribute = enumtype.GetType().GetMember(enumtype.ToString()).First().GetCustomAttribute<DisplayAttribute>();
            return displayattribute != null ? displayattribute.Name : enumtype.ToString();
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> itemsToAdd)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (itemsToAdd == null)
                throw new ArgumentNullException(nameof(itemsToAdd));

            foreach (T item in itemsToAdd)
                collection.Add(item);
        }

        public static void Replace<T>(this ICollection<T> collection, IEnumerable<T> newItems)
        {
            collection.Clear();
            collection.AddRange(newItems);
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            foreach (T item in collection)
                action(item);
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Predicate<T> condition)
        {
            if (collection == null || condition == null)
                throw new ArgumentNullException(collection == null ? nameof(collection) : nameof(condition));

            for (int i = 0; i < collection.Count(); i++)
                if (condition(collection.ElementAt(i)))
                    return i;
            throw new ArgumentException("No item met the condition.");
        }

        public static TimeSpan WithoutMilliseconds(this TimeSpan timeSpan) => new(timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, 0);
    }
}