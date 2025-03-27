using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared.Access.Systems;

public sealed partial class AccessReaderSystem
{

    private void FingerprintInitialize()
    {
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FingerprintReaderComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FingerprintReaderComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<FingerprintReaderComponent, GetAccessOrder>(OnGetAccessMetadata);

        _isAllowedCallbacks.Add(typeof(FingerprintReaderComponent), IsFingerprintAllowed);
    }


    private void OnGetState(Entity<FingerprintReaderComponent> ent, ref ComponentGetState args)
    {
        // Only send the fingerprint of the player's entity if it's allowed, otherwise send an empty list
        var filteredPrint = GetFilteredFingerprints(ent, args.Player?.AttachedEntity);
        args.State = new FingerprintReaderComponentState(ent.Comp.Enabled, filteredPrint, ent.Comp.IgnoreGloves);
    }

    private void OnHandleState(Entity<FingerprintReaderComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not FingerprintReaderComponentState state)
            return;
        ent.Comp.Enabled = state.Enabled;
        ent.Comp.AllowedFingerprints = state.AllowedFingerprints;
        ent.Comp.IgnoreGloves = state.IgnoreGloves;
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        // Only run this on client
        if (_playerManager.LocalSession == null)
            return;
        // If a player is attached to a new entity
        var hasFingerprint = TryComp<FingerprintComponent>(args.Entity, out var fingerprint) && fingerprint.Fingerprint != null;
        var query = EntityQueryEnumerator<FingerprintReaderComponent>();
        while (query.MoveNext(out var uid, out var reader))
        {
            // If we have a fingerprint, we need to dirty it to get the new state
            if (hasFingerprint)
                EntityManager.Dirty(uid, reader);
            // If we don't have a fingerprint, just clear the field because that's what we'd get back from the server anyway
            else
                reader.AllowedFingerprints.Clear();
        }
    }

    private HashSet<string> GetFilteredFingerprints(FingerprintReaderComponent reader, EntityUid? uid)
    {
        // If the reader uses fingerprints and the user has a fingerprint that matches, send it to them
        if (TryComp<FingerprintComponent>(uid, out var fingerprint) &&
            fingerprint.Fingerprint != null &&
            reader.AllowedFingerprints.Contains(fingerprint.Fingerprint))
        {
            return [fingerprint.Fingerprint];
        }
        // Otherwise send them an empty set
        return [];
    }

    /// <summary>
    /// Adds an allowed fingerprint to a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void AddAllowedFingerprint(Entity<FingerprintReaderComponent> reader, string fingerprint)
    {
        if(reader.Comp.AllowedFingerprints.Add(fingerprint))
            Dirty(reader);
    }

    /// <summary>
    /// Adds an allowed fingerprint to a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void AddAllowedFingerprint(Entity<FingerprintReaderComponent> reader, Entity<FingerprintComponent> user)
    {
        if (user.Comp.Fingerprint != null)
            AddAllowedFingerprint(reader, user.Comp.Fingerprint);
    }

    private bool? IsFingerprintAllowed(EntityUid user, EntityUid target, IComponent? component = null)
    {
        if (component is not FingerprintReaderComponent reader)
            return null;
        return IsFingerprintAllowed((target, reader), user);
    }

    /// <summary>
    /// Checks to see if user entity can unlock the access reader via fingerprint, creates a client popup if the
    /// fingerprint isn't allowed or if gloves are blocking the fingerprint.
    /// </summary>
    private bool? IsFingerprintAllowed(Entity<FingerprintReaderComponent> reader, Entity<FingerprintComponent?> user)
    {
        // If no fingerprints are registered, deny access, but continue other metheds
        if (reader.Comp.AllowedFingerprints.Count == 0)
        {
            return null;
        }

        // If user has no fingerprint, deny access fully
        if (!Resolve(user, ref user.Comp, false) || user.Comp.Fingerprint == null)
            return false;

        // If user's gloves are blocking fingerprint, deny access fully
        if (!reader.Comp.IgnoreGloves && TryGetBlockingGloves(user, out var gloves))
        {
            if (reader.Comp.FailGlovesPopup != null)
                _popup.PopupClient(Loc.GetString(reader.Comp.FailGlovesPopup, ("blocker", gloves)), reader, user);
            return false;
        }

        // Check fingerprint match
        if (!reader.Comp.AllowedFingerprints.Contains(user.Comp.Fingerprint))
        {
            if (reader.Comp.FailPopup != null)
                _popup.PopupClient(Loc.GetString(reader.Comp.FailPopup), reader, user);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the blocking gloves of a user. Gloves count as blocking if they hide fingerprints.
    /// </summary>
    /// <param name="user">Entity wearing the gloves.</param>
    /// <param name="blocker">The returned gloves, if they exist.</param>
    /// <returns>True if blocking gloves were found, otherwise False.</returns>
    [PublicAPI]
    public bool TryGetBlockingGloves(EntityUid user, [NotNullWhen(true)] out EntityUid? blocker)
    {
        blocker = null;

        if (_inventorySystem.TryGetSlotEntity(user, "gloves", out var gloves) && HasComp<FingerprintMaskComponent>(gloves))
        {
            blocker = gloves;
            return true;
        }

        return false;
    }


    public void LogFingerprintAccess(Entity<AccessReaderComponent> reader, EntityUid user)
    {
        // TODO: Not implemented
    }
}
