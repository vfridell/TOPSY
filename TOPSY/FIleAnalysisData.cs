using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOPSY
{
    public class FileAnalysisData
    {
        private string _filename;
        private Dictionary<char, int> _specialCharacterHistogram = new Dictionary<char, int>();
        private Dictionary<int, int> _lineLengthHistogram = new Dictionary<int, int>();
        private Dictionary<char, int> _columnsByDelimiter = new Dictionary<char, int>();
        private double _avgCharactersPerLine;
        private double _stdDevLineLength;
        private int _totalCharacters;
        private int _totalLines;
        private int _firstLineLength;
        private bool _headerExists;

        //private double _avgCharactersPerLineSquashed;
        //private double _totalLinesSquashed;  //1/(1 + e^(.001*(5000-x)))

        public int TotalLines => _totalLines;
        public int TotalCharacters => _totalCharacters;
        public bool HeaderExists => _headerExists;
        public int FirstLineLength => _firstLineLength;
        public double AvgCharactersPerLine => _avgCharactersPerLine;
        public double StdDeviationCharsPerLine => _stdDevLineLength;
        public int MaxLengthLine => _lineLengthHistogram.Max(kvp => kvp.Key);
        public int MinLengthLine => _lineLengthHistogram.Min(kvp => kvp.Key);
        public string Filename => _filename;
        public int NumberOfDistinctSpecialChars => _specialCharacterHistogram.Keys.Count;

        public SOMWeightsVector GetSomWeightsVector()
        {
            SOMWeightsVector vector = new SOMWeightsVector();
            vector.Add(_avgCharactersPerLine);
            vector.Add(_stdDevLineLength);
            vector.Add(NumberOfDistinctSpecialChars);
            return vector;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Filename: {Filename}\n");
            sb.Append($"Total Chars: {TotalCharacters}\n");
            sb.Append($"Distinct Special Characters: {NumberOfDistinctSpecialChars}\n");
            sb.Append($"Total Lines: {TotalLines}\n");
            sb.Append($"Max Line Length: {MaxLengthLine}\n");
            sb.Append($"Min Line Length: {MinLengthLine}\n");
            sb.Append($"First Line Length: {FirstLineLength}\n");
            sb.Append($"Avg Chars per Line: {AvgCharactersPerLine}\n");
            sb.Append($"Std Deviation Chars per Line: {StdDeviationCharsPerLine}\n");

            sb.Append("==== Special Chars Histogram ====\n");
            foreach (KeyValuePair<char, int> kvp in _specialCharacterHistogram.OrderByDescending(kvp => kvp.Value))
            {
                if (char.IsControl(kvp.Key))
                    sb.Append($"<{(int)kvp.Key}> => {kvp.Value}\n");
                else
                    sb.Append($"{kvp.Key} => {kvp.Value}\n");
            }
            
            sb.Append("==== Line Length Histogram ====\n");
            foreach (KeyValuePair<int,int> kvp in _lineLengthHistogram.OrderByDescending(kvp => kvp.Value))
            {
                sb.Append($"{kvp.Key} => {kvp.Value}\n");
            }

            return sb.ToString();
        }

        public static FileAnalysisData GetFileAnalysisData(string filename, IFileType fileType)
        {
            return fileType.Analyze(filename);
        }

        public static FileAnalysisData GetAnalysisDataAsciiText(string filename)
        {
            byte lineFeed = 0x0A;
            byte space = 0x20;
            byte pipe = 0x7C;
            byte backslash = 0x5c;
            byte recordSeperator = 0x1E;
            char EOF = '\uffff';
            byte[] finalByte = {lineFeed};

            FileAnalysisData analysisData = new FileAnalysisData();
            analysisData._filename = filename;
            analysisData._totalCharacters = 0;
            analysisData._totalLines = 0;
            analysisData._avgCharactersPerLine = 0.0;
            analysisData._stdDevLineLength = 0.0;
            using (TextReader reader = new StreamReader(File.OpenRead(filename)))
            {
                char c = (char) reader.Read();
                int charactersThisLine = 0;
                while (c != EOF)
                {
                    charactersThisLine++;
                    if (c != space && (char.IsControl(c) || char.IsSeparator(c) || char.IsPunctuation(c)))
                    {
                        if (!analysisData._specialCharacterHistogram.ContainsKey(c)) analysisData._specialCharacterHistogram.Add(c, 0);
                        analysisData._specialCharacterHistogram[c]++;
                    }
                    if (c == lineFeed)
                    {
                        if(!analysisData._lineLengthHistogram.ContainsKey(charactersThisLine)) analysisData._lineLengthHistogram.Add(charactersThisLine,0);
                        analysisData._lineLengthHistogram[charactersThisLine] += 1;
                        analysisData._totalCharacters += charactersThisLine;

                        // calc average and stddev
                        double tmpM = analysisData._avgCharactersPerLine;
                        analysisData._totalLines++;
                        analysisData._avgCharactersPerLine += (charactersThisLine - tmpM) / analysisData._totalLines;
                        analysisData._stdDevLineLength += (charactersThisLine - tmpM) * (charactersThisLine - analysisData._avgCharactersPerLine);
                        if (analysisData._totalLines == 1) analysisData._firstLineLength = charactersThisLine;
                        charactersThisLine = 0;
                    }
                    c = (char) reader.Read();
                }
            }


            if (analysisData.TotalCharacters > 0 && analysisData._totalLines == 0)
            {
                analysisData._totalLines = 1;
                analysisData._firstLineLength = analysisData._totalCharacters;
            }
            analysisData._stdDevLineLength = Math.Sqrt(analysisData._stdDevLineLength / (analysisData._totalLines - 2));
            //analysisData._headerExists
            return analysisData;
        }

    }
}
