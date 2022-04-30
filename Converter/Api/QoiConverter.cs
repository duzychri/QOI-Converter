using System;
using System.IO;
using static Qoi.Constants;

namespace Qoi
{
    /// <summary>
    /// Exposes a decoding and encoding method for the Quite OK Image Format.
    /// </summary>
    public static class QoiConverter
    {
        private static bool CanGetLength(Stream stream)
        {
            try
            { return stream.Length >= 0; }
            catch { return false; }
        }

        /// <summary>
        /// Encodes an image into a QOI format <see cref="byte"/> array.
        /// </summary>
        /// <param name="pixels">The pixels of the image.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="channels">The amount of channels of the pixel.</param>
        /// <param name="colorspace">The colorspace of the image.</param>
        /// <returns>The <see cref="byte"/> array in the QOI format.</returns>
        /// <exception cref="ArgumentException">The input parameters are invalid.</exception>
        public static byte[] Encode(Stream pixels, int width, int height, Channels channels, Colorspace colorspace)
        {
            if (width <= 0)
            { throw new ArgumentException("Parameter 'width' has to be larger than 0."); }
            if (height <= 0)
            { throw new ArgumentException("Parameter 'height' has to be larger than 0."); }
            if (pixels.CanRead == false)
            { throw new ArgumentException("Parameter 'pixels' needs to be readable."); }
            if (CanGetLength(pixels) == false)
            { throw new ArgumentException("Parameter 'pixels' size of stream has to be accessible with the Length property."); }
            if (pixels.Length < HEADER_SIZE + END_MARKER_SIZE || pixels.Length < width * height * (int)channels)
            { throw new ArgumentException("Parameter 'buffer' is to small."); }

            long endOffset = pixels.Length - (int)channels;
            int outputCapacity = width * height * ((int)channels + 1) + HEADER_SIZE + END_MARKER_SIZE;

            Color[] seenPixels = new Color[64];
            Buffer outputBytes = new Buffer(outputCapacity);

            byte run = 0;
            Color previousColor = new Color(0, 0, 0, 255);

            outputBytes.WriteHeader(width, height, (byte)channels, (byte)colorspace);

            Color ReadColor()
            {
                int r = pixels.ReadByte();
                int g = pixels.ReadByte();
                int b = pixels.ReadByte();
                int a = channels == Channels.Rgba ? pixels.ReadByte() : previousColor.a;

                if (r == -1 || g == -1 || b == -1 || a == -1)
                { throw new ArgumentException("Parameter 'pixels' does not have the right number of bytes."); }

                return new Color(r, g, b, a);
            }

            for (int readOffset = 0; readOffset <= endOffset; readOffset += (int)channels)
            {
                Color color = ReadColor();

                if (color == previousColor)
                {
                    run++;
                    if (run == 62 || readOffset == endOffset)
                    {
                        outputBytes.WriteRun(run);
                        run = 0;
                    }
                }
                else
                {
                    if (run > 0)
                    {
                        outputBytes.WriteRun(run);
                        run = 0;
                    }

                    byte hash = (byte)color.GetHashCode();
                    if (color == seenPixels[hash])
                    {
                        outputBytes.WriteIndex(hash);
                    }
                    else
                    {
                        seenPixels[hash] = color;
                        Color difference = color - previousColor;

                        if (difference.a == 0)
                        {
                            int dr_dg = difference.r - difference.g;
                            int db_dg = difference.b - difference.g;

                            if (difference.r >= -2 && difference.r <= 1 &&
                                difference.g >= -2 && difference.g <= 1 &&
                                difference.b >= -2 && difference.b <= 1)
                            {
                                outputBytes.WriteDiff(difference);
                            }
                            else if (
                                difference.g >= -32 && difference.g <= 31 &&
                                dr_dg >= -8 && dr_dg <= 7 &&
                                db_dg >= -8 && db_dg <= 7)
                            {
                                outputBytes.WriteLuma(difference);
                            }
                            else
                            {
                                outputBytes.WriteRGB(color);
                            }
                        }
                        else
                        {
                            outputBytes.WriteRGBA(color);
                        }
                    }

                    previousColor = color;
                }
            }

            outputBytes.WriteEndMarker();

            return outputBytes.ToArray();

        }

