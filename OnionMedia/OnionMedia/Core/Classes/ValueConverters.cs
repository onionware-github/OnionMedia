/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;

namespace OnionMedia.Core.ValueConverters
{
    public class ItemClickEventArgsToClickedItemConverter : IValueConverter
    {
        public static object Convert(ItemClickEventArgs args) => args.ClickedItem;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ItemClickEventArgs args)
                return Convert(args);
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
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

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool boolean)
                return !boolean;

            throw new ArgumentException("Input is not a bool.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class EqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.Equals(parameter))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (!value.Equals(parameter))
                return Visibility.Visible;
            return Visibility.Collapsed;
        }
    }

    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || value is not TimeSpan) return string.Empty;
            return ((TimeSpan)value).ToString(@"hh\:mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class EnumDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            
            if (value is Enum enumtype)
                return enumtype.GetDisplayName();
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BitToKilobitConverter : IValueConverter
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

    public class FFmpegConversionStateToVisibilityConverter : IValueConverter
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

    public class FileSizeConverter : IValueConverter
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
