using RubiksCube.Core.Models;

namespace RubiksCube.Core.Logic;

public readonly record struct Algorithm(string Name, IReadOnlyList<IReadOnlyList<Move>> MoveSets, Func<Cube, bool>[] IsCompleteChecks);