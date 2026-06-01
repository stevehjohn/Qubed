namespace Qubed.Core.Models;

public readonly record struct Move(Face Face, Direction Direction, bool IsSequenceEnd = false);