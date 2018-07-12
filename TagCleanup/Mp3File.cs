using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup
{
    public class Mp3File
    {
        private ILog Logger { get; set; }
        public FileInfo MediaFile { get; set; }
        public TagLib.Id3v1.Tag ID3V1Tag { get; set; }
        public TagLib.Id3v2.Tag ID3V2Tag { get; set; }
        private string[] FramesToRemove { get; set; }

        private static readonly bool SetDiscAndSetNumber = bool.Parse(ConfigurationManager.AppSettings["SetDiscAndSetNumber"] ?? "false");
        private static readonly bool EditAlternateFile = bool.Parse(ConfigurationManager.AppSettings["ModifyAlternateFile"] ?? "false");
        private static readonly string ScanFilePath = ConfigurationManager.AppSettings["MusicPath"] ?? "";
        private static readonly string EditFilePath = ConfigurationManager.AppSettings["AlternatePath"] ?? "";

        public Mp3File(ILog logger, FileInfo file, string[] framesToRemove = null)
        {
            Logger = logger;
            MediaFile = file;
            FramesToRemove = framesToRemove;

            DateTime tagStart = DateTime.Now;
            GetTags();
            DateTime tagEnd = DateTime.Now;

            if (Globals.VerboseLogging)
            {
                Logger.Info($"'{MediaFile.FullName}' ID3 tags loaded in {(tagEnd - tagStart).TotalMilliseconds} milliseconds.");
            }
        }

        private void GetTags()
        {
            using (TagLib.File file = TagLib.File.Create(MediaFile.FullName))
            {
                DateTime id3v1Start = DateTime.Now;

                try
                {
                    TagLib.Id3v1.Tag v1Tag = (TagLib.Id3v1.Tag)file.GetTag(TagLib.TagTypes.Id3v1);
                    ID3V1Tag = new TagLib.Id3v1.Tag();
                    v1Tag.CopyTo(ID3V1Tag, true);
                }
                catch
                {
                    ID3V1Tag = null;
                }

                DateTime id3v1End = DateTime.Now;

                if (Globals.VerboseLogging)
                {
                    Logger.Info($"'{MediaFile.FullName}' ID3V1 tag loaded in {(id3v1End - id3v1Start).TotalMilliseconds} milliseconds.");
                }

                DateTime id3v2Start = DateTime.Now;

                TagLib.Id3v2.Tag v2Tag = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
                ID3V2Tag = new TagLib.Id3v2.Tag();
                v2Tag.CopyTo(ID3V2Tag, true);

                DateTime id3v2End = DateTime.Now;

                if (Globals.VerboseLogging)
                {
                    Logger.Info($"'{MediaFile.FullName}' ID3V2 tag loaded in {(id3v2End - id3v2Start).TotalMilliseconds} milliseconds.");
                }
            }

            ExecuteTagEdits();
        }

        private void ExecuteTagEdits()
        {
            if (!((SetDiscAndSetNumber && ID3V2Tag.DiscCount == 0 && (ID3V2Tag.Disc == 0 || ID3V2Tag.Disc == 1))
                  || (FramesToRemove != null && FramesToRemove.Any(f => !string.IsNullOrWhiteSpace(f))
                      && ID3V2Tag.Frames.Any(f => FramesToRemove.Contains(f.FrameId.ToString())))))
            {
                return;
            }

            string editFile = MediaFile.FullName;

            if (EditAlternateFile)
            {
                if (string.IsNullOrWhiteSpace(EditFilePath))
                {
                    Logger.Info($"AlternatePath not specified in App.config file.");
                }

                editFile = editFile.Replace(ScanFilePath, EditFilePath);
            }

            TagLib.Id3v2.Tag.UseNumericGenres = false;

            if (File.Exists(editFile))
            {
                if (SetDiscAndSetNumber && ID3V2Tag.DiscCount == 0
                    && (ID3V2Tag.Disc == 0 || ID3V2Tag.Disc == 1))
                {
                    Logger.Info($"Adding disc number and disc total.");

                    using (TagLib.File tagFile = TagLib.File.Create(editFile))
                    {
                        tagFile.Tag.Disc = 1;
                        tagFile.Tag.DiscCount = 1;
                        tagFile.Save();
                    }

                    using (TagLib.File tagFile3 = TagLib.File.Create(editFile))
                    {
                        TagLib.Id3v2.Tag v2Tag = (TagLib.Id3v2.Tag)tagFile3.GetTag(TagLib.TagTypes.Id3v2);
                        ID3V2Tag = new TagLib.Id3v2.Tag();
                        v2Tag.CopyTo(ID3V2Tag, true);
                    }
                }

                if (FramesToRemove != null && FramesToRemove.Any(f => !string.IsNullOrWhiteSpace(f))
                    && ID3V2Tag.Frames.Any(f => FramesToRemove.Contains(f.FrameId.ToString())))
                {
                    Logger.Info($"Removing extra frames from '{editFile}'.");

                    DateTime editStart = DateTime.Now;

                    try
                    {
                        TagLib.Tag tempTag = null;
                        tempTag = new TagLib.Id3v2.Tag();

                        using (TagLib.File tagFile = TagLib.File.Create(editFile))
                        {
                            TagLib.Id3v2.Tag v2Tag = (TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2);
                            v2Tag.CopyTo(tempTag, true);
                            tagFile.RemoveTags(TagLib.TagTypes.Id3v2);
                            tagFile.Save();
                        }

                        foreach (var frame in FramesToRemove.Where(f => !string.IsNullOrWhiteSpace(f)))
                        {
                            ((TagLib.Id3v2.Tag)tempTag).RemoveFrames(TagLib.ByteVector.FromString(frame, TagLib.StringType.UTF8));
                        }

                        using (TagLib.File tagFile2 = TagLib.File.Create(editFile))
                        {
                            tempTag.CopyTo(tagFile2.Tag, true);
                            tagFile2.Save();
                        }

                        using (TagLib.File tagFile3 = TagLib.File.Create(editFile))
                        {
                            TagLib.Id3v2.Tag v2Tag = (TagLib.Id3v2.Tag)tagFile3.GetTag(TagLib.TagTypes.Id3v2);
                            ID3V2Tag = new TagLib.Id3v2.Tag();
                            v2Tag.CopyTo(ID3V2Tag, true);
                        }

                        tempTag = null;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("", ex);
                    }

                    DateTime editEnd = DateTime.Now;

                    if (Globals.VerboseLogging)
                    {
                        Logger.Info($"'{editFile}' ID3V2 tag edited in {(editEnd - editStart).TotalMilliseconds} milliseconds.");
                    }
                }
            }
            else
            {
                Logger.Info($"Alternate file '{editFile}' does not exist.");
            }
        }
    }
}
