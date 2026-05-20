using RubiksCube.Core.Models;

namespace RubiksCube.Core.Logic;

public abstract class AlgorithmLibrary
{
    private static readonly List<List<Move>> LayerTwoMovesInternal = [];

    private static readonly List<List<Move>> YellowCrossMovesInternal = [];

    private static readonly string[] LayerTwoAlgorithms =
    [
        "D* R D' R' D' F' D F",
        "D* B D' B' D' R' D R",
        "D* L D' L' D' B' D B",
        "D* F D' F' D' L' D L",
        
        "D* L' D L D F D' F'",
        "D* F' D F D R D' R'",
        "D* R' D R D B D' B'",
        "D* B' D B D L D' L'"
    ];

    private static readonly string[] YellowCrossAlgorithms =
    [
        "F D R D' R' F'",
        "R D B D' B' R'",
        "B D L D' L' B'",
        "L D F D' F' L'",
        
        "F R D R' D' F'",
        "R B D B' D' R'",
        "B L D L' D' B'",
        "L F D F' D' L'"
    ];

    public static IReadOnlyList<IReadOnlyList<Move>> LayerTwoMoves => LayerTwoMovesInternal;

    public static IReadOnlyList<IReadOnlyList<Move>> YellowCrossMoves => YellowCrossMovesInternal;

    static AlgorithmLibrary()
    {
        foreach (var algorithm in LayerTwoAlgorithms)
        {
            LayerTwoMovesInternal.Add(ParseAlgorithm(algorithm));
        }
        
        foreach (var algorithm in YellowCrossAlgorithms)
        {
            YellowCrossMovesInternal.Add(ParseAlgorithm(algorithm));
        }
    }

    private static List<Move> ParseAlgorithm(string algorithm)
    {
        var steps = algorithm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var moves = new List<Move>();

        foreach (var step in steps)
        {
            moves.Add(ParseMove(step));
        }

        return moves;
    }

    private static Move ParseMove(string move)
    {
        Face face;
        
        switch (move[0])
        {
            case 'U':
                face = Face.Up;
                
                break;

            case 'D':
                face = Face.Down;
                
                break;
            
            case 'L':
                face = Face.Left;
                
                break;
            
            case 'R':
                face = Face.Right;
                
                break;
            
            case 'F':
                face = Face.Front;
                
                break;
            
            case 'B':
                face = Face.Back;
                
                break;
            
            default:
                throw new ParseException($"Unknown move {move[0]}");
        }

        var direction = Direction.Clockwise;

        if (move.Length > 1)
        {
            direction = move[1] switch
            {
                '\'' => Direction.AntiClockwise,
                '2' => Direction.HalfTurn,
                '*' => Direction.Any,
                _ => throw new ParseException($"Unknown modifier {move[1]}")
            };
        }

        return new Move(face, direction);
    }
}