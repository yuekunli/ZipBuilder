using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ZipBuilder
{
    public partial class Form1 : Form
    {
        private List<string> selectedPaths = new List<string>();

        public Form1()
        {
            InitializeComponent();
            radioWindows.Checked = true;
        }

        private string GetSelectedOS()
        {
            if (radioWindows.Checked)
            {
                return "Windows";
            }
            if (radioLinux.Checked)
            {
                return "Linux";
            }

            return "Windows";
        }

        private void btnAddFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        selectedPaths.Add(file);
                        listBoxItems.Items.Add(file);
                    }
                }
            }
        }

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPaths.Add(dialog.SelectedPath);
                    listBoxItems.Items.Add(dialog.SelectedPath);
                }
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            var selected = new List<object>();

            foreach (var item in listBoxItems.SelectedItems)
            {
                selected.Add(item);
            }

            foreach (var item in selected)
            {
                selectedPaths.Remove(item.ToString());
                listBoxItems.Items.Remove(item);
            }
        }

        private void btnReset_Click(Object sender, EventArgs e)
        {
            selectedPaths.Clear();

            listBoxItems.Items.Clear();
            lblStatus.Text = "Ready";
            progressBar1.Value = 0;
            radioWindows.Checked = true;
            radioLinux.Checked = false;
        }

        private async void btnCreateZip_Click(object sender, EventArgs e)
        {
            try
            {
                if (selectedPaths.Count == 0)
                {
                    MessageBox.Show("No files or folders selected.");
                    return;
                }

                btnCreateZip.Enabled = false;
                btnAddFiles.Enabled = false;
                btnAddFolder.Enabled = false;
                btnRemove.Enabled = false;
                btnReset.Enabled = false;
                lblStatus.Text = "Creating zip ...";

                using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
                {
                    saveFileDialog1.Filter = "content files (*.content)|*.content";
                    saveFileDialog1.FileName = ".content";

                    if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                        return;

                    //progressBar1.MarqueeAnimationSpeed = 100;

                    var progressUpdater = new Progress<int>(value =>
                    {
                        //if (value < progressBar1.Maximum)
                        //{
                            progressBar1.Value = value;
                            progressBar1.Update();
                            //lblStatus.Text = $"Processed {progressBar1.Value}/{progressBar1.Maximum}";
                        //}
                        //if (progressBar1.Value == progressBar1.Maximum)
                        //{
                        //    progressBar1.Refresh();
                        //    await Task.Yield();
                        //    lblStatus.Text = "Done";
                        //    lblStatus.Update();
                        //}
                    });

                    //var progressMaxSetter = new Progress<int>(value => { progressBar1.Maximum = value; });
                    progressBar1.Maximum = 100;
                    progressBar1.Value = 0;
                    int totalFileCount = 0;
                    var labelProgressMaxSetter = new Progress<int>(value => { totalFileCount = value; });
                    var lableProgressUpdater = new Progress<int>(value => { lblStatus.Text = $"Processing {value} / {totalFileCount}"; });

                    //lblStatus.Text = "Start processing ...";

                    await CreateDeterministicZip(saveFileDialog1.FileName, progressUpdater, labelProgressMaxSetter, lableProgressUpdater);

                    progressBar1.Value = 100;
                    progressBar1.Refresh();
                    await Task.Yield();
                    lblStatus.Text = "Done";
                    lblStatus.Update();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                btnCreateZip.Enabled = true;
                btnAddFiles.Enabled = true;
                btnAddFolder.Enabled = true;
                btnRemove.Enabled = true;
                btnReset.Enabled = true;
            }
        }

        private string GetRelativePath(string baseDir, string fullPath)
        {
            Uri baseUri = new Uri(baseDir.EndsWith("\\") ? baseDir : baseDir + "\\");
            Uri fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(
                baseUri.MakeRelativeUri(fullUri)
                .ToString()
                .Replace('/', '\\'));
        }

        private class FileEntry
        {
            public string FullPath { get; set; }
            public string ZipPath { get; set; }
        }

        private List<FileEntry> GetAllFiles(ref long totalSize)
        {
            var result = new List<FileEntry>();
            totalSize = 0;
            foreach (var path in selectedPaths)
            {
                if (File.Exists(path))
                {
                    FileInfo info = new FileInfo(path);
                    totalSize += info.Length;
                    string zipPath = Path.GetFileName(path);
                    if (GetSelectedOS() == "Windows")
                    {
                        zipPath = zipPath.ToLowerInvariant();
                    }
                    result.Add(new FileEntry
                    {
                        FullPath = path,
                        ZipPath = zipPath
                    });
                }
                else if (Directory.Exists(path))
                {
                    var baseName = Path.GetFileName(path);

                    foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        FileInfo info = new FileInfo(file);
                        totalSize += info.Length;
                        var relative = GetRelativePath(path, file);
                        string zipPath = Path.Combine(baseName, relative);
                        if (GetSelectedOS() == "Windows")
                        {
                            zipPath = zipPath.ToLowerInvariant();
                        }
                        result.Add(new FileEntry
                        {
                            FullPath = file,
                            ZipPath = zipPath
                        });
                    }
                }
            }
            return result;
        }

        private async Task CreateDeterministicZip(string zipPath, IProgress<int> progressUpdater, IProgress<int> lblMaxSetter, IProgress<int> lblUpdater)
        {
            long totalSize = 0;
            var allFiles = GetAllFiles(ref totalSize);
            var sorted = allFiles.OrderBy(f => f.ZipPath, StringComparer.Ordinal).ToList();
            //progressMaxSetter.Report(allFiles.Count);
            lblMaxSetter.Report(allFiles.Count);
            progressUpdater.Report(0);
            int fileCounter = 0;
            long progressSoFar = 0;
            long progressAccumulateForMinMetric = 0;
            long minProgressMetric = totalSize / 100;
            byte[] buffer = new byte[1024 * 1024];
            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var file in sorted)
                {
                    //await AddDeterministicEntry(archive, file.FullPath, file.ZipPath, minProgressMetric, ref progressSoFar);

                    lblUpdater.Report(++fileCounter);

                    var entry = archive.CreateEntry(file.ZipPath.Replace("\\", "/"), CompressionLevel.Optimal);
                    entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    entry.ExternalAttributes = 0;

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(
                            file.FullPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            1024 * 1024,
                            FileOptions.SequentialScan))
                    {
                        int bytesRead = 0;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await entryStream.WriteAsync(buffer, 0, bytesRead);
                            progressAccumulateForMinMetric += bytesRead;
                            if (progressAccumulateForMinMetric > minProgressMetric)
                            {
                                progressSoFar += progressAccumulateForMinMetric;
                                progressUpdater.Report((int)(progressSoFar / minProgressMetric));
                                //progressSoFar = (progressSoFar / minProgressMetric) * minProgressMetric;
                                progressAccumulateForMinMetric = progressSoFar % minProgressMetric;
                                progressSoFar -= progressAccumulateForMinMetric;
                            }
                        }
                    }
                }
            }
        }

        private async Task AddDeterministicEntry(ZipArchive archive, string filePath, string entryName)
        {
            var entry = archive.CreateEntry(entryName.Replace("\\", "/"), CompressionLevel.Optimal);

            entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            entry.ExternalAttributes = 0;

            using (var entryStream = entry.Open())
            //using (var fileStream = File.OpenRead(filePath))
            using (var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.SequentialScan))
            {
                await fileStream.CopyToAsync(entryStream, 1024 * 1024);
            }
        }
    }
}