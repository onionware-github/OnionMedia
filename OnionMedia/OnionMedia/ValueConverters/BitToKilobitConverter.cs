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
    sealed class BitToKilobitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (double.TryParse(value.ToString(), out double bit))
                return Math.Round(bit / 1000, 3);
            throw new ArgumentException("value is not a number.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (double.TryParse(value.ToString(), out double kbit))
                return (long)Math.Round(kbit * 1000, 0);
            throw new ArgumentException("value is not a number.");
        }
    }
}
