using RubiksCube.Core.Exceptions;
using RubiksCube.Core.Models;

namespace RubiksCube.Core.Logic;

public abstract class AlgorithmLibrary
{
    private static readonly List<(string Description, string[] MoveSets)> AlgorithmMacros =
    [
        ("Step 1 - White Cross",
        [
            "F2",
            "U' R U",
            "F' U' R U"
        ]),
        ("Step 2 - Top Corners",
        [
            "F D F'",
            "R' D' R",
            "R' D2 R D R' D' R"
        ])
    ];

    public static readonly List<Algorithm> Algorithms;

    static AlgorithmLibrary()
    {
        Algorithms = [];

        foreach (var macro in AlgorithmMacros)
        {
            var moveSet = new List<List<Move>>();
            
            foreach (var set in macro.MoveSets)   
            {
                foreach (var sequence in ExpandMacro(set))
                {
                    moveSet.Add(ParseMacro(sequence));
                }
            }
            
            var algorithm = new Algorithm(macro.Description, moveSet);
            
            Algorithms.Add(algorithm);
        }
    }

    private static List<string> ExpandMacro(string macro)
    {
        var expandedMacro = new List<string> { macro };

        // ReSharper disable once StringLiteralTypo
        const string faces = "LFRB";
        
        var newSet = new char[macro.Length];

        for (var i = 1; i < 4; i++)
        {
            for (var c = 0; c < macro.Length; c++)
            {
                if (macro[c] is 'L' or 'F' or 'R' or 'B')
                {
                    var newCharacterIndex = (faces.IndexOf(macro[c]) + i) % 4;
                    
                    newSet[c] = faces[newCharacterIndex];
                    
                    continue;
                }
                
                newSet[c] = macro[c];
            }
            
            expandedMacro.Add(new string(newSet));
        }
        
        return expandedMacro;
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