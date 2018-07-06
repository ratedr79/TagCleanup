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

namespace TagCleanup.Data.Tables
{
    [Table(Name = "MediaFiles")]
    public class MediaFiles
    {
        [Column(Name = "ID", IsDbGenerated = true, IsPrimaryKey = true, DbType = "INTEGER")]
        [Key]
        [System.ComponentModel.DataAnnotations.Schema.Index(IsUnique = true)]
        public int ID { get; set; }

        [Column(Name = "FilePath", DbType = "VARCHAR")]
        [StringLength(500)]
        [System.ComponentModel.DataAnnotations.Schema.Index(IsUnique = false)]
        public string FilePath { get; set; }

        [Column(Name = "Album", DbType = "VARCHAR")]
        [StringLength(500)]
        public string Album { get; set; }

        [Column(Name = "Artist", DbType = "VARCHAR")]
        [StringLength(500)]
        public string Artist { get; set; }

        [Column(Name = "AlbumArtist", DbType = "VARCHAR")]
        [StringLength(500)]
        public string AlbumArtist { get; set; }

        [Column(Name = "DiscNumber", DbType = "VARCHAR")]
        [StringLength(45)]
        public string DiscNumber { get; set; }

        [Column(Name = "DiscCount", DbType = "VARCHAR")]
        [StringLength(45)]
        public string DiscCount { get; set; }

        [Column(Name = "DiscNumberAndCount", DbType = "VARCHAR")]
        [StringLength(45)]
        public string DiscNumberAndCount { get; set; }

        [Column(Name = "TrackNumber", DbType = "VARCHAR")]
        [StringLength(45)]
        public string TrackNumber { get; set; }

        [Column(Name = "Year", DbType = "VARCHAR")]
        [StringLength(45)]
        public string Year { get; set; }

        [Column(Name = "Genre", DbType = "VARCHAR")]
        [StringLength(500)]
        public string Genre { get; set; }

        [Column(Name = "Created", DbType = "DATETIME")]
        public DateTime Created { get; set; }

        [Column(Name = "Changed", DbType = "DATETIME")]
        public DateTime Changed { get; set; }

        [Column(Name = "LastScanned", DbType = "DATETIME")]
        public DateTime LastScanned { get; set; }

        [Column(Name = "Title", DbType = "VARCHAR")]
        [StringLength(500)]
        public string Title { get; set; }

        [Column(Name = "ContainsOtherTags", DbType = "BIT")]
        public bool ContainsOtherTags { get; set; }

        [Column(Name = "OtherTags", DbType = "VARCHAR")]
        [StringLength(500)]
        public string OtherTags { get; set; }

        [Column(Name = "ValidV1Tag", DbType = "BIT")]
        public bool ValidV1Tag { get; set; }

        [Column(Name = "ValidV2Tag", DbType = "BIT")]
        public bool ValidV2Tag { get; set; }
    }
}
