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
using GeneticSolverDatabase;
using Microsoft.EntityFrameworkCore;
using System.Windows.Media.Media3D;

namespace GeneticSolverUI
{
    public partial class MainWindow : Window
    {
        private GenSolver? geneticSolver;
        private GeneticAlgorithmContext dbContext = new GeneticAlgorithmContext();

        public MainWindow()
        {
            InitializeComponent();
            LoadRecords();
        }

        private void LoadRecords()
        {
            try
            {
                var records = dbContext.Records
                    .OrderByDescending(r => r.CreationDate)
                    .ToList();
                RecordsListView.ItemsSource = records;
            }
            catch (Exception ex)
            { }
        }

        private void LoadSelectedRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecord = (Record)RecordsListView.SelectedItem;
            if (selectedRecord == null)
            {
                MessageBox.Show("Please select a record to load.");
                return;
            }

            try
            {
                var genSolver = LoadGenSolver(selectedRecord.RecordId);
                if (genSolver != null)
                {
                    geneticSolver = genSolver;
                    geneticSolver.OnIterationPassed += GeneticSolver_OnGenerationCompleted;
                    geneticSolver.flagStop = false;

                    Square1x1CountInput.Text = geneticSolver.countOf1x1.ToString();
                    Square2x2CountInput.Text = geneticSolver.countOf2x2.ToString();
                    Square3x3CountInput.Text = geneticSolver.countOf3x3.ToString();
                    PopulationSizeInput.Text = geneticSolver.populationSize.ToString();
                    MutationRateInput.Text = geneticSolver.mutation.ToString();
                    GenerationText.Text = "Generation: " + geneticSolver.iterationsPassed.ToString();

                    var record = dbContext.Records.FirstOrDefault(r => r.RecordId == selectedRecord.RecordId);
                    NewRecordNameInput.Text = record?.Name;

                    var bestElement = geneticSolver.population.OrderBy(val => val.MinCoverRectSquare()).ElementAt(0);
                    BestSquareText.Text = "Best distance: " + bestElement.MinCoverRectSquare().ToString();

                    UpdateCanvas(bestElement);
                    Continue.IsEnabled = true;

                    MessageBox.Show($"Loaded record '{selectedRecord.Name}' successfully.");
                }
                else
                {
                    MessageBox.Show($"Failed to load the selected record's GenSolver.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading record: {ex.Message}");
            }
        }

        private void DeleteSelectedRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedRecord = (Record)RecordsListView.SelectedItem;

            try
            {
                var recordToDelete = dbContext.Records.FirstOrDefault(r => r.RecordId == selectedRecord.RecordId);
                dbContext.Records.Remove(recordToDelete);
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleteing record: {ex.Message}");
            }
            finally
            {
                LoadRecords();
            }
        }

        private void GeneticSolver_OnGenerationCompleted(int generation, GeneticSolverLibrary.Grid bestGrid)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateCanvas(bestGrid);
                GenerationText.Text = $"Generation: {generation}";
                BestSquareText.Text = $"Best distance: {bestGrid.MinCoverRectSquare()}";
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
            Continue.IsEnabled = false;
            Save.IsEnabled = true;

            var bestGrid = await Task.Factory.StartNew(() =>
            {
                return geneticSolver.Iterations();
            }, TaskCreationOptions.LongRunning);

            UpdateCanvas(bestGrid);
        }

        private async void SaveSolver(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = true;
            Stop.IsEnabled = false;

            stopperFunction();

            if (geneticSolver != null)
            {
                SaveGenSolver(geneticSolver, NewRecordNameInput.Text);
                UpdateCanvas(geneticSolver.population.OrderBy(val => val.MinCoverRectSquare()).ElementAt(0));
            }
        }

        private async void ContinueSolver(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            Stop.IsEnabled = true;
            Continue.IsEnabled = false;
            Save.IsEnabled = true;

            geneticSolver.OnIterationPassed += GeneticSolver_OnGenerationCompleted;

            var bestGrid = await Task.Factory.StartNew(() =>
            {
                return geneticSolver.Iterations();
            }, TaskCreationOptions.LongRunning);

            UpdateCanvas(bestGrid);
        }


        private void UpdateCanvas(GeneticSolverLibrary.Grid gridData)
        {
            VisualizationCanvas.Children.Clear();
            var rectangles = gridData.Figures;

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

        public void SaveGenSolver(GenSolver solver, string recordName)
        {
            // Переместить посчитанный ConcurrentBag в лист для записи в БД 
            solver.SyncPopulationToDb();

            var record = new Record
            {
                Name = recordName,
                CreationDate = DateTime.Now,
                GenSolver = solver
            };

            dbContext.Records.Add(record);
            dbContext.SaveChanges();

            LoadRecords();
        }

        public GenSolver? LoadGenSolver(int recordId)
        {
            if (geneticSolver != null)
            {
                geneticSolver.flagStop = true;
            }

            var record = dbContext.Records
                .Include(r => r.GenSolver)
                    .ThenInclude(gs => gs.GridsForDb)
                    .ThenInclude(g => g.Figures)
                .FirstOrDefault(r => r.RecordId == recordId);

            if (record?.GenSolver != null)
            {
                // Переместить загруженный лист из БД в ConcurrentBag
                record.GenSolver.SyncDbToPopulation();
            }

            return record?.GenSolver;
        }

        private void stopperFunction()
        {
            if (geneticSolver != null)
            {
                geneticSolver.flagStop = true;
            }

            Start.IsEnabled = true;
            Stop.IsEnabled = false;
            Continue.IsEnabled = false;
            Save.IsEnabled = true;
        }
        private async void StopSolver(object sender, RoutedEventArgs e)
        {
            stopperFunction();
        }
    }
}
