using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Animations;

namespace Content.Client.Animations;

public sealed class AnimateOnSpawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming Timing = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimateOnSpawnComponent, AnimationCompletedEvent>(HandleAnimationCompleteEvent);
        SubscribeLocalEvent<AnimateOnSpawnComponent, ComponentInit>(HandleInitEvent);

        _sawmill = _log.GetSawmill("AnimateOnSpawnSystem");
    }

    private void HandleAnimationCompleteEvent(Entity<AnimateOnSpawnComponent> ent, ref AnimationCompletedEvent args)
    {
        _sawmill.Warning("Animation complete!");
    }

    private void HandleInitEvent(Entity<AnimateOnSpawnComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _appearanceSystem.SetData(ent, AnimateOnSpawnVisualState.State, true);
        ent.Comp.EndTime = Timing.CurTime + ent.Comp.Delay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AnimateOnSpawnComponent>();
        while (query.MoveNext(out var ent, out var animate))
        {
            if (Timing.CurTime > animate.EndTime)
            {
                _appearanceSystem.SetData(ent, AnimateOnSpawnVisualState.State, false);
                RemCompDeferred(ent, animate);
            }
        }
    }
}
