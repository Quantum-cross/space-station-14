using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

/// <summary>
/// Displays a sprite on the item that points towards the target component.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPinpointerSystem))]
public sealed partial class PinpointerVocalComponent : Component
{
    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? LockedDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? FarDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? MediumDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? CloseDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? ReachedDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? FarToMediumDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? MediumToCloseDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? CloseToMediumDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? MediumToFarDataset;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? RareDataset;

    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<LocalizedDatasetPrototype>? ItemDataset;

    [DataField]
    public float ProbOfDistanceChangeMessage = 1.0f;
}
