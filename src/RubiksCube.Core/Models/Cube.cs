namespace RubiksCube.Core.Models;

public class Cube
{
    private readonly Colour[][,] _faces = new Colour[6][,];

    private static readonly Dictionary<Face, Slice[]> AffectedSlices =
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
                    new(Face.Down, Axis.Row, 2, true),
                    new(Face.Right, Axis.Column, 2, true),
                    new(Face.Up, Axis.Row, 0, false)
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
                this[Face.Left, x, y] = Colour.Blue;
                this[Face.Right, x, y] = Colour.Green;
            }
        }
    }

    private Cube(Cube cube)
    {
        foreach (var face in Enum.GetValues<Face>())
        {
            this[face] = (Colour[,]) cube._faces[(int) face].Clone();
        }
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

    public void ApplyMove(Face face, Direction direction)
    {
        RotateFace(face, direction);

        RotateEdges(face, direction);
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

    private void RotateFace(Face face, Direction direction)
    {
        var matrix = this[face];

        if (direction == Direction.Clockwise)
        {
            (matrix[0, 0], matrix[2, 0], matrix[2, 2], matrix[0, 2]) = (matrix[0, 2], matrix[0, 0], matrix[2, 0], matrix[2, 2]);

            (matrix[1, 0], matrix[2, 1], matrix[1, 2], matrix[0, 1]) = (matrix[0, 1], matrix[1, 0], matrix[2, 1], matrix[1, 2]);
        }
        else
        {
            (matrix[0, 0], matrix[2, 0], matrix[2, 2], matrix[0, 2]) = (matrix[2, 0], matrix[2, 2], matrix[0, 2], matrix[0, 0]);

            (matrix[1, 0], matrix[2, 1], matrix[1, 2], matrix[0, 1]) = (matrix[2, 1], matrix[1, 2], matrix[0, 1], matrix[1, 0]);
        }
    }

    private void RotateEdges(Face face, Direction direction)
    {
        var slices = AffectedSlices[face];

        Span<Colour> values = stackalloc Colour[12];

        for (var i = 0; i < 4; i++)
        {
            ReadSlice(slices[i], values.Slice(i * 3, 3));
        }

        if (direction == Direction.Clockwise)
        {
            WriteSlice(slices[0], values.Slice(9, 3));

            WriteSlice(slices[1], values.Slice(0, 3));

            WriteSlice(slices[2], values.Slice(3, 3));

            WriteSlice(slices[3], values.Slice(6, 3));
        }
        else
        {
            WriteSlice(slices[0], values.Slice(3, 3));

            WriteSlice(slices[1], values.Slice(6, 3));

            WriteSlice(slices[2], values.Slice(9, 3));

            WriteSlice(slices[3], values.Slice(0, 3));
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
}