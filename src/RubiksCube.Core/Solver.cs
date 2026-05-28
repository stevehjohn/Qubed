using System.Diagnostics;
using RubiksCube.Core.Infrastructure;
using RubiksCube.Core.Logic;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public sealed class Solver
{
    private const int MinDepth = 1;

    private const int MaxDepth = 20;

    private readonly Cube _cube;

    private readonly List<Move> _moves = [];

    private readonly ILogger _logger;

    private readonly int _degreeOfParallelism;

    public Solver(Cube cube, Mode mode = Mode.HalfCores) : this(cube, mode, null)
    {
    }

    public Solver(Cube cube, Mode mode, ILogger logger)
    {
        _cube = cube.Clone();

        _degreeOfParallelism = mode switch
        {
            Mode.Fast => Environment.ProcessorCount - 1,
            Mode.HalfCores => Environment.ProcessorCount / 2,
            Mode.SingleThreaded => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        _logger = logger;
    }

    public void SolveAsync(Action<(bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration)> callback, Action<List<Move>> stepCallback = null)
    {
        Task.Run(() => Solve(stepCallback))
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger?.WriteLine(task.Exception?.GetBaseException().ToString());

                    callback((false, [], TimeSpan.Zero));

                    return;
                }

                callback(task.Result);
            });
    }

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve(Action<List<Move>> stepCallback = null)
    {
        _moves.Clear();

        _logger?.WriteLine();

        if (_cube.IsSolved())
        {
            _logger?.WriteLine(_cube.ToString());

            stepCallback?.Invoke(_moves);

            return (true, _moves, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        _logger?.WriteLine(_cube.ToString());

        _logger?.WriteLine();

        var checks = new List<Func<Cube, bool>>();

        var solved = true;

        var totalNodes = 0;

        foreach (var algorithm in AlgorithmLibrary.Algorithms)
        {
            _logger?.WriteLine($"{algorithm.Name}\n");

            checks.AddRange(algorithm.IsCompleteChecks);

            if (ChecksPass(checks, _cube))
            {
                continue;
            }

            var result = BruteForceAlgorithm(checks, algorithm.MoveSets, stepCallback);

            _logger?.WriteLine($"\nAlgorithm nodes explored: {result.NodesExplored:N0}.");

            solved &= result.ChecksPass;

            totalNodes += result.NodesExplored;

            _logger?.WriteLine();
        }

        _logger?.WriteLine(_cube.ToString());

        _logger?.WriteLine();

        CompressMoves();

        foreach (var move in _moves)
        {
            _logger?.WriteLine(move.ToString());
        }

        stopwatch.Stop();

        _logger?.WriteLine();

        _logger?.WriteLine($"Moves: {_moves.Count}. Duration: {stopwatch.Elapsed:mm\\:ss\\.fff}, Total nodes explored: {totalNodes:N0}.\n");

        return (solved, _moves, stopwatch.Elapsed);
    }

    private (bool ChecksPass, int NodesExplored) BruteForceAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSets, Action<List<Move>> stepCallback)
    {
        var totalStopwatch = Stopwatch.StartNew();

        var branchStopwatch = new Stopwatch();

        var nodesExplored = 0;

        for (var depth = MinDepth; depth <= MaxDepth; depth++)
        {
            _logger?.Write(depth.ToString());

            branchStopwatch.Restart();

            var found = false;

            List<Move> foundMoves = null;

            var innerDepth = depth;

            var stateLock = new Lock();

            nodesExplored = 0;

            Parallel.ForEach(moveSets, new ParallelOptions
            {
                MaxDegreeOfParallelism = _degreeOfParallelism
            }, (moveSet, _, index) =>
            {
                var cubeCopy = _cube.Clone();

                var newMoves = new List<Move>();

                var algorithmIndices = new List<int> { (int) index };

                // ReSharper disable once AccessToModifiedClosure
                Interlocked.Increment(ref nodesExplored);

                foreach (var move in moveSet)
                {
                    cubeCopy.ApplyMove(move);

                    newMoves.Add(move);

                    if (ChecksPass(heuristics, cubeCopy))
                    {
                        break;
                    }
                }

                var visitedDepths = new Dictionary<(ulong A, ulong B, ulong C), int>();

                if (SearchAlgorithm(heuristics, moveSets, cubeCopy, newMoves, algorithmIndices, visitedDepths, innerDepth - 1, ref nodesExplored))
                {
                    lock (stateLock)
                    {
                        if (foundMoves == null || newMoves.Count < foundMoves.Count)
                        {
                            found = true;

                            foundMoves = new List<Move>(newMoves);
                        }
                    }
                }
            });

            _logger?.WriteLine($" {branchStopwatch.Elapsed}");

            if (found)
            {
                _moves.AddRange(foundMoves);

                foreach (var move in foundMoves)
                {
                    _cube.ApplyMove(move);
                }

                _logger?.WriteLine($"\nNew moves: {foundMoves.Count:N0}, duration: {totalStopwatch.Elapsed:ss\\.fff}");

                stepCallback?.Invoke(foundMoves);

                return (true, nodesExplored);
            }
        }

        return (false, nodesExplored);
    }

    private static bool SearchAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSet, Cube cube, List<Move> moves, List<int> algorithmIndices, Dictionary<(ulong A, ulong B, ulong C), int> visitedDepths, int depth, ref int nodesExplored)
    {
        if (ChecksPass(heuristics, cube))
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        var key = cube.GetHash();

        if (visitedDepths.TryGetValue(key, out var seenDepth))
        {
            if (seenDepth >= depth)
            {
                return false;
            }

            visitedDepths[key] = depth;
        }
        else
        {
            visitedDepths.Add(key, depth);
        }

        for (var s = 0; s < moveSet.Count; s++)
        {
            var set = moveSet[s];

            Interlocked.Increment(ref nodesExplored);

            if (moves.Count > 0)
            {
                var lastMove = moves[^1];

                var move = set[0];

                if (lastMove.Face == move.Face)
                {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (lastMove.Direction)
                    {
                        case Direction.HalfTurn when move.Direction == Direction.HalfTurn:
                        case Direction.Clockwise when move.Direction == Direction.AntiClockwise:
                        case Direction.AntiClockwise when move.Direction == Direction.Clockwise:
                            continue;
                    }
                }
            }

            var occurrences = 0;

            if (algorithmIndices.Count > 1)
            {
                for (var o = 0; o < algorithmIndices.Count; o++)
                {
                    if (algorithmIndices[o] == s)
                    {
                        occurrences++;
                    }
                }
            }

            if (occurrences > 2)
            {
                continue;
            }

            foreach (var move in set)
            {
                cube.ApplyMove(move);

                moves.Add(move);

                if (ChecksPass(heuristics, cube))
                {
                    return true;
                }
            }

            algorithmIndices.Add(s);

            if (SearchAlgorithm(heuristics, moveSet, cube, moves, algorithmIndices, visitedDepths, depth - 1, ref nodesExplored))
            {
                return true;
            }

            for (var i = 0; i < set.Count; i++)
            {
                cube.UndoMove();

                moves.RemoveAt(moves.Count - 1);
            }

            algorithmIndices.RemoveAt(algorithmIndices.Count - 1);
        }

        return false;
    }

    private static bool ChecksPass(List<Func<Cube, bool>> heuristics, Cube cube)
    {
        foreach (var heuristic in heuristics)
        {
            if (! heuristic(cube))
            {
                return false;
            }
        }

        return true;
    }

    private void CompressMoves()
    {
        var changed = true;

        while (changed)
        {
            changed = false;

            for (var i = 0; i < _moves.Count - 1; i++)
            {
                var first = _moves[i];

                var second = _moves[i + 1];

                if (first.Face == second.Face)
                {
                    changed = true;

                    var newDirection = GetCompressedDirection(first.Direction, second.Direction);

                    _moves.RemoveRange(i, 2);

                    if (newDirection != null)
                    {
                        var newMove = first with { Direction = newDirection.Value };

                        _moves.Insert(i, newMove);
                    }

                    break;
                }
            }
        }
    }

    private static Direction? GetCompressedDirection(Direction first, Direction second)
    {
        var firstTurns = first switch
        {
            Direction.Clockwise => 1,
            Direction.HalfTurn => 2,
            _ => 3
        };

        var secondTurns = second switch
        {
            Direction.Clockwise => 1,
            Direction.HalfTurn => 2,
            _ => 3
        };

        var totalTurns = (firstTurns + secondTurns) % 4;

        return totalTurns switch
        {
            1 => Direction.Clockwise,
            2 => Direction.HalfTurn,
            3 => Direction.AntiClockwise,
            _ => null
        };
    }
}