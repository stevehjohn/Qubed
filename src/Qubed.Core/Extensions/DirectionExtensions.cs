using Qubed.Core.Models;

namespace Qubed.Core.Extensions;

public static class DirectionExtensions
{
    extension(Direction direction)
    {
        public Direction Opposite() => direction switch
        {
            Direction.Clockwise => Direction.AntiClockwise,
            Direction.AntiClockwise => Direction.Clockwise,
            Direction.HalfTurn => Direction.HalfTurn,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }
}