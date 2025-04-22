using Content.Shared.Humanoid;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Borgs;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BorgCharacterAppearance : ICharacterAppearance, IEquatable<BorgCharacterAppearance>
{
    public bool Equals(BorgCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return true;
    }

    public bool MemberwiseEquals(ICharacterAppearance other)
    {
        return Equals(other as BorgCharacterAppearance);
    }
}
