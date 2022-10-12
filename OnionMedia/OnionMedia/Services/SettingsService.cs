/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using OnionMedia.Core.Services;
using Windows.Storage;

namespace OnionMedia.Services
{
    sealed class SettingsService : ISettingsService
    {
        public object? GetSetting(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            return ApplicationData.Current.LocalSettings.Values[key] ?? null;
        }

        public void SetSetting(string key, object value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
    }
}
