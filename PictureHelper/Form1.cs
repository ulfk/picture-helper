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

        private bool _dropIsEnabled = true;

        private const int DeleteKey = 0x2E;

        private readonly PictureWorker _pictureWorker;

        public Form1()
        {
            InitializeComponent();
            var directoriesValid = CheckTargetDirectoriesExist();
            if (directoriesValid)
            {
                _pictureWorker = new PictureWorker(_targetDir, _targetDirUnknownDate, Log, UpdateProgress, ImageAdded);
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
            EnableDisableUi(false);
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddNewFiles(files);
            ResetProgress();
            EnableDisableUi(true);
        }

        private void EnableDisableUi(bool enable)
        {
            InvokeIfRequired(this, (MethodInvoker)delegate
            {
                buttonExecCopyAndSort.Enabled = enable;
                buttonClearList.Enabled = enable;
            });
            _dropIsEnabled = enable;
        }

        private void AddNewFiles(IEnumerable<string> files)
        {
            EnableDisableUi(false);

            // skip already added files
            var filesToRead = new List<string>();
            foreach (var file in files)
            {
                if (_fileList.Any(f => f.Filename == file))
                {
                    Log($"Bild bereits in der Liste: '{file}'");
                    continue;
                }
                filesToRead.Add(file);
            }

            InitProgress(filesToRead.Count);
            _pictureWorker.ReadImages(
                filesToRead, 
                imageList.ImageSize.Width, 
                imageList.ImageSize.Height,
                () => EnableDisableUi(true));
        }

        public void ImageAdded(FileInfo fileInfo)
        {
            InvokeIfRequired(this, (MethodInvoker)delegate
            {
                try
                {
                    var item = CreateListViewItem(fileInfo);
                    imageList.Images.Add(item.ImageKey, fileInfo.Image);
                    fileListView.Items.Add(item);
                    fileInfo.ImageKey = item.ImageKey;
                    _fileList.Add(fileInfo);
                    Log($"Datei zur Liste hinzugefügt: {fileInfo.Filename}");
                }
                catch (Exception ex)
                {
                    Log($"Fehler beim Verarbeiten der Datei '{fileInfo.Filename}': {ex.Message}", true);
                }
            });
        }

        private ListViewItem CreateListViewItem(FileInfo fileInfoItem)
        {
            return new ListViewItem
            {
                ImageKey = Guid.NewGuid().ToString(),
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
            InvokeIfRequired(fileListView, (MethodInvoker)delegate
            {
                fileListView.Items.Clear();
            });
            imageList.Images.Clear();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (_dropIsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop))
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
            EnableDisableUi(false);
            InitProgress(_fileList.Count);
            _pictureWorker.StartCopyFiles(_fileList, CopyingFinished);
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
            EnableDisableUi(true);
        }

        private void InitProgress(int maxCount)
        {
            progressBarFileCopy.Minimum = 0;
            progressBarFileCopy.Maximum = maxCount;
        }

        public void UpdateProgress(int currentCount)
        {
            InvokeIfRequired(progressBarFileCopy, (MethodInvoker)delegate
            {
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
            if (fileListView.SelectedItems.Count == 0)
            {
                return;
            }

            foreach (ListViewItem selectedItem in fileListView.SelectedItems)
            {
                fileListView.Items.Remove(selectedItem);
                var key = selectedItem.ImageKey;
                // only delete when no longer in use
                if (fileListView.Items.Cast<ListViewItem>().All(x => x.ImageKey != key))
                {
                    imageList.Images.RemoveByKey(key);
                }

                _fileList.RemoveAll(f => f.ImageKey == key);
                Log($"Datei aus Liste entfernt: '{selectedItem.Name}'");
            }
        }

        private bool CheckTargetDirectoriesExist()
        {
            var directoriesValid = DirectoryExists(_targetDir) && DirectoryExists(_targetDirUnknownDate);
            buttonExecCopyAndSort.Enabled = directoriesValid;
            _dropIsEnabled = directoriesValid;
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
