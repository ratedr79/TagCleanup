using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TagCleanup.Data;

namespace TagCleanup
{
    public class TagChecker
    {
        private ILog Logger { get; set; }
        private List<Action> TagActions { get; set; }
        private List<Action> AlbumActions { get; set; }
        private ConcurrentDictionary<string, Data.AlbumDetailed> AlbumDictionary { get; set; }
        public ConcurrentDictionary<string, ConcurrentBag<string>> TagErrorDictionary { get; set; }
        public int FilesChecked { get; set; }
        public int AlbumsChecked { get; set; }
        private ConcurrentBag<string> SpecialtyAlbums { get; set; }
        private ConcurrentBag<string> AlbumSubDirectories { get; set; }
        private ConcurrentBag<string> AdditionalVariousArtistsAlbums { get; set; }
        private ConcurrentBag<string> AlbumArtExceptions { get; set; }
        private ConcurrentBag<string> AlbumYearExceptions { get; set; }
        public DateTime ScanStart { get; set; }
        public DateTime ScanEnd { get; set; }

        private static readonly string SpecialtyAlbumFile = Path.Combine(Program.ExecutionPath, "XML", "SpecialtyAlbumFolders.xml");
        private static readonly string AlbumSubDirectoriesFile = Path.Combine(Program.ExecutionPath, "XML", "AlbumSubDirectories.xml");
        private static readonly string AlbumArtExceptionsFile = Path.Combine(Program.ExecutionPath, "XML", "AlbumArtExceptions.xml");
        private static readonly string AlbumYearExceptionsFile = Path.Combine(Program.ExecutionPath, "XML", "AlbumYearExceptions.xml");
        private static readonly string AdditionalVariousArtistsAlbumFile = Path.Combine(Program.ExecutionPath, "XML", "AdditionalVariousArtistsAlbum.xml");
        private static readonly string ParallelThreads = ConfigurationManager.AppSettings["TagDataThreads"];

        public TagChecker(ILog logger)
        {
            Logger = logger;
            FilesChecked = 0;
            TagActions = new List<Action>();
            AlbumActions = new List<Action>();
            AlbumDictionary = new ConcurrentDictionary<string, Data.AlbumDetailed>();
            TagErrorDictionary = new ConcurrentDictionary<string, ConcurrentBag<string>>();
            SpecialtyAlbums = new ConcurrentBag<string>();
            AlbumSubDirectories = new ConcurrentBag<string>();
            AdditionalVariousArtistsAlbums = new ConcurrentBag<string>();
            AlbumArtExceptions = new ConcurrentBag<string>();
            AlbumYearExceptions = new ConcurrentBag<string>();
            LoadSpecialtyAlbums();
            LoadAlbumSubDirectories();
            LoadAdditionalVariousArtistsAlbums();
            LoadAlbumArtExceptions();
            LoadAlbumYearExceptions();
        }

        private void LoadAlbumYearExceptions()
        {
            if (File.Exists(AlbumYearExceptionsFile))
            {
                Logger.Info("Loading album year exceptions...");

                Data.AlbumYearExceptions.AlbumYearExceptions albumYearExceptions = null;

                XmlSerializer serializer = new XmlSerializer(typeof(Data.AlbumYearExceptions.AlbumYearExceptions));
                using (StreamReader reader = new StreamReader(AlbumYearExceptionsFile))
                {
                    albumYearExceptions = (Data.AlbumYearExceptions.AlbumYearExceptions)serializer.Deserialize(reader);
                }

                if (albumYearExceptions.Items != null)
                {
                    foreach (var folder in albumYearExceptions.Items)
                    {
                        AlbumYearExceptions.Add(folder.Value);
                    }
                }
            }
        }

        private void LoadAlbumArtExceptions()
        {
            if (File.Exists(AlbumArtExceptionsFile))
            {
                Logger.Info("Loading album art exceptions...");

                Data.AlbumArtExceptions.AlbumArtExceptions albumArtExceptions = null;

                XmlSerializer serializer = new XmlSerializer(typeof(Data.AlbumArtExceptions.AlbumArtExceptions));
                using (StreamReader reader = new StreamReader(AlbumArtExceptionsFile))
                {
                    albumArtExceptions = (Data.AlbumArtExceptions.AlbumArtExceptions)serializer.Deserialize(reader);
                }

                if (albumArtExceptions.Items != null)
                {
                    foreach (var folder in albumArtExceptions.Items)
                    {
                        AlbumArtExceptions.Add(folder.Value);
                    }
                }
            }
        }

