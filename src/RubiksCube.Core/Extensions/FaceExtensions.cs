using RubiksCube.Core.Models;

namespace RubiksCube.Core.Extensions;

public static class FaceExtensions
{
    extension(Face face)
    {
        public Face Opposite()
        {
            return face switch
            {
                Face.Up => Face.Down,
                Face.Down => Face.Up,
                Face.Front => Face.Back,
                Face.Back => Face.Front,
                Face.Left => Face.Right,
                Face.Right => Face.Left,
                _ => throw new ArgumentOutOfRangeException(nameof(face), face, null)
            };
        }
    }
}