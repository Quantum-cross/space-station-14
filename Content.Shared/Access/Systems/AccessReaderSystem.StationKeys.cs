using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems;

public sealed partial class AccessReaderSystem
{
    private bool? IsStationKeyAllowed(EntityUid user, EntityUid target, IComponent? component = null)
    {
        if (component is not AccessReaderComponent reader)
            return null;
        if (!reader.Enabled)
            return null;
        return IsStationKeyAllowed(user, (target, reader));
    }

    public bool IsStationKeyAllowed(EntityUid user, Entity<AccessReaderComponent> reader)
    {
        var accessSources = FindPotentialAccessItems(user);
        var access = FindAccessTags(user, accessSources);
        FindStationRecordKeys(user, out var stationKeys, accessSources);

        return IsStationKeyAllowed(access, stationKeys, reader);
    }

    /// <summary>
    /// Check whether the given access permissions satisfy an access reader's requirements.
    /// </summary>
    public bool IsStationKeyAllowed(
        ICollection<ProtoId<AccessLevelPrototype>> access,
        ICollection<StationRecordKey> stationKeys,
        Entity<AccessReaderComponent> reader)
    {
        if (!reader.Comp.Enabled)
            return true;

        if (reader.Comp.ContainerAccessProvider == null)
            return IsStationKeyAllowedInternal(access, stationKeys, reader);

        if (!_containerSystem.TryGetContainer(reader.Owner, reader.Comp.ContainerAccessProvider, out var container))
            return false;

        // If entity is paused then always allow it at this point.
        // Door electronics is kind of a mess but yeah, it should only be an unpaused ent interacting with it
        if (Paused(reader.Owner))
            return true;

        foreach (var entity in container.ContainedEntities)
        {
            if (!TryComp(entity, out AccessReaderComponent? containedReader))
                continue;

            if (IsStationKeyAllowed(access, stationKeys, (entity, containedReader)))
                return true;
        }

        return false;
    }

    private bool IsStationKeyAllowedInternal(ICollection<ProtoId<AccessLevelPrototype>> access, ICollection<StationRecordKey> stationKeys, AccessReaderComponent reader)
    {
        return !reader.Enabled
               || AreAccessTagsAllowed(access, reader)
               || AreStationRecordKeysAllowed(stationKeys, reader);
    }

