using Content.Shared.CCVar;
using Content.Shared.Dataset;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Borgs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
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

    private BorgCharacterProfile(BorgCharacterProfile other) : this(other.Name, other.SpawnPriority)
    {
    }

    private BorgCharacterProfile(string name, SpawnPriorityPreference spawnPriority)
    {
        Name = name;
        SpawnPriority = spawnPriority;
        Appearance = new BorgCharacterAppearance();
        JobPreferences = _jobPreferences;
    }

    public bool Enabled => _enabled;

    [DataField]
    public string Name { get; set; } = "Borgo";

    /// <summary>
    /// <see cref="Appearance"/>
    /// </summary>
    public ICharacterAppearance CharacterAppearance => Appearance;

    /// <summary>
    /// Stores markings, eye colors, etc for the profile.
    /// </summary>
    [DataField]
    public BorgCharacterAppearance Appearance { get; set; } = new();

    /// <summary>
    /// When spawning into a round what's the preferred spot to spawn.
    /// </summary>
    [DataField]
    public SpawnPriorityPreference SpawnPriority { get; private set; } = SpawnPriorityPreference.None;

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
        var configManager = collection.Resolve<IConfigurationManager>();
        var prototypeManager = collection.Resolve<IPrototypeManager>();
        var random = collection.Resolve<IRobustRandom>();

        string name;
        if (string.IsNullOrEmpty(Name))
        {
            ProtoId<LocalizedDatasetPrototype> borgNames = "NamesBorg";
            name = random.Pick(prototypeManager.Index(borgNames));
        }
        else if (Name.Length > ICharacterProfile.MaxNameLength)
        {
            name = Name[..ICharacterProfile.MaxNameLength];
        }
        else
        {
            name = Name;
        }

        name = name.Trim();

        if (configManager.GetCVar(CCVars.RestrictedNames))
        {
            name = ICharacterProfile.RestrictedNameRegex.Replace(name, string.Empty);
        }

        if (configManager.GetCVar(CCVars.ICNameCase))
        {
            // This regex replaces the first character of the first and last words of the name with their uppercase version
            name = ICharacterProfile.ICNameCaseRegex.Replace(name, m => m.Groups["word"].Value.ToUpper());
        }

        if (string.IsNullOrEmpty(name))
        {
            ProtoId<LocalizedDatasetPrototype> borgNames = "NamesBorg";
            name = random.Pick(prototypeManager.Index(borgNames));
        }

        var spawnPriority = SpawnPriority switch
        {
            SpawnPriorityPreference.None => SpawnPriorityPreference.None,
            SpawnPriorityPreference.Arrivals => SpawnPriorityPreference.Arrivals,
            SpawnPriorityPreference.Cryosleep => SpawnPriorityPreference.Cryosleep,
            _ => SpawnPriorityPreference.None // Invalid enum values.
        };

        Name = name;
        SpawnPriority = spawnPriority;
    }


    public ICharacterProfile Validated(ICommonSession session, IDependencyCollection collection)
    {
        var profile = new BorgCharacterProfile(this);
        profile.EnsureValid(session, collection);
        return profile;
    }

    public IReadOnlySet<ProtoId<JobPrototype>> JobPreferences { get; }

    public ICharacterProfile AsEnabled(bool enabledValue = true)
    {
        return new BorgCharacterProfile(){ _enabled = enabledValue };
    }

    public static BorgCharacterProfile Random()
    {
        return new BorgCharacterProfile();
    }
}
