using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TagCleanup.Data;

namespace TagCleanup
{
    public class FileScanner
    {
        private List<Action> ScanActions { get; set; }
        private ConcurrentBag<Action> ScanActionsRetry { get; set; }
        public ConcurrentBag<FileError> Errors { get; set; }
        public int DirectoriesProcessed { get; set; }
        public int DirectoriesSkipped { get; set; }
        public int FilesProcessed { get; set; }
        public int FilesSkipped { get; set; }
        public bool ScanComplete { get; set; }
        public DateTime ScanStart { get; set; }
        public DateTime ScanEnd { get; set; }
        private ILog Logger { get; set; }
        private List<string> IgnoreDirectories { get; set; }
        private List<string> SpecialtyAlbums { get; set; }
        private List<string> AlbumSubDirectories { get; set; }
        private DateTime LastScan { get; set; }

        private static readonly string SpecialtyAlbumFile = Path.Combine(Program.ExecutionPath, "XML", "SpecialtyAlbumFolders.xml");
        private static readonly string IgnoreDirectoryFile = Path.Combine(Program.ExecutionPath, "XML", "Ignore.xml");
        private static readonly string AlbumSubDirectoriesFile = Path.Combine(Program.ExecutionPath, "XML", "AlbumSubDirectories.xml");
        private static readonly string ParallelThreads = ConfigurationManager.AppSettings["ScannerThreads"];
        private static readonly string[] FramesToRemove = (ConfigurationManager.AppSettings["FramesToRemove"] ?? "").Split(',');

        public FileScanner(ILog logger)
        {
            Logger = logger;
            Errors = new ConcurrentBag<FileError>();
            ScanActions = new List<Action>();
            ScanActionsRetry = new ConcurrentBag<Action>();
            IgnoreDirectories = new List<string>();
            SpecialtyAlbums = new List<string>();
            AlbumSubDirectories = new List<string>();

            DirectoriesProcessed = 0;
            DirectoriesSkipped = 0;
            FilesProcessed = 0;
            FilesSkipped = 0;
            ScanComplete = false;
            LoadIgnoredDirectories();
            LoadAlbumSubDirectories();
            LoadSpecialtyAlbums();

            TagLib.Id3v2.Tag.UseNumericGenres = false;
        }

        private void LoadAlbumSubDirectories()
        {
            if (File.Exists(AlbumSubDirectoriesFile))
            {
                Logger.Info("Loading album subdirectories...");

                Data.AlbumSubDirectories.Subdirectories subDirectories = null;

                XmlSerializer serializer = new XmlSerializer(typeof(Data.AlbumSubDirectories.Subdirectories));
                using (StreamReader reader = new StreamReader(AlbumSubDirectoriesFile))
                {
                    subDirectories = (Data.AlbumSubDirectories.Subdirectories)serializer.Deserialize(reader);
                }

                if (subDirectories.Items != null)
                {
                    foreach (var folder in subDirectories.Items)
                    {
                        AlbumSubDirectories.Add(folder.Value);
                    }
                }
            }
        }

        private void LoadSpecialtyAlbums()
        {
            if (File.Exists(SpecialtyAlbumFile))
            {
                Logger.Info("Loading specialty albums...");

                Data.SpecialtyAlbums.SpecialtyAlbums specialtyAlbums = null;

                XmlSerializer serializer = new XmlSerializer(typeof(Data.SpecialtyAlbums.SpecialtyAlbums));
                using (StreamReader reader = new StreamReader(SpecialtyAlbumFile))
                {
                    specialtyAlbums = (Data.SpecialtyAlbums.SpecialtyAlbums)serializer.Deserialize(reader);
                }

                if (specialtyAlbums.Items != null)
                {
                    foreach (var folder in specialtyAlbums.Items)
                    {
                        SpecialtyAlbums.Add($"\\{folder.Value}\\");
                    }
                }
            }
        }

        private void LoadIgnoredDirectories()
        {
            if (File.Exists(IgnoreDirectoryFile))
            {
                Logger.Info("Loading ignore directories...");

                Data.Ignore.Directories ignoreDirectories = null;

                XmlSerializer serializer = new XmlSerializer(typeof(Data.Ignore.Directories));
                using (StreamReader reader = new StreamReader(IgnoreDirectoryFile))
                {
                    ignoreDirectories = (Data.Ignore.Directories)serializer.Deserialize(reader);
                }

                if (ignoreDirectories.Items != null)
                {
                    foreach (var dir in ignoreDirectories.Items)
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(dir.Value);

                        if (directoryInfo.Exists)
                        {
                            IgnoreDirectories.Add(directoryInfo.FullName);
                        }
                    }
                }
            }
        }

