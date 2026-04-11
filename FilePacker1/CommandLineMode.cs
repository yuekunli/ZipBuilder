using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipBuilder
{
    public class CommandLineMode
    {
        public void Run(string[] args)
        {
            try
            {
                if (args.Length < 3)
                {
                    ShowUsage();
                    return;
                }

                var inputs = new List<string>();

                string selectedOS = args[0];
                string outputPath = args[1];
                for (int i = 2; i < args.Length; ++i)
                {
                    inputs.Add(args[i]);
                }

                CreateDeterministicZip(selectedOS, outputPath, inputs);

                Console.WriteLine("Zip created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
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

        private List<FileEntry> GetAllFiles(List<string> inputs, string selectedOS)
        {
            var result = new List<FileEntry>();

            foreach (var path in inputs)
            {
                if (File.Exists(path))
                {
                    string zipPath = Path.GetFileName(path);
                    if (selectedOS == "Windows")
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
                        if (selectedOS == "Windows")
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

        private void CreateDeterministicZip(string selectedOS, string zipPath, List<string> inputs)
        {
            var allFiles = GetAllFiles(inputs, selectedOS);

            var sorted = allFiles.OrderBy(f => f.ZipPath, StringComparer.Ordinal).ToList();

            using (var fs = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var file in sorted)
                {
                    AddDeterministicEntry(archive, file.FullPath, file.ZipPath);
                }
            }
        }

        private void AddDeterministicEntry(ZipArchive archive, string filePath, string entryName)
        {
            var entry = archive.CreateEntry(entryName.Replace("\\", "/"), CompressionLevel.Optimal);

            entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            entry.ExternalAttributes = 0;


            using (var entryStream = entry.Open())
            using (var fileStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.SequentialScan))
            {
                fileStream.CopyTo(entryStream, 1024 * 1024);
            }
        }

        private static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("filepacker.exe Windows|Linux output.zip input1 input2 ...");
        }
    }
}
