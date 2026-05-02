using Mono.Options;
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
            
            //if (args.Length < 3)
            //{
            //    ShowUsage();
            //    return;
            //}

            //var inputs = new List<string>();

            //string selectedOS = args[0];
            //string outputPath = args[1];
            //for (int i = 2; i < args.Length; ++i)
            //{
            //    inputs.Add(args[i]);
            //}


            bool isQuiet = false;
            bool isVerbose = false;
            string targetOS = null;
            string outputPath = null;
            List<string> inputFiles = new List<string>();
            bool isShowHelp = false;

            var p = new OptionSet()
            {
                { "q|quiet", "Suppress messages", v => isQuiet = v != null },
                { "v|verbose", "Detailed progress message", v => isVerbose = v != null },
                { "t|target=", "Target OS (Windows or Linux/macOS)", v => targetOS = v },
                { "o|output=", "Path to output file", v => outputPath = v },
                { "h|help", "Show help", v => isShowHelp = v != null }
            };

            try
            {
                inputFiles = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Command Parsing Error: ");
                Console.Write(e.Message);
                return;
            }

            try
            {
                Console.WriteLine();
                Console.WriteLine();

                if (isShowHelp)
                {
                    ShowHelp();
                    return;
                }

                if (outputPath == null)
                {
                    Console.WriteLine("output path is missing");
                    return;
                }

                if (inputFiles.Count == 0)
                {
                    Console.WriteLine("input file(s) not provided");
                    return;
                }

                if (targetOS == null)
                {
                    Console.WriteLine("target OS is missing");
                    return;
                }

                CreateDeterministicZip(targetOS, isVerbose, outputPath, inputFiles);

                Console.WriteLine("Zip created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private bool shouldNormalizePath(string selectedOS)
        {
            // string.Equals(selectedOS, "windows", StringComparison.OrdinalIgnoreCase)

            if ("windows".StartsWith(selectedOS, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            if ("linux".StartsWith(selectedOS, StringComparison.OrdinalIgnoreCase)) 
            { 
                return false; 
            }
            
            if ("macOS".StartsWith(selectedOS, StringComparison.OrdinalIgnoreCase)) 
            { 
                return false; 
            }
            
            throw new Exception("invalid target operating system: " + selectedOS);
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
                    if (shouldNormalizePath(selectedOS))
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
                        if (shouldNormalizePath(selectedOS))
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

        private void CreateDeterministicZip(string selectedOS, bool isVerbose, string zipPath, List<string> inputs)
        {
            var allFiles = GetAllFiles(inputs, selectedOS);

            var sorted = allFiles.OrderBy(f => f.ZipPath, StringComparer.Ordinal).ToList();
            // f.ZipPath is this file's relative path inside the zip

            using (var outputFs = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(outputFs, ZipArchiveMode.Create))
            {
                foreach (var file in sorted)
                {
                    if (isVerbose) Console.WriteLine("Processing: " + file.FullPath);
                    AddDeterministicEntry(archive, file.FullPath, file.ZipPath);
                }
            }
            ZipEnforcer.SetGeneralPurposeBit(zipPath);
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

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("contentpacker.exe [-verbose] -target [windows|linux|macos] -output output.zip  -input  input1  input2 ...");
            Console.WriteLine();
            Console.WriteLine("single letter options:");
            Console.WriteLine();
            Console.WriteLine("contentpacker.exe [-v] -t [w|l|m] -o output.zip -i input1 input2 input3");
            Console.WriteLine();
            Console.WriteLine("Either using or not using quotes around path is fine:");
            Console.WriteLine();
            Console.WriteLine("contetnpacker.exe -v -t windows -o \"C:\\fully qualified\\path to\\output\" -input input1 \"C:\\fully qualifed\\path to\\input2\"");
        }
    }
}
