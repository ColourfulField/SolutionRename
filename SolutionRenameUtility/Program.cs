using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SolutionRenameUtility
{
    class Program
    {
        private static readonly string _folderSeparator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\\" : "/";
        private static readonly string[] _excludedFolderPaths = new[] {".git", ".idea", ".vs"};

        /// <summary>
        /// contais registry of renamed files. Key is old filename, Value is new filename
        /// </summary>
        private static readonly Dictionary<string, string> _renamedFiles = new Dictionary<string, string>();
        private static string _rootPath;

    static void Main(string[] args)
        {
            _rootPath = Directory.GetCurrentDirectory();

            #region Argument checks 

            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }
            
            if (args.Length == 3)
            {
                _rootPath = args[2];
                if (!Directory.Exists(_rootPath))
                {
                    PrintUsage();
                    return;
                }
            }

            var oldSolutionName = args[0];
            var newSolutionName = args[1];
            if (oldSolutionName.Length <= 3 || newSolutionName.Length <= 3)
            {
                Console.WriteLine("Please specify longer solution names. Current name is unsafe to rename as it may accidentally lead to undesired renames.");
                return;
            }

            #endregion

            DisplayWarningMessage(oldSolutionName, newSolutionName, _rootPath);

            var key = Console.ReadKey();
            Console.WriteLine();
            if (key.Key == ConsoleKey.Y)
            {
                //CreateBackup(path);
                RenameSolution(oldSolutionName, newSolutionName, _rootPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Solution renamed successfully!");
                ListRenamedFiles();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Aborting...");
            }
            
        }

        private static void RenameSolution(string oldSolutionName, string newSolutionName, string path)
        {
            var filePaths = Directory.GetFiles(path);

            foreach (var filePath in filePaths)
            {
                ModifyFile(filePath, oldSolutionName, newSolutionName);
            }

            var folderPaths = Directory.GetDirectories(path);

            foreach (var folderPath in folderPaths)
            {
                if (_excludedFolderPaths.Any(x => folderPath.Contains(x)))
                {
                    continue;
                }
                string renamedFolderPath = Rename(folderPath, oldSolutionName, newSolutionName);
                RenameSolution(oldSolutionName, newSolutionName, renamedFolderPath);
            }

        }

        private static void ModifyFile(string filePath, string oldSolutionName, string newSolutionName)
        {
            try
            {
                var newFilePath = Rename(filePath, oldSolutionName, newSolutionName);

                var fileText = File.ReadAllText(newFilePath);
                var modifiedFileText = fileText.Replace(oldSolutionName, newSolutionName);
                if (modifiedFileText != fileText && !filePath.EndsWith(".dll") && !filePath.EndsWith(".exe"))
                {

                    File.WriteAllText(newFilePath, modifiedFileText);
                    _renamedFiles.Add(filePath, newFilePath);
                }
            }
            catch (Exception ex)
            {
                HandleFileRenameException(ex);
            }
        }

        private static string Rename(string path, string oldName, string newName)
        {
            // Reassemble folder path with new name
            var pathParts = path.Split(_folderSeparator);
            StringBuilder newPath = new StringBuilder();
            foreach (var pathPart in pathParts.Take(pathParts.Length - 1))
            {
                newPath.Append($"{pathPart}{_folderSeparator}");
            }

            newPath.Append(pathParts.Last().Replace(oldName, newName));

            Rename(path, newPath.ToString());

            return newPath.ToString();
        }

        private static void Rename(string oldPath, string newPath)
        {
            if (newPath!= oldPath)
            {
                if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                }
                else
                {
                    File.Move(oldPath, newPath);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Incorrent argument list. Usage: \"SolutionRenameUtility OldSolutionName NewSolutionName Path \"." +
                              "The path parameter is optional if you launch this utility from a root solution folder.");
        }

        private static void HandleFileRenameException(Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Rolling back changes...");
            Console.ForegroundColor = ConsoleColor.White;

            try
            {
                RollBackUpdate();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Rollback complete");
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to roll back changes");
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(0);
            }
        }

        private static void RollBackUpdate()
        {
            foreach (var renamedFile in _renamedFiles)
            {
                Rename(renamedFile.Value, renamedFile.Key);
            }
        }

        private static void ListRenamedFiles()
        {
            Console.ForegroundColor = ConsoleColor.White;
            foreach (var renamedFile in _renamedFiles)
            {
                Console.WriteLine($"--Renamed {renamedFile.Key} to {renamedFile.Value}");
            }
        }

        private static void DisplayWarningMessage(string oldSolutionName, string newSolutionName, string path)
        {
            Console.Write("Are you sure you want to rename ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(oldSolutionName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" to ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(newSolutionName);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" in ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(path + "?\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Y/N ");
        }
    }
}