        private void LoadAdditionalVariousArtistsAlbums()
        {
            if (File.Exists(AdditionalVariousArtistsAlbumFile))
            {
                Logger.Info("Loading additional album various artist...");

                Data.AdditionalVariousArtistsAlbums.AdditionalVariousArtistsAlbums additionalVAAlbums = null;

                XmlSerializer serializer = new XmlSerializer(typeof(Data.AdditionalVariousArtistsAlbums.AdditionalVariousArtistsAlbums));
                using (StreamReader reader = new StreamReader(AdditionalVariousArtistsAlbumFile))
                {
                    additionalVAAlbums = (Data.AdditionalVariousArtistsAlbums.AdditionalVariousArtistsAlbums)serializer.Deserialize(reader);
                }

                if (additionalVAAlbums.Items != null)
                {
                    foreach (var folder in additionalVAAlbums.Items)
                    {
                        AdditionalVariousArtistsAlbums.Add(folder.Value);
                    }
                }
            }
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

        public void CheckForErrors()
        {
            Logger.Info($"Checking for tag errors...");

            FilesChecked = 0;
            AlbumsChecked = 0;
            ScanStart = DateTime.Now;

            using (MySQLContext db = new MySQLContext(Logger))
            {
                foreach (var mediaFile in db.MediaFiles)
                {
                    var albumDirectory = Path.GetDirectoryName(mediaFile.FilePath) + "\\";

                    if (!AlbumDictionary.ContainsKey(albumDirectory))
                    {
                        AlbumDictionary[albumDirectory] = null;
                        AlbumActions.Add(() => LoadAlbumDetails(albumDirectory));
                    }

                    TagActions.Add(() => CheckTagDetails(mediaFile));
                }
            }

            if (!string.IsNullOrEmpty(ParallelThreads))
            {
                Logger.Info($"Running with maximum number of threads: {ParallelThreads}.");
                var tagOptions = new ParallelOptions { MaxDegreeOfParallelism = int.Parse(ParallelThreads) };
                Logger.Info($"Loading album information.");
                Parallel.Invoke(tagOptions, AlbumActions.ToArray());
                Logger.Info($"Checking tag data.");
                Parallel.Invoke(tagOptions, TagActions.ToArray());
            }
            else
            {
                Parallel.Invoke(AlbumActions.ToArray());
                Parallel.Invoke(TagActions.ToArray());
            }

            ScanEnd = DateTime.Now;
        }

        private void LoadAlbumDetails(string albumDirectory, int retry = 0)
        {
            try
            {
                AlbumDetailed album = null;

                using (MySQLContext db = new MySQLContext(Logger))
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Loading album details for '{albumDirectory}'.");
                    }

                    var baseAlbum = db.Albums.FirstOrDefault(a => a.FolderPath == albumDirectory);

                    if (baseAlbum == null)
                    {
                        throw new Exception($"Could not locate album entry for '{albumDirectory}'.");
                    }

                    album = GetAlbumDetails(baseAlbum, db);
                    baseAlbum = null;

                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Caching '{albumDirectory}' details.");
                    }
                }

                AlbumDictionary[albumDirectory] = album;

                AlbumsChecked++;

