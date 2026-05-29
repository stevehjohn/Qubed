using Microsoft.Xna.Framework;
using Qubed.Core.Models;

namespace RubiksCube.FrontEnd.Display;

public sealed class Sticker(Face face, Vector3 normal, Color color)
{
    public Face Face { get; set; } = face;

    public Vector3 Normal { get; set; } = normal;

    public Color Color { get; } = color;
}