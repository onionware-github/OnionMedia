/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace OnionMedia.ValueConverters
{
    sealed class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a bool to a Visibility.
        /// </summary>
        /// <param name="value">The bool to convert.</param>
        /// <param name="parameter">When parameter is true, invert the result.</param>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            if (value is not bool) throw new ArgumentException("Input is not a bool.");
            if (!bool.TryParse(parameter.ToString(), out bool inverseResult))
                throw new ArgumentException("Input is not a bool.");

            if ((bool)value)
            {
                if (inverseResult)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
            if (inverseResult)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts a Visibility to a bool.
        /// </summary>
        /// <param name="value">The Visibility to convert.</param>
        /// <param name="parameter">When parameter is true, invert the result.</param>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                if (visibility == Visibility.Visible)
                {
                    if (parameter is bool inverseresult && inverseresult)
                        return false;
                    return true;
                }

                if (parameter is bool inverseResult && inverseResult)
                    return true;
                return false;
            }
            throw new ArgumentException("Input is not from type Visibility.");
        }
    }
}
