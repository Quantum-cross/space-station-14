using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.NameIdentifier;
using Content.Shared.StationRecords;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Access.Systems;

public sealed partial class AccessReaderSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedStationRecordsSystem _recordsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;


    private delegate bool? IsAllowedFunc(EntityUid user, EntityUid target, IComponent component);

    private static Dictionary<Type, IsAllowedFunc> _isAllowedCallbacks = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessReaderComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<AccessReaderComponent, LinkAttemptEvent>(OnLinkAttempt);

        SubscribeLocalEvent<AccessReaderComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AccessReaderComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<AccessReaderComponent, GetAccessOrder>(OnGetAccessMetadata);

        _isAllowedCallbacks.Add(typeof(AccessReaderComponent), IsStationKeyAllowed);

        FingerprintInitialize();
    }

    private void OnGetAccessMetadata<TComp>(Entity<TComp> ent, ref GetAccessOrder args)
        where TComp : AccessReaderComponentBase
    {
        if (ent.Comp.Enabled)
            args.AccessOrder.Add(new AccessOrderEntry(ent.Comp.GetType(), ent.Comp.Priority, ent.Comp.TerminateAfterDenial));
    }

    private void OnGetState(EntityUid uid, AccessReaderComponent component, ref ComponentGetState args)
    {
        // TODO: Should we be sending the access log to every client?
        args.State = new AccessReaderComponentState(component.Enabled, component.DenyTags, component.AccessLists,
            _recordsSystem.Convert(component.AccessKeys), component.AccessLog, component.AccessLogLimit);
    }

    private void OnHandleState(EntityUid uid, AccessReaderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AccessReaderComponentState state)
            return;
        component.Enabled = state.Enabled;
        component.AccessKeys.Clear();
        foreach (var key in state.AccessKeys)
        {
            var id = EnsureEntity<AccessReaderComponent>(key.Item1, uid);
            if (!id.IsValid())
                continue;

            component.AccessKeys.Add(new StationRecordKey(key.Item2, id));
        }

        component.AccessLists = new(state.AccessLists);
        component.DenyTags = new(state.DenyTags);
        component.AccessLog = new(state.AccessLog);
        component.AccessLogLimit = state.AccessLogLimit;
    }

    private void OnLinkAttempt(EntityUid uid, AccessReaderComponent component, LinkAttemptEvent args)
    {
        if (args.User == null) // AutoLink (and presumably future external linkers) have no user.
            return;
        if (!IsAllowed(args.User.Value, uid))
            args.Cancel();
    }

    private void OnEmagged(EntityUid uid, AccessReaderComponent reader, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        if (!reader.BreakOnAccessBreaker)
            return;

        if (!GetMainAccessReader(uid, out var accessReader))
            return;

        if (accessReader.Value.Comp.AccessLists.Count < 1)
            return;

        args.Repeatable = true;
        args.Handled = true;
        accessReader.Value.Comp.AccessLists.Clear();
        accessReader.Value.Comp.AccessLog.Clear();
        Dirty(uid, reader);
    }

    /// <summary>
    /// Checks to see if the user has access to the AccessReader on target using all the methods enabled in order on the
    /// access reader.
    /// </summary>
    /// <param name="user">The entity that wants access.</param>
    /// <param name="target">The entity to search for an access reader</param>
    public bool IsAllowed(EntityUid user, EntityUid target)
    {
        var evt = new GetAccessOrder();
        RaiseLocalEvent(target, evt);
        var atLeastOneMethodDenied = false;
        foreach (var entry in evt.AccessOrder)
        {
            var access = EntityManager.GetComponent(target, entry.AccessType);
            if (access is not AccessReaderComponentBase)
                continue;
            if(!_isAllowedCallbacks.TryGetValue(access.GetType(), out var callback))
                continue;
            var res = callback.Invoke(user, target, access);
            if (!res.HasValue)
                continue;
            if (entry.TerminateAfterDenial)
                return res.Value;
            if (res.Value)
                return true;
            atLeastOneMethodDenied = true;
        }

        return !atLeastOneMethodDenied;
    }

    public bool IsAllowed(EntityUid user, Entity<AccessReaderComponent?> reader)
    {
        return IsAllowed(user, reader.Owner);
    }

    public bool IsAllowed(EntityUid user, EntityUid target, AccessReaderComponent? reader)
    {
        return IsAllowed(user, (target, reader));
    }

    /// <summary>
    /// Checks to see if the user has access to the AccessReader on target using all the methods enabled in order on the
    /// access reader. Override using Entity pattern
    /// </summary>
    /// <param name="user">The entity that wants access.</param>
    /// <param name="reader">Target access reader entity</param>
    /// <param name="logAccess">log attempt to the reader</param>
    // public bool IsAllowed(EntityUid user, Entity<AccessReaderComponent> reader, bool logAccess = true)
    // {
    //     // Access reader component is disabled, always allowed
    //     if (!reader.Comp.Enabled)
    //         return true;
    //
    //     foreach (var accessType in reader.Comp.CheckOrder)
    //     {
    //         switch (IsAllowed(accessType, user, reader))
    //         {
    //             case AccessResult.Allowed:
    //                 LogAccess(accessType, user, reader);
    //                 return true;
    //             case AccessResult.DeniedTerminate:
    //                 return false;
    //             case AccessResult.DeniedContinue:
    //             default:
    //                 break;
    //         }
    //     }
    //     return false;
    // }

    /// <summary>
    /// Logs successful access given an access method
    /// </summary>
    /// <param name="accessType"></param>
    /// <param name="user"></param>
    /// <param name="reader"></param>
    // private void LogAccess(AccessReaderAccessType accessType, EntityUid user, Entity<AccessReaderComponent> reader)
    // {
    //     switch (accessType)
    //     {
    //         case AccessReaderAccessType.StationKeys:
    //             LogIdentityAccess(reader, user);
    //             return;
    //         case AccessReaderAccessType.Fingerprint:
    //             LogFingerprintAccess(reader, user);
    //             return;
    //         default:
    //             return;
    //     }
    // }

    public bool GetMainAccessReader(EntityUid uid, [NotNullWhen(true)] out Entity<AccessReaderComponent>? ent)
    {
        ent = null;
        if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
            return false;

        ent = (uid, accessReader);

        if (ent.Value.Comp.ContainerAccessProvider == null)
            return true;

        if (!_containerSystem.TryGetContainer(uid, ent.Value.Comp.ContainerAccessProvider, out var container))
            return true;

        foreach (var entity in container.ContainedEntities)
        {
            if (TryComp<AccessReaderComponent>(entity, out var containedReader))
            {
                ent = (entity, containedReader);
                return true;
            }
        }

        return true;
    }

    public void SetAccesses(EntityUid uid, AccessReaderComponent component, List<ProtoId<AccessLevelPrototype>> accesses)
    {
        component.AccessLists.Clear();
        foreach (var access in accesses)
        {
            component.AccessLists.Add(new HashSet<ProtoId<AccessLevelPrototype>>(){access});
        }
        Dirty(uid, component);
        RaiseLocalEvent(uid, new AccessReaderConfigurationChangedEvent());
    }

    /// <summary>
    /// Logs an access for a specific entity using the user's identity.
    /// </summary>
    /// <param name="ent">The reader to log the access on</param>
    /// <param name="accessor">The accessor to log</param>
    public void LogIdentityAccess(Entity<AccessReaderComponent> ent, EntityUid accessor)
    {
        if (IsPaused(ent) || ent.Comp.LoggingDisabled)
            return;

        string? name = null;
        if (TryComp<NameIdentifierComponent>(accessor, out var nameIdentifier))
            name = nameIdentifier.FullIdentifier;

        // TODO pass the ID card on IsAllowed() instead of using this expensive method
        // Set name if the accessor has a card and that card has a name and allows itself to be recorded
        var getIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(ent, accessor, true);
        RaiseLocalEvent(getIdentityShortInfoEvent);
        if (getIdentityShortInfoEvent.Title != null)
        {
            name = getIdentityShortInfoEvent.Title;
        }

        LogAccess(ent, name ?? Loc.GetString("access-reader-unknown-id"));
    }

    /// <summary>
    /// Logs an access with a predetermined name
    /// </summary>
    /// <param name="ent">The reader to log the access on</param>
    /// <param name="name">The name to log as</param>
    public void LogAccess(Entity<AccessReaderComponent> ent, string name)
    {
        if (IsPaused(ent) || ent.Comp.LoggingDisabled)
            return;

        if (ent.Comp.AccessLog.Count >= ent.Comp.AccessLogLimit)
            ent.Comp.AccessLog.Dequeue();

        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        ent.Comp.AccessLog.Enqueue(new AccessRecord(stationTime, name));
    }
}
