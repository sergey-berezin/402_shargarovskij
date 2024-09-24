using System.Security.Cryptography;

namespace GeneticSolverLibrary
{
    public class Figure
    {
        //сторона + координаты левого нижнего угла
        public int len { get; set; }
        public int x { get; set; }
        public int y { get; set; }

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
        public List<Figure> vals { get; set; }
        public Grid(List<Figure> _vals)
        {
            this.vals = _vals;
        }
        public int MinCoverRectSquare()
        {
            int maxx = -1000, maxy = -1000, minx = 1000, miny = 1000;
            foreach(Figure val in vals)
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
        public bool flagStop = false;
        private readonly Random rand = new Random();
        private List<Grid> population = new List<Grid>();
        public Grid answer
        {
            get { return this.population.OrderBy(val => val.MinCoverRectSquare()).First(); }
        }
        public Grid InitGrid(int c1, int c2, int c3)
        {
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
        public GenSolver(int size, int c1, int c2, int c3)
        {
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
            int n = grid.vals.Count;
            for (int i = 0; i < n; ++i)
            {
                for (int j = i + 1; j < n; ++j)
                {
                    if (grid.vals[i].IfIntersectFigure(grid.vals[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private Grid Crossover(Grid grid1, Grid grid2)
        {
            int n = grid1.vals.Count;
            List<Figure> grid = new List<Figure>();
            int border = rand.Next(1, grid1.vals.Count - 1);
            for (int i = 0; i < n; ++i)
            {
                if (i < border)
                {
                    grid.Add(Figure.clone(grid1.vals[i]));
                }
                else
                {
                    grid.Add(Figure.clone(grid2.vals[i]));
                }
            }
            return new Grid(grid);
        }
        private void Mutate(Grid grid)
        { 
            foreach (Figure val in grid.vals)
            {
                if (rand.Next(0, 100) < 20)
                {
                    val.x += rand.Next(-1, 2);
                    val.y += rand.Next(-1, 2);
                }
            }
        }
        private void Selection()
        {
            List<Grid> selection = population.OrderBy(val => val.MinCoverRectSquare()).ToList();
            List<Grid> selectedPopulation = new List<Grid>();
            selectedPopulation.Add(selection[0]);

            while (selectedPopulation.Count < population.Count)
            {
                Grid grid1 = selection[rand.Next(0, selection.Count / 2 + 1)], grid2 = selection[rand.Next(0, selection.Count / 2 + 1)];
                Grid selectedGrid = Crossover(grid1, grid2);
                Mutate(selectedGrid);
                if (!IfIntersectGrid(selectedGrid))
                {
                    selectedPopulation.Add(selectedGrid);
                }
            }
            population = selectedPopulation;
        }
        public event Action<int, Grid>? OnIterationPassed;
        public Grid Iterations()
        {
            int i = 0;
            while (true)
            {
                ++i;
                Selection();
                if (flagStop)
                {
                    return this.answer;
                }
                OnIterationPassed?.Invoke(i, this.answer);
            }
        }
    }

}