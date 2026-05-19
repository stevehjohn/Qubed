using RubiksCube.Core.Models;

namespace RubiksCube.Core.Extension;

public static class ColourExtensions
{
    extension(Colour colour)
    {
        public char ToInitial() => colour.ToString().ToUpper()[0];
    }
}