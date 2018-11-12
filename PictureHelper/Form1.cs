using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PictureHelper.Properties;

namespace PictureHelper
{
    public partial class Form1 : Form
    {
        private readonly List<FileInfo> _fileList = new List<FileInfo>();
        private readonly string _targetDir = Settings.Default.TargetDir;
        private readonly string _targetDirUnknownDate = Settings.Default.TargetDirUnknownDate;

        private bool _DropIsEnabled = true;

        private const int DeleteKey = 0x2E;

        private readonly PictureWorker _pictureWorker;

        public Form1()
        {
            InitializeComponent();
            var directoriesValid = CheckTargetDirectoriesExist();
            if (directoriesValid)
            {
                _pictureWorker = new PictureWorker(_targetDir, _targetDirUnknownDate, Log, UpdateProgress, CopyingFinished);
            }
        }

        public void Log(string text, bool isError = false)
        {
            InvokeIfRequired(textBoxOutput, (MethodInvoker)delegate
            {
                textBoxOutput.AppendText($"{(isError ? "ERROR" : "INFO ")} - {text}{Environment.NewLine}");
            });
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            EnableDisableUI(false);
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddNewFiles(files);
            ResetProgress();
            EnableDisableUI(true);
        }

        private void EnableDisableUI(bool enable)
        {
            InvokeIfRequired(this, (MethodInvoker)delegate
            {
                buttonExecCopyAndSort.Enabled = enable;
                buttonClearList.Enabled = enable;
            });
            _DropIsEnabled = enable;
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
                    var fileListItem = _pictureWorker.ReadImage(file, imageList.ImageSize.Width, imageList.ImageSize.Height);
                    imageList.Images.Add(fileListItem.Image);
                    var item = CreateListViewItem(imageList.Images.Count - 1, fileListItem);
                    fileListView.Items.Add(item);
                    _fileList.Add(fileListItem);
                    Log($"Datei hinzugefügt: {file}");
                }
                catch (Exception ex)
                {
                    Log($"Fehler bei Verarbeitung der Datei '{file}': {ex.Message}", true);
                }
            }
        }

        private ListViewItem CreateListViewItem(int imageIndex, FileInfo fileInfoItem)
        {
            return new ListViewItem
            {
                ImageIndex = imageIndex,
                Text = fileInfoItem.DateTakenValid
                    ? fileInfoItem.DateTaken.ToString("yyyy-MM-dd")
                    : "DATUM FEHLT",
                Name = fileInfoItem.Filename,
                ToolTipText = fileInfoItem.Filename
            };
        }

        private void Clear()
        {
            _fileList.Clear();
            InvokeIfRequired(fileListView, (MethodInvoker)delegate ()
            {
                fileListView.Items.Clear();
            });
            imageList.Images.Clear();
            //textBoxOutput.Text = string.Empty;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (_DropIsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void buttonClearList_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void buttonExecCopyAndSort_Click(object sender, EventArgs e)
        {
            EnableDisableUI(false);
            _pictureWorker.StartCopyFiles(_fileList);
        }

        public void CopyingFinished(List<string> skippedFiles)
        {
            var filesToCopy = _fileList.Count;
            var filesSkipped = skippedFiles.Count;
            Clear();
            if (filesSkipped > 0)
            {
                ResetProgress();
                InvokeIfRequired(progressBarFileCopy, (MethodInvoker)delegate
                {
                    AddNewFiles(skippedFiles);
                });
                Log($"{(filesToCopy - filesSkipped)} Dateien kopiert, {filesSkipped} übersprungen. Siehe vorherige Ausgaben für Details");
            }
            else
            {
                Log($"{filesToCopy} Dateien kopiert.");
            }
            EnableDisableUI(true);
        }

        public void UpdateProgress(int maxCount, int currentCount)
        {
            InvokeIfRequired(progressBarFileCopy, (MethodInvoker)delegate
            {
                progressBarFileCopy.Minimum = 0;
                progressBarFileCopy.Maximum = maxCount;
                progressBarFileCopy.Value = currentCount;
            });
        }

        private void InvokeIfRequired(Control target, Delegate methodToInvoke)
        {
            if (target.InvokeRequired)
            {
                target.Invoke(methodToInvoke);
            }
            else
            {
                methodToInvoke.DynamicInvoke();
            }
        }

        private void ResetProgress()
        {
            InvokeIfRequired(progressBarFileCopy, (MethodInvoker)delegate
            {
                progressBarFileCopy.Value = 0;
            });
        }

        private void fileListView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == DeleteKey)
            {
                RemoveSelectedFilesFromList();
            }
        }

        private void RemoveSelectedFilesFromList()
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

        private bool CheckTargetDirectoriesExist()
        {
            var directoriesValid = DirectoryExists(_targetDir) && DirectoryExists(_targetDirUnknownDate);
            buttonExecCopyAndSort.Enabled = directoriesValid;
            _DropIsEnabled = directoriesValid;
            return directoriesValid;
        }

        private bool DirectoryExists(string dir)
        {
            var exists = Directory.Exists(dir);
            if (exists)
            {
                Log($"Verzeichnis existiert:  {dir}");
            }
            else
            {
                Log($"Verzeichnis wurde NICHT gefunden:  {dir}", true);
            }

            return exists;
        }
    }
}
