using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
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
using Microsoft.Win32;

namespace TOPSY
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Analyze_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (AnalysisDataRepository.AnalysisDataList.Count == 0) return;

                int numberOfItems = AnalysisDataRepository.AnalysisDataList[0].GetSomWeightsVector().Count;
                SOMLattice lattice = new SOMLattice(20, 20, numberOfItems);
                lattice.Initialize();
                SOMTrainer trainer = new SOMTrainer();
                List<SOMWeightsVector> weightsList = AnalysisDataRepository.AnalysisDataList.Select(a => a.GetSomWeightsVector()).ToList();

                SOMAnalysisWindow analysisWindow = new SOMAnalysisWindow();
                Progress<int> progressReport = new Progress<int>((i) => ProgressBar1.Value = i);
                await Task.Run(() => trainer.Train(lattice, weightsList, progressReport, CancellationToken.None), CancellationToken.None);
                analysisWindow.Render(lattice, 0);
                analysisWindow.Render(lattice);
                analysisWindow.Show();
                SOMLattice.WriteLatticeData(lattice);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Test_OnClick(object sender, RoutedEventArgs e)
        {
            SOMLattice lattice = new SOMLattice(20, 20, 3);
            SOMAnalysisWindow analysisWindow = new SOMAnalysisWindow();
            lattice.InitializeTest();
            analysisWindow.Render(lattice, 1);
            analysisWindow.Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenLatticeFileExecute,
                OpenLatticeFileCanExecute));
        }

        private void OpenLatticeFileExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog(this).Value)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (
                    Stream stream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read,
                        FileShare.None))
                {
                    var lattice = (SOMLattice) formatter.Deserialize(stream);
                    SOMAnalysisWindow analysisWindow = new SOMAnalysisWindow();
                    analysisWindow.Render(lattice, 1);
                    analysisWindow.Show();
                }
            }
        }

        void OpenLatticeFileCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
    }
}