        /// <summary>
        /// Encodes an image into a QOI format <see cref="byte"/> array.
        /// </summary>
        /// <param name="pixels">The pixels of the image.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="channels">The amount of channels of the pixel.</param>
        /// <param name="colorspace">The colorspace of the image.</param>
        /// <returns>The <see cref="byte"/> array in the QOI format.</returns>
        /// <exception cref="ArgumentException">The input parameters are invalid.</exception>
        public static byte[] Encode(byte[] pixels, int width, int height, Channels channels, Colorspace colorspace)
        {
            if (width <= 0)
            { throw new ArgumentException("Parameter 'width' has to be larger than 0."); }
            if (height <= 0)
            { throw new ArgumentException("Parameter 'height' has to be larger than 0."); }
            if (pixels.Length < HEADER_SIZE + END_MARKER_SIZE || pixels.Length < width * height * (int)channels)
            { throw new ArgumentException("Parameter 'buffer' is to small."); }

            int endOffset = pixels.Length - (int)channels;
            int outputCapacity = width * height * ((int)channels + 1) + HEADER_SIZE + END_MARKER_SIZE;

            Color[] seenPixels = new Color[64];
            Buffer outputBytes = new Buffer(outputCapacity);

            byte run = 0;
            Color previousColor = new Color(0, 0, 0, 255);

            outputBytes.WriteHeader(width, height, (byte)channels, (byte)colorspace);


            for (int readOffset = 0; readOffset <= endOffset; readOffset += (int)channels)
            {
                Color color = new Color(
                    pixels[readOffset + 0],
                    pixels[readOffset + 1],
                    pixels[readOffset + 2],
                    channels == Channels.Rgba ? pixels[readOffset + 3] : previousColor.a
                );

                if (color == previousColor)
                {
                    run++;
                    if (run == 62 || readOffset == endOffset)
                    {
                        outputBytes.WriteRun(run);
                        run = 0;
                    }
                }
                else
                {
                    if (run > 0)
                    {
                        outputBytes.WriteRun(run);
                        run = 0;
                    }

                    byte hash = (byte)color.GetHashCode();
                    if (color == seenPixels[hash])
                    {
                        outputBytes.WriteIndex(hash);
                    }
                    else
                    {
                        seenPixels[hash] = color;
                        Color difference = color - previousColor;

                        if (difference.a == 0)
                        {
                            int dr_dg = difference.r - difference.g;
                            int db_dg = difference.b - difference.g;

                            if (difference.r >= -2 && difference.r <= 1 &&
                                difference.g >= -2 && difference.g <= 1 &&
                                difference.b >= -2 && difference.b <= 1)
                            {
                                outputBytes.WriteDiff(difference);
                            }
                            else if (
                                difference.g >= -32 && difference.g <= 31 &&
                                dr_dg >= -8 && dr_dg <= 7 &&
                                db_dg >= -8 && db_dg <= 7)
                            {
                                outputBytes.WriteLuma(difference);
                            }
                            else
                            {
                                outputBytes.WriteRGB(color);
                            }
                        }
                        else
                        {
                            outputBytes.WriteRGBA(color);
                        }
                    }

                    previousColor = color;
                }
            }

            outputBytes.WriteEndMarker();

            return outputBytes.ToArray();
        }

        /// <summary>
        /// Decodes a byte array in the QOI format into an image.
        /// </summary>
        /// <param name="buffer">The <see cref="byte"/> array to decode.</param>
        /// <returns>The decoded bytes and image information.</returns>
        /// <exception cref="FormatException">The <see cref="byte"/> array is not a Quite OK Image Format (QOI) encoded image.</exception>
        public static (byte[] pixels, int width, int height, Channels channels, Colorspace colorspace) Decode(byte[] buffer)
        {
            Buffer inputBuffer = new Buffer(buffer);
            var header = inputBuffer.ReadHeader();

            if (header.magicNumber != HEADER_MAGIC_NUMBER)
            {
                throw new FormatException($"The header does not have the appropriate magic number. Expected value was: 'qoif', but actual value is: '{header.magicNumber:X4}'.");
            }
            if (header.width <= 0)
            {
                throw new FormatException($"Header value 'width' has to be larger than 0.");
            }
            if (header.height <= 0) { throw new FormatException($"Header value 'height' has to be larger than 0."); }
            if (header.channels != 3 && header.channels != 4) { throw new FormatException($"Header value 'channels' has to be 3 or 4."); }
            if (header.colorspace != 0 && header.colorspace != 1) { throw new FormatException($"Header value 'colorspace' has to be 0 or 1."); }

            int writeIndex = 0;
            Color previousColor = new Color(0, 0, 0, 255);
            Channels channels = Channels.Rgba;
            int size = header.width * header.height * (int)channels;
            byte[] outputBuffer = new byte[size];
            Color[] seenPixels = new Color[64];

            void writeColor(Color color)
            {
                outputBuffer[writeIndex + 0] = (byte)color.r;
                outputBuffer[writeIndex + 1] = (byte)color.g;
                outputBuffer[writeIndex + 2] = (byte)color.b;
                outputBuffer[writeIndex + 3] = (byte)color.a;

                writeIndex += 4;
            };

            while (writeIndex < size)
            {
                if (inputBuffer.IsRgb())
                {
                    Color color = inputBuffer.ReadRgb((byte)previousColor.a);
                    writeColor(color);

                    previousColor = color;
                    seenPixels[(byte)color.GetHashCode()] = color;
                    continue;
                }

                if (inputBuffer.IsRgba())
                {
                    Color color = inputBuffer.ReadRgba();
                    writeColor(color);

                    previousColor = color;
                    seenPixels[(byte)color.GetHashCode()] = color;
                    continue;
                }

                if (inputBuffer.IsIndex())
                {
                    byte index = inputBuffer.ReadIndex();
                    Color color = seenPixels[index];
                    writeColor(color);

                    previousColor = color;
                    continue;
                }

                if (inputBuffer.IsRun())
                {
                    int run = inputBuffer.ReadRun();

                    for (int n = 0; n < run; n++)
                    {
                        writeColor(previousColor);
                    }
                    continue;
                }

                if (inputBuffer.IsDiff())
                {
                    Color color = inputBuffer.ReadDiff(previousColor);
                    writeColor(color);

                    seenPixels[(byte)color.GetHashCode()] = color;
                    previousColor = color;
                    continue;
                }

                if (inputBuffer.IsLuma())
                {
                    Color color = inputBuffer.ReadLuma(previousColor);
                    writeColor(color);

                    seenPixels[(byte)color.GetHashCode()] = color;
                    previousColor = color;
                    continue;
                }

            }

            if (inputBuffer.IsValidEndMarker() == false)
            { throw new FormatException("Invalid end marker."); }

            return (pixels: outputBuffer, width: header.width, height: header.height, channels: channels, colorspace: (Colorspace)header.colorspace);
        }
    }
}