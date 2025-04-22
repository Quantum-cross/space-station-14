using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Animations;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class AnimateOnSpawnComponent : Component
{
    [DataField(required: true)]
    public TimeSpan Delay;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan EndTime;
}

[Serializable, NetSerializable]
public enum AnimateOnSpawnVisualState : byte
{
    State,
}