        public void ScanFolder(string path, string[] fileExtensions, bool includeSubdirectories = true, bool clearDatabase = false, bool resetDatabase = false)
        {
            ScanComplete = false;
            ScanStart = DateTime.Now;
            MySQLContext databaseContext = null;
            LastScan = DateTime.Parse("1/1/1990");

            DirectoriesProcessed = 0;
            DirectoriesSkipped = 0;
            FilesProcessed = 0;
            FilesSkipped = 0;

            try
            {
                if (Directory.Exists(path))
                {
                    if (resetDatabase)
                    {
                        Logger.Info("Resetting database...");

                        using (MySQLContext db = new MySQLContext(Logger))
                        {
                            db.DropTablesIfExist();
                        }

                        Globals.TablesChecked = false;
                    }
                    else if (clearDatabase)
                    {
                        Logger.Info("Clearing database...");

                        using (MySQLContext db = new MySQLContext(Logger))
                        {
                            db.MediaFiles.Clear();
                            db.Albums.Clear();
                            db.Scans.Clear();
                            db.SaveChanges();
                        }
                    }

                    Logger.Info("Getting last scan date...");
                    using (MySQLContext db = new MySQLContext(Logger))
                    {
                        if (db.Scans.Any(s => s.FolderPath == path))
                        {
                            LastScan = db.Scans.Where(s => s.FolderPath == path).Select(s => s.LastScanned).Max();
                        }
                        
                        try
                        {
                            var scanData = new Repository.Scan(Logger, path);
                            scanData.AddOrUpdate();
                        }
                        catch (Exception scanEx)
                        {
                            Logger.Fatal($"There was an error updating the last scan information. Error: {scanEx.Message}", scanEx);
                        }
                    }

                    Logger.Info("Beginning loading directories...");
                    ScanDirectory(path, fileExtensions, includeSubdirectories);

                    Logger.Info($"Loaded {ScanActions.Count()} files and directories.");
                    Logger.Info("Beginning scanning files and directories.");

                    while (ScanActionsRetry.Any() || ScanActions.Any())
                    {
                        if (!string.IsNullOrEmpty(ParallelThreads))
                        {
                            Logger.Info($"Running with maximum number of threads: {ParallelThreads}.");
                            var scanOptions = new ParallelOptions { MaxDegreeOfParallelism = int.Parse(ParallelThreads) };
                            Parallel.Invoke(scanOptions, ScanActions.ToArray());
                        }
                        else
                        {
                            Parallel.Invoke(ScanActions.ToArray());
                        }

                        ScanActions.Clear();

                        if (ScanActionsRetry.Any())
                        {
                            Logger.Info($"Processes to retry: {ScanActionsRetry.Count()}. Re-running errored processes.");
                            ScanActions.Clear();
                            ScanActions.AddRange(ScanActionsRetry);
                            ScanActionsRetry = new ConcurrentBag<Action>();
                        }
                    }

                    Logger.Info($"Scanned {FilesProcessed} files, skipped {FilesSkipped} files.");
                    Logger.Info($"Scanned {DirectoriesProcessed} albums, skipped {DirectoriesSkipped} directories.");
                    Logger.Info($"Encountered {Errors.Count} errors.");

                    Logger.Info($"Cleaning up database...");

                    using (var db = new MySQLContext(Logger))
                    {
                        foreach (var album  in db.Albums.Where(a => a.LastScanned < LastScan))
                        {
                            if (Directory.Exists(album.FolderPath))
                            {
                                album.LastScanned = LastScan;
                            }
                            else
                            {
                                db.Albums.Remove(album);
                            }
                        }

                        db.SaveChanges();

                        foreach (var mp3File in db.MediaFiles.Where(f => f.LastScanned < LastScan))
                        {
                            if (File.Exists(mp3File.FilePath))
                            {
                                mp3File.LastScanned = LastScan;
                            }
                            else
                            {
                                db.MediaFiles.Remove(mp3File);
                            }
                        }

                        db.SaveChanges();
                    }

                    foreach (var error in Errors)
                    {
                        Logger.Info($"File: {error.FilePath}; Error: {error.Details}");
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

            if (databaseContext != null)
            {
                try
                {
                    databaseContext.Dispose();
                }
                catch
                {

                }
                finally
                {
                    databaseContext = null;
                }
            }
        }

        private void ScanDirectory(string directory, string[] fileExtensions, bool includeSubdirectories)
        {
            if (IgnoreDirectories.Contains(directory))
            {
                if (Globals.VerboseLogging)
                {
                    Logger.Info($"Skipping directory '{directory}' as set in ignore file.");
                }

                return;
            }

            if (includeSubdirectories)
            {
                foreach (var subDirectory in Directory.GetDirectories(directory))
                {
                    ScanDirectory(subDirectory, fileExtensions, includeSubdirectories);
                }
            }

            if (Globals.VerboseLogging)
            {
                Logger.Info($"Scanning directory '{directory}'.");
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            if (directoryInfo.EnumerateFiles("*.mp3", SearchOption.TopDirectoryOnly).Any())
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

        private void ProcessDirectory(DirectoryInfo directory, int retry = 0)
        {
            DateTime processStart = DateTime.Now;

            try
            {
                if (Globals.VerboseLogging)
                {
                    Logger.Info($"Loading details for directory '{directory.FullName}'.");
                }

                var album = new Repository.Album(Logger, directory, SpecialtyAlbums, AlbumSubDirectories);

                if (album.Changed > LastScan && album.Exists())
                {
                    album.LoadData();
                    album.AddOrUpdate();
                    DirectoriesProcessed++;
                }
                else
                {
                    DirectoriesSkipped++;
                }

                if ((DirectoriesProcessed + DirectoriesSkipped) % int.Parse(ConfigurationManager.AppSettings["ProcessUpdateCounter"] ?? "100") == 0)
                {
                    Logger.Info($"Processed {(DirectoriesProcessed + DirectoriesSkipped)} directories...");
                }
            }
            catch (Exception ex)
            {
                if (retry < 3)
                {
                    retry++;
                    Logger.Info($"Error processing directory: {directory.FullName}. Retrying {retry} of 3...");
                    ProcessDirectory(directory, retry);
                }
                else
                {
                    Logger.Error($"Error processing directory: {directory.FullName}", ex);

                    if (!ex.Message.Contains("An error occurred while starting a transaction on the provider connection"))
                    {
                        Errors.Add(new FileError(ex, directory.FullName, $"Error processing directory. Error: {ex.Message}"));
                    }
                    else
                    {
                        ScanActionsRetry.Add(() => ProcessDirectory(directory));
                    }
                }
            }

            DateTime processEnd = DateTime.Now;

            if (Globals.VerboseLogging)
            {
                Logger.Info($"Directory '{directory.FullName}' processed in {(processEnd - processStart).TotalMilliseconds} milliseconds.");
            }
        }

        private void ProcessFile(FileInfo file, int retry = 0)
        {
            DateTime processStart = DateTime.Now;

            try
            {
                if (Globals.VerboseLogging)
                {
                    Logger.Info($"Loading details for file '{file.FullName}'.");
                }

                var mediaFile = new Repository.MediaFile(Logger, file, FramesToRemove);

                if (mediaFile.Changed > LastScan && mediaFile.Exists())
                {
                    mediaFile.LoadTagData();
                    mediaFile.AddOrUpdate();
                    FilesProcessed++;
                }
                else
                {
                    FilesSkipped++;
                }

                if ((FilesProcessed + FilesSkipped) % int.Parse(ConfigurationManager.AppSettings["ProcessUpdateCounter"] ?? "100") == 0)
                {
                    Logger.Info($"Processed {FilesProcessed + FilesSkipped} files...");
                }
            }
            catch (Exception ex)
            {
                if (retry < 3)
                {
                    retry++;
                    Logger.Info($"Error processing file: {file.FullName}. Retrying {retry} of 3...");
                    ProcessFile(file, retry);
                }
                else
                {
                    Logger.Error($"Error processing file: {file.FullName}", ex);

                    if (!ex.Message.Contains("An error occurred while starting a transaction on the provider connection"))
                    {
                        Errors.Add(new FileError(ex, file.FullName, $"Error processing file. Error: {ex.Message}"));
                    }
                    else
                    {
                        ScanActionsRetry.Add(() => ProcessFile(file));
                    }
                }
            }

            DateTime processEnd = DateTime.Now;

            if (Globals.VerboseLogging)
            {
                Logger.Info($"File '{file.FullName}' processed in {(processEnd - processStart).TotalMilliseconds} milliseconds.");
            }
        }
    }
}
