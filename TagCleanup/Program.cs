using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using log4net;
using System.IO;

namespace TagCleanup
{
    class Program
    {
        public static readonly string ExecutionPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly char[] YesResponse = { '1', 'y' };

        static void Main(string[] args)
        {
            Globals.VerboseLogging = bool.Parse(ConfigurationManager.AppSettings["VerboseLogging"] ?? "false");

#if DEBUG
            Console.WriteLine($"Enable verbose logging?");
            var verboseKey = Console.ReadKey();

            Globals.VerboseLogging = YesResponse.Contains(verboseKey.KeyChar);
            Console.WriteLine($"");
#endif

            bool scanForFiles = bool.Parse(ConfigurationManager.AppSettings["ScanFiles"] ?? "true");
            bool resetDatabase = bool.Parse(ConfigurationManager.AppSettings["ResetDatabase"] ?? "false");
            bool clearDatabase = bool.Parse(ConfigurationManager.AppSettings["ClearDatabase"] ?? "false");
            bool checkForErrors = bool.Parse(ConfigurationManager.AppSettings["CheckForErrors"] ?? "true");

#if DEBUG
            Console.WriteLine($"Scan for files?");
            var scanKey = Console.ReadKey();

            scanForFiles = YesResponse.Contains(scanKey.KeyChar);
            Console.WriteLine($"");

            if (scanForFiles)
            {
                Console.WriteLine($"Reset the database?");
                var resetKey = Console.ReadKey();

                resetDatabase = YesResponse.Contains(resetKey.KeyChar);
                Console.WriteLine($"");

                if (!resetDatabase)
                {
                    Console.WriteLine($"Clear existing data?");
                    var clearKey = Console.ReadKey();

                    clearDatabase = YesResponse.Contains(clearKey.KeyChar);
                    Console.WriteLine($"");
                }
            }

            Console.WriteLine($"");
#endif

            if (scanForFiles)
            {
                var actions = new FileScanner(Logger);
                actions.ScanFolder(ConfigurationManager.AppSettings["MusicPath"], new string[] { "mp3" }, true, clearDatabase, resetDatabase);

                Console.WriteLine($"Directories Scanned: {actions.DirectoriesProcessed.ToString()}");
                Console.WriteLine($"Directories Skipped: {actions.DirectoriesSkipped.ToString()}");
                Console.WriteLine($"Files Scanned: {actions.FilesProcessed.ToString()}");
                Console.WriteLine($"Files Skipped: {actions.FilesSkipped.ToString()}");
                Console.WriteLine($"Scan Completed: {actions.ScanComplete.ToString()}");
                Console.WriteLine($"Scan Started: {actions.ScanStart.ToString()}");
                Console.WriteLine($"Scan Ended: {actions.ScanEnd.ToString()}");
                Console.WriteLine($"Number of Errors: {actions.Errors.Count.ToString()}");
            }

            if (checkForErrors)
            {
                var sbTagLog = new StringBuilder();
                var checker = new TagChecker(Logger);
                checker.CheckForErrors();

                LogAndWrite(sbTagLog, $"Tag data checked in {checker.FilesChecked} files.");
                LogAndWrite(sbTagLog, $"Tag data check Started: {checker.ScanStart.ToString()}");
                LogAndWrite(sbTagLog, $"Tag data check Ended: {checker.ScanEnd.ToString()}");
                LogAndWrite(sbTagLog, "");

                if (checker.TagErrorDictionary.Count() > 0)
                {
                    Console.WriteLine("");

                    SortedList<string, string> sorted = new SortedList<string, string>();

                    using (var db = new Data.MySQLContext(Logger))
                    {
                        foreach (var erroredFile in checker.TagErrorDictionary.Keys)
                        {
                            sorted.Add(erroredFile, erroredFile);

                            var mediaFile = db.MediaFiles.FirstOrDefault(mf => mf.FilePath == erroredFile);

                            if (mediaFile != null)
                            {
                                db.MediaFiles.Remove(mediaFile);
                            }

                            var albumPath = Path.GetDirectoryName(erroredFile);
                            var mediaAlbum = db.Albums.FirstOrDefault(a => a.FolderPath == albumPath);

                            if (mediaAlbum != null)
                            {
                                db.Albums.Remove(mediaAlbum);
                            }

                            db.SaveChanges();
                        }
                    }

                    foreach (var erroredFile in sorted.Keys)
                    {
                        LogAndWrite(sbTagLog, $"ERROR IN FILE {erroredFile}");

                        foreach (var error in checker.TagErrorDictionary[erroredFile])
                        {
                            LogAndWrite(sbTagLog, $"  > {error}");
                        }
                    }

                    LogAndWrite(sbTagLog, "");
                }
                else
                {
                    LogAndWrite(sbTagLog, "There were no issues with tag data found.");
                    LogAndWrite(sbTagLog, "");
                }

                var outputFileName = $"TagData_{DateTime.Now.ToString("yyyyMMddhhmmss")}.txt";
                var outputDirectory = new DirectoryInfo(Path.Combine(Program.ExecutionPath, "OutputLogs"));

                if (!outputDirectory.Exists)
                {
                    outputDirectory.Create();
                }

                string outputFile = Path.Combine(outputDirectory.FullName, outputFileName);
                File.WriteAllText(outputFile, sbTagLog.ToString());
            }

#if DEBUG
            Console.WriteLine($"Press any key to exit.");
            var anyKey = Console.ReadKey();
#endif
        }

        public static void LogAndWrite(StringBuilder sb, string line)
        {
            sb.AppendLine(line);
            Console.WriteLine(line);
        }
    }
}
