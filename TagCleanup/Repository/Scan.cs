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
    public class Scan
    {
        public string FolderPath { get; set; }
        public DateTime LastScanned { get; set; }

        private ILog Logger { get; set; }

        public Scan(ILog logger, string path) : this (logger, path, DateTime.Now)
        {
        }

        public Scan(ILog logger, string path, DateTime lastScanned)
        {
            Logger = logger;
            FolderPath = path;
            LastScanned = lastScanned;
        }

        public bool Exists(MySQLContext db)
        {
            return db.Scans.Any(s => s.FolderPath == FolderPath);
        }

        public void AddOrUpdate()
        {
            using (var db = new MySQLContext(Logger))
            {
                if (!Exists(db))
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Adding new entry for scan of '{FolderPath}'.");
                    }

                    Add(db);
                }
                else
                {
                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"Updating entry for scan of '{FolderPath}'.");
                    }

                    Update(db);
                }
            }
        }

        public void Add(MySQLContext db)
        {
            Data.Tables.Scans scan = new Data.Tables.Scans()
            {
                FolderPath = FolderPath,
                LastScanned = LastScanned
            };

            db.Scans.Add(scan);
            db.SaveChanges();
        }

        public void Update(MySQLContext db)
        {
            var scan = db.Scans.FirstOrDefault(s => s.FolderPath == FolderPath);

            if (scan != null)
            {
                scan.LastScanned = LastScanned;

                db.SaveChanges();
            }
        }
    }
}
