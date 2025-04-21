using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences;

/// <summary>
/// Borg character profile. Looks immutable, but uses non-immutable semantics internally for serialization/code sanity purposes.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class BorgCharacterProfile : ICharacterProfile
{

    private static readonly ProtoId<JobPrototype> ConstantJob = "Borg";

    [DataField(readOnly: true)]
    private HashSet<ProtoId<JobPrototype>> _jobPreferences = [ConstantJob];

    [DataField]
    private bool _enabled;

    public bool Enabled => _enabled;

    public string Name { get; }
    public ICharacterAppearance CharacterAppearance { get; }
    public bool MemberwiseEquals(ICharacterProfile maybeOther)
    {
        if (maybeOther is not BorgCharacterProfile other)
            return false;
        if (Name != other.Name)
            return false;
        if (Enabled != other.Enabled)
            return false;
        return true;
    }

    public void EnsureValid(ICommonSession session, IDependencyCollection collection)
    {
        throw new NotImplementedException();
    }


    public ICharacterProfile Validated(ICommonSession session, IDependencyCollection collection)
    {
        throw new NotImplementedException();
    }

    public IReadOnlySet<ProtoId<JobPrototype>> JobPreferences { get; }
    public ICharacterProfile AsEnabled(bool enabledValue = true)
    {
        return new BorgCharacterProfile(){ _enabled = enabledValue };
    }
}
