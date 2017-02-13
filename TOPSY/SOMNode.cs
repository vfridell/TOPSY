using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TOPSY
{
    // ReSharper disable once InconsistentNaming
    public class SOMTrainer
    {
        private readonly double _startLearningRate = 0.07;
        private readonly int _numIterations = 500;

        private double _latticeRadius;
        private double _timeConstant;

        public SOMTrainer() { }

        public SOMTrainer(double learnRate, int iterations)
        {
            _startLearningRate = learnRate;
            _numIterations = iterations;
        }

        private double GetNeighborhoodRadius(double iteration)
        {
            return _latticeRadius*Math.Exp(-iteration/_timeConstant);
        }

        private double GetDistanceFallOff(double distSq, double radius)
        {
            double radiusSq = radius*radius;
            return Math.Exp(-(distSq)/(2*radiusSq));
        }

        // Train the given lattice based on a vector of input vectors
        public void Train(SOMLattice lattice, List<SOMWeightsVector> inputVectorsList, IProgress<int> progressReport, CancellationToken token )
        {
            _latticeRadius = Math.Max(lattice.Height, lattice.Width)/2;
            _timeConstant = _numIterations/Math.Log(_latticeRadius);
            int iteration = 0;
            double distanceFallOff;
            double learningRate = _startLearningRate;

            while (iteration < _numIterations)
            {
                double neighborhoodRadius = GetNeighborhoodRadius(iteration);
                foreach (SOMWeightsVector currentVector in inputVectorsList)
                {
                    SOMNode bmuNode = lattice.GetBestMatchingUnitNode(currentVector);

                    for (int x = (int)-neighborhoodRadius + bmuNode.X; x <= neighborhoodRadius + bmuNode.X; x++)
                    {
                        int minY = (int)Math.Max(-neighborhoodRadius + bmuNode.Y, -x - neighborhoodRadius);
                        int maxY = (int)Math.Min(neighborhoodRadius + bmuNode.Y, -x + neighborhoodRadius);
                        for (int y = minY; y <= maxY; y++)
                        {
                            if(x < 0 || y < 0) continue;
                            double distance = bmuNode.Distance(lattice.GetNode(x, y));
                            if (distance <= neighborhoodRadius * neighborhoodRadius)
                            {
                                distanceFallOff = GetDistanceFallOff(distance, neighborhoodRadius);
                                lattice.GetNode(x,y).AdjustWeights(currentVector, learningRate, distanceFallOff);
                            }
                            token.ThrowIfCancellationRequested();
                        }
                    }
                }
                iteration++;
                learningRate = _startLearningRate*Math.Exp(-(double)iteration/_numIterations);
                progressReport.Report(iteration / (_numIterations/100));
            }
        }

    }

// ReSharper disable once InconsistentNaming
    [Serializable]
    public class SOMLattice
    {
        private SOMNode[,] _nodes;
        private int _height;
        private int _width;
        private int _numWeights;
        private double[] _maxWeights;
        private double[] _minWeights;

        public int Height => _height;
        public int Width => _width;
        public int NumWeights => _numWeights;

        public double MaxWeight(int i) => _maxWeights[i];
        public double MinWeight(int i) => _minWeights[i];

        public SOMLattice(int height, int width, int numWeights)
        {
            _nodes = new SOMNode[width,height];
            _height = height;
            _width = width;
            _numWeights = numWeights;
            _maxWeights = new double[_numWeights];
            _minWeights = new double[_numWeights];
        }

        public void Initialize()
        {
            for (int x = 0; x < _height; x++)
            {
                for (int y = 0; y < _width; y++)
                {
                    _nodes[x, y] = new SOMNode(_numWeights, x, y);
                    AdjustMaxMinWeights(_nodes[x,y]);
                }
            }
        }

        public void InitializeTest()
        {
            double xstep = .5f / (float)_width;
            double ystep = .5f / (float)_height;
            for (int x = 0; x < _height; x++)
            {
                for (int y = 0; y < _width; y++)
                {
                    _nodes[x, y] = new SOMNode(_numWeights, x, y);
                    _nodes[x, y].SetWeight(0, (xstep * x) + (ystep * y));
                    _nodes[x, y].SetWeight(1, (xstep * x) + (ystep * y));
                    _nodes[x, y].SetWeight(2, (xstep * x) + (ystep * y));

                    AdjustMaxMinWeights(_nodes[x,y]);
                }
            }
        }

        private void AdjustMaxMinWeights(SOMNode node)
        {
            for (int i = 0; i < _numWeights; i++)
            {
                _minWeights[i] = Math.Min(_minWeights[i], node.GetWeight(i));
                _maxWeights[i] = Math.Max(_maxWeights[i], node.GetWeight(i));
            }
        }

        public SOMNode GetNode(int x, int y)
        {
            return _nodes[x, y];
        }

        public SOMNode GetBestMatchingUnitNode(SOMWeightsVector input)
        {
            SOMNode bmuNode = _nodes[0, 0];
            double bestDistance = input.EuclideanDistance(bmuNode.WeightsVector);
            double currentDistance;

            for (int x = 0; x < _height; x++)
            {
                for (int y = 0; y < _width; y++)
                {
                    currentDistance = input.EuclideanDistance(_nodes[x, y].WeightsVector);
                    if (currentDistance < bestDistance)
                    {
                        bmuNode = _nodes[x, y];
                        bestDistance = currentDistance;
                    }
                }
            }
            return bmuNode;
        }

        public void AdjustWeights(int x, int y, SOMWeightsVector input, double learningRate, double distanceFallOff)
        {
            GetNode(x, y).AdjustWeights(input, learningRate, distanceFallOff);
            AdjustMaxMinWeights(GetNode(x,y));
        }

        public static string WriteLatticeData(SOMLattice lattice)
        {
            string filename = $"lattice_{lattice.Height}X{lattice.Width}_{DateTime.Now.ToString("yyyy.MM.dd.HHmmss")}";
            BinaryFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(filename + ".bin", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, lattice);
            }
            return filename;
        }
    }

    // ReSharper disable once InconsistentNaming
    [Serializable]
    public class SOMNode
    {
        private static Random _rand = new Random();
        private SOMWeightsVector _weightsVector;
        private int _x, _y;

        public int X => _x;
        public int Y => _y;
        public SOMWeightsVector WeightsVector => _weightsVector;

        public SOMNode(int numWeights, int x, int y)
        {
            _x = x;
            _y = y;
            _weightsVector = new SOMWeightsVector();
            for (int i = 0; i < numWeights; i++)
            {
                _weightsVector.Add(_rand.NextDouble());
            }
        }

        public double Distance(SOMNode other) => (X - other.X)*(X - other.X) + (Y - other.Y)*(Y - other.Y);

        public void SetWeight(int n, double value)
        {
            if(n > _weightsVector.Count) throw new ArgumentOutOfRangeException($"Index {n} does not exist in a node with {_weightsVector.Count} weights");
            _weightsVector[n] = value;
        }

        public double GetWeight(int n)
        {
            if(n > _weightsVector.Count) throw new ArgumentOutOfRangeException($"Index {n} does not exist in a node with {_weightsVector.Count} weights");
            return _weightsVector[n];
        }

        public void AdjustWeights(SOMWeightsVector input, double learningRate, double distanceFallOff)
        {
            for (int i = 0; i < _weightsVector.Count; i++)
            {
                _weightsVector[i] = _weightsVector[i] + distanceFallOff*learningRate*(input[i] - _weightsVector[i]);
            }
        }
    }

    // ReSharper disable once InconsistentNaming
    [Serializable]
    public class SOMWeightsVector : List<double>
    {
        public double EuclideanDistance(SOMWeightsVector other)
        {
            if (other.Count != Count)
                throw  new Exception("Vectors must have the same number of elements");

            double sum = 0;
            for (int i = 0; i < Count; i++)
            {
                sum += (this[i] - other[i])*(this[i] - other[i]);
            }
            return sum;
        }
    }
}
