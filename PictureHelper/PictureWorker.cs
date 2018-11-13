using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureHelper
{
    public class PictureWorker
    {
        private readonly string _targetDir;
        private readonly string _targetDirUnknownDate;
        private readonly Action<string, bool> _logFunc;
        private readonly Action<int> _updateProgressFunc;
        private readonly Action<FileInfo> _imageAddedFunc;
        private List<FileInfo> _fileList;

        public PictureWorker(
            string targetDir, 
            string targetDirUnknownDate, 
            Action<string, bool> logFunc, 
            Action<int> updateProgressFunc, 
            Action<FileInfo> imageAddedFunc)
        {
            _targetDir = targetDir;
            _targetDirUnknownDate = targetDirUnknownDate;
            _logFunc = logFunc;
            _updateProgressFunc = updateProgressFunc;
            _imageAddedFunc = imageAddedFunc;
        }

        public void StartCopyFiles(List<FileInfo> fileList, Action<List<string>> copyingFinishedFunc)
        {
            _fileList = fileList;
            Task.Run(() => CopyFiles(copyingFinishedFunc));
        }

        public void ReadImages(List<string> fileNames, int with, int height, Action readingFinished)
        {
            Task.Run(() => ReadImageWorker(fileNames, with, height, readingFinished));
        }

        public void ReadImageWorker(List<string> fileNames, int with, int height, Action readingFinished)
        {
            var fileCounter = 0;
            foreach (var filename in fileNames)
            {
                try
                {
                    using (var image = Image.FromFile(filename))
                    {
                        var dateAvailable = GetDateTaken(image.PropertyItems, out var dateTaken);
                        var resizedImage = ResizeImage(image, with, height);
                        var fileInfo = new FileInfo
                        {
                            DateTaken = dateTaken,
                            DateTakenValid = dateAvailable,
                            Filename = filename,
                            Image = resizedImage
                        };
                        _imageAddedFunc(fileInfo);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Fehler beim Lesen der Datei '{filename}': {ex.Message}");
                }

                _updateProgressFunc(++fileCounter);
            }

            readingFinished();
        }

        private Image ResizeImage(Image image, int width, int height)
        {
            var sourceWidth = image.Width;
            var sourceHeight = image.Height;
            var destX = 0;
            var destY = 0;
            float ratio;

            var ratioWidth = ((float)width / (float)sourceWidth);
            var ratioHeight = ((float)height / (float)sourceHeight);
            if (ratioHeight < ratioWidth)
            {
                ratio = ratioHeight;
                destX = Convert.ToInt32((width - (sourceWidth * ratio)) / 2);
            }
            else
            {
                ratio = ratioWidth;
                destY = Convert.ToInt32((height - (sourceHeight * ratio)) / 2);
            }

            var destWidth = (int)(sourceWidth * ratio);
            var destHeight = (int)(sourceHeight * ratio);

            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            bitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

                graphics.DrawImage(image,
                    new Rectangle(destX, destY, destWidth, destHeight),
                    new Rectangle(0, 0, sourceWidth, sourceHeight),
                    GraphicsUnit.Pixel);
            }

            return bitmap;
        }

        private readonly int[] _dateTakenIds = { 0x9003, 0x9004 };

        private bool GetDateTaken(PropertyItem[] imagePropertyItems, out DateTime dateTaken)
        {
            dateTaken = DateTime.Now;
            var prop = imagePropertyItems.FirstOrDefault(p => _dateTakenIds.Contains(p.Id) && p.Type == 2);
            if (prop == null)
            {
                return false;
            }

            var dateStr = Encoding.ASCII.GetString(TrimByteArray(prop.Value, prop.Len));
            // Expecting string like this:  2012:04:28 17:46:41
            if (dateStr.Length < 19)
            {
                Log($"Ungültiges Aufnahmedatum: '{dateStr}'");
                return false;
            }

            dateTaken = DateTime.ParseExact(dateStr, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            return true;
        }

        private byte[] TrimByteArray(byte[] bytes, int length)
        {
            return bytes?.Where((b, i) => i < length && b >= 32 && b <= 127).ToArray();
        }

        //private void LogImageProperties(PropertyItem[] imagePropertyItems)
        //{
        //    //.Where(p => DateTakenIds.Concat(p.Id)
        //    foreach (var prop in imagePropertyItems)
        //    {
        //        string value = "";
        //        switch (prop.Type)
        //        {
        //            case 2:
        //                value = Encoding.ASCII.GetString(TrimByteArray(prop.Value, prop.Len));
        //                break;
        //        }
        //        Log($"  {prop.Id:X}  {prop.Type}  {prop.Len}  {value}");
        //    }
        //}

        private void Log(string text, bool isError = false)
        {
            _logFunc(text, isError);
        }

        private void CopyFiles(Action<List<string>> copyFinishedFunc)
        {
            var skippedFiles = new List<string>();
            var fileCounter = 0;
            foreach (var fileInfo in _fileList)
            {
                var destFileName = CreateDestFileName(fileInfo);
                try
                {
                    if (File.Exists(destFileName))
                    {
                        Log($"Ziel-Datei existiert bereits:  '{destFileName}'");
                        skippedFiles.Add(fileInfo.Filename);
                        continue;
                    }
                    Log($"Kopiere  '{fileInfo.Filename}'  nach  '{destFileName}'");
                    File.Copy(fileInfo.Filename, destFileName);
                }
                catch (Exception ex)
                {
                    Log($"Fehler beim Kopieren von '{fileInfo.Filename}' nach '{destFileName}': {ex.Message}");
                }

                _updateProgressFunc(++fileCounter);
            }

            copyFinishedFunc(skippedFiles);
        }
        
        private string CreateDestFileName(FileInfo fileInfo)
        {
            var destDir = GetTargetDirEnsureExisting(fileInfo);
            var filename = Path.GetFileName(fileInfo.Filename);
            var datePrefix = GetDatePrefix(fileInfo);
            return Path.Combine(destDir, $"{datePrefix}{filename}");
        }

        private string GetDatePrefix(FileInfo fileInfo)
        {
            var result = string.Empty;
            if (fileInfo.DateTakenValid)
            {
                result = $"{fileInfo.DateTaken:yyyy-MM-dd_HH-mm}_";
            }

            return result;
        }

        private string GetTargetDirEnsureExisting(FileInfo fileInfo)
        {
            var destDir = _targetDirUnknownDate;
            if (fileInfo.DateTakenValid)
            {
                var dir = Path.Combine(_targetDir, $"{fileInfo.DateTaken:yyyy-MM}");
                if (CreateDirectoryIfNotExists(dir))
                {
                    destDir = dir;
                }
            }

            return destDir;
        }

        private bool CreateDirectoryIfNotExists(string dir)
        {
            var result = true;
            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    Log($"Verzeichnis angelegt:  {dir}");
                }
                catch (Exception ex)
                {
                    Log($"Fehler beim Anlegen des Verzeichnisses '{dir}': {ex.Message}", true);
                    result = false;
                }
            }

            return result;
        }
    }
}