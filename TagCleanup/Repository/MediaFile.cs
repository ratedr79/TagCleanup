using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagCleanup.Data;

namespace TagCleanup.Repository
{
    public class MediaFile : Data.TagData
    {
        private ILog Logger { get; set; }
        private Mp3File FileWithTags { get; set; }

        public MediaFile(ILog logger, FileInfo file, string[] framesToRemove = null) : this(logger, new Mp3File(logger, file, framesToRemove))
        {
        }

        public MediaFile(ILog logger, Mp3File file)
        {
            Logger = logger;
            FileWithTags = file;

            DateTime fileStart = DateTime.Now;
            LoadFileData(FileWithTags.MediaFile);
            DateTime fileEnd = DateTime.Now;

            if (Globals.VerboseLogging)
            {
                Logger.Info($"'{FileWithTags.MediaFile.FullName}' data loaded in {(fileEnd - fileStart).TotalMilliseconds} milliseconds.");
            }
        }

        public void LoadTagData()
        {
            DateTime tagStart = DateTime.Now;
            LoadTagData(FileWithTags.ID3V2Tag, FileWithTags.ID3V1Tag);
            DateTime tagEnd = DateTime.Now;

            if (Globals.VerboseLogging)
            {
                Logger.Info($"'{FileWithTags.MediaFile.FullName}' tag data parsed in {(tagEnd - tagStart).TotalMilliseconds} milliseconds.");
            }
        }

        public bool Exists()
        {
            bool exists = false;

            using (MySQLContext db = new MySQLContext(Logger))
            {
                exists = db.MediaFiles.Any(a => a.FilePath == FilePath);
            }

            return exists;
        }

        public bool Exists(MySQLContext db)
        {
            return db.MediaFiles.Any(a => a.FilePath == FilePath);
        }

        public void AddOrUpdate()
        {
            if (!TagDataLoaded)
            {
                Logger.Info($"Tag data not loaded for file '{FilePath}'.");
                return;
            }

            DateTime processStart = DateTime.Now;

            using (var db = new MySQLContext(Logger))
            {
                if (!Exists(db))
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Adding new entry for file '{FilePath}'.");
                    }

                    Add(db);
                }
                else
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Updating entry for file '{FilePath}'.");
                    }

                    Update(db);
                }
            }

            DateTime processEnd = DateTime.Now;

            if (Globals.VerboseLogging)
            {
                Logger.Info($"'{FilePath}' AddOrUpdate completed in {(processEnd - processStart).TotalMilliseconds} milliseconds.");
            }
        }

        private void Add(MySQLContext db)
        {
            Data.Tables.MediaFiles mediaFile = new Data.Tables.MediaFiles()
            {
                FilePath = FilePath,
                Album = Album,
                Artist = Artist,
                AlbumArtist = AlbumArtist,
                DiscNumber = DiscNumber,
                DiscCount = DiscCount,
                DiscNumberAndCount = DiscNumberAndCount,
                TrackNumber = TrackNumber,
                Title = Title,
                Year = Year,
                Genre = Genre,
                Created = Created,
                Changed = Changed,
                LastScanned = DateTime.Now,
                ContainsOtherTags = ContainsOtherTags,
                OtherTags = OtherTags,
                ValidV1Tag = ValidV1Tag,
                ValidV2Tag = ValidV2Tag
            };

            db.MediaFiles.Add(mediaFile);
            db.SaveChanges();
        }

        private void Update(MySQLContext db)
        {
            var mediaFile = db.MediaFiles.FirstOrDefault(f => f.FilePath == FilePath);

            if (mediaFile != null)
            {
                mediaFile.FilePath = FilePath;
                mediaFile.Album = Album;
                mediaFile.Artist = Artist;
                mediaFile.AlbumArtist = AlbumArtist;
                mediaFile.DiscNumber = DiscNumber;
                mediaFile.DiscCount = DiscCount;
                mediaFile.DiscNumberAndCount = DiscNumberAndCount;
                mediaFile.TrackNumber = TrackNumber;
                mediaFile.Title = Title;
                mediaFile.Year = Year;
                mediaFile.Genre = Genre;
                mediaFile.Created = Created;
                mediaFile.Changed = Changed;
                mediaFile.LastScanned = DateTime.Now;
                mediaFile.ContainsOtherTags = ContainsOtherTags;
                mediaFile.OtherTags = OtherTags;
                mediaFile.ValidV1Tag = ValidV1Tag;
                mediaFile.ValidV2Tag = ValidV2Tag;

                db.SaveChanges();
            }
        }
    }
}
