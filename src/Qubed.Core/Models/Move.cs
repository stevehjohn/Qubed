namespace Qubed.Core.Models;

public readonly record struct Move(Face Face, Direction Direction, bool IsSequenceEnd = false)
{
    public override string ToString()
    {
        return
            $"{Face switch {
                Face.Up => 'U',
                Face.Down => 'D',
                Face.Front => 'F',
                Face.Back => 'B',
                Face.Left => 'L',
                Face.Right => 'R',
                _ => throw new ArgumentOutOfRangeException()
            }}{Direction switch {
                Direction.Clockwise => string.Empty,
                Direction.AntiClockwise => "'",
                Direction.HalfTurn => "2",
                _ => throw new ArgumentOutOfRangeException()
            }
        }";
    }
}