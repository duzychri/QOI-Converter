![QOI Logo](https://qoiformat.org/qoi-logo.svg)

# QOI Converter

A pure C# implementation of converter for the [Quite OK Image Format](https://github.com/phoboslab/qoi).

## How to use

### Encoding

```csharp
Image image = Image.FromFile("...");
byte[] pixels = ExtractPixelsFromImage(image);
byte[] qoiData = QoiConverter.Encode(pixels, image.Width, image.Height, Channels.Rgba, Colorspace.Linear);
```

### Decoding

```csharp
byte[] qoiBytes = File.ReadAllBytes("...");
(byte[] bytes, int width, int height, Channels channels, Colorspace colorspace) = QoiConverter.Decode(qoiBytes);
```