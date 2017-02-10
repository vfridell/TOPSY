using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TOPSY
{
    public static class FileTypeDetector
    {
        public static IFileType Detect(string filename)
        {
            if(!File.Exists(filename)) throw new FileNotFoundException(filename);

            FileInfo fileInfo = new FileInfo(filename);
            switch (fileInfo.Extension)
            {
                case ".txt":
                case ".csv":
                    return new Ascii();
                case ".xls":
                case ".xlsx":
                    return new Excel();
                default:
                    return new Ascii();
                    //throw new Exception($"Unsupported file type {fileInfo.Extension}");
            }
        }
    }
}
