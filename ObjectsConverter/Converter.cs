using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ObjectsConverter
{
    public static class Converter
    {
        public static async Task<byte[]> FileToByteArrayAsync(string _sourceFile)
        {
            return await Task.Run(() =>
            {
                return FileToByteArray(_sourceFile);
            });
        }
        public static byte[] FileToByteArray(string _sourceFile)
        {
            try
            {
                FileStream stream = File.OpenRead(_sourceFile);
                byte[] build = new byte[stream.Length];

                stream.Read(build, 0, build.Length);
                stream.Close();
                return build;
            }
            catch (ArgumentNullException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
        public static async Task<bool> ByteArrayToFileAsync(byte[] _byte, string _destFile)
        {
            return await Task.Run(() =>
            {
                return ByteArrayToFile(_byte, _destFile);
            });
        }
        public static bool ByteArrayToFile(byte[] _byte, string _destFile)
        {
            try
            {
                using (Stream file = File.OpenWrite(_destFile))
                {
                    file.Write(_byte, 0, _byte.Length);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Generate file SHA256 hash
        public static string GetSHA256(string filepath)
        {
            using (var sha256 = SHA256Managed.Create())
            {
                using (var file = File.OpenRead(filepath))
                {
                    var generatedHash = sha256.ComputeHash(file);
                    return ClearSeparator(BitConverter.ToString(generatedHash));
                }
            }
        }
        private static string ClearSeparator(string hash) => hash.Replace("-", string.Empty);

        //Parse all lines from file to array
        public static List<string> FileToArray(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(nameof(filePath));
            }
            List<string> temp = new List<string>();

            using (var stream = new StreamReader(filePath))
            {
                while (!stream.EndOfStream)
                {
                    temp.Add(stream.ReadLine());
                }
            }
            return temp;
        }

        public static async Task<List<string>> FileToArrayAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                return FileToArray(filePath);
            });
        }
        //Convert any text file to string
        public static string FileToString(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            string tempString = string.Empty;
            using (var stream = new StreamReader(filePath))
            {
                tempString = stream.ReadToEnd();
            }

            return tempString;
        }
        public static async Task<string> FileToStringAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                return FileToString(filePath);
            });
        }

        public static bool SourceToFile<T>(T source, string targetFile) where T : class
        {
            try
            {
                var tempSource = source.ToString();
                File.WriteAllText(targetFile, tempSource);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> SourceToFileAsync<T>(T source, string targetFile) where T : class
        {
            return await Task.Run(() =>
            {
                return SourceToFile(source, targetFile);
            });
        }

        public static Icon ByteArrayToIcon(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return new Icon(ms);
            }
        }

        public static byte[] IconToByteArray(Icon icon)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                icon.Save(ms);
                return ms.ToArray();
            }
        }

        public static ImageSource IconToImageSource(Icon icon)
        {
            if (icon == null)
            {
                return null;
            }
            ImageSource tempImage = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            return tempImage;
        }

        public static byte[] SerializeObject(object obj)
        {
            if (obj == null)
            {
                return null;
            }
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
        public static T DeserializeObject<T>(byte[] rawBytes)
        {
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();

                ms.Write(rawBytes, 0, rawBytes.Length);
                ms.Seek(0, SeekOrigin.Begin);

                return (T)bf.Deserialize(ms);
            }
        }

        #region Convert to icon
        internal static bool ConvertToIcon(Bitmap inputBitmap, Stream output)
        {
            if (inputBitmap == null)
                return false;

            int[] sizes = new int[] { 256 };

            // Generate bitmaps for all the sizes and toss them in streams
            List<MemoryStream> imageStreams = new List<MemoryStream>();
            foreach (int size in sizes)
            {
                Bitmap newBitmap = ResizeImage(inputBitmap, size, size);
                if (newBitmap == null)
                    return false;
                MemoryStream memoryStream = new MemoryStream();
                newBitmap.Save(memoryStream, ImageFormat.Png);
                imageStreams.Add(memoryStream);
            }

            BinaryWriter iconWriter = new BinaryWriter(output);
            if (output == null || iconWriter == null)
                return false;

            int offset = 0;

            // 0-1 reserved, 0
            iconWriter.Write((byte)0);
            iconWriter.Write((byte)0);

            // 2-3 image type, 1 = icon, 2 = cursor
            iconWriter.Write((short)1);

            // 4-5 number of images
            iconWriter.Write((short)sizes.Length);

            offset += 6 + (16 * sizes.Length);

            for (int i = 0; i < sizes.Length; i++)
            {
                // image entry 1
                // 0 image width
                iconWriter.Write((byte)sizes[i]);
                // 1 image height
                iconWriter.Write((byte)sizes[i]);

                // 2 number of colors
                iconWriter.Write((byte)0);

                // 3 reserved
                iconWriter.Write((byte)0);

                // 4-5 color planes
                iconWriter.Write((short)0);

                // 6-7 bits per pixel
                iconWriter.Write((short)32);

                // 8-11 size of image data
                iconWriter.Write((int)imageStreams[i].Length);

                // 12-15 offset of image data
                iconWriter.Write((int)offset);

                offset += (int)imageStreams[i].Length;
            }

            for (int i = 0; i < sizes.Length; i++)
            {
                // write image data
                // png data must contain the whole png data file
                iconWriter.Write(imageStreams[i].ToArray());
                imageStreams[i].Close();
            }

            iconWriter.Flush();

            return true;
        }

        internal static bool ConvertToIcon(Stream input, Stream output)
        {
            Bitmap inputBitmap = (Bitmap)Bitmap.FromStream(input);
            return ConvertToIcon(inputBitmap, output);
        }

        internal static bool ConvertToIcon(string inputPath, string outputPath)
        {
            using (FileStream inputStream = new FileStream(inputPath, FileMode.Open))
            using (FileStream outputStream = new FileStream(outputPath, FileMode.OpenOrCreate))
            {
                return ConvertToIcon(inputStream, outputStream);
            }
        }

        internal static bool ConvertToIcon(Image inputImage, string outputPath)
        {
            using (FileStream outputStream = new FileStream(outputPath, FileMode.OpenOrCreate))
            {
                return ConvertToIcon(new Bitmap(inputImage), outputStream);
            }
        }
        internal static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        #endregion
    }
}