                if (AlbumsChecked % int.Parse(ConfigurationManager.AppSettings["ProcessUpdateCounter"] ?? "100") == 0)
                {
                    Logger.Info($"Checked {AlbumsChecked} albums...");
                }
            }
            catch (Exception ex)
            {
                if (retry < 3)
                {
                    retry++;
                    Logger.Info($"Error loading album information: {albumDirectory}. Retrying {retry} of 3...");
                    LoadAlbumDetails(albumDirectory, retry);
                }
                else
                {
                    Logger.Error($"Error loading album information: {albumDirectory}", ex);
                }
            }
        }

        private void CheckTagDetails(Data.Tables.MediaFiles mediaFile)
        {
            try
            {
                bool isSpecialtyAlbum = false;
                bool isNamedAsVariousArtistAlbum = AdditionalVariousArtistsAlbums.Contains(Path.GetDirectoryName(mediaFile.FilePath));

                foreach (var specialtyAlbumFolder in SpecialtyAlbums)
                {
                    isSpecialtyAlbum = mediaFile.FilePath.Contains(specialtyAlbumFolder);

                    if (isSpecialtyAlbum)
                    {
                        break;
                    }
                }

                if (Globals.VerboseLogging)
                {
                    Logger.Info($"Checking for tag errors in file '{mediaFile.FilePath}'");
                }

                AlbumDetailed album = AlbumDictionary[Path.GetDirectoryName(mediaFile.FilePath) + "\\"];

                if (!mediaFile.ValidV1Tag)
                {
                    LogTagError(mediaFile, $"File has an invalid V1 Tag.");
                }

                if (!mediaFile.ValidV2Tag)
                {
                    LogTagError(mediaFile, $"File has an invalid V2 Tag.");
                }

                if ((mediaFile.Year ?? "").Length != 4)
                {
                    LogTagError(mediaFile, $"Tag has invalid year. Year: {mediaFile.Year}");
                }

                if ((mediaFile.Artist ?? "").Length < 1)
                {
                    LogTagError(mediaFile, $"Tag has invalid artist. Artist: {mediaFile.Artist}");
                }

                if ((mediaFile.TrackNumber ?? "").Length < 1)
                {
                    LogTagError(mediaFile, $"Tag has invalid track number. Track Number: {mediaFile.TrackNumber}");
                }

                if ((mediaFile.Album ?? "").Length < 1)
                {
                    LogTagError(mediaFile, $"Tag has invalid album name. Album Name: {mediaFile.Album}");
                }

                if ((mediaFile.Title ?? "").Length < 1)
                {
                    LogTagError(mediaFile, $"Tag has invalid title. Title: {mediaFile.Title}");
                }
                else if ((mediaFile.Title ?? "").Contains("\\"))
                {
                    LogTagError(mediaFile, $"Tag title has invalid character. Title: {mediaFile.Title}");
                }

                if ((mediaFile.Genre ?? "").Length < 1)
                {
                    LogTagError(mediaFile, $"Tag title has invalid genre. Genre: {mediaFile.Genre}");
                }

                if (mediaFile.ValidV2Tag && (mediaFile.AlbumArtist ?? "").Length < 1)
                {
                    LogTagError(mediaFile, $"Tag title has invalid album artist. Album Artist: {mediaFile.AlbumArtist}");
                }

                if (mediaFile.ValidV2Tag && !string.IsNullOrEmpty(mediaFile.DiscNumber))
                {
                    var discNumberAndTotal = mediaFile.DiscNumberAndCount.Split('/');

                    if (discNumberAndTotal.Count() > 1)
                    {
                        if (discNumberAndTotal[1] != album.BaseAlbum.Discs)
                        {
                            LogTagError(mediaFile, $"Disc total tag does not match disc total of folder structure. Track details: {discNumberAndTotal[1]}; Album details: {album.BaseAlbum.Discs}");
                        }
                    }
                    else
                    {
                        if (discNumberAndTotal[0].StartsWith("0"))
                        {
                            LogTagError(mediaFile, $"Disc number tag is not properly formatted. Details: {discNumberAndTotal[0]}");
                        }

                        if (!int.TryParse(discNumberAndTotal[0], out int discNumber))
                        {
                            LogTagError(mediaFile, $"Disc number tag is not a number. Details: {discNumberAndTotal[0]}");
                        }

                        if (discNumberAndTotal[0] != album.BaseAlbum.Disc)
                        {
                            LogTagError(mediaFile, $"Disc number tag does not match disc number of folder structure. Track details: {discNumberAndTotal[0]}; Album details: {album.BaseAlbum.Disc}");
                        }
                    }
                }

                if (mediaFile.ContainsOtherTags)
                {
                    LogTagError(mediaFile, $"File contains other tag data. Other tags: {mediaFile.OtherTags}");
                }

                if (!album.AllAlbumNamesMatch)
                {
                    LogTagError(mediaFile, $"Other tracks in this folder have different album names. Album path: {Path.GetDirectoryName(mediaFile.FilePath)}");
                }

                if (!album.AllDiscNumbersMatch)
                {
                    LogTagError(mediaFile, $"Other tracks in this folder have different disc numbers. Album path: {Path.GetDirectoryName(mediaFile.FilePath)}");
                }

                if (!album.AllAlbumArtistsMatch)
                {
                    LogTagError(mediaFile, $"Other tracks in this folder have different album artists. Album path: {Path.GetDirectoryName(mediaFile.FilePath)}");
                }

                if (!album.AllAlbumGenresMatch)
                {
                    LogTagError(mediaFile, $"Other tracks in this folder have different genres. Album path: {Path.GetDirectoryName(mediaFile.FilePath)}");
                }

                if (!album.AllAlbumYearsMatch && !AlbumYearExceptions.Contains(Path.GetDirectoryName(mediaFile.FilePath)))
                {
                    LogTagError(mediaFile, $"Other tracks in this folder have different years. Album path: {Path.GetDirectoryName(mediaFile.FilePath)}");
                }

                if (!album.HasAlbumArt && !AlbumArtExceptions.Contains(Path.GetDirectoryName(mediaFile.FilePath)))
                {
                    LogTagError(mediaFile, $"Album does not have album art. Album: {album.BaseAlbum.FolderPath}");
                }
                else if (!album.ValidAlbumArt)
                {
                    LogTagError(mediaFile, $"Album does not have a valid JPG file. Album: {album.BaseAlbum.FolderPath}");
                }

                try
                {
                    if (!isSpecialtyAlbum)
                    {
                        var fileDirectory = new DirectoryInfo(mediaFile.FilePath).Parent;
                        var albumDirectoryName = fileDirectory.Name;

                        if (AlbumSubDirectories.Contains(albumDirectoryName))
                        {
                            albumDirectoryName = fileDirectory.Parent.Name;
                        }

                        var albumTagName = $"{mediaFile.Year} - {ReplaceInvalidPathCharacters(mediaFile.Album)}";

                        if (albumDirectoryName != albumTagName)
                        {
                            LogTagError(mediaFile, $"Album tag does not match folder name. Tag: {albumTagName}; Folder: {albumDirectoryName}");
                        }

                        if (!isNamedAsVariousArtistAlbum)
                        {
                            var mp3FileName = new FileInfo(mediaFile.FilePath).Name.Replace(".MP3", ".mp3");
                            var tagFileName = $"{mediaFile.TrackNumber.Split('/')[0].PadLeft(2, '0') } - {ReplaceInvalidFileCharacters(mediaFile.Title)}.mp3";

                            if (mp3FileName != tagFileName)
                            {
                                LogTagError(mediaFile, $"File tag does not match file name. Tag: {tagFileName}; File: {mp3FileName}");
                            }
                        }
                    }
                    else
                    {
                        if (Globals.VerboseLogging)
                        {
                            Logger.Info($"Skipping album name check for '{mediaFile.FilePath}' (Specialty album).");
                        }
                    }
                }
                catch (Exception specialEx)
                {
                    LogTagError(mediaFile, $"There was an error checking specialty album information. Error: {specialEx.Message}");
                }
            }
            catch (Exception ex)
            {
                LogTagError(mediaFile, $"Fatal error checking tag data. Error: {ex.Message}");
            }

            FilesChecked++;

            if (FilesChecked % int.Parse(ConfigurationManager.AppSettings["ProcessUpdateCounter"] ?? "100") == 0)
            {
                Logger.Info($"Checked {FilesChecked} tags...");
            }
        }

        private Data.AlbumDetailed GetAlbumDetails(Data.Tables.Albums baseAlbum, MySQLContext db)
        {
            var albumDetailed = new Data.AlbumDetailed(Logger, baseAlbum, db);
            return albumDetailed;
        }

        private void LogTagError(Data.Tables.MediaFiles file, string message)
        {
            if (Globals.VerboseLogging)
            {
                Logger.Info($"Logging error for file '{file.FilePath}'");
            }

            if (!TagErrorDictionary.ContainsKey(file.FilePath))
            {
                TagErrorDictionary[file.FilePath] = new ConcurrentBag<string>();
            }

            TagErrorDictionary[file.FilePath].Add(message);
        }

        private string ReplaceInvalidPathCharacters(string name)
        {
            name = name.Replace(": ", " -");
            name = name.Replace(":", "");
            name = name.Replace("/", "-");
            name = name.Replace("?", "");
            name = name.Replace("\"", "");
            name = name.Replace("*", "");
            name = name.Replace("<", "-");
            name = name.Replace(">", "-");

            foreach (char c in Path.GetInvalidPathChars().Where(c => c.ToString() != "."))
            {
                name = name.Replace(c.ToString(), "");
            }

            while (name.EndsWith("."))
            {
                name = name.Trim('.');
            }

            return name;
        }

        private string ReplaceInvalidFileCharacters(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c.ToString(), "");
            }

            return name;
        }
    }
}
