namespace Content.Shared.Access.Components;

public abstract partial class AccessReaderComponentBase : Component
{
    [DataField]
    public bool Enabled;

    [DataField]
    public int Priority;

    [DataField]
    public bool TerminateAfterDenial;
}
