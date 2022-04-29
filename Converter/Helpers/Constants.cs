namespace Qoi
{
    internal static class Constants
    {
        public const int HEADER_SIZE = 14;
        /// <remarks>Seven 0 bytes and one 1 byte.</remarks>
        public const int END_MARKER_SIZE = 8;

        public const byte QOI_OP_INDEX = 0b00000000;
        public const byte QOI_OP_DIFF = 0b01000000;
        public const byte QOI_OP_LUMA = 0b10000000;
        public const byte QOI_OP_RUN = 0b11000000;

        public const byte QOI_OP_RGB = 0b11111110;
        public const byte QOI_OP_RGBA = 0b11111111;

        public const byte QOI_MASK = 0b11000000;
        public const byte QOI_MASK_VALUE = 0b00111111;

        public static readonly byte[] END_MARKER_BYTES = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };
        public static readonly int HEADER_MAGIC_NUMBER = 'q' << 24 | 'o' << 16 | 'i' << 8 | 'f' << 0;
    }
}
