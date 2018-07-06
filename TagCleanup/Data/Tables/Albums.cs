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
    [Table(Name = "Albums")]
    public class Albums
    {
        [Column(Name = "ID", IsDbGenerated = true, IsPrimaryKey = true, DbType = "INTEGER")]
        [Key]
        [System.ComponentModel.DataAnnotations.Schema.Index(IsUnique = true)]
        public int ID { get; set; }

        [Column(Name = "FolderPath", DbType = "VARCHAR")]
        [StringLength(500)]
        [System.ComponentModel.DataAnnotations.Schema.Index(IsUnique = false)]
        public string FolderPath { get; set; }

        [Column(Name = "Album", DbType = "VARCHAR")]
        [StringLength(500)]
        public string Album { get; set; }

        [Column(Name = "AlbumArtist", DbType = "VARCHAR")]
        [StringLength(500)]
        public string AlbumArtist { get; set; }

        [Column(Name = "Discs", DbType = "VARCHAR")]
        [StringLength(45)]
        public string Discs { get; set; }

        [Column(Name = "Disc", DbType = "VARCHAR")]
        [StringLength(45)]
        public string Disc { get; set; }

        [Column(Name = "Year", DbType = "VARCHAR")]
        [StringLength(45)]
        public string Year { get; set; }

        [Column(Name = "LastScanned", DbType = "DATETIME")]
        public DateTime LastScanned { get; set; }
    }
}
