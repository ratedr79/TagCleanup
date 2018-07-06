using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup.Data
{
    public class TagData : FileData
    {
        public bool ValidV1Tag { get; set; }
        public bool ValidV2Tag { get; set; }
        public bool TagDataLoaded { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }
        public string AlbumArtist { get; set; }
        public string DiscNumber { get; set; }
        public string DiscCount { get; set; }
        public string DiscNumberAndCount { get; set; }
        public string TrackNumber { get; set; }
        public string Title { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string OtherTags { get; set; }
        public bool ContainsOtherTags { get; set; }

        public static string[] ValidFrameNames = (ConfigurationManager.AppSettings["AllowedID3Frames"]
                                                    ?? "TIT2,TPE1,TPE2,TALB,TYER,TCOM,TCON,TRCK,TPOS,TPUB,TCOP,TDRC").Split(',');
        public static bool IgnoreITunesTotalTags = bool.Parse(ConfigurationManager.AppSettings["IgnoreTotalDiscTotalTracks"] ?? "false");

        public void LoadTagData(TagLib.Id3v2.Tag id3V2Tag, TagLib.Id3v1.Tag id3V1Tag, bool forceIDV2Tag = false)
        {
            ValidV1Tag = id3V1Tag != null;
            ValidV2Tag = id3V2Tag != null;

            if ((forceIDV2Tag && id3V2Tag == null)
                || (id3V1Tag == null && id3V2Tag == null))
            {
                TagDataLoaded = false;
                return;
            }

            if (id3V2Tag != null)
            {
                LoadV2Tag(id3V2Tag);
            }
            else if (id3V1Tag != null)
            {
                LoadV1Tag(id3V1Tag);
            }

            TagDataLoaded = true;
        }

        private void LoadV1Tag(TagLib.Id3v1.Tag id3Tag)
        {
            Album = id3Tag.Album;
            Artist = id3Tag.JoinedPerformers;
            AlbumArtist = id3Tag.JoinedAlbumArtists;
            DiscNumber = id3Tag.Disc != 0 ? id3Tag.Disc.ToString() : "";
            DiscCount = id3Tag.DiscCount != 0 ? id3Tag.DiscCount.ToString() : "";
            DiscNumberAndCount = "";
            TrackNumber = id3Tag.Track.ToString();
            Year = id3Tag.Year.ToString();
            Genre = id3Tag.JoinedGenres;
            Title = id3Tag.Title;
            ContainsOtherTags = !string.IsNullOrEmpty(id3Tag.Comment);
            OtherTags = ContainsOtherTags ? string.Join(",", Data.ID3V2Definitions.FrameDefintions["COMM"]) : "";
        }

        private void LoadV2Tag(TagLib.Id3v2.Tag id3Tag)
        {
            Album = id3Tag.Album;
            Artist = id3Tag.JoinedPerformers;
            AlbumArtist = id3Tag.JoinedAlbumArtists;
            DiscNumber = id3Tag.Disc != 0 ? id3Tag.Disc.ToString() : "";
            DiscCount = id3Tag.DiscCount != 0 ? id3Tag.DiscCount.ToString() : "";
            DiscNumberAndCount = id3Tag.Frames.Any(f => f.FrameId.ToString() == "TPOS")
                                    ? id3Tag.Frames.First(f => f.FrameId.ToString() == "TPOS").ToString()
                                    : "";
            TrackNumber = id3Tag.Track.ToString();
            Year = id3Tag.Year.ToString();
            Genre = id3Tag.JoinedGenres;
            Title = id3Tag.Title;
            ContainsOtherTags = id3Tag.Frames.Any(f => !ValidFrameNames.Contains(f.FrameId.ToString()));

            if (ContainsOtherTags)
            {
                List<string> invalidTagsDescriptions = new List<string>();
                var invalidTagsFrameIDs = id3Tag.Frames.Where(f => !ValidFrameNames.Contains(f.FrameId.ToString())).ToList();

                foreach (var frame in invalidTagsFrameIDs)
                {
                    try
                    {
                        //The frame can be of multiple types, so let's try and get the description
                        var frameProperties = frame.GetType().GetProperties();
                        var frameDescription = frameProperties.FirstOrDefault(p => p.Name == "Description");
                        var descriptionValue = (string)frameDescription.GetValue(frame, null);

                        if (string.IsNullOrWhiteSpace(descriptionValue))
                        {
                            if (Data.ID3V2Definitions.FrameDefintions.ContainsKey(frame.FrameId.ToString()))
                            {
                                descriptionValue = Data.ID3V2Definitions.FrameDefintions[frame.FrameId.ToString()];
                            }
                            else
                            {
                                descriptionValue = frame.FrameId.ToString();
                            }
                        }

                        invalidTagsDescriptions.Add(descriptionValue);

                        frameDescription = null;
                        frameProperties = null;
                    }
                    catch
                    {
                        if (Data.ID3V2Definitions.FrameDefintions.ContainsKey(frame.FrameId.ToString()))
                        {
                            invalidTagsDescriptions.Add(Data.ID3V2Definitions.FrameDefintions[frame.FrameId.ToString()]);
                        }
                        else
                        {
                            invalidTagsDescriptions.Add(frame.FrameId.ToString());
                        }
                    }
                }

                OtherTags = string.Join(",", invalidTagsDescriptions);

                if ((OtherTags.ToLower() == "totaldiscs,totaltracks" || OtherTags.ToLower() == "totaltracks,totaldiscs"
                    || OtherTags.ToLower() == "totaldiscs" || OtherTags.ToLower() == "totaltracks")
                    && IgnoreITunesTotalTags)
                {
                    ContainsOtherTags = false;
                    OtherTags = "";
                }
            }
            else
            {
                OtherTags = "";
            }
        }
    }
}
