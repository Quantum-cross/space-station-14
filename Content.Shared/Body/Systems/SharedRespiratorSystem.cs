using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Body.Systems;

public abstract class SharedRespiratorSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RespiratorComponent>();
        while (query.MoveNext(out var uid, out var respirator))
        {
            if (GameTiming.CurTime < respirator.NextUpdate)
                continue;

            respirator.NextUpdate += respirator.AdjustedUpdateInterval;

            if (MobState.IsDead(uid))
                continue;

            UpdateSaturation(uid, -(float)respirator.UpdateInterval.TotalSeconds, respirator);

            if (!MobState.IsIncapacitated(uid)) // cannot breathe in crit.
            {
                switch (respirator.Status)
                {
                    case RespiratorStatus.Inhaling:
                        Inhale((uid, respirator));
                        respirator.Status = RespiratorStatus.Exhaling;
                        break;
                    case RespiratorStatus.Exhaling:
                        Exhale((uid, respirator));
                        respirator.Status = RespiratorStatus.Inhaling;
                        break;
                }
            }

            UpdateSuffocation((uid, respirator));
            if (GameTiming.CurTime < respirator.NextUpdate)
                Dirty<RespiratorComponent>((uid, respirator));
        }

    }

    public void UpdateSaturation(EntityUid uid, float amount, RespiratorComponent? respirator = null)
    {
        if (!Resolve(uid, ref respirator, false))
            return;

        respirator.Saturation += amount;
        respirator.Saturation =
            Math.Clamp(respirator.Saturation, respirator.MinSaturation, respirator.MaxSaturation);
    }

    protected virtual void UpdateSuffocation(Entity<RespiratorComponent> ent)
    {

    }

    public virtual void Inhale(Entity<RespiratorComponent?> ent)
    {

    }

    public virtual void Exhale(Entity<RespiratorComponent> ent)
    {
        DoExhaleEffect(ent);
    }

    public virtual void DoExhaleEffect(Entity<RespiratorComponent> ent)
    {

    }

}

/// <summary>
/// Event raised when an entity is exhaling
/// </summary>
/// <param name="Gas">The gas mixture we're exhaling into.</param>
/// <param name="Handled">Whether we have successfully exhaled or not.</param>
[ByRefEvent]
public record struct ExhaledGasEvent(GasMixture Gas, bool Handled = false);
