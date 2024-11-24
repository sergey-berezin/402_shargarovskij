using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

using Microsoft.EntityFrameworkCore;

namespace GeneticSolverLibrary
{
    public class Figure
    {
        public int FigureId { get; set; } // Primary Key
        //сторона + координаты левого нижнего угла
        public int len { get; set; }
        public int x { get; set; }
        public int y { get; set; }

        // Foreign key 
        public int GridId { get; set; }
        public Grid Grid { get; set; } = null!; // Navigation property

        public Figure() { }

        public Figure(int a, int _x, int _y)
        {
            this.len = a;
            this.x = _x;
            this.y = _y;
        }
        public static Figure clone(Figure prev)
        {
            return new Figure(prev.len, prev.x, prev.y);
        }

        public bool IfIntersectFigure(Figure b) // false - не пересекаются
        {
            if (this.x + this.len <= b.x || this.x >= b.x + b.len) return false; //левее или правее
            if (this.y + this.len <= b.y || this.y >= b.y + b.len) return false; // выше или ниже
            return true;
        }
    }

    public class Grid
    {
        public int GridId { get; set; } // Primary Key
        public List<Figure> Figures { get; set; }

        public int GenSolverId { get; set; }
        public GenSolver GenSolver { get; set; } = null!;

        public Grid() { }

        public Grid(List<Figure> _vals)
        {
            this.Figures = _vals;
        }
        public int MinCoverRectSquare()
        {
            int maxx = -1000, maxy = -1000, minx = 1000, miny = 1000;
            foreach (Figure val in Figures)
            {
                if (val.x < minx) minx = val.x;
                if (val.y < miny) miny = val.y;
                if (val.x + val.len > maxx) maxx = val.x + val.len;
                if (val.y + val.len > maxy) maxy = val.y + val.len;
            }
            return (maxx - minx) * (maxy - miny);
        }
    }

    public class GenSolver
    {
        public int GenSolverId { get; set; }  // Primary Key

        public int mutation { get; set; }
        public int populationSize { get; set; }

        public int countOf1x1 { get; set; }
        public int countOf2x2 { get; set; }
        public int countOf3x3 { get; set; }

        public int iterationsPassed { get; set; } = 0;


        [NotMapped]
        public bool flagStop = false;

        public List<Grid> GridsForDb { get; set; } = new List<Grid>();

        [NotMapped]
        public ConcurrentBag<Grid> population = new ConcurrentBag<Grid>();

        public void SyncPopulationToDb()
        {
            GridsForDb = population.ToList();
        }

        public void SyncDbToPopulation()
        {
            population = new ConcurrentBag<Grid>(GridsForDb);
        }

        public GenSolver() { }

        public Grid answer
        {
            get { return this.population.OrderBy(val => val.MinCoverRectSquare()).First(); }
        }
        public Grid InitGrid(int c1, int c2, int c3)
        {
            countOf1x1 = c1;
            countOf2x2 = c2;
            countOf3x3 = c3;

            Random rand = new Random();
            int maxlen = c1 + c2 * 2 + c3 * 3;
            List<Figure> _vals = new List<Figure>();
            for (int i = 0; i < c1; ++i)
            {
                _vals.Add(new Figure(1, rand.Next(0, maxlen), rand.Next(0, maxlen)));
            }
            for (int i = 0; i < c2; ++i)
            {
                _vals.Add(new Figure(2, rand.Next(0, maxlen), rand.Next(0, maxlen)));
            }
            for (int i = 0; i < c3; ++i)
            {
                _vals.Add(new Figure(3, rand.Next(0, maxlen), rand.Next(0, maxlen)));
            }
            return new Grid(_vals);
        }
        public GenSolver(int size, int c1, int c2, int c3, int mutation)
        {
            this.mutation = mutation;
            this.populationSize = size;
            for (int i = 0; i < size;)
            {
                Grid grid = InitGrid(c1, c2, c3);
                if (!IfIntersectGrid(grid))
                {
                    population.Add(grid);
                    ++i;
                }
            }
        }
        public bool IfIntersectGrid(Grid grid) // false - не пересекаются, true - пересекаются
        {
            int n = grid.Figures.Count;
            for (int i = 0; i < n; ++i)
            {
                for (int j = i + 1; j < n; ++j)
                {
                    if (grid.Figures.ElementAt(i).IfIntersectFigure(grid.Figures.ElementAt(j)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private Grid Crossover(Grid grid1, Grid grid2)
        {
            Random rand = new Random();
            int n = grid1.Figures.Count;
            List<Figure> grid = new List<Figure>();
            int border = rand.Next(1, grid1.Figures.Count - 1);
            for (int i = 0; i < n; ++i)
            {
                if (i < border)
                {
                    grid.Add(Figure.clone(grid1.Figures.ElementAt(i)));
                }
                else
                {
                    grid.Add(Figure.clone(grid2.Figures.ElementAt(i)));
                }
            }
            return new Grid(grid);
        }
        private void Mutate(Grid grid)
        {
            Random rand = new Random();
            foreach (Figure val in grid.Figures)
            {
                if (rand.Next(0, 100) < this.mutation)
                {
                    val.x += rand.Next(-1, 2);
                    val.y += rand.Next(-1, 2);
                }
            }
        }
        private void Selection()
        {
            ConcurrentQueue<Grid> selection = new ConcurrentQueue<Grid>(population.OrderBy(val => val.MinCoverRectSquare()));
            ConcurrentQueue<Grid> selectedPopulation = new ConcurrentQueue<Grid>();
            selectedPopulation.Enqueue(selection.ElementAt(0));

            Parallel.For(0, populationSize, i => {
                Random rand = new Random();
                Grid grid1 = selection.ElementAt(rand.Next(0, selection.Count / 2 + 1)), grid2 = selection.ElementAt(rand.Next(0, selection.Count / 2 + 1));
                Grid selectedGrid = Crossover(grid1, grid2);
                Mutate(selectedGrid);
                if (!IfIntersectGrid(selectedGrid))
                {
                    selectedPopulation.Enqueue(selectedGrid);
                }

            });
            population = new ConcurrentBag<Grid>(selectedPopulation);
        }
        public event Action<int, Grid>? OnIterationPassed;
        public Grid Iterations()
        {
            while (true)
            {
                ++iterationsPassed;
                Selection();
                if (flagStop)
                {
                    return this.answer;
                }
                OnIterationPassed?.Invoke(iterationsPassed, this.answer);
            }
        }
    }

}