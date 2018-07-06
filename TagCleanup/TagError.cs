using Id3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup
{
    public class TagError
    {
        public string FilePath { get; set; }
        public string Details { get; set; }
        public Data.TagData ID3V2Tag { get; set; }

        public TagError(string path, string details, TagLib.Id3v2.Tag id3V2Tag, TagLib.Id3v1.Tag id3V1Tag)
        {
            FilePath = path;
            Details = details;
            ID3V2Tag = new Data.TagData();
            ID3V2Tag.LoadTagData(id3V2Tag, id3V1Tag);
        }

        public override string ToString()
        {
            return String.Concat(FilePath, Details);
        }
    }
}
