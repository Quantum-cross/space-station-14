using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Effects;

public sealed class BreathCloudSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BreathCloudComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BreathCloudComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<BreathCloudComponent> ent, ref ComponentInit args)
    {
        ent.Comp.RotationSpeed = _random.NextFloat(-ent.Comp.RotationSpeedMax, ent.Comp.RotationSpeedMax);
        ent.Comp.RotationAngle = _random.NextAngle();
    }

    private void OnComponentStartup(Entity<BreathCloudComponent> ent, ref ComponentStartup args)
    {
        _sprite.LayerSetColor((ent, null), 0, (255, 255, 255, 0));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BreathCloudComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var breathCloud, out var sprite))
        {
            var lifetime = _timing.CurTime - breathCloud.CreationTick.Value * _timing.TickPeriod;
            var alpha = AlphaCurve(lifetime);
            _sprite.LayerSetColor((uid, sprite), 0, (1f, 1f, 1f, alpha));
            var angle = breathCloud.RotationAngle + breathCloud.RotationSpeed * lifetime.TotalSeconds;
            _sprite.LayerSetRotation((uid, sprite), 0, angle);
            var scale = ScaleCurve(lifetime);
            _sprite.LayerSetScale((uid, sprite), 0, new Vector2(scale, scale));
        }
    }

    private float AlphaCurve(TimeSpan time)
    {
        var t = (float)time.TotalSeconds;
        return MathHelper.Clamp01(1f - t * t/4f);
    }

    private float ScaleCurve(TimeSpan time)
    {
        var t = (float)time.TotalSeconds;
        return Math.Clamp(t - t * t / 4f, 0.1f, 1f);
    }
}
