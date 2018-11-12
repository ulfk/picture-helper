using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
        private readonly Action<List<string>> _copyFinishedFunc;
        private List<FileInfo> _fileList;

        public PictureWorker(
            string targetDir, 
            string targetDirUnknownDate, 
            Action<string, bool> logFunc, 
            Action<int, int> updateProgressFunc, 
            Action<List<string>> copyFinishedFunc)
        {
            _targetDir = targetDir;
            _targetDirUnknownDate = targetDirUnknownDate;
            _logFunc = logFunc;
            _updateProgressFunc = updateProgressFunc;
            _copyFinishedFunc = copyFinishedFunc;
        }

        public void StartCopyFiles(List<FileInfo> fileList)
        {
            _fileList = fileList;
            Task.Run((Action)CopyFiles);
        }

        public FileInfo ReadImage(string filename, int with, int height)
        {
            var image = Image.FromFile(filename);
            var dateAvailable = GetDateTaken(image.PropertyItems, out var dateTaken);
            //TODO resize image to display size, rotate if needed, preserve aspect ratio
            var resizedImage = ResizeImage(image, with, height);
            image.Dispose();
            return new FileInfo{DateTaken = dateTaken, DateTakenValid = dateAvailable, Filename = filename, Image = resizedImage };
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

                var year = int.Parse(dateStr.Substring(0, 4));
                var month = int.Parse(dateStr.Substring(5, 2));
                var day = int.Parse(dateStr.Substring(8, 2));
                var hour = int.Parse(dateStr.Substring(11, 2));
                var min = int.Parse(dateStr.Substring(14, 2));
                var sec = int.Parse(dateStr.Substring(17, 2));
                dateTaken = new DateTime(year, month, day, hour, min, sec);
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

        private void CopyFiles()
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

            _copyFinishedFunc(skippedFiles);
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