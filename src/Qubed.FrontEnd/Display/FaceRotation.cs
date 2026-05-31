using Qubed.Core.Models;

namespace Qubed.FrontEnd.Display;

public sealed class FaceRotation
{
    private readonly Move _move;

    public FaceRotation(Move move)
    {
        _move = move;
    }

    public Face Face => _move.Face;

    public Direction Direction => _move.Direction;

    public float Elapsed { get; set; }
    
    public bool MidClickPlayed { get; set; }
}