using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.FingerprintReader;

/// <summary>
/// Component for checking if a user's fingerprint matches allowed fingerprints
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(AccessReaderSystem))]
public sealed partial class FingerprintReaderComponent : AccessReaderComponentBase
{
    /// <summary>
    /// The fingerprints that are allowed to access this entity.
    /// </summary>
    [DataField]
    public HashSet<string> AllowedFingerprints = new();

    /// <summary>
    /// Whether to ignore gloves when checking fingerprints.
    /// </summary>
    [DataField]
    public bool IgnoreGloves;

    /// <summary>
    /// The popup to show when access is denied due to fingerprint mismatch.
    /// </summary>
    [DataField]
    public LocId? FailPopup;

    /// <summary>
    /// The popup to show when access is denied due to wearing gloves.
    /// </summary>
    [DataField]
    public LocId? FailGlovesPopup;

    [DataField]
    public bool TerminateOnDeny;
}

[Serializable, NetSerializable]
public sealed class FingerprintReaderComponentState(bool enabled, HashSet<string> allowedFingerprints, bool ignoreGloves) : ComponentState
{
    public bool Enabled = enabled;
    public HashSet<string> AllowedFingerprints = allowedFingerprints;
    public bool IgnoreGloves = ignoreGloves;
}
