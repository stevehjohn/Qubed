using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private readonly record struct SearchStateKey(
        ulong First,
        ulong Second,
        ulong Third,
        int PreviousFace,
        int PreviousDirection,
        int SameFaceTurnCount);

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
            
            var visitedStates = new Dictionary<SearchStateKey, int>();

            if (Search(_cube.Clone(), path, maxDepth, null, 0, visitedStates))
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

    private static bool Search(
        Cube cube,
        List<Move> path,
        int depthLeft,
        Move? lastMove,
        int sameFaceTurnCount,
        Dictionary<SearchStateKey, int> visitedStates)
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

        var stateKey = GetStateKey(cube, lastMove, sameFaceTurnCount);

        if (visitedStates.TryGetValue(stateKey, out var previousDepthLeft) && previousDepthLeft >= depthLeft)
        {
            return false;
        }

        visitedStates[stateKey] = depthLeft;

        foreach (var move in AllMoves)
        {
            if (lastMove.HasValue && ShouldSkipMove(lastMove.Value, move, sameFaceTurnCount))
            {
                continue;
            }

            cube.ApplyMove(move.Face, move.Direction);
            
            path.Add(move);

            var nextSameFaceTurnCount = NextSameFaceTurnCount(lastMove, move, sameFaceTurnCount);

            if (Search(cube, path, depthLeft - 1, move, nextSameFaceTurnCount, visitedStates))
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

    private static SearchStateKey GetStateKey(
        Cube cube,
        Move? lastMove,
        int sameFaceTurnCount)
    {
        Span<ulong> key = stackalloc ulong[3];
        var stickerIndex = 0;

        foreach (var face in Enum.GetValues<Face>())
        {
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var part = stickerIndex / 18;

                    key[part] = key[part] * 6 + (uint)cube[face, x, y];
                    stickerIndex++;
                }
            }
        }

        return new SearchStateKey(
            key[0],
            key[1],
            key[2],
            lastMove.HasValue ? (int)lastMove.Value.Face : -1,
            lastMove.HasValue ? (int)lastMove.Value.Direction : 0,
            sameFaceTurnCount);
    }

    private static bool ShouldSkipMove(Move previousMove, Move move, int sameFaceTurnCount)
    {
        if (previousMove.Face == move.Face)
        {
            return previousMove.Direction != move.Direction || sameFaceTurnCount >= 2;
        }

        return AreOppositeFaces(previousMove.Face, move.Face) && move.Face < previousMove.Face;
    }

    private static int NextSameFaceTurnCount(Move? previousMove, Move move, int sameFaceTurnCount) =>
        previousMove.HasValue && previousMove.Value.Face == move.Face
            ? sameFaceTurnCount + 1
            : 1;

    private static bool AreOppositeFaces(Face a, Face b) =>
        a switch
        {
            Face.Up => b == Face.Down,
            Face.Down => b == Face.Up,
            Face.Front => b == Face.Back,
            Face.Back => b == Face.Front,
            Face.Left => b == Face.Right,
            Face.Right => b == Face.Left,
            _ => false
        };
}
