namespace RubiksCube.Core.Models;

public class Cube
{
    private readonly Colour[][,] _faces = new Colour[6][,];

    private readonly Dictionary<Face, Face[]> _affectedEdges =
        new()
        {
            { Face.Up, [Face.Front, Face.Left, Face.Back, Face.Right] },
            { Face.Down, [Face.Right, Face.Back, Face.Left, Face.Front] },
            { Face.Front, [Face.Up, Face.Right, Face.Down, Face.Left] },
            { Face.Back, [Face.Left, Face.Down, Face.Right, Face.Up] },
            { Face.Left, [Face.Up, Face.Front, Face.Down, Face.Back] },
            { Face.Right, [Face.Back, Face.Down, Face.Front, Face.Up] }
        };

    private readonly Dictionary<Face, Dictionary<Face, (int Row, int Column)>> _affectedSlices =
        new()
        {
            {
                Face.Up, new()
                {
                    { Face.Front, (1, 1) },
                    { Face.Left, (1, 1) },
                    { Face.Back, (1, 1) },
                    { Face.Right, (1, 1) }
                }
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