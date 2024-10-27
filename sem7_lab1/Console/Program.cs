using GeneticSolverLibrary;

public class Program
{
    public static void Main()
    {
        GenSolver MySolver = new GenSolver(1000, 15, 5, 3, 20);
        //GenSolver MySolver = new GenSolver(1000, 2, 2, 1, 20);
        MySolver.OnIterationPassed += Solver_OnIterationPassed;
        Console.WriteLine(MySolver.answer.MinCoverRectSquare());
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("Cancelling calculations...");
            e.Cancel = true;
            MySolver.flagStop = true;
        };
        Grid ans = MySolver.Iterations();
        for (int i = 0; i < ans.vals.Count; ++i)
        {
            Console.WriteLine($"Figure {i} len {ans.vals.ElementAt(i).len}, x {ans.vals.ElementAt(i).x} y {ans.vals.ElementAt(i).y}");
        }
    }
    private static void Solver_OnIterationPassed(int iteration, Grid best)
    {
        Console.WriteLine("iters: {0}, best square: {1}", iteration, best.MinCoverRectSquare());
    }
}