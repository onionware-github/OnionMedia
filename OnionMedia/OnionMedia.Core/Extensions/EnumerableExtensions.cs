/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace OnionMedia.Core.Extensions
{
    public static class EnumerableExtensions
    {
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

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action, Predicate<T> condition)
        {
            if (condition != null)
                collection.Where(i => condition(i)).ForEach(action);
            else
                collection.ForEach(action);
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

        /// <summary>
        /// Rounds a number up to the nearest number from <paramref name="numbers"/>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="numbers"/> is null.</exception>
        public static int RoundUpToNearestNeighbor(this int number, IEnumerable<int> numbers)
        {
            if (numbers == null)
                throw new ArgumentNullException(nameof(numbers));

            foreach (var num in numbers.Distinct())
            {
                if (number <= num)
                    return num;
            }
            return number;
        }
    }
}
