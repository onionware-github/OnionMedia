/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using OnionMedia.Core.Models;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System;
using System.Linq;
using FFMpegCore;
using System.Security.Cryptography;

namespace OnionMedia.Core.Services;

public sealed class FFmpegStartup : IFFmpegStartup
{
    public FFmpegStartup(IPathProvider pathProvider)
    {
        this.pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
    }
    private readonly IPathProvider pathProvider;

    public async Task InitializeFormatsAndCodecsAsync()
    {
        byte[] hash;
        FFmpegCodecConfig codecs;
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(pathProvider.FFmpegPath))
                hash = md5.ComputeHash(stream);
        }
        
        string serializedCodecsPath = Path.Combine(pathProvider.LocalCache, "codecs.json");
        try
        {
            try
            {
                //Try to read from codecs.json file
                codecs = JsonSerializer.Deserialize<FFmpegCodecConfig>(File.ReadAllText(Path.Combine(pathProvider.InstallPath, "Data", "codecs.json")));
                GlobalResources.FFmpegCodecs = codecs.FFmpegHash.SequenceEqual(hash) ? codecs : throw new Exception();
            }
            catch
            {
                //When something goes wrong while deserializing the file, look at the LocalCache Directory
                codecs = JsonSerializer.Deserialize<FFmpegCodecConfig>(await File.ReadAllTextAsync(serializedCodecsPath));
                GlobalResources.FFmpegCodecs = codecs.FFmpegHash.SequenceEqual(hash) ? codecs : throw new Exception();
            }
        }
        catch
        {
            //When no (valid) file was found, generate a new one in the LocalCache Directory
            GlobalResources.FFmpegCodecs = new FFmpegCodecConfig(FFmpegCodec.GetEncodableVideoCodecs(), FFmpegCodec.GetEncodableAudioCodecs(), FFMpeg.GetContainerFormats().Where(f => f.MuxingSupported).Select(f => new FFmpegContainerFormat(f.Name, f.Description)), hash);
            Directory.CreateDirectory(pathProvider.LocalCache);
            File.WriteAllText(serializedCodecsPath, JsonSerializer.Serialize(GlobalResources.FFmpegCodecs));
        }
    }
}