using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class CreateIcon
{
    private static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };

    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: CreateIcon <input.png> <output.ico>");
            return 1;
        }

        using (Image source = Image.FromFile(args[0]))
        {
            byte[][] images = new byte[Sizes.Length][];
            for (int i = 0; i < Sizes.Length; i++)
                images[i] = RenderPng(source, Sizes[i]);

            using (FileStream stream = new FileStream(args[1], FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)Sizes.Length);

                int offset = 6 + (16 * Sizes.Length);
                for (int i = 0; i < Sizes.Length; i++)
                {
                    writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
                    writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(images[i].Length);
                    writer.Write(offset);
                    offset += images[i].Length;
                }

                for (int i = 0; i < images.Length; i++)
                    writer.Write(images[i]);
            }
        }
        return 0;
    }

    private static byte[] RenderPng(Image source, int size)
    {
        using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        using (MemoryStream stream = new MemoryStream())
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, size, size));
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }
}
