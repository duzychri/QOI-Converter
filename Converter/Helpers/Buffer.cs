using System;
using System.Linq;
using System.Diagnostics;
using static Qoi.Constants;

namespace Qoi
{
    [DebuggerDisplay("Offset = {offset}")]
    internal class Buffer
    {
        private enum BufferMode
        {
            Read,
            Write
        }

        public int offset;
        public readonly byte[] buffer;
        private readonly BufferMode mode;

        /**
         * Creates a new QoiArray class.
         * @param {number | Uint8Array} capacityOrBuffer The capacity of the empty array or an already allocated array.
         */
        public Buffer(byte[] buffer)
        {
            if (buffer is null) { throw new ArgumentNullException(nameof(buffer)); }

            offset = 0;
            mode = BufferMode.Read;
            this.buffer = buffer;
        }

        public Buffer(int capacity)
        {
            if (capacity < 0) { throw new ArgumentException($"Capacity needs to be larger than 0.", nameof(capacity)); }

            offset = 0;
            mode = BufferMode.Write;
            buffer = new byte[capacity];
        }

        #region General Methods

        public byte[] ToArray()
        {
            return buffer.Take(offset).ToArray();
        }

        #endregion General Methods

        #region Check Methods

        public bool IsRgb()
        {
            return Peek8() == QOI_OP_RGB;
        }

        public bool IsRgba()
        {
            return Peek8() == QOI_OP_RGBA;
        }

        public bool IsIndex()
        {
            return (Peek8() & QOI_MASK) == QOI_OP_INDEX;
        }

        public bool IsRun()
        {
            return (Peek8() & QOI_MASK) == QOI_OP_RUN;
        }

        public bool IsDiff()
        {
            return (Peek8() & QOI_MASK) == QOI_OP_DIFF;
        }

        public bool IsLuma()
        {
            return (Peek8() & QOI_MASK) == QOI_OP_LUMA;
        }

        public bool IsValidEndMarker()
        {
            for (int n = 0; n < END_MARKER_BYTES.Length; n++)
            {
                byte value = Read8();
                if (value != END_MARKER_BYTES[n]) { return false; }
            }
            return true;
        }

        #endregion Check Methods

        #region Read Methods

        public (int magicNumber, int width, int height, byte channels, byte colorspace) ReadHeader()
        {
            int magicNumber = Read32();
            int width = Read32();
            int height = Read32();
            byte channels = Read8();
            byte colorspace = Read8();
            return (magicNumber, width, height, channels, colorspace);
        }

        public byte ReadRun()
        {
            byte value = Read8();
            return (byte)((value & QOI_MASK_VALUE) + 1);
        }

        public byte ReadIndex()
        {
            byte value = Read8();
            return (byte)(value & QOI_MASK_VALUE);
        }

        public Color ReadDiff(Color color)
        {
            byte value = Read8();

            int r = ((value & 0b00110000) >> 4) - 2;
            int g = ((value & 0b00001100) >> 2) - 2;
            int b = ((value & 0b00000011) >> 0) - 2;

            color.r += r;
            color.g += g;
            color.b += b;
            return color;
        }

        public Color ReadLuma(Color color)
        {
            byte value_1 = Read8();
            byte value_2 = Read8();

            int dr_dg = ((value_2 & 0b11110000) >> 4) - 8;
            int db_dg = ((value_2 & 0b00001111) >> 0) - 8;

            int differenceGreen = (value_1 & QOI_MASK_VALUE) - 32;
            int differenceRed = dr_dg + differenceGreen;
            int differenceBlue = db_dg + differenceGreen;

            if (differenceGreen < -32 && differenceGreen > 31 || db_dg < -8 && db_dg > 7 || dr_dg < -8 && dr_dg > 7)
            {
                throw new Exception("Invalid luma difference.");
            }

            color.r += differenceRed;
            color.g += differenceGreen;
            color.b += differenceBlue;

            return color;
        }

        public Color ReadRgb(byte alpha)
        {
            byte _ = Read8();
            byte r = Read8();
            byte g = Read8();
            byte b = Read8();
            return new Color(r, g, b, alpha);
        }

        public Color ReadRgba()
        {
            byte _ = Read8();
            byte r = Read8();
            byte g = Read8();
            byte b = Read8();
            byte a = Read8();
            return new Color(r, g, b, a);
        }

        #endregion Read Methods

        #region Write Methods

