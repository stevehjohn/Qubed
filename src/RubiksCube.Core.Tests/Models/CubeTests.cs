using Qubed.Core.Models;
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