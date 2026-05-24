using System.Text;
using RubiksCube.Core.Extensions;

namespace RubiksCube.Core.Models;

public class Cube
{
    private readonly Colour[][,] _faces = new Colour[6][,];

    private readonly Stack<Move> _history = [];
    
    private readonly Random _random = new();

    private readonly Dictionary<Face, Slice[]> _affectedSlices =
        new()
        {
            {
                Face.Up,
                [
                    new(Face.Front, Axis.Row, 0, false),
                    new(Face.Left, Axis.Row, 0, false),
                    new(Face.Back, Axis.Row, 0, false),
                    new(Face.Right, Axis.Row, 0, false)
                ]
            },
            {
                Face.Down,
                [
                    new(Face.Right, Axis.Row, 2, false),
                    new(Face.Back, Axis.Row, 2, false),
                    new(Face.Left, Axis.Row, 2, false),
                    new(Face.Front, Axis.Row, 2, false)
                ]
            },
            {
                Face.Front,
                [
                    new(Face.Up, Axis.Row, 2, false),
                    new(Face.Right, Axis.Column, 0, false),
                    new(Face.Down, Axis.Row, 0, true),
                    new(Face.Left, Axis.Column, 2, true)
                ]
            },
            {
                Face.Back,
                [
                    new(Face.Left, Axis.Column, 0, false),
                    new(Face.Down, Axis.Row, 2, false),
                    new(Face.Right, Axis.Column, 2, true),
                    new(Face.Up, Axis.Row, 0, true)
                ]
            },
            {
                Face.Left,
                [
                    new(Face.Up, Axis.Column, 0, false),
                    new(Face.Front, Axis.Column, 0, false),
                    new(Face.Down, Axis.Column, 0, false),
                    new(Face.Back, Axis.Column, 2, true)
                ]
            },
            {
                Face.Right,
                [
                    new(Face.Back, Axis.Column, 0, true),
                    new(Face.Down, Axis.Column, 2, false),
                    new(Face.Front, Axis.Column, 2, false),
                    new(Face.Up, Axis.Column, 2, false)
                ]
            }
        };

