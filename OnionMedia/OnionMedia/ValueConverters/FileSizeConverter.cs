/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.UI.Xaml.Data;
using System;

namespace OnionMedia.ValueConverters
{
    sealed class FileSizeConverter : IValueConverter
    {
        static readonly string[] sizeunits = { "B", "KB", "MB", "GB" };
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not long size && (value == null || !long.TryParse(value.ToString(), out size)))
                throw new ArgumentException("value is not from type long.");

            double formattedSize = size;
            int index;
            for (index = 0; index < sizeunits.Length && size >= 1000; index++, size /= 1000)
                formattedSize /= 1000;

            return $"{Math.Round(formattedSize, 2)} {sizeunits[index]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
