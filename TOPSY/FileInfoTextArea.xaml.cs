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

namespace TOPSY
{
    /// <summary>
    /// Interaction logic for FileInfoTextArea.xaml
    /// </summary>
    public partial class FileInfoTextArea : UserControl
    {
        public FileInfoTextArea()
        {
            InitializeComponent();
        }

        private void TextBox_OnDrop(object sender, DragEventArgs e)
        {
            try
            {
                TextBox.Clear();
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                    string filename = files[0];

                    FileAnalysisData analysisData = FileTypeDetector.Detect(filename).Analyze(filename);
                    AnalysisDataRepository.AddData(analysisData);
                    TextBox.Text = analysisData.ToString();
                }
                else
                {
                    TextBox.Text = "Unsupported object dropped";
                }
            }
            catch (Exception ex)
            {
                TextBox.Text = ex.Message;
            }
        }

        private void TextBox_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}
