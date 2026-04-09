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

        private void btnCreateZip_Click(object sender, EventArgs e)
        {
            if (selectedPaths.Count == 0)
            {
                MessageBox.Show("No files or folders selected.");
                return;
            }

            saveFileDialog1.Filter = "Zip files (*.zip)|*.zip";
            saveFileDialog1.FileName = "archive.zip";

            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                //var allFiles = GetAllFiles();

                //progressBar1.Value = 0;
                //progressBar1.Maximum = allFiles.Count;

                lblStatus.Text = "Creating zip ...";

                //using (ZipArchive archive = ZipFile.Open(saveFileDialog1.FileName, ZipArchiveMode.Create))
                //{
                //    foreach (var fileInfo in allFiles)
                //    {
                //        archive.CreateEntryFromFile(fileInfo.FullPath, fileInfo.ZipPath, CompressionLevel.Optimal);

                //        progressBar1.Value++;

                //        lblStatus.Text = $"Processing {progressBar1.Value} / {progressBar1.Maximum}";
                //        Application.DoEvents(); // keep UI responsive
                //    }
                //}

                CreateDeterministicZip(saveFileDialog1.FileName);

                lblStatus.Text = "Done";
                MessageBox.Show("Zip file created successfully!", "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void AddFileToZip(ZipArchive archive, string filePath, string entryName)
        {
            archive.CreateEntryFromFile(filePath, entryName, CompressionLevel.Optimal);
        }

        private void AddDirectoryToZip(ZipArchive archive, string folderPath, string entryName)
        {
            foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(folderPath, file);
                var zipPath = Path.Combine(entryName, relativePath);

                archive.CreateEntryFromFile(file, zipPath, CompressionLevel.Optimal);
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

        private List<FileEntry> GetAllFiles()
        {
            var result = new List<FileEntry>();

            foreach (var path in selectedPaths)
            {
                if (File.Exists(path))
                {
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

        private void CreateDeterministicZip(string zipPath)
        {
            var allFiles = GetAllFiles();

            var sorted = allFiles.OrderBy(f => f.ZipPath, StringComparer.Ordinal).ToList();

            progressBar1.Value = 0;
            progressBar1.Maximum = allFiles.Count;

            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var file in sorted)
                {
                    AddDeterministicEntry(archive, file.FullPath, file.ZipPath);

                    progressBar1.Value++;

                    lblStatus.Text = $"Processing {progressBar1.Value}/{progressBar1.Maximum}";
                    Application.DoEvents(); // keep UI responsive
                }
            }
        }

        private void AddDeterministicEntry(ZipArchive archive, string filePath, string entryName)
        {
            var entry = archive.CreateEntry(entryName.Replace("\\", "/"), CompressionLevel.Optimal);

            entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            entry.ExternalAttributes = 0;


            using (var entryStream = entry.Open())
            using (var fileStream = File.OpenRead(filePath))
            {
                fileStream.CopyTo(entryStream);
            }
        }
    }
}