using System.Drawing;

namespace RubiksCube.Core.Models;

public readonly record struct Move(Face Face, Direction Direction);