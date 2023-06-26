/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

namespace OnionMedia.Core.Models
{
    /// <summary>
    /// Contains some information about a library used in the program.
    /// </summary>
    /// <param name="Libraryname">The name of the library.</param>
    /// <param name="Author">The author of the library.</param>
    /// <param name="LicenseType">The type of license that the library uses.</param>
    /// <param name="LicensePath">The path to the textfile that contains the license.</param>
    public record LibraryInfo(string Libraryname, string Author, string LicenseType, string LicensePath, string ProjectUrl, int Year = 0);
}
