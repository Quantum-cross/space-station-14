using Content.Server.Chat.Systems;
using Content.Server.Vocalization.Systems;
using Content.Shared.Pinpointer;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Server.Pinpointer;

public sealed class PinpointerVocalSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinpointerVocalComponent, OnPinpointerTarget>(HandlePinpointerTarget);
        SubscribeLocalEvent<PinpointerVocalComponent, OnPinpointerDistanceChanged>(HandlePinpointerDistanceChanged);
    }

    private void HandlePinpointerDistanceChanged(Entity<PinpointerVocalComponent> ent, ref OnPinpointerDistanceChanged args)
    {
        if (args.PrevDistance == args.NewDistance)
            return;

        if (_random.NextFloat() > ent.Comp.ProbOfDistanceChangeMessage)
            return;

        var datasetId = (args.PrevDistance, args.NewDistance) switch
        {
            (Distance.Far, Distance.Medium) => ent.Comp.FarToMediumDataset,
            (Distance.Medium, Distance.Close) => ent.Comp.MediumToCloseDataset,
            (Distance.Close, Distance.Medium) => ent.Comp.CloseToMediumDataset,
            (Distance.Medium, Distance.Far) => ent.Comp.MediumToFarDataset,
            _ => null,
        };

        if (datasetId is null)
            return;

        var dataset = _protoMan.Index(datasetId);
        var message = _random.Pick(dataset);

        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    private void HandlePinpointerTarget(Entity<PinpointerVocalComponent> ent, ref OnPinpointerTarget args)
    {
        if (ent.Comp.LockedDataset is null)
            return;

        var dataset = _protoMan.Index(ent.Comp.LockedDataset.Value);
        var message = Loc.GetString(_random.Pick(dataset.Values), ("variety", args.TargetName));

        _chat.TrySendInGameICMessage(ent, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }
}
