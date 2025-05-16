using System.Collections;
using System.Linq;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Instruments;

[NetworkedComponent]
[Access(typeof(SharedNoteEmitterSystem))]
public abstract partial class SharedNoteEmitterComponent : Component
{
    [DataField]
    public byte Program;

    [DataField]
    public byte Bank;

    [ViewVariables]
    public EntityUid? Master;
}

[Serializable, NetSerializable]
public sealed class NoteEmitterComponentState : ComponentState
{
    public byte Program;
    public byte Bank;
    public NetEntity? Master;
}

public record NoteEmitterNote
{
    public byte Note;
    public TimeSpan Start;
    public TimeSpan Duration;
}

[Serializable, NetSerializable]
public sealed class NoteEmitterNoteEvent(NetEntity uid, NoteEmitterNote note) : EntityEventArgs
{
    public NetEntity Uid { get; } = uid;
    public NoteEmitterNote Note { get; } = note;
}

[Serializable, NetSerializable]
public sealed class NoteEmitterNoteSequenceEvent(NetEntity uid, IEnumerable<NoteEmitterNote> noteSequence) : EntityEventArgs
{
    public NetEntity Uid { get; } = uid;
    public List<NoteEmitterNote> NoteSequence { get; } = noteSequence.ToList();
}
