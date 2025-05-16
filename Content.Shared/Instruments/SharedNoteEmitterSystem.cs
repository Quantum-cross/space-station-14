namespace Content.Shared.Instruments;

public abstract class SharedNoteEmitterSystem : EntitySystem
{
    public abstract bool ResolveNoteEmitter(EntityUid uid, ref SharedNoteEmitterComponent? component);

    public virtual void SetupRenderer(EntityUid uid, SharedNoteEmitterComponent? component = null)
    {

    }

    public virtual void EndRenderer(EntityUid uid, SharedNoteEmitterComponent? component = null)
    {

    }

}
