using Content.Shared.Construction.Components;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Silicons.Bots;

public sealed class LightBarrierSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LightBarrierComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorStateChanged(Entity<LightBarrierComponent> ent, ref AnchorStateChangedEvent evt)
    {
        if (evt.Anchored)
        {
            var shape = new PolygonShape();
            shape.SetAsBox(0.5f, 0.5f);
            _fixtures.TryCreateFixture(ent.Owner,
                shape,
                "barrier",
                density: 0.0f,
                collisionLayer: (int)CollisionGroup.BotImpassible);
            _physics.SetCanCollide(ent, true);

        }
        else
        {
            _fixtures.DestroyFixture(ent.Owner, "barrier");
        }
    }
}
