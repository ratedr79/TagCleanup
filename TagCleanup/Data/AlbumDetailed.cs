using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup.Data
{
    public class AlbumDetailed
    {
        private bool _allAlbumNamesMatch;
        private bool _allDiscNumbersMatch;
        private bool _allAlbumArtistsMatch;
        private bool _allAlbumGenresMatch;
        private bool _allAlbumYearsMatch;
        private bool _disposeDB;
        private bool _hasAlbumArt;
        private bool _validAlbumArt;

        ILog Logger { get; set; }
        public Data.Tables.Albums BaseAlbum { get; set; }

        public bool AllAlbumNamesMatch { get { return _allAlbumNamesMatch; } }
        public bool AllDiscNumbersMatch { get { return _allDiscNumbersMatch; } }
        public bool AllAlbumArtistsMatch { get { return _allAlbumArtistsMatch; } }
        public bool AllAlbumGenresMatch { get { return _allAlbumGenresMatch; } }
        public bool AllAlbumYearsMatch { get { return _allAlbumYearsMatch; } }
        public bool HasAlbumArt { get { return _hasAlbumArt; } }
        public bool ValidAlbumArt { get { return _validAlbumArt; } }

        private MySQLContext db { get; set; }

        public AlbumDetailed(ILog logger, Data.Tables.Albums baseAlbum) : this (logger, baseAlbum, null)
        { }

        public AlbumDetailed(ILog logger, Data.Tables.Albums baseAlbum, MySQLContext dbContext)
        {
            Logger = logger;
            BaseAlbum = baseAlbum;
            db = dbContext;

            CheckFileDetails();
        }

        private void CheckFileDetails()
        {
            try
            {
                if (db == null)
                {
                    db = new MySQLContext(Logger);
                    _disposeDB = true;
                }

                if (Globals.VerboseLogging)
                {
                    Logger.Info($"Loading detailed information for album '{BaseAlbum.FolderPath}'.");
                }

                var subFiles = db.MediaFiles.Where(mf => mf.FilePath.StartsWith(BaseAlbum.FolderPath));
                var firstFile = subFiles.FirstOrDefault();

                if (firstFile != null)
                {
                    _allAlbumNamesMatch = !subFiles.Any(sf => sf.Album != firstFile.Album);
                    _allDiscNumbersMatch = !subFiles.Any(sf => sf.DiscNumber != firstFile.DiscNumber);
                    _allAlbumArtistsMatch = !subFiles.Any(sf => sf.AlbumArtist != firstFile.AlbumArtist);
                    _allAlbumGenresMatch = !subFiles.Any(sf => sf.Genre != firstFile.Genre);
                    _allAlbumYearsMatch = !subFiles.Any(sf => sf.Year != firstFile.Year);
                }

                FileInfo albumArt = new FileInfo(Path.Combine(BaseAlbum.FolderPath, "Folder.jpg"));

                if (!albumArt.Exists)
                {
                    albumArt = new FileInfo(Path.Combine(BaseAlbum.FolderPath, "Folder.png"));

                    if (albumArt.Exists)
                    {
                        _hasAlbumArt = true;
                        _validAlbumArt = false;
                    }
                    else
                    {
                        _hasAlbumArt = false;
                        _validAlbumArt = false;
                    }
                }
                else
                {
                    _hasAlbumArt = true;
                    _validAlbumArt = true;
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (_disposeDB)
                {
                    db.Dispose();
                    db = null;
                }
            }
        }
    }
}
