using Qubed.Core.Models;
using Xunit;

namespace Qubed.Core.Tests;

public class SolverTests
{
    [Fact]
    public void Solve()
    {
        var cube = new Cube();
        
        cube.Scramble();
        
        Console.WriteLine(cube);
        
        var solver = new Solver(cube);
        
        var result = solver.Solve();
        
        Console.WriteLine(cube);
        
        Assert.True(result.Solved);
    }
}