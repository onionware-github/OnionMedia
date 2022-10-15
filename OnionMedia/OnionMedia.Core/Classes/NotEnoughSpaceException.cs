/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Runtime.Serialization;

namespace OnionMedia.Core.Classes;

public class NotEnoughSpaceException : IOException
{
    public NotEnoughSpaceException() : base() { }
    public NotEnoughSpaceException(string message) : base(message) { }
    public NotEnoughSpaceException(string message, Exception innerException) : base(message, innerException) { }
    public NotEnoughSpaceException(string message, int hresult) : base(message, hresult) { }
    public NotEnoughSpaceException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public static void ThrowIfNotEnoughSpace(IOException ioexception)
    {
        if ((uint)ioexception?.HResult is NOTENOUGHSPACE_EXCEPTION_HRESULT1 or NOTENOUGHSPACE_EXCEPTION_HRESULT2)
            throw new NotEnoughSpaceException(ioexception.Message, ioexception);
    }

    public const uint NOTENOUGHSPACE_EXCEPTION_HRESULT1 = 0x80070070;
    public const uint NOTENOUGHSPACE_EXCEPTION_HRESULT2 = 0x80070027;
}
