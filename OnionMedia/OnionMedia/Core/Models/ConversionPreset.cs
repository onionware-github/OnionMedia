/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using FFMpegCore.Enums;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnionMedia.Core.Models
{
    [ObservableObject]
    public partial class ConversionPreset
    {
        public ConversionPreset()
        {
            Name = "newPreset".GetLocalized();
            Format = GlobalResources.FFmpegCodecs.ContainerFormats.First(f => f.Name == "mp4");
            VideoCodec = GlobalResources.FFmpegCodecs.Videocodecs.Any(c => c.Name == "h264") ? GlobalResources.FFmpegCodecs.Videocodecs.First(c => c.Name == "h264") : GlobalResources.FFmpegCodecs.Videocodecs.First();
            AudioCodec = GlobalResources.FFmpegCodecs.Audiocodecs.Any(c => c.Name == "aac") ? GlobalResources.FFmpegCodecs.Audiocodecs.First(c => c.Name == "aac") : GlobalResources.FFmpegCodecs.Audiocodecs.First();
            VideoEncoder = VideoCodec.Encoders.Any() ? VideoCodec.Encoders.First() : VideoCodec.Name;
            AudioEncoder = AudioCodec.Encoders.Any() ? AudioCodec.Encoders.First() : AudioCodec.Name;
            VideoAvailable = true;
            AudioAvailable = true;
        }

        public ConversionPreset(string name) : this()
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : "unnamed".GetLocalized();
        }

        public ConversionPreset(string name, FFmpegContainerFormat containerFormat, FFmpegCodec videoCodec, FFmpegCodec audioCodec) : this(name)
        {
            Format = containerFormat;
            VideoCodec = videoCodec;
            AudioCodec = audioCodec;
            VideoEncoder = VideoCodec.Encoders.Any() ? VideoCodec.Encoders.First() : VideoCodec.Name;
            AudioEncoder = AudioCodec.Encoders.Any() ? AudioCodec.Encoders.First() : AudioCodec.Name;
            VideoAvailable = true;
            AudioAvailable = true;
        }

        public string Name { get; set; }
        public FFmpegContainerFormat Format { get; set; }

        public bool VideoAvailable { get; set; }

        public FFmpegCodec VideoCodec
        {
            get => videoCodec;
            set
            {
                if (SetProperty(ref videoCodec, value))
                    VideoEncoder = VideoCodec.Encoders.Any() ? VideoCodec.Encoders.First() : VideoCodec.Name;
            }
        }
        private FFmpegCodec videoCodec;

        [ObservableProperty]
        private string videoEncoder;


        public bool AudioAvailable { get; set; }

        public FFmpegCodec AudioCodec
        {
            get => audioCodec;
            set
            {
                if (SetProperty(ref audioCodec, value))
                    AudioEncoder = AudioCodec.Encoders.Any() ? AudioCodec.Encoders.First() : AudioCodec.Name;
            }
        }
        private FFmpegCodec audioCodec;

        [ObservableProperty]
        private string audioEncoder;


        public virtual bool EqualsOptionsFrom(ConversionPreset otherPreset)
            => otherPreset != null && Format.Equals(otherPreset.Format) && VideoAvailable.Equals(otherPreset.VideoAvailable)
            && VideoCodec.Equals(otherPreset.VideoCodec) && AudioAvailable.Equals(otherPreset.AudioAvailable) && AudioCodec.Equals(otherPreset.AudioCodec);

        public virtual bool Equals(ConversionPreset other)
            => EqualsOptionsFrom(other) && Name.Equals(other.Name);

        public ConversionPreset Clone() => MemberwiseClone() as ConversionPreset;
    }

    public record FFmpegContainerFormat(string Name, string Description);
}
