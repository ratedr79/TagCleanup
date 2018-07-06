using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup.Data
{
    public class FileData
    {
        public string FilePath { get; set; }
        public DateTime Created { get; set; }
        public DateTime Changed { get; set; }
        public DateTime LastScanned { get; set; }

        public void LoadFileData(FileInfo file)
        {
            FilePath = file.FullName;
            Created = file.CreationTime;
            Changed = file.LastWriteTime;
            LastScanned = DateTime.Now;
        }
    }
}
