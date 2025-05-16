using Content.Shared.Instruments;
using Robust.Client.Audio.Midi;

namespace Content.Client.Instruments;

[RegisterComponent]
public sealed partial class NoteEmitterComponent : SharedNoteEmitterComponent
{


    [ViewVariables]
    public uint SequenceDelay;

    [ViewVariables]
    public uint SequenceStartTick;
}
