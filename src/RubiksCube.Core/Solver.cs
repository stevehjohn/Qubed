using System.Diagnostics;
using System.Text;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MaxWhiteCrossDepth = 8;

    private readonly Cube _cube;

    private static readonly Move[] AllMoves;

    private static readonly EdgeLocation[] EdgeLocations =
    [
        new(new Sticker(Face.Up, 1, 0), new Sticker(Face.Back, 1, 0)),
        new(new Sticker(Face.Up, 1, 2), new Sticker(Face.Front, 1, 0)),
        new(new Sticker(Face.Up, 0, 1), new Sticker(Face.Left, 1, 0)),
        new(new Sticker(Face.Up, 2, 1), new Sticker(Face.Right, 1, 0)),
        new(new Sticker(Face.Down, 1, 0), new Sticker(Face.Front, 1, 2)),
        new(new Sticker(Face.Down, 1, 2), new Sticker(Face.Back, 1, 2)),
        new(new Sticker(Face.Down, 0, 1), new Sticker(Face.Left, 1, 2)),
        new(new Sticker(Face.Down, 2, 1), new Sticker(Face.Right, 1, 2)),
        new(new Sticker(Face.Front, 0, 1), new Sticker(Face.Left, 2, 1)),
        new(new Sticker(Face.Front, 2, 1), new Sticker(Face.Right, 0, 1)),
        new(new Sticker(Face.Back, 2, 1), new Sticker(Face.Left, 0, 1)),
        new(new Sticker(Face.Back, 0, 1), new Sticker(Face.Right, 2, 1))
    ];

    static Solver()
    {
        var faces = Enum.GetValues<Face>();

        AllMoves = new Move[faces.Length * 3];

        var index = 0;

        foreach (var face in faces)
        {
            AllMoves[index++] = new Move(face, Direction.Clockwise);

            AllMoves[index++] = new Move(face, Direction.AntiClockwise);

            AllMoves[index++] = new Move(face, Direction.HalfTurn);
        }
    }

    public Solver(Cube cube) => _cube = cube.Clone();

    private readonly List<Move> _moves = [];

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve()
    {
        _moves.Clear();

        if (_cube.IsSolved())
        {
            return (true, _moves, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        var solved = FindWhiteCross();
        
        stopwatch.Stop();

        return (solved, _moves, stopwatch.Elapsed);
    }

    private bool FindWhiteCross()
    {
        if (HasWhiteCross(_cube))
        {
            return true;
        }

        var queue = new Queue<SearchNode>();
        var visited = new HashSet<string> { GetWhiteEdgesSignature(_cube) };

        queue.Enqueue(new SearchNode(_cube.Clone(), []));

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();

            if (node.Moves.Count >= MaxWhiteCrossDepth)
            {
                continue;
            }

            foreach (var move in AllMoves)
            {
                var cube = node.Cube.Clone();
                cube.ApplyMove(move.Face, move.Direction);

                var signature = GetWhiteEdgesSignature(cube);

                if (!visited.Add(signature))
                {
                    continue;
                }

                var moves = new List<Move>(node.Moves.Count + 1);
                moves.AddRange(node.Moves);
                moves.Add(move);

                if (HasWhiteCross(cube))
                {
                    foreach (var solutionMove in moves)
                    {
                        _cube.ApplyMove(solutionMove.Face, solutionMove.Direction);
                    }

                    _moves.AddRange(moves);

                    return true;
                }

                queue.Enqueue(new SearchNode(cube, moves));
            }
        }

        return false;
    }

    private static bool HasWhiteCross(Cube cube)
    {
        return
            cube[Face.Up, 1, 0] == Colour.White &&
            cube[Face.Back, 1, 0] == cube[Face.Back, 1, 1] &&
            cube[Face.Up, 0, 1] == Colour.White &&
            cube[Face.Left, 1, 0] == cube[Face.Left, 1, 1] &&
            cube[Face.Up, 2, 1] == Colour.White &&
            cube[Face.Right, 1, 0] == cube[Face.Right, 1, 1] &&
            cube[Face.Up, 1, 2] == Colour.White &&
            cube[Face.Front, 1, 0] == cube[Face.Front, 1, 1];
    }

    private static string GetWhiteEdgesSignature(Cube cube)
    {
        var builder = new StringBuilder(EdgeLocations.Length * 4);

        foreach (var edge in EdgeLocations)
        {
            var first = cube[edge.First.Face, edge.First.X, edge.First.Y];
            var second = cube[edge.Second.Face, edge.Second.X, edge.Second.Y];

            if (first != Colour.White && second != Colour.White)
            {
                continue;
            }

            builder
                .Append((int)(first == Colour.White ? second : first))
                .Append('@')
                .Append(Array.IndexOf(EdgeLocations, edge))
                .Append(first == Colour.White ? '+' : '-')
                .Append(';');
        }

        return builder.ToString();
    }

    private sealed record SearchNode(Cube Cube, IReadOnlyList<Move> Moves);

    private readonly record struct EdgeLocation(Sticker First, Sticker Second);

    private readonly record struct Sticker(Face Face, int X, int Y);
}
