using RubiksCube.Core.Exceptions;
using RubiksCube.Core.Models;

namespace RubiksCube.Core.Logic;

public abstract class AlgorithmLibrary
{
    private static readonly List<(string Description, string[] MoveSets, Func<Cube, bool>[] IsCompleteChecks)> AlgorithmMacros =
    [
        (
            "Step 1 - White Cross",
            [
                "F2",
                "U' R U",
                "F' U' R U"
            ],
            [
                cube => cube[Face.Up, 1, 0] == Colour.White
                        && cube[Face.Up, 2, 1] == Colour.White
                        && cube[Face.Up, 1, 2] == Colour.White
                        && cube[Face.Up, 0, 1] == Colour.White
                        && cube[Face.Left, 1, 0] == Colour.Green
                        && cube[Face.Front, 1, 0] == Colour.Red
                        && cube[Face.Right, 1, 0] == Colour.Blue
                        && cube[Face.Back, 1, 0] == Colour.Orange
            ]
        ),
        (
            "Step 2 - Top Corners",
            [
                "F D F'",
                "R' D' R",
                "R' D2 R D R' D' R"
            ],
            [
                cube => cube[Face.Up, 0, 0] == Colour.White
                        && cube[Face.Left, 0, 0] == Colour.Green
                        && cube[Face.Back, 2, 0] == Colour.Orange,
                cube => cube[Face.Up, 2, 0] == Colour.White
                        && cube[Face.Right, 2, 0] == Colour.Blue
                        && cube[Face.Back, 0, 0] == Colour.Orange,
                cube => cube[Face.Up, 2, 2] == Colour.White
                        && cube[Face.Front, 2, 0] == Colour.Red
                        && cube[Face.Right, 0, 0] == Colour.Blue,
                cube => cube[Face.Up, 0, 2] == Colour.White
                        && cube[Face.Left, 2, 0] == Colour.Green
                        && cube[Face.Front, 0, 0] == Colour.Red
            ]
        ),
        (
            "Step 3.1 - Middle Layer Edges Red & Green",
            [
                "D' L' D L D F D' F'",
                "D R D' R' D' F' D F"
            ],
            [
                cube => cube[Face.Front, 0, 1] == Colour.Red
                        && cube[Face.Left, 2, 1] == Colour.Green
            ]
        ),
        (
            "Step 3.2 - Middle Layer Edges Red & Blue",
            [
                "D' L' D L D F D' F'",
                "D R D' R' D' F' D F"
            ],
            [
                cube => cube[Face.Front, 2, 1] == Colour.Red
                        && cube[Face.Right, 0, 1] == Colour.Blue
            ]
        ),
        (
            "Step 3.3 - Middle Layer Edges Orange & Green",
            [
                "D' L' D L D F D' F'",
                "D R D' R' D' F' D F"
            ],
            [
                cube => cube[Face.Back, 2, 1] == Colour.Orange
                        && cube[Face.Left, 0, 1] == Colour.Green
            ]
        ),
        (
            "Step 3.4 - Middle Layer Edges Blue & Orange",
            [
                "D' L' D L D F D' F'",
                "D R D' R' D' F' D F"
            ],
            [
                cube => cube[Face.Right, 2, 1] == Colour.Blue
                        && cube[Face.Back, 0, 1] == Colour.Orange
            ]
        ),
        (
            "Step 4 - Yellow Cross",
            [
                "F' R' D' R D F",
                "F' D' R' D R F"
            ],
            [
                cube => cube[Face.Down, 1, 0] == Colour.Yellow
                        && cube[Face.Down, 2, 1] == Colour.Yellow
                        && cube[Face.Down, 1, 2] == Colour.Yellow
                        && cube[Face.Down, 0, 1] == Colour.Yellow
            ]
        ),
        (
            "Step 4 - Align Yellow Cross",
            [
                "R' D2 R D R' D R D"
            ],
            [
                cube => cube[Face.Front, 1, 2] == Colour.Red
                        && cube[Face.Right, 1, 2] == Colour.Blue
                        && cube[Face.Back, 1, 2] == Colour.Orange
                        && cube[Face.Left, 1, 2] == Colour.Green
            ]
        ),
        (
            "Step 5 - Yellow Corners",
            [
                "L D' R' D L' D' R D"
            ],
            [
                cube =>
                    CornerHas(cube,
                        (Face.Down, 0, 0),
                        (Face.Left, 0, 2),
                        (Face.Back, 2, 2),
                        Colour.Yellow, Colour.Green, Colour.Orange)
                    && CornerHas(cube,
                        (Face.Down, 2, 0),
                        (Face.Back, 0, 2),
                        (Face.Right, 2, 2),
                        Colour.Yellow, Colour.Orange, Colour.Blue)
                    && CornerHas(cube,
                        (Face.Down, 2, 2),
                        (Face.Right, 0, 2),
                        (Face.Front, 2, 2),
                        Colour.Yellow, Colour.Blue, Colour.Red)
                    && CornerHas(cube,
                        (Face.Down, 0, 2),
                        (Face.Front, 0, 2),
                        (Face.Left, 2, 2),
                        Colour.Yellow, Colour.Red, Colour.Green)
            ]
        ),
        (
            "Step 6 - Completion",
            [
                "R' D' R D R' D' R D"
            ],
            [
                cube => cube.IsSolved()
            ]
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

    private static bool CornerHas(
        Cube cube,
        (Face Face, int X, int Y) a,
        (Face Face, int X, int Y) b,
        (Face Face, int X, int Y) c,
        Colour x,
        Colour y,
        Colour z)
    {
        Span<Colour> actual =
        [
            cube[a.Face, a.X, a.Y],
            cube[b.Face, b.X, b.Y],
            cube[c.Face, c.X, c.Y]
        ];

        return actual.Contains(x)
               && actual.Contains(y)
               && actual.Contains(z);
    }
}