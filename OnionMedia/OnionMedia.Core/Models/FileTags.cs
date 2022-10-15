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
    public class FileTags
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string Genre { get; set; }

        public uint Track { get; set; }

        public uint Year { get; set; }


        public FileTags Clone() => MemberwiseClone() as FileTags;

        public bool EqualsTagsFrom(FileTags tagsToCompare, bool nullEqualsEmptyString = true)
        {
            var tags = Clone();
            var otherTags = tagsToCompare.Clone();
            if (nullEqualsEmptyString)
            {
                if (tags.Title == string.Empty)
                    tags.Title = null;
                if (tags.Description == string.Empty)
                    tags.Description = null;
                if (tags.Artist == string.Empty)
                    tags.Artist = null;
                if (tags.Album == string.Empty)
                    tags.Album = null;
                if (tags.Genre == string.Empty)
                    tags.Genre = null;

                if (otherTags.Title == string.Empty)
                    otherTags.Title = null;
                if (otherTags.Description == string.Empty)
                    otherTags.Description = null;
                if (otherTags.Artist == string.Empty)
                    otherTags.Artist = null;
                if (otherTags.Album == string.Empty)
                    otherTags.Album = null;
                if (otherTags.Genre == string.Empty)
                    otherTags.Genre = null;
            }

            return tags.Title == otherTags.Title && tags.Description == otherTags.Description && tags.Artist == otherTags.Artist &&
            tags.Album == otherTags.Album && tags.Genre == otherTags.Genre && tags.Track == otherTags.Track && tags.Year == otherTags.Year;
        }
    }
}
