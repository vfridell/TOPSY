using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOPSY
{
    public interface IFileType
    {
        IEnumerable<string> GetLines(string filename);
        FileAnalysisData Analyze(string filename);
    }

    public class Ascii : IFileType
    {
        public FileAnalysisData Analyze(string filename)
        {
            return FileAnalysisData.GetAnalysisDataAsciiText(filename);
        }

        public IEnumerable<string> GetLines(string filename)
        {
            throw new NotImplementedException();
        }
    }

    public class Excel : IFileType
    {
        public FileAnalysisData Analyze(string filename)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetLines(string filename)
        {
            throw new NotImplementedException();
        }
    }
}