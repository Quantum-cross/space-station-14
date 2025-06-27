using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Events;

[ByRefEvent]
public readonly record struct StationJobsGetCandidatesEvent(
    NetUserId Player,
    List<ProtoId<JobPrototype>> Jobs,
    Dictionary<ProtoId<JobPrototype>, JobDenialReason> JobDenials);
