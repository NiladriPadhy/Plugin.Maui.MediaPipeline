namespace Plugin.Maui.MediaPipeline;

internal static class JpegExif
{
    const byte MarkerSoi = 0xD8;
    const byte MarkerEoi = 0xD9;
    const byte MarkerSos = 0xDA;
    const byte MarkerApp1 = 0xE1;
    const byte MarkerCom = 0xFE;
    const ushort OrientationTag = 0x0112;
    const ushort GpsIfdTag = 0x8825;

    static ReadOnlySpan<byte> ExifHeader => "Exif\0\0"u8;

    public static bool IsJpeg(ReadOnlySpan<byte> data) =>
        data.Length >= 3 && data[0] == 0xFF && data[1] == MarkerSoi;

    public static bool HasExif(ReadOnlySpan<byte> data) => TryGetExifTiff(data, out _);

    public static bool HasGps(ReadOnlySpan<byte> data) =>
        TryGetExifTiff(data, out var tiff) && TryReadTag(tiff, GpsIfdTag, out _, out var count, out var value) && count >= 1 && ReadUInt32(value, IsLittleEndian(tiff)) != 0;

    public static bool TryReadSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (!IsJpeg(data))
        {
            return false;
        }

        var index = 2;
        while (index + 8 < data.Length)
        {
            if (data[index] != 0xFF)
            {
                return false;
            }

            var marker = data[index + 1];
            if (marker is MarkerSos or MarkerEoi)
            {
                return false;
            }

            if (IsStandalone(marker))
            {
                index += 2;
                continue;
            }

            var length = (data[index + 2] << 8) | data[index + 3];
            var segmentSize = 2 + length;
            if (length < 2 || index + segmentSize > data.Length)
            {
                return false;
            }

            // SOF0–SOF3, SOF5–SOF7, SOF9–SOF11, SOF13–SOF15
            if (marker is (>= 0xC0 and <= 0xC3) or (>= 0xC5 and <= 0xC7) or (>= 0xC9 and <= 0xCB) or (>= 0xCD and <= 0xCF))
            {
                if (length < 7)
                {
                    return false;
                }

                height = (data[index + 5] << 8) | data[index + 6];
                width = (data[index + 7] << 8) | data[index + 8];
                return width > 0 && height > 0;
            }

            index += segmentSize;
        }

