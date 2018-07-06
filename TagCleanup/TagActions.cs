using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TagCleanup.Data;

namespace TagCleanup
{
    public class TagActions
    {
        private List<Action> ScanActions { get; set; }
        public ConcurrentBag<FileError> Errors { get; set; }
        public int DirectoriesProcessed { get; set; }
        public int FilesProcessed { get; set; }
        public bool ScanComplete { get; set; }
        public DateTime ScanStart { get; set; }
        public DateTime ScanEnd { get; set; }
        ILog Logger { get; set; }

        public TagActions(ILog logger)
        {
            Logger = logger;
            Errors = new ConcurrentBag<FileError>();
            ScanActions = new List<Action>();

            DirectoriesProcessed = 0;
            FilesProcessed = 0;
            ScanComplete = false;
        }

        public void ScanFolder(string path, string[] fileExtensions, bool includeSubdirectories = true, bool clearDatabase = false)
        {
            ScanComplete = false;
            ScanStart = DateTime.Now;

            try
            {
                if (Directory.Exists(path))
                {
                    if (clearDatabase)
                    {
                        using (DatabaseContext db = new DatabaseContext(Logger))
                        {
                            db.MediaFiles.Clear();
                            db.Albums.Clear();
                            db.SaveChanges();
                        }
                    }

                    ScanDirectory(path, fileExtensions, includeSubdirectories);

                    Logger.Info("Directory load complete, beginning scanning files.");
                    var scanOptions = new ParallelOptions { MaxDegreeOfParallelism = 20 };
                    Parallel.Invoke(scanOptions, ScanActions.ToArray());
                    Logger.Info($"Scaned {FilesProcessed} files.");
                    Logger.Info($"Scaned {DirectoriesProcessed} albums.");
                    Logger.Info($"Encountered {Errors.Count} errors.");

                    foreach(var error in Errors)
                    {
                        Logger.Info(error.Details);
                    }
                }
                else
                {
                    throw new Exception($"Directory '{path}' does not exist.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error scanning files: {path}", ex);
                Errors.Add(new FileError(ex, path, "Error scanning files."));
            }

            ScanEnd = DateTime.Now;
            ScanComplete = true;
        }

        public void ScanDirectory(string directory, string[] fileExtensions, bool includeSubdirectories)
        {
            if (includeSubdirectories)
            {
                foreach (var subDirectory in Directory.GetDirectories(directory))
                {
                    ScanDirectory(subDirectory, fileExtensions, includeSubdirectories);
                }
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            if (directoryInfo.EnumerateFiles().Any())
            {
                ScanActions.Add(() => ProcessDirectory(directoryInfo));

                foreach (string extension in fileExtensions)
                {
                    foreach (var file in directoryInfo.GetFiles("*." + extension.ToLower()))
                    {
                        ScanActions.Add(() => ProcessFile(file));
                    }
                }
            }
        }

        private void ProcessDirectory(DirectoryInfo directory)
        {
            try
            {
                var album = new Repository.Album(Logger, directory);
                album.AddOrUpdate();

                DirectoriesProcessed++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing directory: {directory.FullName}", ex);
                Errors.Add(new FileError(ex, directory.FullName, "Error processing directory."));
            }
        }

        private void ProcessFile(FileInfo file)
        {
            try
            {
                var mediaFile = new Repository.MediaFile(Logger, file);
                mediaFile.AddOrUpdate();

                FilesProcessed++;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing file: {file.FullName}", ex);
                Errors.Add(new FileError(ex, file.FullName, "Error processing file."));
            }
        }
    }
}
