using Id3Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup
{
    public static class TagHandlerExtentions
    {
        public static string AlbumArtist(this TagHandler tag)
        {
            if (tag.FrameModel.Any(f => f.FrameId == "TPE2"))
            {
                return tag.FrameModel.FirstOrDefault(f => f.FrameId == "TPE2").ToString();
            }

            return "";
        }
    }
}
