using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Robust.Server.Graphics;
using Robust.Shared.Graphics;

namespace Content.Server.DeviceLinking.Systems;

public sealed class SharedSignalMonitorSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SignalMonitorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SignalMonitorComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<SignalMonitorComponent, NewLinkEvent>(OnLink);
        SubscribeLocalEvent<SignalMonitorComponent, PortDisconnectedEvent>(OnUnlink);
    }

    private void OnInit(Entity<SignalMonitorComponent> ent, ref ComponentInit evt)
    {
        _signalSystem.EnsureSinkPorts(ent.Owner, ent.Comp.InputPort);
    }

    private void OnSignalReceived(Entity<SignalMonitorComponent> ent, ref SignalReceivedEvent evt)
    {
        if (evt.Port != ent.Comp.InputPort)
            return;
        if (evt.Data != null && evt.Data.TryGetValue(DeviceNetworkConstants.LogicState, out SignalState state))
            _appearance.SetData(ent.Owner, SignalMonitorLayers.Screen, state == SignalState.High ? SignalMonitorState.High : SignalMonitorState.Low);
    }

    private void OnLink(Entity<SignalMonitorComponent> ent, ref NewLinkEvent evt)
    {
        if (evt.Sink == ent.Owner && evt.SinkPort == ent.Comp.InputPort)
        {
            _appearance.SetData(ent.Owner, SignalMonitorLayers.Screen, SignalMonitorState.Low);
        }
    }

    private void OnUnlink(Entity<SignalMonitorComponent> ent, ref PortDisconnectedEvent evt)
    {
        if (evt.Port == ent.Comp.InputPort)
        {
            _appearance.SetData(ent.Owner, SignalMonitorLayers.Screen, SignalMonitorState.Idle);
        }
    }
}
