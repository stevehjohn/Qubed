using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private readonly Cube _cube;

    private static readonly Move[] AllMoves = Enum
        .GetValues<Face>()
        .SelectMany(f => new[]
        {
            new Move { Face = f, Direction = Direction.Clockwise },
            new Move { Face = f, Direction = Direction.AntiClockwise }
        })
        .ToArray();

    public Solver(Cube cube) => _cube = cube.Clone();

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve()
    {
        var path = new List<Move>();

        if (_cube.IsSolved())
        {
            return (true, path, TimeSpan.Zero);
        }

        var stopwatch = new Stopwatch();
        
        for (var maxDepth = 1; maxDepth <= 20; maxDepth++)
        {
            stopwatch.Restart();
            
            Console.Write($"Depth: {maxDepth}");
            
            if (Search(_cube.Clone(), path, maxDepth, null))
            {
                stopwatch.Stop();
                
                Console.WriteLine($", elapsed time: {stopwatch.Elapsed}.");
                
                return (true, path, stopwatch.Elapsed);
            }

            stopwatch.Stop();
                        
            Console.WriteLine($", elapsed time: {stopwatch.Elapsed}.");
        }

        return (false, path, stopwatch.Elapsed);
    }

    private static bool Search(Cube cube, List<Move> path, int depthLeft, Move? lastMove)
    {
        if (cube.IsSolved())
        {
            return true;
        }
        
        if (depthLeft == 0)
        {
            return false;
        }

        if (Heuristic(cube) > depthLeft)
        {
            return false;
        }

        foreach (var move in AllMoves)
        {
            if (lastMove.HasValue && IsTrivialReverse(lastMove.Value, move))
            {
                continue;
            }

            if (lastMove.HasValue && lastMove.Value.Face == move.Face)
            {
                continue;
            }

            cube.ApplyMove(move.Face, move.Direction);
            
            path.Add(move);

            if (Search(cube, path, depthLeft - 1, move))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
            
            cube.ApplyMove(move.Face, move.Direction == Direction.Clockwise 
                ? Direction.AntiClockwise 
                : Direction.Clockwise);
        }

        return false;
    }

    private static int Heuristic(Cube cube)
    {
        var wrong = 0;
        
        foreach (var face in Enum.GetValues<Face>())
        {
            var center = cube[face, 1, 1];

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    if (cube[face, x, y] != center) wrong++;
                }
            }
        }
        return wrong / 8;
    }

    private static bool IsTrivialReverse(Move a, Move b) => a.Face == b.Face && a.Direction != b.Direction;
}
