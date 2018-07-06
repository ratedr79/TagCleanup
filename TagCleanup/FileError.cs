using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup
{
    public class FileError
    {
        public string FilePath { get; set; }
        public string Details { get; set; }
        public Exception BaseException { get; set; }

        public FileError(Exception ex, string path, string details)
        {
            FilePath = path;
            Details = details;
            BaseException = ex;
        }
    }
}
