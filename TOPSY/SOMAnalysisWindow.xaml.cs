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
using System.Windows.Shapes;

namespace TOPSY
{
    /// <summary>
    /// Interaction logic for SOMAnalysisWindow.xaml
    /// </summary>
    public partial class SOMAnalysisWindow : Window
    {
        public double MaxWeight = Double.MaxValue;
        public double MinWeight = 0;


        public SOMAnalysisWindow()
        {
            InitializeComponent();
        }


        public void Render(SOMLattice lattice, int iteration)
        {
            for (int x = 0; x < lattice.Height; x++)
            {
                for (int y = 0; y < lattice.Width; y++)
                {
                    SOMNode node = lattice.GetNode(x, y);
                    DrawNode(node);
                }
            }
        }

        public void Render(SOMLattice lattice)
        {
            Dictionary<Tuple<int,int>, List<FileAnalysisData>> LocationPlacementCount = new Dictionary<Tuple<int, int>, List<FileAnalysisData>>();
            foreach (FileAnalysisData item in AnalysisDataRepository.AnalysisDataList)
            {
                SOMNode bestNode = lattice.GetBestMatchingUnitNode(item.GetSomWeightsVector());
                var loc = new Tuple<int, int>(bestNode.X, bestNode.Y);
                if (!LocationPlacementCount.ContainsKey(loc)) LocationPlacementCount.Add(loc, new List<FileAnalysisData>());
                LocationPlacementCount[loc].Add(item);
            }

            foreach (var kvp in LocationPlacementCount)
            {
                DrawNode(kvp.Key, kvp.Value);
            }

        }

        private void DrawNode(Tuple<int, int> loc, List<FileAnalysisData> fileAnalysisDataList)
        {
            TextBlock tb = new TextBlock();
            tb.Text = fileAnalysisDataList.Count.ToString();
            tb.Foreground = new SolidColorBrush(Colors.Black);
            tb.FontSize = 10.0;

            Canvas.SetZIndex(tb, 99);
            Canvas.SetLeft(tb, (loc.Item1 * 11) + 2);
            Canvas.SetBottom(tb, (loc.Item2 * 11) - 2);
            tb.MouseLeftButtonDown += (sender, args) =>
            {
                int x = loc.Item1;
                int y = loc.Item2;
/*                fileAnalysisDataList
                Piece piece = _imageToPieceMap[(()e.Source)];
                if (piece is BeetleStack) piece = new Beetle(piece.color, piece.number);
                _selectedPiece = piece;
                AddFutureMoveDrawing(piece);*/
            };
            MainCanvas.Children.Add(tb);
            
        }

        private void DrawNode(SOMNode node)
        {
            int rectSize = 10;
            Rectangle rect = new Rectangle();
            rect.Width = rectSize;
            rect.Height = rectSize;

            byte red = GetColorFromWeight(node.GetWeight(0));
            byte green = GetColorFromWeight(node.GetWeight(1));
            byte blue = GetColorFromWeight(node.GetWeight(2));

            rect.Fill =  new SolidColorBrush(Color.FromRgb(red, green, blue));
            //rect.Fill = Brushes.Tan;
            //double imageXOffset = rect.Width / 2;
            //double imageYOffset = rect.Height / 2;

            Canvas.SetZIndex(rect, -1);
            Canvas.SetLeft(rect, node.X * 11);
            Canvas.SetBottom(rect, node.Y * 11);
            MainCanvas.Children.Add(rect);
        }

        private byte GetColorFromWeight(double weight)
        {
            //int squashedValue = (int)(1.0 / (1.0 + Math.Exp(5.0 - (5.0 * weight / 128.0))));
            double expandedValue = weight * 256.0;
            byte b = (byte) expandedValue;
            return b;
        }
    }

}
