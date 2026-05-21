using RubiksCube.Core.Exceptions;
using RubiksCube.Core.Models;

namespace RubiksCube.Core.Logic;

public abstract class AlgorithmLibrary
{
    private static readonly List<(string Description, string[] Moves)> AlgorithmMacros =
    [
        ("Step 1 - White Cross",
        [
            "F2",
            "U' R U",
            "F' U' R U"
        ])
    ];

    public static readonly List<Algorithm> Algorithms;

    static AlgorithmLibrary()
    {
        Algorithms = [];

        foreach (var macro in AlgorithmMacros)
        {
            var moveSet = new List<List<Move>>();
            
            foreach (var move in macro.Moves)   
            {
                moveSet.Add(ParseMacro(move));
            }
            
            var algorithm = new Algorithm(macro.Description, moveSet);
            
            Algorithms.Add(algorithm);
        }
    }

    private static List<Move> ParseMacro(string macro)
    {
        var steps = macro.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var moves = new List<Move>();

        foreach (var step in steps)
        {
            moves.Add(ParseMove(step));
        }

        return moves;
    }

    private static Move ParseMove(string move)
    {
        var face = move[0] switch
        {
            'U' => Face.Up,
            'D' => Face.Down,
            'L' => Face.Left,
            'R' => Face.Right,
            'F' => Face.Front,
            'B' => Face.Back,
            _ => throw new ParseException($"Unknown move {move[0]}")
        };

        var direction = Direction.Clockwise;

        if (move.Length > 1)
        {
            direction = move[1] switch
            {
                '\'' => Direction.AntiClockwise,
                '2' => Direction.HalfTurn,
                _ => throw new ParseException($"Unknown modifier {move[1]}")
            };
        }

        return new Move(face, direction);
    }
}