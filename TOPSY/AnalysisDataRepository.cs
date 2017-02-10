using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOPSY
{
    public static class AnalysisDataRepository
    {
        private static List<FileAnalysisData> _analysisDataList = new List<FileAnalysisData>();

        public static IReadOnlyList<FileAnalysisData> AnalysisDataList => _analysisDataList.AsReadOnly();

        public static void AddData(FileAnalysisData data)
        {
            _analysisDataList.Add(data);
        }

        public static void ClearData()
        {
            _analysisDataList.Clear();
        }
    }
}
