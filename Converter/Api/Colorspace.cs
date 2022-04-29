namespace Qoi
{
    /// <summary>
    /// The colorspace of the image.
    /// </summary>
    public enum Colorspace : byte
    {
        /// <summary>
        /// Gamma scaled RGB channels and a linear alpha channel.
        /// </summary>
        sRGB = 0,
        /// <summary>
        /// All channels are linear.
        /// </summary>
        Linear = 1
    }
}