        return false;
    }

    public static int ReadOrientation(ReadOnlySpan<byte> data)
    {
        if (!TryGetExifTiff(data, out var tiff) || !TryReadTag(tiff, OrientationTag, out var type, out var count, out var value) || count != 1)
        {
            return 1;
        }

        var orientation = type == 3
            ? ReadUInt16(value, IsLittleEndian(tiff))
            : (int)ReadUInt32(value, IsLittleEndian(tiff));

        return orientation is >= 1 and <= 8 ? orientation : 1;
    }

    public static byte[] StripMetadata(ReadOnlySpan<byte> data)
    {
        if (!IsJpeg(data))
        {
            return data.ToArray();
        }

        using var output = new MemoryStream(data.Length);
        output.WriteByte(0xFF);
        output.WriteByte(MarkerSoi);

        var index = 2;
        while (index < data.Length - 1)
        {
            if (data[index] != 0xFF)
            {
                output.Write(data[index..]);
                break;
            }

            var marker = data[index + 1];
            if (marker == MarkerSoi)
            {
                index += 2;
                continue;
            }

            if (marker == MarkerEoi)
            {
                output.WriteByte(0xFF);
                output.WriteByte(MarkerEoi);
                break;
            }

            if (IsStandalone(marker))
            {
                output.Write(data.Slice(index, 2));
                index += 2;
                continue;
            }

            if (index + 3 >= data.Length)
            {
                break;
            }

            var length = (data[index + 2] << 8) | data[index + 3];
            var segmentSize = 2 + length;
            if (length < 2 || index + segmentSize > data.Length)
            {
                output.Write(data[index..]);
                break;
            }

            if (marker == MarkerSos)
            {
                output.Write(data[index..]);
                break;
            }

            if (ShouldStrip(marker))
            {
                index += segmentSize;
                continue;
            }

            output.Write(data.Slice(index, segmentSize));
            index += segmentSize;
        }

        return output.ToArray();
    }

    public static byte[] WithOrientation(ReadOnlySpan<byte> jpeg, int orientation)
    {
        if (orientation is < 1 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(orientation));
        }

        var body = IsJpeg(jpeg) ? StripMetadata(jpeg) : jpeg.ToArray();
        if (!IsJpeg(body))
        {
            throw new MediaPipelineException(MediaPipelineError.InvalidImage, "Orientation can only be written to a JPEG.");
        }

        var app1 = BuildOrientationApp1(orientation);
        var output = new byte[body.Length + app1.Length];
        body[..2].CopyTo(output);
        app1.CopyTo(output.AsSpan(2));
        body[2..].CopyTo(output.AsSpan(2 + app1.Length));
        return output;
    }

    static bool ShouldStrip(byte marker) => marker is MarkerApp1 or MarkerCom;

    static bool IsStandalone(byte marker) => marker == 0x01 || marker is >= 0xD0 and <= 0xD9;

    static bool TryGetExifTiff(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> tiff)
    {
        tiff = default;
        if (!IsJpeg(data))
        {
            return false;
        }

        var index = 2;
        while (index + 4 < data.Length)
        {
            if (data[index] != 0xFF)
            {
                return false;
            }

            var marker = data[index + 1];
            if (marker is MarkerSos or MarkerEoi)
            {
                return false;
            }

            if (IsStandalone(marker))
            {
                index += 2;
                continue;
            }

            var length = (data[index + 2] << 8) | data[index + 3];
            var segmentSize = 2 + length;
            if (length < 2 || index + segmentSize > data.Length)
            {
                return false;
            }

            if (marker == MarkerApp1)
            {
                var payload = data.Slice(index + 4, length - 2);
                if (payload.Length > ExifHeader.Length && payload[..ExifHeader.Length].SequenceEqual(ExifHeader))
                {
                    tiff = payload[ExifHeader.Length..];
                    return tiff.Length >= 8;
                }
            }

            index += segmentSize;
        }

        return false;
    }

    static bool TryReadTag(ReadOnlySpan<byte> tiff, ushort tag, out ushort type, out int count, out ReadOnlySpan<byte> valueOrOffset)
    {
        type = 0;
        count = 0;
        valueOrOffset = default;
        if (tiff.Length < 8)
        {
            return false;
        }

        var little = IsLittleEndian(tiff);
        var ifdOffset = (int)ReadUInt32(tiff.Slice(4, 4), little);
        if (ifdOffset < 0 || ifdOffset + 2 > tiff.Length)
        {
            return false;
        }

        var entryCount = ReadUInt16(tiff.Slice(ifdOffset, 2), little);
        var entriesStart = ifdOffset + 2;
        for (var i = 0; i < entryCount; i++)
        {
            var entry = entriesStart + (i * 12);
            if (entry + 12 > tiff.Length)
            {
                return false;
            }

            var currentTag = ReadUInt16(tiff.Slice(entry, 2), little);
            if (currentTag != tag)
            {
                continue;
            }

            type = (ushort)ReadUInt16(tiff.Slice(entry + 2, 2), little);
            count = (int)ReadUInt32(tiff.Slice(entry + 4, 4), little);
            valueOrOffset = tiff.Slice(entry + 8, 4);
            return true;
        }

        return false;
    }

    static bool IsLittleEndian(ReadOnlySpan<byte> tiff) => tiff[0] == (byte)'I' && tiff[1] == (byte)'I';

    static int ReadUInt16(ReadOnlySpan<byte> value, bool littleEndian) =>
        littleEndian ? value[0] | (value[1] << 8) : (value[0] << 8) | value[1];

    static uint ReadUInt32(ReadOnlySpan<byte> value, bool littleEndian) =>
        littleEndian
            ? (uint)(value[0] | (value[1] << 8) | (value[2] << 16) | (value[3] << 24))
            : (uint)((value[0] << 24) | (value[1] << 16) | (value[2] << 8) | value[3]);

    static byte[] BuildOrientationApp1(int orientation)
    {
        var tiff = new byte[26];
        tiff[0] = (byte)'I';
        tiff[1] = (byte)'I';
        tiff[2] = 0x2A;
        tiff[4] = 8;
        tiff[8] = 1;
        tiff[10] = 0x12;
        tiff[11] = 0x01;
        tiff[12] = 3;
        tiff[14] = 1;
        tiff[18] = (byte)orientation;

        var payloadLength = ExifHeader.Length + tiff.Length;
        var segment = new byte[2 + 2 + payloadLength];
        segment[0] = 0xFF;
        segment[1] = MarkerApp1;
        var length = payloadLength + 2;
        segment[2] = (byte)(length >> 8);
        segment[3] = (byte)length;
        ExifHeader.CopyTo(segment.AsSpan(4));
        tiff.CopyTo(segment.AsSpan(4 + ExifHeader.Length));
        return segment;
    }
}
