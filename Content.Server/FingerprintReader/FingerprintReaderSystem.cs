using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;

namespace Content.Server.FingerprintReader;

public sealed class FingerprintReaderSystem : SharedFingerprintReaderSystem
{
    [Dependency] private readonly SharedFingerprintReaderSystem _sharedFingerprint = default!;
    [Dependency] private readonly DeviceNetworkSystem  _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FingerprintReaderComponent, FingerprintReaderSetAttemptEvent>(OnSetAttempt);
        SubscribeLocalEvent<FingerprintReaderComponent, DeviceNetworkPacketEvent>(OnDeviceNetworkPacket);
    }

    private void OnSetAttempt(Entity<FingerprintReaderComponent> ent, ref FingerprintReaderSetAttemptEvent evt)
    {
        if (!TryComp(ent.Owner, out FingerprintComponent? fingerprint) || fingerprint.Fingerprint == null)
        {
            return;
        }

        if (TryComp(ent.Owner, out DeviceNetworkComponent? deviceNetwork))
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = "query_fingerprint",
                ["sender"] = "test",
                ["fingerprint"] = fingerprint.Fingerprint,
            };
            _deviceNetworkSystem.QueuePacket(evt.Target, null, payload, device: deviceNetwork);
            ent.Comp.ActiveSetRequests.Add(fingerprint.Fingerprint, new SetRequest(ent.Owner));
        }
    }

    private void OnDeviceNetworkPacket(Entity<FingerprintReaderComponent> ent, ref DeviceNetworkPacketEvent evt)
    {
        if (!evt.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;
        if (!evt.Data.TryGetValue("fingerprint", out string? fingerprint))
            return;

        if (command == "query_fingerprint")
        {
            if (!evt.Data.TryGetValue("sender", out string? sender))
                return;
            if (!ent.Comp.AllowedFingerprints.Contains(fingerprint))
                return;
            var data = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = "fingerprint_exists",
                ["fingerprint"] = fingerprint,
            };
            _deviceNetworkSystem.QueuePacket(ent.Owner, sender, data);
            return;
        }

        if (command == "fingerprint_exists")
        {
            if (!ent.Comp.ActiveSetRequests.TryGetValue(fingerprint, out var request))
                return;
            var failEvent = new FingerprintReaderSetAttemptFailedNearbyEvent(GetNetEntity(request.User), GetNetEntity(ent.Owner));
            RaiseNetworkEvent(failEvent, request.User);
            return;
        }
    }
}
