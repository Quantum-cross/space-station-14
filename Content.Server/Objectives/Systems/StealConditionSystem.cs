using Content.Server.Mind;
using Content.Server.Objectives.Components;
using Content.Server.Roles;
using Content.Server.Storage.Components;
using Content.Server.Thief.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CharacterInfo;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;

namespace Content.Server.Objectives.Systems;

public sealed class StealConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;



    private ISawmill _log = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;

    // private HashSet<Entity<TransformComponent>> _nearestEnts = new();
    // private HashSet<EntityUid> _countedItems = new();

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();

        SubscribeLocalEvent<StealConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<StealConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<StealStorageComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<StealStorageComponent, EntRemovedFromContainerMessage>(OnRemoved);

        SubscribeLocalEvent<PullerComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<PullerComponent, PullStoppedMessage>(OnPullStopped);

        _log = _logManager.GetSawmill("StealConditionSystem");
    }

    /// start checks of target acceptability, and generation of start values.
    private void OnAssigned(Entity<StealConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        List<StealTargetComponent?> targetList = new();

        var query = AllEntityQuery<StealTargetComponent>();
        while (query.MoveNext(out var target))
        {
            if (condition.Comp.StealGroup != target.StealGroup)
                continue;

            targetList.Add(target);
        }

        // cancel if the required items do not exist
        if (targetList.Count == 0 && condition.Comp.VerifyMapExistence)
        {
            args.Cancelled = true;
            return;
        }

        //setup condition settings
        var maxSize = condition.Comp.VerifyMapExistence
            ? Math.Min(targetList.Count, condition.Comp.MaxCollectionSize)
            : condition.Comp.MaxCollectionSize;
        var minSize = condition.Comp.VerifyMapExistence
            ? Math.Min(targetList.Count, condition.Comp.MinCollectionSize)
            : condition.Comp.MinCollectionSize;

        condition.Comp.CollectionSize = _random.Next(minSize, maxSize);
    }

    //Set the visual, name, icon for the objective.
    private void OnAfterAssign(Entity<StealConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var group = _proto.Index(condition.Comp.StealGroup);
        string localizedName = Loc.GetString(group.Name);

        var title =condition.Comp.OwnerText == null
            ? Loc.GetString(condition.Comp.ObjectiveNoOwnerText, ("itemName", localizedName))
            : Loc.GetString(condition.Comp.ObjectiveText, ("owner", Loc.GetString(condition.Comp.OwnerText)), ("itemName", localizedName));

        var description = condition.Comp.CollectionSize > 1
            ? Loc.GetString(condition.Comp.DescriptionMultiplyText, ("itemName", localizedName), ("count", condition.Comp.CollectionSize))
            : Loc.GetString(condition.Comp.DescriptionText, ("itemName", localizedName));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, group.Sprite, args.Objective);
    }
    private void OnGetProgress(Entity<StealConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress((args.MindId, args.Mind), condition);
    }

    private float GetProgress(Entity<MindComponent> mind, StealConditionComponent condition)
    {
        if (!_containerQuery.TryGetComponent(mind.Comp.CurrentEntity, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var count = 0;

        // _countedItems.Clear();

        //check stealAreas
        if (condition.CheckStealAreas)
        {
            var areasQuery = AllEntityQuery<StealAreaComponent>();
            while (areasQuery.MoveNext(out var area))
            {
                if (!area.Owners.Contains(mind.Owner))
                    continue;

                if (!_role.MindHasRole<ThiefRoleComponent>(mind.Owner, out var role))
                    continue;

                foreach (var ent in role.Value.Comp2.TrackedItems)
                {
                    CheckEntity(ent, condition, ref containerStack, ref count);
                }
            }
        }

        //check pulling object
        // if (TryComp<PullerComponent>(mind.Owner, out var pull)) //TO DO: to make the code prettier? don't like the repetition
        // {
        //     var pulledEntity = pull.Pulling;
        //     if (pulledEntity != null)
        //     {
        //         CheckEntity(pulledEntity.Value, condition, ref containerStack, ref count);
        //     }
        // }

        // recursively check each container for the item
        // checks inventory, bag, implants, etc.
        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    // check if this is the item
                    count += CheckStealTarget(entity, condition);

                    // if it is a container check its contents
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack?.Push(containerManager);
                }
            }
        } while (containerStack?.TryPop(out currentManager) ?? false);

        var result = count / (float) condition.CollectionSize;
        result = Math.Clamp(result, 0, 1);
        return result;
    }

    private void CheckEntity(EntityUid entity, StealConditionComponent condition, ref Stack<ContainerManagerComponent>? containerStack, ref int counter)
    {
        // check if this is the item
        counter += CheckStealTarget(entity, condition);

        if (containerStack == null)
            return;

        //we don't check the inventories of sentient entity
        if (!TryComp<MindContainerComponent>(entity, out var pullMind))
        {
            // if it is a container check its contents
            if (_containerQuery.TryGetComponent(entity, out var containerManager))
                containerStack.Push(containerManager);
        }
    }

    private int CheckStealTarget(EntityUid entity, StealConditionComponent condition)
    {
        // if (_countedItems.Contains(entity))
            // return 0;

        // check if this is the target
        if (!TryComp<StealTargetComponent>(entity, out var target))
            return 0;

        if (target.StealGroup != condition.StealGroup)
            return 0;

        // check if cartridge is installed
        if (TryComp<CartridgeComponent>(entity, out var cartridge) &&
            cartridge.InstallationStatus is not InstallationStatus.Cartridge)
            return 0;

        // check if needed target alive
        if (condition.CheckAlive)
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (!_mobState.IsAlive(entity, state))
                    return 0;
            }
        }

        // _countedItems.Add(entity);

        return TryComp<StackComponent>(entity, out var stack) ? stack.Count : 1;
    }

    // public void TrackItem(Entity<StealAreaComponent> area, EntityUid toTrack)
    // {
    //     foreach(var owner in area.Comp.Owners)
    //     {
    //         if (!_role.MindHasRole<ThiefRoleComponent>(owner, out var role))
    //             continue;
    //
    //         role.Value.Comp2.TrackedItems.Add(toTrack);
    //
    //         if (!HasComp<StorageComponent>(toTrack) && !HasComp<EntityStorageComponent>(toTrack))
    //             continue;
    //
    //         var stealStorage = EnsureComp<StealStorageComponent>(toTrack);
    //         stealStorage.Owners = new HashSet<EntityUid>(area.Comp.Owners);
    //         // stealStorage.StealArea = area.Owner;
    //         OnStorageAdded((toTrack, stealStorage));
    //     }
    // }

    public void TrackItem(Entity<MindComponent?> ent, EntityUid toTrack)
    {
        if(!_role.MindHasRole<ThiefRoleComponent>(ent, out var thief))
            return;


        thief.Value.Comp2.TrackedItems.Add(toTrack);

        var hasStorage = HasComp<StorageComponent>(toTrack) || HasComp<EntityStorageComponent>(toTrack);



        var stealStorage = EnsureComp<StealStorageComponent>(toTrack);
        stealStorage.Owners.Add(ent.Owner);
        // stealStorage.StealArea = area.Owner;
        OnStorageAdded(ent, (toTrack, stealStorage));

        if (!Resolve(ent, ref ent.Comp))
            return;
        foreach (var obj in ent.Comp.Objectives)
        {
            if (!TryComp<StealConditionComponent>(obj, out var stealCondition))
                continue;

            if (CheckStealTarget(toTrack, stealCondition) == 1)
                stealCondition.TrackedItems.Add(toTrack);
        }
        var evt = new ObjectiveUpdateEvent()
        RaiseLocalEvent();
    }

    public void UntrackItem(Entity<MindComponent?> ent, EntityUid toUntrack)
    {
        if(!_role.MindHasRole<ThiefRoleComponent>(ent.Owner, out var thief))
            return;

        thief.Value.Comp2.TrackedItems.Remove(toUntrack);

        if (!TryComp<StealStorageComponent>(toUntrack, out var stealStorage))
            return;

        stealStorage.Owners.Remove(ent.Owner);

        OnStorageRemoved(ent, toUntrack);

        if(stealStorage.Owners.Count == 0)
            RemComp<StealStorageComponent>(toUntrack);
    }

    private void OnInserted(Entity<StealStorageComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        foreach (var owner in ent.Comp.Owners)
        {
            TrackItem(owner, args.Entity);
            _log.Debug($"Item inserted into tracked container {args.Entity.Id}");
        }

        // if (!TryComp<StealAreaComponent>(ent.Comp.StealArea, out var area))
        //     return;

        // TrackItem((ent.Comp.StealArea, area), args.Entity);
    }

    private void OnRemoved(Entity<StealStorageComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        foreach (var owner in ent.Comp.Owners)
        {
            UntrackItem(owner, args.Entity);
            _log.Debug($"Item inserted into tracked container {args.Entity.Id}");
        }

        // if (!TryComp<StealAreaComponent>(ent.Comp.StealArea, out var area))
        //     return;
        //
        // UntrackItem((ent.Comp.StealArea, area), args.Entity);
        // _log.Debug($"Item removed from tracked container {args.Entity.Id}");
    }

    private void OnStorageAdded(Entity<MindComponent?> thief, Entity<StealStorageComponent> ent)
    {
        if (TryComp<StorageComponent>(ent, out var storage))
        {
            foreach (var item in storage.StoredItems.Keys)
            {
                TrackItem(thief, item);
            }

            return;
        }

        if (TryComp<EntityStorageComponent>(ent, out var entStorage))
        {
            foreach (var item in entStorage.Contents.ContainedEntities)
            {
                TrackItem(thief, item);
            }

            return;
        }
    }

    private void OnStorageRemoved(Entity<MindComponent?> ent, EntityUid toRemove)
    {
        if (TryComp<StorageComponent>(toRemove, out var storage))
        {
            foreach (var item in storage.StoredItems.Keys)
            {
                UntrackItem(ent, item);
            }

            return;
        }

        if (TryComp<EntityStorageComponent>(toRemove, out var entStorage))
        {
            foreach (var item in entStorage.Contents.ContainedEntities)
            {
                UntrackItem(ent, item);
            }

            return;
        }
    }

    private void OnPullStarted(Entity<PullerComponent> ent, ref PullStartedMessage args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mind))
            return;

        if (!_role.MindHasRole<ThiefRoleComponent>(mindId, out var role))
            return;

        TrackItem(mindId, args.PulledUid);
    }

    private void OnPullStopped(Entity<PullerComponent> ent, ref PullStoppedMessage args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mind))
            return;

        if (!_role.MindHasRole<ThiefRoleComponent>(mindId, out var role))
            return;

        UntrackItem(mindId, args.PulledUid);
    }
}
