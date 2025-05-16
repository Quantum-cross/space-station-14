using System.Linq;
using Content.Shared.Instruments;
using Robust.Client.Audio.Midi;
using Robust.Shared.Audio.Midi;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Instruments;

public sealed class NoteEmitterSystem : SharedNoteEmitterSystem
{

    // [Dependency] private readonly IMidiManager  _midiManager = default!;
    //
    // public override void Initialize()
    // {
    //     base.Initialize();
    //
    //     UpdatesOutsidePrediction = true;
    //
    //     // SubscribeNetworkEvent<NoteEmitterNoteEvent>(OnNoteEventRx);
    //     SubscribeNetworkEvent<NoteEmitterNoteSequenceEvent>(OnNoteEventRx);
    //
    //
    //     SubscribeLocalEvent<NoteEmitterComponent, ComponentShutdown>(OnShutdown);
    //     SubscribeLocalEvent<NoteEmitterComponent, ComponentHandleState>(OnHandleState);
    // }
    //
    // private void OnHandleState(EntityUid uid, SharedNoteEmitterComponent comp, ref ComponentHandleState args)
    // {
    //     if (args.Current is not NoteEmitterComponentState state)
    //         return;
    //
    //     comp.Program = state.Program;
    //     comp.Bank = state.Bank;
    //     comp.Master = EnsureEntity<NoteEmitterComponent>(state.Master, uid);
    //     SetupRenderer(uid, comp);
    // }
    //
    // private void OnShutdown(EntityUid uid, NoteEmitterComponent component, ComponentShutdown args)
    // {
    //     EndRenderer(uid, component);
    // }
    //
    // public override void EndRenderer(EntityUid uid, SharedNoteEmitterComponent? component = null)
    // {
    //     if (!ResolveNoteEmitter(uid, ref component))
    //         return;
    //
    //     if (component is not NoteEmitterComponent emitter)
    //         return;
    //
    //     if (emitter.Renderer is { } renderer)
    //     {
    //         renderer.Master = null;
    //         renderer.SystemReset();
    //         renderer.ClearAllEvents();
    //
    //         // We dispose of the synth two seconds from now to allow the last notes to stop from playing.
    //         // Don't use timers bound to the entity in case it is getting deleted.
    //         Timer.Spawn(2000, () => { renderer.Dispose(); });
    //     }
    //
    //     emitter.Renderer = null;
    // }
    //
    //
    // public override bool ResolveNoteEmitter(EntityUid uid, ref SharedNoteEmitterComponent? component)
    // {
    //     if (component is not null)
    //         return true;
    //
    //     TryComp<NoteEmitterComponent>(uid, out var localComp);
    //     component = localComp;
    //     return component != null;
    // }
    //
    // public override void SetupRenderer(EntityUid uid, SharedNoteEmitterComponent? component = null)
    // {
    //     if (!ResolveNoteEmitter(uid, ref component))
    //         return;
    //
    //     if (component is not NoteEmitterComponent emitter)
    //         return;
    //
    //     if (emitter.IsRendererAlive)
    //         return;
    //
    //     emitter.Renderer = _midiManager.GetNewRenderer();
    //
    //     if (emitter.Renderer is null)
    //         return;
    //
    //     emitter.Renderer.SendMidiEvent(RobustMidiEvent.SystemReset(emitter.Renderer.SequencerTick));
    //     UpdateRenderer(uid, emitter);
    //     emitter.Renderer.OnMidiPlayerFinished += () =>
    //     {
    //         EndRenderer(uid, emitter);
    //     };
    // }
    //
    // private void UpdateRenderer(EntityUid uid, NoteEmitterComponent? emitter = null)
    // {
    //     if (!Resolve(uid, ref emitter) || emitter.Renderer is not {} renderer)
    //         return;
    //
    //     renderer.TrackingEntity = uid;
    //     renderer.FilteredChannels.SetAll(false);
    //     renderer.DisablePercussionChannel = false;
    //     renderer.DisableProgramChangeEvent = false;
    //
    //     for (int i = 0; i < RobustMidiEvent.MaxChannels; i++)
    //     {
    //         renderer.SendMidiEvent(RobustMidiEvent.AllNotesOff((byte)i, 0));
    //     }
    //
    //     renderer.MidiBank = emitter.Bank;
    //     renderer.MidiProgram = emitter.Program;
    //
    //     renderer.Master = null;
    // }
    //
    // private void OnNoteEventRx(NoteEmitterNoteSequenceEvent noteEvent)
    // {
    //     var uid = GetEntity(noteEvent.Uid);
    //
    //     if (!TryComp<NoteEmitterComponent>(uid, out var emitter))
    //         return;
    //
    //     var renderer = emitter.Renderer;
    //
    //     if (renderer is null)
    //     {
    //         if (emitter.SequenceStartTick == 0)
    //             SetupRenderer(uid, emitter);
    //         return;
    //     }
    //
    //
    //     if (emitter.SequenceStartTick <= 0)
    //         emitter.SequenceStartTick = noteEvent.NoteSequence.Min(x => x.Start);
    // }
    public override bool ResolveNoteEmitter(EntityUid uid, ref SharedNoteEmitterComponent? component)
    {
        throw new NotImplementedException();
    }
}
