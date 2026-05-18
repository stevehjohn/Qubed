namespace RubiksCube.Core.Models;

public readonly record struct Slice(Face Face, Axis Axis, int Index, bool Reversed );