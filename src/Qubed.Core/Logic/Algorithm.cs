using Qubed.Core.Models;

namespace Qubed.Core.Logic;

public readonly record struct Algorithm(string Name, IReadOnlyList<IReadOnlyList<Move>> MoveSets, Func<Cube, bool> IsCompleteChecks);