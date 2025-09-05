
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Client.Effects;

[RegisterComponent, Access(typeof(BreathCloudSystem))]
public sealed partial class BreathCloudComponent : Component
{
    [DataField]
    public float RotationSpeedMax = 2f;

    [DataField(readOnly: true)]
    public float RotationSpeed;

    [DataField(readOnly: true)]
    public Angle RotationAngle;
}
