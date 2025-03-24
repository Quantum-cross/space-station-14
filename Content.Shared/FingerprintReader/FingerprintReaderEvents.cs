namespace Content.Shared.FingerprintReader;

public sealed class FingerprintReaderSetAttemptEvent(EntityUid user, EntityUid target) : EntityEventArgs
{
    public EntityUid User { get; } = user;
    public EntityUid Target { get; } = target;
}

public sealed class FingerprintReaderSetAttemptFailedNearbyEvent(NetEntity user, NetEntity target) : EntityEventArgs
{
    public NetEntity User { get; } = user;
    public NetEntity Target { get; } = target;
}
