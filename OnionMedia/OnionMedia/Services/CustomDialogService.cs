﻿/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using Microsoft.UI.Xaml;
using OnionMedia.Core.Services;
using OnionMedia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnionMedia.Services
{
    internal class CustomDialogService : ICustomDialogService
    {
        public async Task ShowThirdPartyLicensesDialogAsync() => await new LicensesDialog() { XamlRoot = xamlRoot }.ShowAsync();


        private readonly XamlRoot xamlRoot = GlobalResources.XamlRoot;
    }
}
