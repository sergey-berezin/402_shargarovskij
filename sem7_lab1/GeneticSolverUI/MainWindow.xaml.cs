using System;
using System.Collections.Generic;
using System.Data;
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

using GeneticSolverLibrary;

namespace GeneticSolverUI
{
    public partial class MainWindow : Window
    {
        private GenSolver? geneticSolver;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void GeneticSolver_OnGenerationCompleted(int generation, GeneticSolverLibrary.Grid bestGrid)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCanvas(bestGrid);
                GenerationText.Text = $"Generation: {generation}";
                BestDistanceText.Text = $"Best distance: {bestGrid.MinCoverRectSquare()}";
            });
        }

        private async void StartSolver(object sender, RoutedEventArgs e)
        {
            int square1x1Count = int.Parse(Square1x1CountInput.Text);
            int square2x2Count = int.Parse(Square2x2CountInput.Text);
            int square3x3Count = int.Parse(Square3x3CountInput.Text);
            int populationSize = int.Parse(PopulationSizeInput.Text);
            int mutationRate = int.Parse(MutationRateInput.Text);

            geneticSolver = new GenSolver(populationSize, square1x1Count, square2x2Count, square3x3Count, mutationRate);
            geneticSolver.OnIterationPassed += GeneticSolver_OnGenerationCompleted;

            Start.IsEnabled = false;
            Stop.IsEnabled = true;

            var bestGrid = await Task.Run(() => geneticSolver.Iterations());

            Start.IsEnabled = true;
            Stop.IsEnabled = false;
            UpdateCanvas(bestGrid);
        }

        private void UpdateCanvas(GeneticSolverLibrary.Grid gridData)
        {
            VisualizationCanvas.Children.Clear();
            var rectangles = gridData.vals;

            double maxX = rectangles.Max(rect => rect.x + rect.len);
            double maxY = rectangles.Max(rect => rect.y + rect.len);
            double minX = rectangles.Min(rect => rect.x);
            double minY = rectangles.Min(rect => rect.y);

            double canvasWidth = VisualizationCanvas.ActualWidth;
            double canvasHeight = VisualizationCanvas.ActualHeight;
            double scaleX = canvasWidth / maxX;
            double scaleY = canvasHeight / maxY;
            double scale = Math.Min(scaleX, scaleY);

            foreach (var rect in rectangles)
            {
                Rectangle rectangle = new Rectangle
                {
                    Width = rect.len * scale,
                    Height = rect.len * scale,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(rectangle, (rect.x - minX) * scale);
                Canvas.SetBottom(rectangle, (rect.y - minY) * scale);

                VisualizationCanvas.Children.Add(rectangle);
            }
        }

        private async void StopSolver(object sender, RoutedEventArgs e)
        {
            if (geneticSolver != null)
            {
                geneticSolver.flagStop = true;
            }
        }
    }
}