        public void WriteHeader(int width, int height, byte channels, byte colorspace)
        {
            Write32(HEADER_MAGIC_NUMBER);
            Write32(width);
            Write32(height);
            Write8(channels);
            Write8(colorspace);
        }

        public void WriteEndMarker()
        {
            for (int n = 0; n < END_MARKER_BYTES.Length; n++)
            {
                Write8(END_MARKER_BYTES[n]);
            }
        }

        public void WriteRun(byte run)
        {
            byte value = (byte)(QOI_OP_RUN | (run - 1));
            Write8(value);
        }

        public void WriteIndex(byte hash)
        {
            byte value = (byte)(QOI_OP_INDEX | hash);
            Write8(value);
        }

        public void WriteDiff(Color difference)
        {
            if (!(difference.r >= -2 && difference.r <= 1 &&
                difference.g >= -2 && difference.g <= 1 &&
                difference.b >= -2 && difference.b <= 1))
            {
                throw new Exception("Invalid diff difference.");
            }

            byte value = (byte)(QOI_OP_DIFF
                | ((difference.r + 2) << 4)
                | ((difference.g + 2) << 2)
                | ((difference.b + 2) << 0));
            Write8(value);
        }

        public void WriteLuma(Color difference)
        {
            int dr_dg = difference.r - difference.g;
            int db_dg = difference.b - difference.g;

            if (difference.g < -32 && difference.g > 31 || db_dg < -8 && db_dg > 7 || dr_dg < -8 && dr_dg > 7)
            {
                throw new Exception("Invalid luma difference.");
            }

            byte value_1 = (byte)(QOI_OP_LUMA | (difference.g + 32));
            byte value_2 = (byte)(((dr_dg + 8) << 4) | ((db_dg + 8) << 0));

            Write8(value_1);
            Write8(value_2);
        }

        public void WriteRGB(Color value)
        {
            Write8(QOI_OP_RGB);
            Write8((byte)value.r);
            Write8((byte)value.g);
            Write8((byte)value.b);
        }

        public void WriteRGBA(Color value)
        {
            Write8(QOI_OP_RGBA);
            Write8((byte)value.r);
            Write8((byte)value.g);
            Write8((byte)value.b);
            Write8((byte)value.a);
        }

        #endregion Write Methods

        #region General Methods

        /**
         * Writes an 8-bit integer value to the output buffer.
         * @param {number} value The integer to write.
         * @throws The value is not an integer or larger than 8 bit.
         */
        private void Write8(byte value)
        {
            if (mode == BufferMode.Read)
            {
                throw new InvalidOperationException("Can't write in read mode.");
            }
            buffer[offset] = value;
            offset += 1;
        }

        /**
         * Writes an 32-bit integer value to the output buffer.
         * @param {number} value The integer to write.
         * @throws The value is not an integer.
         */
        private void Write32(int value)
        {
            if (mode == BufferMode.Read)
            {
                throw new InvalidOperationException("Can't write in read mode.");
            }

            buffer[offset + 0] = (byte)((0xff000000 & value) >> 24);
            buffer[offset + 1] = (byte)((0x00ff0000 & value) >> 16);
            buffer[offset + 2] = (byte)((0x0000ff00 & value) >> 8);
            buffer[offset + 3] = (byte)((0x000000ff & value) >> 0);
            offset += 4;
        }

        /**
         * Read an 8-bit integer from the input buffer, but doesn"t increment the offset.
         * @returns A 8-bit integer.
         */
        private byte Peek8()
        {
            if (mode == BufferMode.Write)
            {
                throw new InvalidOperationException("Can't read in write mode.");
            }
            return buffer[offset];
        }

        /**
         * Read an 8-bit integer from the input buffer.
         * @returns A 8-bit integer.
         */
        private byte Read8()
        {
            if (mode == BufferMode.Write)
            {
                throw new InvalidOperationException("Can't read in write mode.");
            }
            byte value = buffer[offset + 0];
            offset += 1;
            return value;
        }

        /**
         * Read an 32-bit integer from the input buffer.
         * @returns A 32-bit integer.
         */
        private int Read32()
        {
            if (mode == BufferMode.Write)
            {
                throw new InvalidOperationException("Can't read in write mode.");
            }
            byte r = buffer[offset + 0];
            byte g = buffer[offset + 1];
            byte b = buffer[offset + 2];
            byte a = buffer[offset + 3];
            offset += 4;
            int value = r << 24 | g << 16 | b << 8 | a << 0;
            return value;
        }

        #endregion General Methods
    }
}
