using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FileHelpers;
using FileHelpers.Dynamic;

namespace TOPSY
{
    /// <summary>
    /// Interaction logic for ComparisonDataViewer.xaml
    /// </summary>
    public partial class ComparisonDataViewer : UserControl
    {
        public ComparisonDataViewer()
        {
            InitializeComponent();
        }

        private void dataGrid_Drop(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filename = files[0];

                var detector = new FileHelpers.Detection.SmartFormatDetector();
                var formats = detector.DetectFileFormat(filename);

                foreach (var format in formats)
                {
                    Console.WriteLine("Format Detected, confidence:" + format.Confidence + "%");
                    var delimited = format.ClassBuilderAsDelimited;

                    Console.WriteLine("    Delimiter:" + delimited.Delimiter);
                    Console.WriteLine("    Fields:");

                    foreach (var field in delimited.Fields)
                    {
                        if (format.Confidence == 100)
                        {
                            dataGrid.Items.Add(new { label = field.FieldName, value = field.FieldType });
                            Console.WriteLine("        " + field.FieldName + ": " + field.FieldType);
                        }
                    }

                    int i = 0;
                    foreach(DelimitedFieldBuilder field in format.ClassBuilderAsDelimited.Fields)
                    {
                        if (!isValidIdentifier(field.FieldName)) field.FieldName = makeValidIdentifier(field.FieldName, i);
                        i++;
                    }
                    //format.ClassBuilderAsDelimited.AddField("", typeof(string));
                    
                    //string sourceCode = format.ClassBuilderAsDelimited.GetClassSourceCode(NetLanguage.CSharp);
                    Type t = format.ClassBuilderAsDelimited.CreateRecordClass();
                    //var record = Activator.CreateInstance(t);
                    var engine = new DelimitedFileEngine(t);
                    object[] records = engine.ReadFile(filename);
                }

                

            }
            else
            {
                MessageBox.Show("Boo", "Invalid object dropped", MessageBoxButton.OK);
            }
        }

        static bool isValidIdentifier(string value)
        {
            Microsoft.CSharp.CSharpCodeProvider prov = new Microsoft.CSharp.CSharpCodeProvider();
            return prov.IsValidIdentifier(value);
        }

        static string makeValidIdentifier(string original, int index = 0)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in original)
            {
                if(char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return string.Format("a{0}_{1}", index, sb);
        }
    }
}
