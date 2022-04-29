using Qoi;
using System.Drawing;
using System.Drawing.Imaging;

public static class Program
{
    public static void Main()
    {
        string projectDirectory = Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName!;
        string filesDirectory = Path.Combine(projectDirectory, "Resources");

        var fileNames = Directory.GetFiles(filesDirectory).Select(f => Path.GetFileName(f).Split('.')[0]).Distinct();

        foreach (string fileName in fileNames)
        {
            string imagePath = Path.Combine(filesDirectory, fileName + ".png");
            string qoiPath = Path.Combine(filesDirectory, fileName + ".qoi");
            TestEncoding(fileName, imagePath, qoiPath);
        }
    }

    private static void TestEncoding(string name, string imagePath, string qoiPath)
    {
        // Encode
        Image image = Image.FromFile(imagePath);
        byte[] pixels = ExtractPixelsFromImage(image);
        byte[] encodedQoiBytes = QoiConverter.Encode(pixels, image.Width, image.Height, Channels.Rgba, Colorspace.Linear);

        // Decode
        byte[] qoiBytes = File.ReadAllBytes(qoiPath);
        var decodedImage = QoiConverter.Decode(qoiBytes);

        // Check Encoding
        bool encodingSuccess = encodedQoiBytes.SequenceEqual(qoiBytes);
        Console.WriteLine(encodingSuccess ? $"Encoding image {name} was successfull!" : $"Encoding image {name} failed!");

        // Check Decoding
        bool decodingSuccess = decodedImage.pixels.SequenceEqual(pixels) && image.Width == decodedImage.width && image.Height == decodedImage.height;
        Console.WriteLine(decodingSuccess ? $"Decoding image {name} was successfull!" : $"Decoding image {name} failed!");

        // Save the results to allow for visual comparison
        File.WriteAllBytes(name + ".qoi", encodedQoiBytes);
        ConvertPixelsToImage(decodedImage.pixels, decodedImage.width, decodedImage.height, (int)decodedImage.channels).Save(name + ".png", ImageFormat.Png);
    }

    private static byte[] ExtractPixelsFromImage(Image image)
    {
        Bitmap bitmap = image as Bitmap ?? new Bitmap(image);
        byte[] pixels = new byte[bitmap.Width * bitmap.Height * 4];
        int pixelsIndex = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                pixels[pixelsIndex++] = pixel.R;
                pixels[pixelsIndex++] = pixel.G;
                pixels[pixelsIndex++] = pixel.B;
                pixels[pixelsIndex++] = pixel.A;
            }
        }
        return pixels;
    }

    private static Image ConvertPixelsToImage(byte[] pixels, int width, int height, int channels)
    {
        Bitmap image = new(width, height);

        int pixelIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte r = pixels[pixelIndex++];
                byte g = pixels[pixelIndex++];
                byte b = pixels[pixelIndex++];
                byte a = channels == 4 ? pixels[pixelIndex++] : (byte)0;
                Color pixel = channels == 4 ? Color.FromArgb(a, r, g, b) : Color.FromArgb(255, r, g, b);
                image.SetPixel(x, y, pixel);
            }
        }

        return image;
    }
}