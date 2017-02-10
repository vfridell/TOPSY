using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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

        private void Analyze_OnClick(object sender, RoutedEventArgs e)
        {
            SOMAnalysisWindow analysisWindow = new SOMAnalysisWindow();
            SOMTrainer trainer = new SOMTrainer();
            int numberOfItems = AnalysisDataRepository.AnalysisDataList[0].GetSomWeightsVector().Count;
            SOMLattice lattice = new SOMLattice(10, 10, numberOfItems);
            lattice.Initialize();
            List<SOMWeightsVector> weightsList =
                AnalysisDataRepository.AnalysisDataList.Select(a => a.GetSomWeightsVector()).ToList();
            analysisWindow.Show();
            //trainer.Train2(lattice, weightsList, new LatticeRenderer(analysisWindow));
            trainer.Train(lattice, weightsList, new LatticeRenderer(analysisWindow)).ContinueWith((t) => {
                SOMLattice.WriteLatticeData(lattice);
            });
        }

        private void Test_OnClick(object sender, RoutedEventArgs e)
        {
            SOMAnalysisWindow analysisWindow = new SOMAnalysisWindow();
            SOMLattice lattice = new SOMLattice(10, 10, 3);
            lattice.InitializeTest();
            var renderer = new LatticeRenderer(analysisWindow);
            renderer.Render(lattice, 1);
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
                    var renderer = new LatticeRenderer(analysisWindow);
                    renderer.Render(lattice, 1);
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
