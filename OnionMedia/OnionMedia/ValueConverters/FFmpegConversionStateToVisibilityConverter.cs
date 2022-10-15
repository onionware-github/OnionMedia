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
using OnionMedia.Core.Enums;
using System;

namespace OnionMedia.ValueConverters
{
    sealed class FFmpegConversionStateToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Visibility.Visible, when <paramref name="value"/> matches <paramref name="parameter", otherwise Visibility.Collapsed. Write a '!' to the beginning of the parameter to inverse the result./>
        /// </summary>
        /// <param name="value">Value 1 to compare.</param>
        /// <param name="parameter">Value 2 to compare.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not FFmpegConversionState)
                throw new ArgumentException("value is not from type FFmpegConversionState.");

            if (parameter == null || string.IsNullOrWhiteSpace(parameter.ToString()))
                throw new ArgumentNullException(nameof(parameter));

            string paramString = parameter.ToString();
            bool invertResult = paramString.StartsWith('!');
            if (invertResult)
                paramString = paramString.Remove(0, 1);

            if (!Enum.TryParse(paramString, out FFmpegConversionState state))
                throw new ArgumentException("parameter is null or not from type FFmpegConversionState.");

            bool result = (FFmpegConversionState)value == state;
            if (invertResult) result = !result;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
