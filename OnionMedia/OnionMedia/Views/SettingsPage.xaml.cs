/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml.Controls;

using OnionMedia.ViewModels;
using System;
using Windows.Globalization.NumberFormatting;

namespace OnionMedia.Views
{
    // TODO WTS: Change the URL for your privacy policy, currently set to https://YourPrivacyUrlGoesHere
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }
        private DecimalFormatter RoundingFormatter { get; }

        public SettingsPage()
        {
            ViewModel = Ioc.Default.GetService<SettingsViewModel>();
            DecimalFormatter formatter = new();
            formatter.IntegerDigits = 1;
            formatter.FractionDigits = 0;
            formatter.NumberRounder = new IncrementNumberRounder() { Increment = 1, RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp };
            RoundingFormatter = formatter;

            InitializeComponent();
        }

        private async void HyperlinkButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var dlg = new Dialogs.LicensesDialog()
            {
                XamlRoot = XamlRoot
            };
            await dlg.ShowAsync();
        }
    }
}
