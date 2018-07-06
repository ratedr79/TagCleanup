using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagCleanup
{
    public static class StringExtentions
    {
        public static string TagDataTrim(this string tag)
        {
            var terminator = "\0";

            if (tag.EndsWith(terminator))
            {
                return tag.Substring(0, tag.Length - terminator.Length);
            }

            return tag;
        }

        public static string Utf16ToUtf8(this string utf16String)
        {
            byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf16String);
            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);

            return Encoding.Default.GetString(utf8Bytes);
        }
    }
}
