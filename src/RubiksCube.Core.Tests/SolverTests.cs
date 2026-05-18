using RubiksCube.Core.Models;
using Xunit;

namespace RubiksCube.Core.Tests;

public class SolverTests
{
    [Fact]
    public void Solve_FindsDoubleTurnSolution()
    {
        var cube = new Cube();

        cube.ApplyMove(Face.Up, Direction.Clockwise);
        cube.ApplyMove(Face.Up, Direction.Clockwise);

        var result = new Solver(cube).Solve();

        Assert.True(result.Solved);
        Assert.Equal(2, result.Moves.Count);
        AssertSolvedAfterMoves(cube, result.Moves);
    }

    [Fact]
    public void Solve_FindsSolutionWithoutRevisitingEquivalentSearchStates()
    {
        var cube = new Cube();
        var scramble = new[]
        {
            new Move(Face.Up, Direction.Clockwise),
            new Move(Face.Front, Direction.Clockwise),
            new Move(Face.Right, Direction.AntiClockwise),
            new Move(Face.Down, Direction.Clockwise),
            new Move(Face.Left, Direction.AntiClockwise),
            new Move(Face.Back, Direction.Clockwise)
        };

        foreach (var move in scramble)
        {
            cube.ApplyMove(move.Face, move.Direction);
        }

        var result = new Solver(cube).Solve();

        Assert.True(result.Solved);
        Assert.True(result.Moves.Count <= scramble.Length);
        AssertSolvedAfterMoves(cube, result.Moves);
    }

    private static void AssertSolvedAfterMoves(Cube scrambledCube, IReadOnlyList<Move> moves)
    {
        var cube = scrambledCube.Clone();

        foreach (var move in moves)
        {
            cube.ApplyMove(move.Face, move.Direction);
        }

        Assert.True(cube.IsSolved());
    }
}
