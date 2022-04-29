namespace Qoi
{
    /// <summary>
    /// The channel count of the image.
    /// </summary>
    public enum Channels : byte
    {
        /// <summary>
        /// Three channels; red, green and blue.
        /// </summary>
        Rgb = 3,
        /// <summary>
        /// Four channels; red, green, blue and alpha.
        /// </summary>
        Rgba = 4
    }
}