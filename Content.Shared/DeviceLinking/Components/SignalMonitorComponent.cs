using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.DeviceLinking.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SignalMonitorComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public SignalMonitorState InputState;

    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "Input";
}

[Serializable, NetSerializable]
public enum SignalMonitorLayers : byte
{
    Screen,
}

[Serializable, NetSerializable]
public enum SignalMonitorState : byte
{
    Idle,
    Low,
    High,
}

