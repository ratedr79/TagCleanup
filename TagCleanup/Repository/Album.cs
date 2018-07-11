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
    public class Album
    {
        public string FolderPath { get; set; }
        public string AlbumName { get; set; }
        public string AlbumArtist { get; set; }
        public string TotalDiscs { get; set; }
        public string DiscNumber { get; set; }
        public string Year { get; set; }
        public DirectoryInfo Directory { get; set; }
        public DateTime LastScanned { get; set; }
        public DateTime Changed { get; set; }

        private ILog Logger { get; set; }
        private List<string> SubDirectories { get; set; }
        private List<string> SpecialtyAlbums { get; set; }
        private bool IsSpecialtyAlbum { get; set; }
        private string SpecialtyAlbumName { get; set; }

        public Album(ILog logger, DirectoryInfo directory, List<string> specialtyAlbums, List<string> subDirectories)
        {
            SpecialtyAlbums = specialtyAlbums;
            SubDirectories = subDirectories;
            Logger = logger;
            FolderPath = directory.FullName;
            Changed = directory.LastWriteTime;
            Directory = directory;

            if (!FolderPath.EndsWith("\\"))
            {
                FolderPath = FolderPath + "\\";
            }
        }

        public void LoadData()
        {
            CheckSpecialtyAlbum();

            AlbumName = GetAlbumName(Directory);
            AlbumArtist = GetAlbumArtist(Directory);
            TotalDiscs = GetDiscTotal(Directory);
            DiscNumber = GetDiscNumber(Directory);
            Year = GetAlbumYear(Directory);
        }

        private void CheckSpecialtyAlbum()
        {
            foreach (var specialtyAlbumFolder in SpecialtyAlbums)
            {
                IsSpecialtyAlbum = FolderPath.Contains(specialtyAlbumFolder);

                if (IsSpecialtyAlbum)
                {
                    SpecialtyAlbumName = specialtyAlbumFolder.Replace("\\", "");
                    break;
                }
            }
        }

        private string GetDiscNumber(DirectoryInfo directory)
        {
            int discNumber;

            if (directory.Name.StartsWith("Disc ") && int.TryParse(directory.Name.Replace("Disc ", ""), out discNumber))
            {
                return discNumber.ToString();
            }
            else
            {
                return "1";
            }
        }

        private string GetDiscTotal(DirectoryInfo directory)
        {
            int discNumber;

            if (directory.Name.StartsWith("Disc ") && int.TryParse(directory.Name.Replace("Disc ", ""), out discNumber))
            {
                int discTotal = 1;

                foreach(var parentDirectory in directory.Parent.GetDirectories())
                {
                    int parentDiscNumber;

                    if (parentDirectory.Name.StartsWith("Disc ") && int.TryParse(parentDirectory.Name.Replace("Disc ", ""), out parentDiscNumber))
                    {
                        if (parentDiscNumber > discTotal)
                        {
                            discTotal = parentDiscNumber;
                        }
                    }
                }

                return discTotal.ToString();
            }
            else
            {
                return "1";
            }
        }

        private string GetAlbumName(DirectoryInfo directory)
        {
            if (SubDirectories.Contains(directory.Name))
            {
                return GetAlbumName(directory.Parent);
            }
            else
            {
                if (IsSpecialtyAlbum)
                {
                    return directory.Name;
                }
                else
                {
                    int startIndex = directory.Name.IndexOf(" - ") + 3;

                    return directory.Name.Substring(startIndex);
                }
            }
        }

        public string GetAlbumYear(DirectoryInfo directory)
        {
            if (SubDirectories.Contains(directory.Name))
            {
                return GetAlbumYear(directory.Parent);
            }
            else
            {
                if (IsSpecialtyAlbum)
                {
                    return "0000";
                }
                else
                {
                    int endIndex = directory.Name.IndexOf(" - ");

                    return directory.Name.Substring(0, endIndex);
                }
            }
        }

        public string GetAlbumArtist(DirectoryInfo directory)
        {
            if (SubDirectories.Contains(directory.Name))
            {
                return GetAlbumArtist(directory.Parent);
            }
            else
            {
                if (IsSpecialtyAlbum)
                {
                    return SpecialtyAlbumName;
                }
                else
                {
                    return directory.Parent.Name;
                }
            }
        }

        public bool Exists()
        {
            bool exists = false;

            using (MySQLContext db = new MySQLContext(Logger))
            {
                exists = db.Albums.Any(a => a.FolderPath == FolderPath);
            }

            return exists;
        }

        public bool Exists(MySQLContext db)
        {
            return db.Albums.Any(a => a.FolderPath == FolderPath);
        }

        public void AddOrUpdate()
        {
            using (var db = new MySQLContext(Logger))
            {
                if (!Exists(db))
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Adding new entry for album '{FolderPath}'.");
                    }

                    Add(db);
                }
                else
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Updating entry for album '{FolderPath}'.");
                    }

                    Update(db);
                }
            }
        }

        public void Add(MySQLContext db)
        {
            Data.Tables.Albums album = new Data.Tables.Albums()
            {
                Album = AlbumName,
                AlbumArtist = AlbumArtist,
                Discs = TotalDiscs,
                Disc = DiscNumber,
                FolderPath = FolderPath,
                Year = Year,
                LastScanned = DateTime.Now
            };

            db.Albums.Add(album);
            db.SaveChanges();
        }

        public void Update(MySQLContext db)
        {
            var album = db.Albums.FirstOrDefault(a => a.FolderPath == FolderPath);

            if (album != null)
            {
                album.FolderPath = FolderPath;
                album.Album = AlbumName;
                album.AlbumArtist = AlbumArtist;
                album.Discs = TotalDiscs;
                album.Disc = DiscNumber;
                album.Year = Year;
                album.LastScanned = DateTime.Now;

                db.SaveChanges();
            }
        }
    }
}
