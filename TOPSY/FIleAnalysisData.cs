using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TOPSY
{
    public class FileAnalysisData
    {
        private string _filename;
        private Dictionary<char, int> _specialCharacterHistogram = new Dictionary<char, int>();
        private Dictionary<char, int> _regularCharacterHistogram = new Dictionary<char, int>();
        private Dictionary<int, int> _lineLengthHistogram = new Dictionary<int, int>();
        private Dictionary<char, int> _columnsByDelimiter = new Dictionary<char, int>();
        private double _avgCharactersPerLine;
        private double _stdDevLineLength;
        private int _totalCharacters;
        private int _totalLines;
        private int _firstLineLength;
        private bool _headerExists;
        private char _mostCommonSpecialChar;
        private double _ratioMostCommonSecondMost;
        private bool _crlfTerminated;
        private double _ratioSpecialCharToRegular;
        private int _firstLineNumWords;
        private double _ratioCRtoLF;
        private int _totalWords;
        private char _secondMostCommonSpecialChar;

        //private double _avgCharactersPerLineSquashed;
        //private double _totalLinesSquashed;  //1/(1 + e^(.001*(5000-x)))

        public int TotalLines => _totalLines;
        public int TotalCharacters => _totalCharacters;
        public int TotalWords=> _totalWords;
        public bool HeaderExists => _headerExists;
        public int FirstLineLength => _firstLineLength;
        public double AvgCharactersPerLine => _avgCharactersPerLine;
        public double StdDeviationCharsPerLine => _stdDevLineLength;
        public int MaxLengthLine => _lineLengthHistogram.Max(kvp => kvp.Key);
        public int MinLengthLine => _lineLengthHistogram.Min(kvp => kvp.Key);
        public string Filename => _filename;
        public int NumberOfDistinctSpecialChars => _specialCharacterHistogram.Keys.Count;
        public char MostCommonSpecialChar => _mostCommonSpecialChar;
        public char SecondMostCommonSpecialChar => _secondMostCommonSpecialChar;
        public double RatioMostCommonSecondMost => _ratioMostCommonSecondMost;
        public double RatioSpecialCharToRegular => _ratioSpecialCharToRegular;
        public bool CRLF_Terminated => _crlfTerminated;
        public double RatioCR_to_LF => _ratioCRtoLF;
        public int FirstLineNumWords => _firstLineNumWords;
        public double RatioFirstLineWordsToAverageWords => (double)_firstLineNumWords/((double)_totalWords/_totalLines);

        public SOMWeightsVector GetSomWeightsVector()
        {
            SOMWeightsVector vector = new SOMWeightsVector();
            //vector.Add(_avgCharactersPerLine);
            //vector.Add(_stdDevLineLength);
            vector.Add(NumberOfDistinctSpecialChars);
            vector.Add(RatioSpecialCharToRegular);
            vector.Add(MostCommonSpecialChar);
            vector.Add(SecondMostCommonSpecialChar);
            //vector.Add(RatioMostCommonSecondMost);
            //vector.Add(CRLF_Terminated ? 1.0 : 0.0);
            return vector;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Filename: {Filename}\n");
            sb.Append($"Total Chars: {TotalCharacters}\n");
            sb.Append($"Most Common Special Character: " + (char.IsControl(MostCommonSpecialChar) ? $"<{(int)MostCommonSpecialChar}>\n" : $"{MostCommonSpecialChar}\n"));
            sb.Append($"Second Most Common Special Character: " + (char.IsControl(SecondMostCommonSpecialChar) ? $"<{(int)SecondMostCommonSpecialChar}>\n" : $"{SecondMostCommonSpecialChar}\n"));
            sb.Append($"First to Second Special Character Ratio: {RatioMostCommonSecondMost}\n");
            sb.Append($"Special Character To Regular Character Ratio: {RatioSpecialCharToRegular}\n");
            sb.Append($"Distinct Special Characters: {NumberOfDistinctSpecialChars}\n");
            sb.Append($"Number of 'words' on the First Line: {FirstLineNumWords}\n");
            sb.Append($"Average 'words' per Line: {((double)_totalWords / _totalLines)}\n");
            sb.Append($"Ratio of 'words' on the First Line to Average Words per line: {RatioFirstLineWordsToAverageWords}\n");
            sb.Append($"CRLF Terminated: {CRLF_Terminated}\n");
            sb.Append($"Ratio CR to LF: {RatioCR_to_LF}\n");
            sb.Append($"Total Lines: {TotalLines}\n");
            sb.Append($"Max Line Length: {MaxLengthLine}\n");
            sb.Append($"Min Line Length: {MinLengthLine}\n");
            sb.Append($"First Line Length: {FirstLineLength}\n");
            sb.Append($"Avg Chars per Line: {AvgCharactersPerLine}\n");
            sb.Append($"Std Deviation Chars per Line: {StdDeviationCharsPerLine}\n");

            sb.Append("==== Regular Chars Histogram ====\n");
            AppendHistogram(_regularCharacterHistogram, sb);

            sb.Append("==== Special Chars Histogram ====\n");
            foreach (KeyValuePair<char, int> kvp in _specialCharacterHistogram.OrderByDescending(kvp => kvp.Value))
            {
                if (char.IsControl(kvp.Key))
                    sb.Append($"<{(int)kvp.Key}> => {kvp.Value}\n");
                else
                    sb.Append($"{kvp.Key} => {kvp.Value}\n");
            }
            
            sb.Append("==== Line Length Histogram ====\n");
            AppendHistogram(_lineLengthHistogram, sb);

            return sb.ToString();
        }

        public void AppendHistogram<T, Q>(Dictionary<T, Q> histogram, StringBuilder sb)
        {
            foreach (KeyValuePair<T, Q> kvp in histogram.OrderByDescending(kvp => kvp.Value))
            {
                sb.Append($"{kvp.Key} => {kvp.Value}\n");
            }
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
            char EOF = '\uffff';
            byte[] finalByte = {lineFeed};
            byte carriageReturn = 0x0D;
            int lineBufferSize = 2048;

            FileAnalysisData analysisData = new FileAnalysisData();
            analysisData._filename = filename;
            analysisData._totalCharacters = 0;
            analysisData._totalLines = 0;
            analysisData._avgCharactersPerLine = 0.0;
            analysisData._stdDevLineLength = 0.0;
            analysisData._totalWords = 0;
            Regex wordlikeRegex = new Regex(@"[ \w][ \w][ \w]+");
            using (TextReader reader = new StreamReader(File.OpenRead(filename)))
            {
                char c = (char) reader.Read();
                int charactersThisLine = 0;
                var thisLine = new char[lineBufferSize];
                while (c != EOF)
                {
                    if(charactersThisLine < lineBufferSize) thisLine[charactersThisLine] = c;
                    charactersThisLine++;
                    if (c != space && (char.IsControl(c) || char.IsSeparator(c) || char.IsPunctuation(c) || char.IsHighSurrogate(c) || char.IsLowSurrogate(c) || char.IsSymbol(c)))
                    {
                        if (!analysisData._specialCharacterHistogram.ContainsKey(c)) analysisData._specialCharacterHistogram.Add(c, 0);
                        analysisData._specialCharacterHistogram[c]++;
                    }
                    else
                    {
                        if (!analysisData._regularCharacterHistogram.ContainsKey(c)) analysisData._regularCharacterHistogram.Add(c, 0);
                        analysisData._regularCharacterHistogram[c]++;
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

                        analysisData._totalWords += wordlikeRegex.Matches(new string(thisLine)).Count;
                        if (analysisData._totalLines == 1)
                        {
                            analysisData._firstLineLength = charactersThisLine;
                            analysisData._firstLineNumWords = analysisData._totalWords;
                        }
                        charactersThisLine = 0;

                        thisLine = new char[lineBufferSize];
                    }
                    c = (char) reader.Read();
                }
            }
                               
            analysisData._ratioCRtoLF = 0.0;
            if (analysisData._specialCharacterHistogram.ContainsKey((char) lineFeed) &&
                analysisData._specialCharacterHistogram.ContainsKey((char) carriageReturn))
            {
                analysisData._ratioCRtoLF = (double)analysisData._specialCharacterHistogram[(char) carriageReturn] / analysisData._specialCharacterHistogram[(char) lineFeed];
                analysisData._crlfTerminated = analysisData._specialCharacterHistogram[(char)lineFeed] == analysisData._specialCharacterHistogram[(char)carriageReturn];
            }

            if (analysisData._crlfTerminated) analysisData._specialCharacterHistogram.Remove((char) carriageReturn);

            if (analysisData.TotalCharacters > 0 && analysisData._totalLines == 0)
            {
                analysisData._totalLines = 1;
                analysisData._firstLineLength = analysisData._totalCharacters;
            }

            analysisData._ratioSpecialCharToRegular = (double)analysisData._specialCharacterHistogram.Values.Sum()/
                                                      analysisData._regularCharacterHistogram.Values.Sum();
            analysisData._stdDevLineLength = Math.Sqrt(analysisData._stdDevLineLength / (analysisData._totalLines - 2));
            //analysisData._headerExists
            var orderedSpecialCharHistogram = analysisData._specialCharacterHistogram.OrderByDescending(kvp => kvp.Value);
            analysisData._mostCommonSpecialChar = orderedSpecialCharHistogram.First().Key;
            analysisData._secondMostCommonSpecialChar = orderedSpecialCharHistogram.Take(2).Last().Key;
            analysisData._ratioMostCommonSecondMost = (double)orderedSpecialCharHistogram.First().Value / orderedSpecialCharHistogram.ToList()[1].Value;
            return analysisData;
        }

    }
}
