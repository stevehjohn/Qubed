namespace RubiksCube.Core.Models;

public class Cube
{
    private readonly Colour[][,] _faces = new Colour[6][,];
    
    private readonly Dictionary<Face, Slice[]> _affectedSlices =
        new()
        {
            {
                Face.Up,
                [
                    new Slice(Face.Front, Axis.Column, 0, false),
                    new Slice(Face.Left, Axis.Column, 0, false),
                    new Slice(Face.Back, Axis.Column, 0, false),
                    new Slice(Face.Right, Axis.Column, 0, false)
                ]
            },
            {
                Face.Down,
                [
                    new Slice(Face.Right, Axis.Column, 0, false),
                    new Slice(Face.Back, Axis.Column, 0, false),
                    new Slice(Face.Left, Axis.Column, 0, false),
                    new Slice(Face.Front, Axis.Column, 0, false)
                ]
            },
            {
                Face.Front,
                [
                    new Slice(Face.Up, Axis.Column, 0, false),
                    new Slice(Face.Right, Axis.Column, 0, false),
                    new Slice(Face.Down, Axis.Column, 0, false),
                    new Slice(Face.Left, Axis.Column, 0, false)
                ]
            },
            {
                Face.Back,
                [
                    new Slice(Face.Left, Axis.Column, 0, false),
                    new Slice(Face.Down, Axis.Column, 0, false),
                    new Slice(Face.Right, Axis.Column, 0, false),
                    new Slice(Face.Up, Axis.Column, 0, false)
                ]
            },
            {
                Face.Left,
                [
                    new Slice(Face.Up, Axis.Column, 0, false),
                    new Slice(Face.Front, Axis.Column, 0, false),
                    new Slice(Face.Down, Axis.Column, 0, false),
                    new Slice(Face.Back, Axis.Column, 0, false)
                ]
            },
            {
                Face.Right,
                [
                    new Slice(Face.Back, Axis.Column, 0, false),
                    new Slice(Face.Down, Axis.Column, 0, false),
                    new Slice(Face.Front, Axis.Column, 0, false),
                    new Slice(Face.Up, Axis.Column, 0, false)
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
    }
}