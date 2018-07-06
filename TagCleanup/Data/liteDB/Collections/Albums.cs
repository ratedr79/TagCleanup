using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Linq.Mapping;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup.Data.liteDB.Collections
{
    public class Albums
    {
        public int ID { get; set; }

        public string FolderPath { get; set; }

        public string Album { get; set; }

        public string AlbumArtist { get; set; }

        public string Discs { get; set; }

        public string Disc { get; set; }

        public string Year { get; set; }

        public DateTime LastScanned { get; set; }
    }
}
