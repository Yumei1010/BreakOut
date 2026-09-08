using GFramework.Game.Abstractions.Data;
using GFramework.Game.Abstractions.Enums;

namespace BreakOut.scripts.data.setting;

public sealed record LocalDataLocation : IDataLocation
{
    public string Key { get; init; } = "local";
    public StorageKinds Kinds { get; init; } = StorageKinds.Local;
    public string? Namespace { get; init; } = "";
    public IReadOnlyDictionary<string, string>? Metadata { get; init; } = null;
}