    public Cube()
    {
        foreach (var face in Enum.GetValues<Face>())
        {
            this[face] = new Colour[3, 3];
        }

        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                this[Face.Up, x, y] = Colour.White;
                this[Face.Down, x, y] = Colour.Yellow;
                this[Face.Front, x, y] = Colour.Red;
                this[Face.Back, x, y] = Colour.Orange;
                this[Face.Left, x, y] = Colour.Green;
                this[Face.Right, x, y] = Colour.Blue;
            }
        }
    }

    private Cube(Cube cube)
    {
        foreach (var face in Enum.GetValues<Face>())
        {
            this[face] = (Colour[,]) cube._faces[(int) face].Clone();
        }

        _history.Clear();
    }

    public Colour this[Face face, int x, int y]
    {
        get => _faces[(int) face][x, y];
        set => _faces[(int) face][x, y] = value;
    }

    private Colour[,] this[Face face]
    {
        get => _faces[(int) face];
        init => _faces[(int) face] = value;
    }

    public void UndoMove()
    {
        var move = _history.Pop();

        var opposite = move.Direction.Opposite();

        RotateFace(move.Face, opposite);

        RotateEdges(move.Face, opposite);
    }

    public void ApplyMove(Face face, Direction direction)
    {
        RotateFace(face, direction);

        RotateEdges(face, direction);

        _history.Push(new Move(face, direction));
    }

    public void ApplyMove(Move move)
    {
        RotateFace(move.Face, move.Direction);

        RotateEdges(move.Face, move.Direction);

        _history.Push(move);
    }

    public Cube Clone()
    {
        return new Cube(this);
    }

    public bool IsSolved()
    {
        foreach (var face in Enum.GetValues<Face>())
        {
            var center = this[face, 1, 1];

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    if (this[face, x, y] != center)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public void Scramble(int turns)
    {
        for (var i = 0; i < turns; i++)
        {
            var face = (Face) _random.Next(6);
            
            var direction = (Direction) _random.Next(3);
            
            ApplyMove(face, direction);
        }
    }

    public (ulong A, ulong B, ulong C) GetHash()
    {
        ulong a = 0, b = 0, c = 0;
        
        var i = 0;

        for (var faceIdx = 0; faceIdx < 6; faceIdx++)
        {
            var faceMatrix = _faces[faceIdx];
        
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    var colour = (ulong)faceMatrix[x, y];

                    switch (i)
                    {
                        case < 21:
                            a |= colour << (i * 3);
                            break;
                        case < 42:
                            b |= colour << ((i - 21) * 3);
                            break;
                        default:
                            c |= colour << ((i - 42) * 3);
                            break;
                    }
                    i++;
                }
            }
        }

        return (a, b, c);
    }

    private void RotateFace(Face face, Direction direction)
    {
        var matrix = this[face];

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (direction)
        {
            case Direction.Clockwise:
                (matrix[0, 0], matrix[2, 0], matrix[2, 2], matrix[0, 2]) = (matrix[0, 2], matrix[0, 0], matrix[2, 0], matrix[2, 2]);

                (matrix[1, 0], matrix[2, 1], matrix[1, 2], matrix[0, 1]) = (matrix[0, 1], matrix[1, 0], matrix[2, 1], matrix[1, 2]);

                break;

            case Direction.AntiClockwise:
                (matrix[0, 0], matrix[2, 0], matrix[2, 2], matrix[0, 2]) = (matrix[2, 0], matrix[2, 2], matrix[0, 2], matrix[0, 0]);

                (matrix[1, 0], matrix[2, 1], matrix[1, 2], matrix[0, 1]) = (matrix[2, 1], matrix[1, 2], matrix[0, 1], matrix[1, 0]);

                break;

            case Direction.HalfTurn:
                (matrix[0, 0], matrix[2, 2], matrix[2, 0], matrix[0, 2]) = (matrix[2, 2], matrix[0, 0], matrix[0, 2], matrix[2, 0]);

                (matrix[1, 0], matrix[1, 2], matrix[0, 1], matrix[2, 1]) = (matrix[1, 2], matrix[1, 0], matrix[2, 1], matrix[0, 1]);

                break;
        }
    }

    private void RotateEdges(Face face, Direction direction)
    {
        var slices = _affectedSlices[face];

        Span<Colour> a = stackalloc Colour[3];

        Span<Colour> b = stackalloc Colour[3];

        Span<Colour> c = stackalloc Colour[3];

        Span<Colour> d = stackalloc Colour[3];

        ReadSlice(slices[0], a);

        ReadSlice(slices[1], b);

        ReadSlice(slices[2], c);

        ReadSlice(slices[3], d);

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (direction)
        {
            case Direction.Clockwise:
                WriteSlice(slices[0], d);

                WriteSlice(slices[1], a);

                WriteSlice(slices[2], b);

                WriteSlice(slices[3], c);

                break;

            case Direction.AntiClockwise:
                WriteSlice(slices[0], b);

                WriteSlice(slices[1], c);

                WriteSlice(slices[2], d);

                WriteSlice(slices[3], a);

                break;

            case Direction.HalfTurn:
                WriteSlice(slices[0], c);

                WriteSlice(slices[1], d);

                WriteSlice(slices[2], a);

                WriteSlice(slices[3], b);

                break;
        }
    }

    private void ReadSlice(Slice slice, Span<Colour> values)
    {
        for (var i = 0; i < 3; i++)
        {
            var source = slice.Reversed ? 2 - i : i;

            values[i] = slice.Axis == Axis.Row
                ? this[slice.Face, source, slice.Index]
                : this[slice.Face, slice.Index, source];
        }
    }

    private void WriteSlice(Slice slice, ReadOnlySpan<Colour> values)
    {
        for (var i = 0; i < 3; i++)
        {
            var target = slice.Reversed ? 2 - i : i;

            if (slice.Axis == Axis.Row)
            {
                this[slice.Face, target, slice.Index] = values[i];
            }
            else
            {
                this[slice.Face, slice.Index, target] = values[i];
            }
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        for (var y = 0; y < 3; y++)
        {
            builder.Append(new string(' ', 7));

            for (var x = 0; x < 3; x++)
            {
                builder.Append($"{this[Face.Up, x, y].ToInitial()} ");
            }

            builder.AppendLine();
        }

        builder.AppendLine();

        for (var y = 0; y < 3; y++)
        {
            foreach (var face in new[] { Face.Left, Face.Front, Face.Right, Face.Back })
            {
                for (var x = 0; x < 3; x++)
                {
                    builder.Append($"{this[face, x, y].ToInitial()} ");
                }

                builder.Append(' ');
            }

            builder.AppendLine();
        }

        builder.AppendLine();

        for (var y = 0; y < 3; y++)
        {
            builder.Append(new string(' ', 7));

            for (var x = 0; x < 3; x++)
            {
                builder.Append($"{this[Face.Down, x, y].ToInitial()} ");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}