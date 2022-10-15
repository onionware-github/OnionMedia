/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OnionMedia.Core.Services;

namespace OnionMedia.Core.Models
{
    public record FFmpegCodecConfig(IEnumerable<FFmpegCodec> Videocodecs, IEnumerable<FFmpegCodec> Audiocodecs, IEnumerable<FFmpegContainerFormat> ContainerFormats, byte[] FFmpegHash);

    public record FFmpegCodec(string Name, string Description, string[] Encoders)
    {
        public bool MultipleEncoders => Encoders.Length > 1;

        public static IEnumerable<FFmpegCodec> GetEncodableVideoCodecs()
            => GetEncodableCodecs(GETVIDEOCODECSREGEX);

        public static IEnumerable<FFmpegCodec> GetEncodableAudioCodecs()
            => GetEncodableCodecs(GETAUDIOCODECSREGEX);

        protected static IEnumerable<FFmpegCodec> GetEncodableCodecs(string regex)
        {
            //Get codecs from ffmpeg
            Process p = new();
            p.StartInfo.FileName = pathProvider.FFmpegPath;
            p.StartInfo.Arguments = "-codecs";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            //Filter names, descriptions and encoders from the output
            foreach (Match match in Regex.Matches(output, regex, RegexOptions.Multiline).Where(m => m.Groups[1].Success && m.Groups[2].Success))
            {
                string codecname = match.Groups[1].Value.Trim();
                string codecdesc = match.Groups[2].Value.Trim();
                var encodersmatch = Regex.Match(codecdesc, GETENCODERSREGEX);
                string[] encoders;
                string description = new(codecdesc.TakeWhile(c => c != '\n').ToArray());
                if (encodersmatch.Success && encodersmatch.Groups[1].Success)
                {
                    encoders = encodersmatch.Groups[1].Value.Split(' ');
                    description = description.Replace(encodersmatch.Value, string.Empty);
                }
                else
                    encoders = Array.Empty<string>();

                Match containsDecoders = Regex.Match(description, GETDECODERSREGEX);
                if (containsDecoders.Success)
                    description = description.Replace(containsDecoders.Value, string.Empty);

                yield return new(codecname, description.Trim(), encoders);
            }
        }

        static readonly IPathProvider pathProvider = IoC.Default.GetService<IPathProvider>();

        //Group 1 = Codecname; Group 2 = Description
        const string GETVIDEOCODECSREGEX = @"^\s*.EV... ([A-Za-z0-9_]+)\s+(.+)$";
        const string GETAUDIOCODECSREGEX = @"^\s*.EA... ([A-Za-z0-9_]+)\s+(.+)$";

        const string GETENCODERSREGEX = @"[(]encoders: ([A-Za-z0-9-_\s]*) [)]";
        const string GETDECODERSREGEX = @"[(]decoders: ([A-Za-z0-9-_\s]*) [)]";
    }
}
