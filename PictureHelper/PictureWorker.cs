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
        private readonly Action<int, int> _updateProgressFunc;
        private readonly Action<FileInfo> _imageAddedFunc;
        private List<FileInfo> _fileList;

        public PictureWorker(
            string targetDir, 
            string targetDirUnknownDate, 
            Action<string, bool> logFunc, 
            Action<int, int> updateProgressFunc, 
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

        public void ReadImages(List<string> filenames, int with, int height, Action readingFinished)
        {
            Task.Run(() => ReadImageWorker(filenames, with, height, readingFinished));
        }

        public void ReadImageWorker(List<string> filenames, int with, int height, Action readingFinished)
        {
            var count = 0;
            foreach (var filename in filenames)
            {
                try
                {
                    var image = Image.FromFile(filename);
                    var dateAvailable = GetDateTaken(image.PropertyItems, out var dateTaken);
                    var resizedImage = ResizeImage(image, with, height);
                    image.Dispose();
                    var fileInfo = new FileInfo
                    {
                        DateTaken = dateTaken,
                        DateTakenValid = dateAvailable,
                        Filename = filename,
                        Image = resizedImage
                    };
                    _imageAddedFunc(fileInfo);
                }
                catch (Exception ex)
                {
                    Log($"Fehler beim Lesen der Datei '{filename}': {ex.Message}");
                }

                _updateProgressFunc(filenames.Count, ++count);
            }

            readingFinished();
        }

        private Image ResizeImage(Image imgPhoto, int Width, int Height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent;

            var nPercentW = ((float)Width / (float)sourceWidth);
            var nPercentH = ((float)Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = Convert.ToInt32((Width - (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = Convert.ToInt32((Height - (sourceHeight * nPercent)) / 2);
            }

            var destWidth = (int)(sourceWidth * nPercent);
            var destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(Width, Height,
                PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode =
                InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }

        private readonly int[] _dateTakenIds = { 0x9003, 0x9004 };

        private bool GetDateTaken(PropertyItem[] imagePropertyItems, out DateTime dateTaken)
        {
            dateTaken = DateTime.Now;
            var prop = imagePropertyItems.FirstOrDefault(p => _dateTakenIds.Contains(p.Id) && p.Type == 2);
            if (prop != null)
            {
                var dateStr = Encoding.ASCII.GetString(TrimByteArray(prop.Value, prop.Len));
                // Erwarteter String: 2012:04:28 17:46:41
                if (dateStr.Length < 19)
                {
                    Log($"Ungültiges Aufnahmedatum: '{dateStr}'");
                    return false;
                }

                dateTaken = DateTime.ParseExact(dateStr, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);

                //var year = int.Parse(dateStr.Substring(0, 4));
                //var month = int.Parse(dateStr.Substring(5, 2));
                //var day = int.Parse(dateStr.Substring(8, 2));
                //var hour = int.Parse(dateStr.Substring(11, 2));
                //var min = int.Parse(dateStr.Substring(14, 2));
                //var sec = int.Parse(dateStr.Substring(17, 2));
                //dateTaken = new DateTime(year, month, day, hour, min, sec);
                return true;
            }

            return false;
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
            var count = 0;
            foreach (var fileInfo in _fileList)
            {
                var destFileName = CreateDestFileName(fileInfo);
                try
                {
                    if (File.Exists(destFileName))
                    {
                        Log($"Bild existiert bereits:  '{destFileName}'");
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

                _updateProgressFunc(_fileList.Count, ++count);
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