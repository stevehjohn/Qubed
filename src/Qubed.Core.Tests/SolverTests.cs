using Qubed.Core.Models;
using Xunit;

namespace Qubed.Core.Tests;

public class SolverTests
{
    private readonly ITestOutputHelper _outputHelper;
    
    public SolverTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    
    [Fact]
    public void Solve()
    {
        var cube = new Cube();
        
        cube.Scramble();
        
        Assert.NotEqual(
            """
                   W W W 
                   W W W 
                   W W W 

            G G G  R R R  B B B  O O O  
            G G G  R R R  B B B  O O O  
            G G G  R R R  B B B  O O O  

                   Y Y Y 
                   Y Y Y 
                   Y Y Y 
            """, cube.ToString());
        
        _outputHelper.WriteLine($"{cube}\n");
        
        var solver = new Solver(cube);
        
        var result = solver.Solve();

        foreach (var move in result.Moves)
        {
            cube.ApplyMove(move);
        }
        
        Assert.Equal(
            """
                   W W W 
                   W W W 
                   W W W 

            G G G  R R R  B B B  O O O  
            G G G  R R R  B B B  O O O  
            G G G  R R R  B B B  O O O  

                   Y Y Y 
                   Y Y Y 
                   Y Y Y 
            """, cube.ToString());
        
        _outputHelper.WriteLine(cube.ToString());
        
        Assert.True(result.Solved);
    }
}