    /// <summary>
    /// Compares the given tags with the readers access list to see if it is allowed.
    /// </summary>
    /// <param name="accessTags">A list of access tags</param>
    /// <param name="reader">An access reader to check against</param>
    public bool AreAccessTagsAllowed(ICollection<ProtoId<AccessLevelPrototype>> accessTags, AccessReaderComponent reader)
    {
        if (reader.DenyTags.Overlaps(accessTags))
        {
            // Sec owned by cargo.

            // Note that in resolving the issue with only one specific item "counting" for access, this became a bit more strict.
            // As having an ID card in any slot that "counts" with a denied access group will cause denial of access.
            // DenyTags doesn't seem to be used right now anyway, though, so it'll be dependent on whoever uses it to figure out if this matters.
            return false;
        }

        if (reader.AccessLists.Count == 0)
            return true;

        foreach (var set in reader.AccessLists)
        {
            if (set.IsSubsetOf(accessTags))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Compares the given stationrecordkeys with the accessreader to see if it is allowed.
    /// </summary>
    public bool AreStationRecordKeysAllowed(ICollection<StationRecordKey> keys, AccessReaderComponent reader)
    {
        foreach (var key in reader.AccessKeys)
        {
            if (keys.Contains(key))
                return true;
        }

        return false;
    }

        /// <summary>
    /// Finds all the items that could potentially give access to a given entity
    /// </summary>
    public HashSet<EntityUid> FindPotentialAccessItems(EntityUid uid)
    {
        FindAccessItemsInventory(uid, out var items);

        var ev = new GetAdditionalAccessEvent
        {
            Entities = items
        };
        RaiseLocalEvent(uid, ref ev);

        foreach (var item in new ValueList<EntityUid>(items))
        {
            items.UnionWith(FindPotentialAccessItems(item));
        }
        items.Add(uid);
        return items;
    }

    /// <summary>
    /// Finds the access tags on the given entity
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="items">All of the items to search for access. If none are passed in, <see cref="FindPotentialAccessItems"/> will be used.</param>
    public ICollection<ProtoId<AccessLevelPrototype>> FindAccessTags(EntityUid uid, HashSet<EntityUid>? items = null)
    {
        HashSet<ProtoId<AccessLevelPrototype>>? tags = null;
        var owned = false;

        items ??= FindPotentialAccessItems(uid);

        foreach (var ent in items)
        {
            FindAccessTagsItem(ent, ref tags, ref owned);
        }

        return (ICollection<ProtoId<AccessLevelPrototype>>?) tags ?? Array.Empty<ProtoId<AccessLevelPrototype>>();
    }

    /// <summary>
    /// Finds the access tags on the given entity
    /// </summary>
    /// <param name="uid">The entity that is being searched.</param>
    /// <param name="recordKeys"></param>
    /// <param name="items">All of the items to search for access. If none are passed in, <see cref="FindPotentialAccessItems"/> will be used.</param>
    public bool FindStationRecordKeys(EntityUid uid, out ICollection<StationRecordKey> recordKeys, HashSet<EntityUid>? items = null)
    {
        recordKeys = new HashSet<StationRecordKey>();

        items ??= FindPotentialAccessItems(uid);

        foreach (var ent in items)
        {
            if (FindStationRecordKeyItem(ent, out var key))
                recordKeys.Add(key.Value);
        }

        return recordKeys.Any();
    }

    /// <summary>
    ///     Try to find <see cref="AccessComponent"/> on this item
    ///     or inside this item (if it's pda)
    ///     This version merges into a set or replaces the set.
    ///     If owned is false, the existing tag-set "isn't ours" and can't be merged with (is read-only).
    /// </summary>
    private void FindAccessTagsItem(EntityUid uid, ref HashSet<ProtoId<AccessLevelPrototype>>? tags, ref bool owned)
    {
        if (!FindAccessTagsItem(uid, out var targetTags))
        {
            // no tags, no problem
            return;
        }
        if (tags != null)
        {
            // existing tags, so copy to make sure we own them
            if (!owned)
            {
                tags = new(tags);
                owned = true;
            }
            // then merge
            tags.UnionWith(targetTags);
        }
        else
        {
            // no existing tags, so now they're ours
            tags = targetTags;
            owned = false;
        }
    }

    public bool FindAccessItemsInventory(EntityUid uid, out HashSet<EntityUid> items)
    {
        items = new();

        foreach (var item in _handsSystem.EnumerateHeld(uid))
        {
            items.Add(item);
        }

        // maybe its inside an inventory slot?
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
        {
            items.Add(idUid.Value);
        }

        return items.Any();
    }

    /// <summary>
    ///     Try to find <see cref="AccessComponent"/> on this item
    ///     or inside this item (if it's pda)
    /// </summary>
    private bool FindAccessTagsItem(EntityUid uid, out HashSet<ProtoId<AccessLevelPrototype>> tags)
    {
        tags = new();
        var ev = new GetAccessTagsEvent(tags, _prototype);
        RaiseLocalEvent(uid, ref ev);

        return tags.Count != 0;
    }

    /// <summary>
    ///     Try to find <see cref="StationRecordKeyStorageComponent"/> on this item
    ///     or inside this item (if it's pda)
    /// </summary>
    private bool FindStationRecordKeyItem(EntityUid uid, [NotNullWhen(true)] out StationRecordKey? key)
    {
        if (TryComp(uid, out StationRecordKeyStorageComponent? storage) && storage.Key != null)
        {
            key = storage.Key;
            return true;
        }

        if (TryComp<PdaComponent>(uid, out var pda) &&
            pda.ContainedId is { Valid: true } id)
        {
            if (TryComp<StationRecordKeyStorageComponent>(id, out var pdastorage) && pdastorage.Key != null)
            {
                key = pdastorage.Key;
                return true;
            }
        }

        key = null;
        return false;
    }
}
