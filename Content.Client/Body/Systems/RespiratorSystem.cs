using System.Numerics;
using Content.Server.Body.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Body.Systems;

public sealed class RespiratorSystem : SharedRespiratorSystem
{

    [Dependency] private readonly SharedAtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;
    [Dependency] private readonly SharedGasTileOverlaySystem _gasTileOverlay = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void DoExhaleEffect(Entity<RespiratorComponent> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (IsPaused(ent))
            return;

        base.DoExhaleEffect(ent);

        if (_internals.AreInternalsWorking(ent))
            return;

        var gridUid = _transform.GetGrid(ent.Owner);

        if (gridUid == null || !TryComp<MapGridComponent>(gridUid, out var gridComp))
            return;

        if (!_transform.TryGetGridTilePosition(ent.Owner, out var gridPos, gridComp))
            return;

        if (!TryComp<GasTileOverlayComponent>(gridUid, out var gasTileOverlayComp))
            return;

        var gasTileIndices = SharedGasTileOverlaySystem.GetGasChunkIndices(gridPos);

        if (!gasTileOverlayComp.Chunks.TryGetValue(gasTileIndices, out var gasTileChunk))
            return;

        float? temperature = null;

        var enumerator = new GasChunkEnumerator(gasTileChunk);
        while (enumerator.MoveNext(out var gas))
        {
            if (gridPos != gasTileChunk.Origin + (enumerator.X, enumerator.Y))
                continue;

            temperature = gas.Temperature;
            break;
        }

        if (temperature > 250)
            return;

        SpawnAtPosition("BreathCloud", new EntityCoordinates(ent.Owner, new Vector2(0f, 0f)));
    }
}
