using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Common.ExtensionMethods;
using Microsoft.Extensions.Logging;

namespace Common.Utility
{
    public static class FileUtility
    {
        private static readonly string NumberPattern = " ({0})";

        public static string ReplaceInvalidFilenameCharacters(string filename, char replacement = '_')
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(invalidCharacter, replacement);
            }

            return filename;
        }

        /// <summary>
        /// Checks if a given filename exists - if not, returns it. If so, appends a sequential number in brackets to the end.
        /// By default (ie if checkFileExistenceOverrideFunc == null) then the file system will be used for checking existence of filenames.
        /// If checkFileExistenceOverrideFunc is passed, then custom filename existence checking behaviour can be specified - to compare a filename
        /// to a list of strings if using outside of a file system scope, for example.
        /// </summary>
        public static string GetNextAvailableFilename(
            string filePath,
            Func<string, bool> checkFileExistenceOverrideFunc = null)
        {
            // if no override was passed for checking for the existence of a filename, default to using the file system
            checkFileExistenceOverrideFunc ??= File.Exists;

            // short-cut if already available
            if (!checkFileExistenceOverrideFunc(filePath))
            {
                return filePath;
            }

            // if path has extension then insert the number pattern just before the extension and return next filename
            if (Path.HasExtension(filePath))
            {
                return GetNextFilename(
                    pattern: filePath.Insert(filePath.LastIndexOf(Path.GetExtension(filePath)), NumberPattern),
                    checkFileExistenceFunc: checkFileExistenceOverrideFunc);
            }

            // otherwise just append the pattern to the path and return next filename
            return GetNextFilename(filePath + NumberPattern);
        }

        /// <summary>
        /// If any path segment after the 1st parameter begins with a '\', then the framework method considers that to be the root, and returns only that.
        /// So this is a wrapper method to fix that helpful functionality.
        /// </summary>
        public static string CombinePath(params string[] paths)
        {
            for (var i = 1; i < paths.Length; i++)
            {
                paths[i] = paths[i].TrimStart(Path.DirectorySeparatorChar);
            }

            return Path.Combine(paths);
        }

        public static void DeleteFilesIfExist(IEnumerable<string> filePaths)
        {
            foreach (var file in filePaths)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        public static void CloneDirectory(string root, string dest, List<string> excludeFolders = null, List<string> includeOnlyFolders = null, bool overwrite = false)
        {
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException("Root folder does not exist");
            }

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }
            else if (overwrite)
            {
                DeleteDirectoryRecursive(dest);
            }

            foreach (var directory in Directory.GetDirectories(root))
            {
                if (includeOnlyFolders != null)
                {
                    if (!includeOnlyFolders.Contains(Path.GetFileName(directory)))
                    {
                        continue;
                    }
                }
                else if (excludeFolders != null && excludeFolders.Contains(Path.GetFileName(directory)))
                {
                    continue;
                }

                string dirName = Path.GetFileName(directory);

                if (!Directory.Exists(FileUtility.CombinePath(dest, dirName)))
                {
                    Directory.CreateDirectory(FileUtility.CombinePath(dest, dirName));
                }

                CloneDirectory(directory, FileUtility.CombinePath(dest, dirName), overwrite: overwrite);
            }

            foreach (var file in Directory.GetFiles(root))
            {
                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                }

                File.Copy(file, FileUtility.CombinePath(dest, Path.GetFileName(file)), overwrite);
            }
        }

        /// <summary>
        /// Deletes a folder, all sub-folders and all files.
        /// </summary>
        public static void DeleteDirectoryRecursive(string path)
        {
            DeleteDirectoryRecursive(new DirectoryInfo(path));
        }

        /// <summary>
        /// Deletes a folder, all sub-folders and all files.
        /// </summary>
        public static void DeleteDirectoryRecursive(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
            {
                return;
            }

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                DeleteDirectoryRecursive(dir);
            }

            baseDir.Delete(true);
        }

        /// <summary>
        /// Recurse through a directory structure, executing the given action for every file.
        /// </summary>
        public static void ForAllFilesRecursive(string path, Action<FileInfo> fileAction)
        {
            ForAllFilesRecursive(new DirectoryInfo(path), fileAction);
        }

        /// <summary>
        /// Recurse through a directory structure, executing the given action for every file.
        /// </summary>
        public static void ForAllFilesRecursive(DirectoryInfo baseDir, Action<FileInfo> fileAction)
        {
            if (!baseDir.Exists)
            {
                return;
            }

            foreach (var file in baseDir.EnumerateFiles())
            {
                fileAction(file);
            }

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                ForAllFilesRecursive(dir, fileAction);
            }
        }

        public static void CleanupOldTempFiles(
            string folder,
            int cleanUpFilesGreaterThanDaysOld,
            Func<FileInfo, string> groupFilesBy,
            ILogger logger)
        {
            var tempFiles = new DirectoryInfo(folder).GetFiles();

            var expiredFiles = tempFiles
                .GroupBy(groupFilesBy)
                .Select(x => new
                {
                    Name = x.Key,
                    DaysOld = (DateTime.UtcNow - x.Max(y => y.LastWriteTimeUtc)).TotalDays,
                    Files = x.Select(y => new FileCleanupInfo
                    {
                        SuccessfullyCleanedUp = false,
                        CleanUpError = (string)null,
                        FullPath = y.FullName
                    }).ToList()
                })
                .Where(x => x.DaysOld > cleanUpFilesGreaterThanDaysOld)
                .ToList();

            foreach (var expiredFileGroup in expiredFiles)
            {
                foreach (var expiredFile in expiredFileGroup.Files)
                {
                    try
                    {
                        File.Delete(expiredFile.FullPath);
                        expiredFile.SuccessfullyCleanedUp = true;
                    }
                    catch (Exception ex)
                    {
                        expiredFile.CleanUpError = ex.Message;
                    }
                }
            }

            var successfullyCleanedUpFileKeys = expiredFiles
                .SelectMany(x => x.Files)
                .Where(x => x.SuccessfullyCleanedUp)
                .Select(x => Path.GetFileNameWithoutExtension(x.FullPath))
                .Distinct()
                .ToList();

            if (successfullyCleanedUpFileKeys.Any())
            {
                logger.LogInformation($"Successfully cleaned up the following temp file keys which were > {cleanUpFilesGreaterThanDaysOld} days old: {successfullyCleanedUpFileKeys.StringJoin(", ")}");
            }

            var failedCleanUpFiles = expiredFiles
                .SelectMany(x => x.Files)
                .Where(x => !x.SuccessfullyCleanedUp)
                .Select(x => new
                {
                    Filename = Path.GetFileName(x.FullPath),
                    Error = x.CleanUpError
                })
                .ToList();

            if (failedCleanUpFiles.Any())
            {
                logger.LogError($"Failed to clean up the following temp files which were > {cleanUpFilesGreaterThanDaysOld} days old: {failedCleanUpFiles.Select(x => $"{x.Filename} ({x.Error})").StringJoin(", ")}");
            }
        }

        private static string GetNextFilename(
            string pattern,
            Func<string, bool> checkFileExistenceFunc = null)
        {
            // if no override was passed for checking for the existence of a filename, default to using the file system
            checkFileExistenceFunc ??= File.Exists;

            var tmp = string.Format(pattern, 1);

            if (tmp == pattern)
            {
                throw new ArgumentException("The pattern must include an index place-holder", nameof(pattern));
            }

            if (!checkFileExistenceFunc(tmp))
            {
                return tmp; // short-circuit if no matches
            }

            int min = 1, max = 2; // min is inclusive, max is exclusive/untested

            while (checkFileExistenceFunc(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                var pivot = (max + min) / 2;

                if (checkFileExistenceFunc(string.Format(pattern, pivot)))
                {
                    min = pivot;
                }
                else
                {
                    max = pivot;
                }
            }

            return string.Format(pattern, max);
        }
    }

    public static class FileExtension
    {
        // I honestly have no idea what "mbe" is, but the legacy code uses it everywhere, so we need to replicate
        // it seems to be ".eml" format (MIME) but just renamed to something weird
        public static readonly string LogicanEmail = ".mbe";
    }

    internal class FileCleanupInfo
    {
        internal string FullPath { get; set; }

        internal bool SuccessfullyCleanedUp { get; set; }

        internal string CleanUpError { get; set; }
    }
}
