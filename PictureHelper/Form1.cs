using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PictureHelper.Properties;

namespace PictureHelper
{
    public partial class Form1 : Form
    {
        private readonly List<FileInfo> _fileList = new List<FileInfo>();
        private readonly string _targetDir = Settings.Default.TargetDir;
        private const int DeleteKey = 0x2E;

        public Form1()
        {
            InitializeComponent();
            CheckTargetDirExists();
        }

        private void Log(string text, bool isError = false)
        {
            textBoxOutput.AppendText($"{(isError ? "ERROR" : "INFO ")} - {text}{Environment.NewLine}");
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddNewFiles(files);
        }

        private void AddNewFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (_fileList.Any(f => f.Filename == file))
                {
                    Log($"Bild bereits in der Liste: '{file}'");
                    continue;
                }
                try
                {
                    var image = Image.FromFile(file);
                    var dateAvailable = GetDateTaken(image.PropertyItems, out var dateTaken);
                    imageList.Images.Add(image);
                    var item = new ListViewItem();
                    item.ImageIndex = imageList.Images.Count - 1;
                    item.Text = dateAvailable ? dateTaken.ToString("yyyy-MM-dd") : "DATUM FEHLT";
                    item.Name = file;
                    fileListView.Items.Add(item);
                    _fileList.Add(new FileInfo { Filename = file, DateTaken = dateTaken, DateTakenValid = dateAvailable });
                    Log($"Datei hinzugefügt: {file}");
                }
                catch (Exception ex)
                {
                    Log($"Fehler bei Verarbeitung der Datei '{file}': {ex.Message}", true);
                }
            }
        }

        private void Clear()
        {
            _fileList.Clear();
            fileListView.Items.Clear();
            imageList.Images.Clear();
            textBoxOutput.Text = string.Empty;
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

        private readonly int[] _dateTakenIds = {0x9003,0x9004};

        private bool GetDateTaken(PropertyItem[] imagePropertyItems, out DateTime dateTaken)
        {
            dateTaken = DateTime.Now;
            var prop = imagePropertyItems.FirstOrDefault(p => _dateTakenIds.Contains(p.Id) && p.Type == 2);
            if (prop != null)
            {
                var dateStr = Encoding.ASCII.GetString(TrimByteArray(prop.Value, prop.Len));
                // Erwarteter String: 2012:04:28 17:46:41
                var year = int.Parse(dateStr.Substring(0, 4));
                var month = int.Parse(dateStr.Substring(5, 2));
                var day = int.Parse(dateStr.Substring(8, 2));
                dateTaken = new DateTime(year, month, day);
                return true;
            }

            return false;
        }

        private byte[] TrimByteArray(byte[] bytes, int length)
        {
            return bytes?.Where((b,i) => i< length && b >= 32 && b <= 127).ToArray();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void buttonClearList_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void buttonExecCopyAndSort_Click(object sender, EventArgs e)
        {
            foreach (var fileInfo in _fileList)
            {
                //TODO:
                // check if directory for month exists:
                //    if not, create directory for month
                // copy file to month-directory
            }
        }

        private void fileListView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == DeleteKey)
            {
                var indexList = new List<int>();
                var nameList = new List<string>();
                for (var idx = 0; idx < fileListView.SelectedItems.Count; idx++)
                {
                    var entry = fileListView.SelectedItems[idx];
                    indexList.Add(entry.ImageIndex);
                    nameList.Add(entry.Name);
                    fileListView.Items.Remove(entry);
                    Log($"Bild '{entry.Name}' aus der Liste entfernt.");
                }

                indexList.OrderByDescending(i => i).ToList()
                    .ForEach(i => imageList.Images.RemoveAt(i));
                _fileList.RemoveAll(f => nameList.Contains(f.Filename));
            }
        }

        private void CheckTargetDirExists()
        {
            if (Directory.Exists(_targetDir))
            {
                Log($"Zielverzeichnis:  {_targetDir}");
            }
            else
            {
                Log($"Zielverzeichnis wurde NICHT gefunden:  {_targetDir}", true);
            }
        }
    }
}
