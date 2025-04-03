using Content.Server.Explosion.Components;
using Content.Shared.Explosion.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class TriggerSystem
{
    private void InitializeOnStorageOpen()
    {
        SubscribeLocalEvent<OnUseTimerTriggerComponent, BoundUIOpenedEvent>(OnStorageOpen);
    }

    private void OnStorageOpen(Entity<OnUseTimerTriggerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!HasComp<TriggerOnStorageOpenComponent>(ent))
            return;

        StartTimer((ent.Owner, ent.Comp), args.Actor);
    }
}
