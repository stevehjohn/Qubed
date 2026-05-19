using RubiksCube.Core.Models;
using Xunit;

namespace RubiksCube.Core.Tests.Models;

public class CubeTests
{
    [Theory]
    [InlineData(Face.Up)]
    [InlineData(Face.Down)]
    [InlineData(Face.Front)]
    [InlineData(Face.Back)]
    [InlineData(Face.Left)]
    [InlineData(Face.Right)]
    public void FourClockwiseTurnsReturnsToSolved(Face face)
    {
        var cube = new Cube();
        
        var expected = new Cube();

        for (var i = 0; i < 4; i++)
        {
            cube.ApplyMove(face, Direction.Clockwise);
        }

        AssertCubeEqual(expected, cube);
    }
    
    [Theory]
    [InlineData(Face.Up)]
    [InlineData(Face.Down)]
    [InlineData(Face.Front)]
    [InlineData(Face.Back)]
    [InlineData(Face.Left)]
    [InlineData(Face.Right)]
    public void ClockwiseThenAntiClockwiseReturnsToSolved(Face face)
    {
        var cube = new Cube();
        
        var expected = new Cube();

        cube.ApplyMove(face, Direction.Clockwise);
        
        cube.ApplyMove(face, Direction.AntiClockwise);

        AssertCubeEqual(expected, cube);
    }

    [Fact]
    public void DownClockwiseCyclesBottomRowsInSingmasterOrder()
    {
        var cube = new Cube();

        cube.ApplyMove(Face.Down, Direction.Clockwise);

        AssertRow(cube, Face.Front, 2, Colour.Green);
        AssertRow(cube, Face.Right, 2, Colour.Red);
        AssertRow(cube, Face.Back, 2, Colour.Blue);
        AssertRow(cube, Face.Left, 2, Colour.Orange);
    }

    private static void AssertRow(Cube cube, Face face, int y, Colour expected)
    {
        for (var x = 0; x < 3; x++)
        {
            Assert.Equal(expected, cube[face, x, y]);
        }
    }
    
    private static void AssertCubeEqual(Cube expected, Cube actual)
    {
        foreach (var face in Enum.GetValues<Face>())
        {
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    Assert.Equal(expected[face, x, y], actual[face, x, y]);
                }
            }
        }
    }
}
