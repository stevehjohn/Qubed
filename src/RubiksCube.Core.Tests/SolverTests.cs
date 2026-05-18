using RubiksCube.Core.Models;
using Xunit;

namespace RubiksCube.Core.Tests;

public class SolverTests
{
    public static TheoryData<Move[]> WhiteCrossScrambles()
    {
        var data = new TheoryData<Move[]>();

        data.Add([new Move(Face.Front, Direction.Clockwise)]);
        data.Add(
        [
            new Move(Face.Front, Direction.Clockwise),
            new Move(Face.Right, Direction.Clockwise),
            new Move(Face.Up, Direction.AntiClockwise)
        ]);
        data.Add(
        [
            new Move(Face.Right, Direction.Clockwise),
            new Move(Face.Up, Direction.Clockwise),
            new Move(Face.Front, Direction.HalfTurn),
            new Move(Face.Left, Direction.AntiClockwise),
            new Move(Face.Down, Direction.Clockwise)
        ]);
        data.Add(
        [
            new Move(Face.Back, Direction.HalfTurn),
            new Move(Face.Left, Direction.Clockwise),
            new Move(Face.Down, Direction.AntiClockwise),
            new Move(Face.Right, Direction.HalfTurn),
            new Move(Face.Front, Direction.Clockwise),
            new Move(Face.Up, Direction.Clockwise)
        ]);

        return data;
    }

    [Theory]
    [MemberData(nameof(WhiteCrossScrambles))]
    public void SolveFindsWhiteCross(Move[] scramble)
    {
        var cube = new Cube();

        ApplyMoves(cube, scramble);

        var solver = new Solver(cube);

        var result = solver.Solve();

        ApplyMoves(cube, result.Moves);

        Assert.True(result.Solved);
        AssertWhiteCross(cube);
    }

    [Fact]
    public void SolveReturnsNoMovesForSolvedCube()
    {
        var solver = new Solver(new Cube());

        var result = solver.Solve();

        Assert.True(result.Solved);
        Assert.Empty(result.Moves);
    }

    private static void ApplyMoves(Cube cube, IEnumerable<Move> moves)
    {
        foreach (var move in moves)
        {
            cube.ApplyMove(move.Face, move.Direction);
        }
    }

    private static void AssertWhiteCross(Cube cube)
    {
        Assert.Equal(Colour.White, cube[Face.Up, 1, 0]);
        Assert.Equal(cube[Face.Back, 1, 1], cube[Face.Back, 1, 0]);
        Assert.Equal(Colour.White, cube[Face.Up, 0, 1]);
        Assert.Equal(cube[Face.Left, 1, 1], cube[Face.Left, 1, 0]);
        Assert.Equal(Colour.White, cube[Face.Up, 2, 1]);
        Assert.Equal(cube[Face.Right, 1, 1], cube[Face.Right, 1, 0]);
        Assert.Equal(Colour.White, cube[Face.Up, 1, 2]);
        Assert.Equal(cube[Face.Front, 1, 1], cube[Face.Front, 1, 0]);
    }
}
