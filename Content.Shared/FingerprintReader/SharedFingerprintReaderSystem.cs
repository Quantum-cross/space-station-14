using System.Diagnostics.CodeAnalysis;
using Content.Shared.Forensics.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.FingerprintReader;

// TODO: This has a lot of overlap with the AccessReaderSystem, maybe merge them in the future?
public class SharedFingerprintReaderSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<ActivatableUIComponent, GetVerbsEvent<ActivationVerb>>(GetActivationVerb);
        SubscribeLocalEvent<FingerprintReaderComponent, GetVerbsEvent<Verb>>(GetVerb);
        SubscribeAllEvent<FingerprintReaderSetAttemptFailedNearbyEvent>(OnAttemptFailedNearby);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQuery<FingerprintReaderComponent>();

        foreach (var comp in query)
        {
            var toDelete = new HashSet<string>();
            foreach (var (fingerprint, request) in comp.ActiveSetRequests)
            {
                request.Timeout--;
                if (request.Timeout != 0)
                    continue;
                comp.AllowedFingerprints.Add(fingerprint);
                toDelete.Add(fingerprint);
            }

            foreach (var fingerprint in toDelete)
            {
                comp.ActiveSetRequests.Remove(fingerprint);
            }
        }
    }

    private void OnAttemptFailedNearby(FingerprintReaderSetAttemptFailedNearbyEvent evt)
    {
        var user = GetEntity(evt.User);
        var target = GetEntity(evt.Target);
        if (!TryComp<FingerprintReaderComponent>(target, out var fingerprintReader))
            return;

        if (fingerprintReader.FailNearbyPopup != null)
            _popup.PopupClient(Loc.GetString(fingerprintReader.FailNearbyPopup), target, user);

        if (!TryComp<FingerprintComponent>(user, out var fingerprint) || fingerprint.Fingerprint == null)
            return;

        fingerprintReader.ActiveSetRequests.Remove(fingerprint.Fingerprint);
    }

    private void GetVerb(Entity<FingerprintReaderComponent> reader, ref GetVerbsEvent<Verb> evt)
    {
        if (!reader.Comp.IsUserSettable)
            return;

        var user = evt.User;
        evt.Verbs.Add(new Verb
        {
            Act = () => TrySetFingerprint(user, reader),
            Text = Loc.GetString("Set Fingerprint"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        });
    }

    private void TrySetFingerprint(EntityUid user, Entity<FingerprintReaderComponent> target)
    {
        if ((target.Comp.AllowedFingerprints.Count + 1) >= target.Comp.FingerprintCapacity)
        {
            if (target.Comp.FailCapacityPopup != null)
                _popup.PopupClient(Loc.GetString(target.Comp.FailCapacityPopup), target, user);
            return;
        }

        if (!target.Comp.IgnoreGloves && TryGetBlockingGloves(user, out var gloves))
        {
            if (target.Comp.FailGlovesPopup != null)
                _popup.PopupClient(Loc.GetString(target.Comp.FailGlovesPopup, ("blocker", gloves)), target, user);
            return;
        }

        var evt = new FingerprintReaderSetAttemptEvent(user, target.Owner);
        RaiseLocalEvent(target.Owner, evt);
    }

    /// <summary>
    /// Checks if the given user has fingerprint access to the target entity.
    /// </summary>
    /// <param name="target">The target entity.</param>
    /// <param name="user">User trying to gain access.</param>
    /// <returns>True if access was granted, otherwise false.</returns>
    [PublicAPI]
    public bool IsAllowed(Entity<FingerprintReaderComponent?> target, EntityUid user)
    {
        if (!Resolve(target, ref target.Comp, false))
            return true;

        if (target.Comp.AllowedFingerprints.Count == 0)
            return true;

        // Check for gloves first
        if (!target.Comp.IgnoreGloves && TryGetBlockingGloves(user, out var gloves))
        {
            if (target.Comp.FailGlovesPopup != null)
                _popup.PopupClient(Loc.GetString(target.Comp.FailGlovesPopup, ("blocker", gloves)), target, user);
            return false;
        }

        // Check fingerprint match
        if (!TryComp<FingerprintComponent>(user, out var fingerprint) || fingerprint.Fingerprint == null ||
            !target.Comp.AllowedFingerprints.Contains(fingerprint.Fingerprint))
        {
            if (target.Comp.FailPopup != null)
                _popup.PopupClient(Loc.GetString(target.Comp.FailPopup), target, user);

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

        if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves) && HasComp<FingerprintMaskComponent>(gloves))
        {
            blocker = gloves;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the allowed fingerprints for a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void SetAllowedFingerprints(Entity<FingerprintReaderComponent> target, HashSet<string> fingerprints)
    {
        target.Comp.AllowedFingerprints = fingerprints;
        Dirty(target);
    }

    /// <summary>
    /// Adds an allowed fingerprint to a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void AddAllowedFingerprint(Entity<FingerprintReaderComponent> target, string fingerprint)
    {
        target.Comp.AllowedFingerprints.Add(fingerprint);
        Dirty(target);
    }

    /// <summary>
    /// Removes an allowed fingerprint from a fingerprint reader
    /// </summary>
    [PublicAPI]
    public void RemoveAllowedFingerprint(Entity<FingerprintReaderComponent> target, string fingerprint)
    {
        target.Comp.AllowedFingerprints.Remove(fingerprint);
        Dirty(target);
    }

    public sealed class SetRequest(EntityUid user)
    {
        public EntityUid User { get; set; } = user;
        public int Timeout { get; set; } = 3;
    }
}
