using Qubed.Core.Exceptions;
using Qubed.Core.Models;

namespace Qubed.Core.Logic;

public static class AlgorithmLibrary
{
    private static readonly string[] F2LMoves =
    [
        "R' D' R",
        "F D F'",
        "R' D R",
        "F D' F'",
        "R' D R D'",
        "R' D' R D",
        "R' D2 R",
        "R F' R' F",
        "R' D2 R D' R' D R",
        "R' D R D' R' D R",
        "R' D' R D R' D' R"
    ];

    private static readonly List<(string Description, string[] MoveSets, Func<Cube, bool> IsCompleteChecks)> AlgorithmMacros =
    [
        (
            "Step 1 - White Cross",
            [
                "U",
                "U'",
                "U2",
                "F2",
                "R",
                "F R",
                "F R U",
                "R U",
                "F' U' R U",
                "R' U' R U R'"
            ],
            cube => cube[Face.Up, 1, 0] == Colour.White
                    && cube[Face.Up, 2, 1] == Colour.White
                    && cube[Face.Up, 1, 2] == Colour.White
                    && cube[Face.Up, 0, 1] == Colour.White
                    && cube[Face.Left, 1, 0] == Colour.Green
                    && cube[Face.Front, 1, 0] == Colour.Red
                    && cube[Face.Right, 1, 0] == Colour.Blue
                    && cube[Face.Back, 1, 0] == Colour.Orange
        ),
        (
            "Step 2.1 - F2L Slot 1 (Red & Green)",
            F2LMoves,
            cube => cube[Face.Up, 0, 2] == Colour.White
                    && cube[Face.Left, 2, 0] == Colour.Green
                    && cube[Face.Front, 0, 0] == Colour.Red
                    && cube[Face.Front, 0, 1] == Colour.Red
                    && cube[Face.Left, 2, 1] == Colour.Green
        ),
        (
            "Step 2.2 - F2L Slot 2 (Red & Blue)",
            F2LMoves,
            cube => cube[Face.Up, 2, 2] == Colour.White
                    && cube[Face.Front, 2, 0] == Colour.Red
                    && cube[Face.Right, 0, 0] == Colour.Blue
                    && cube[Face.Front, 2, 1] == Colour.Red
                    && cube[Face.Right, 0, 1] == Colour.Blue
        ),
        (
            "Step 2.3 - F2L Slot 3 (Blue & Orange)",
            F2LMoves,
            cube => cube[Face.Up, 2, 0] == Colour.White
                    && cube[Face.Right, 2, 0] == Colour.Blue
                    && cube[Face.Back, 0, 0] == Colour.Orange
                    && cube[Face.Right, 2, 1] == Colour.Blue
                    && cube[Face.Back, 0, 1] == Colour.Orange
        ),
        (
            "Step 2.4 - F2L Slot 4 (Orange & Green)",
            F2LMoves,
            cube => cube[Face.Up, 0, 0] == Colour.White
                    && cube[Face.Left, 0, 0] == Colour.Green
                    && cube[Face.Back, 2, 0] == Colour.Orange
                    && cube[Face.Back, 2, 1] == Colour.Orange
                    && cube[Face.Left, 0, 1] == Colour.Green
        ),
        (
            "Step 3 - Last Layer",
            [
                "F' R' D' R D F",
                "F' D' R' D R F",
                "D",
                "D'",
                "D2",
                "R' D R' D' R' D' R' D R D R2",
                "R2 D' R' D' R D R D R D' R",
                "L' D R D' L D R' D'",
                "R' F R' B2 R F' R' B2 R2",
                "R' D' R D R' D' R D"
            ],
            cube => cube.IsSolved()
        )
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

            var algorithm = new Algorithm(macro.Description, moveSet, macro.IsCompleteChecks);

            Algorithms.Add(algorithm);
        }
    }

    public static int GetProgress(Cube cube)
    {
        if (! (cube[Face.Down, 1, 0] == Colour.White
               && cube[Face.Down, 2, 1] == Colour.White
               && cube[Face.Down, 1, 2] == Colour.White
               && cube[Face.Down, 0, 1] == Colour.White))
        {
            return 0;
        }

        if (! Algorithms[0].IsCompleteChecks(cube))
        {
            return 1;
        }

        return 2;
    }

    private static List<string> ExpandMacro(string macro)
    {
        var expandedMacro = new List<string> { macro };

        // ReSharper disable once StringLiteralTypo
        const string faces = "LFRB";

        var newSet = new char[macro.Length];

        for (var i = 1; i < 4; i++)
        {
            var replaced = false;

            for (var c = 0; c < macro.Length; c++)
            {
                if (macro[c] is 'L' or 'F' or 'R' or 'B')
                {
                    var newCharacterIndex = (faces.IndexOf(macro[c]) + i) % 4;

                    newSet[c] = faces[newCharacterIndex];

                    replaced = true;

                    continue;
                }

                newSet[c] = macro[c];
            }

            if (! replaced)
            {
                break;
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