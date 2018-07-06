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
    public class MediaFiles
    {
        public int ID { get; set; }

        public string FilePath { get; set; }

        public string Album { get; set; }

        public string Artist { get; set; }

        public string AlbumArtist { get; set; }

        public string DiscNumber { get; set; }

        public string DiscCount { get; set; }

        public string TrackNumber { get; set; }

        public string Year { get; set; }

        public string Genre { get; set; }

        public DateTime Created { get; set; }

        public DateTime Changed { get; set; }

        public DateTime LastScanned { get; set; }

        public string Title { get; set; }

        public bool ContainsOtherTags { get; set; }

        public string OtherTags { get; set; }

        public bool ValidV1Tag { get; set; }

        public bool ValidV2Tag { get; set; }
    }
}
