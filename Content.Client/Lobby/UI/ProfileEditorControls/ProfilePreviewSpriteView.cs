using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView : SpriteView
{

    private IClientPreferencesManager _preferencesManager = default!;
    private IPrototypeManager _prototypeManager = default!;
    private IEntityManager _entManager = default!;
    private ISharedPlayerManager _playerManager = default!;

    public string? JobName { get; private set; }

    public EntityUid PreviewDummy { get; private set; } = EntityUid.Invalid;

    public void Initialize(IClientPreferencesManager prefMan,
        IPrototypeManager protoMan,
        IEntityManager entMan,
        ISharedPlayerManager playerMan)
    {
        _preferencesManager = prefMan;
        _prototypeManager = protoMan;
        _entManager = entMan;
        _playerManager = playerMan;
    }

    public void LoadPreview(ICharacterProfile profile, JobPrototype? jobOverride = null, bool showClothes = true)
    {
        _entManager.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                LoadHumanoidEntity(humanoid, jobOverride, showClothes);
                break;
            case BorgCharacterProfile borg:
                LoadBorgEntity(borg);
                break;
            default:
                return;
        }

        SetEntity(PreviewDummy);
        InvalidateMeasure();
        _entManager.System<MetaDataSystem>().SetEntityName(PreviewDummy, profile.Name);
    }

    public void ReloadProfilePreview(ICharacterProfile profile)
    {
        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                ReloadHumanoidEntity(humanoid);
                break;
            case BorgCharacterProfile borg:
                ReloadBorgEntity(borg);
                break;
        }
    }

    private void LoadBorgEntity(BorgCharacterProfile borg)
    {
        var borgJob = _prototypeManager.Index(BorgCharacterProfile.ConstantJob);
        var previewEntity = borgJob.JobEntity;
        PreviewDummy = _entManager.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
    }

    private void ReloadBorgEntity(BorgCharacterProfile borg)
    {

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _entManager.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private JobPrototype? GetPreferredJob(ICharacterProfile profile)
    {
        ProtoId<JobPrototype> highPriorityJob = default;
        if (profile.JobPreferences.Count == 1)
        {
            highPriorityJob = profile.JobPreferences.First();
        }
        else
        {
            var priorities = _preferencesManager.Preferences?.JobPriorities ?? [];
            foreach (var priority in new List<JobPriority>{JobPriority.High, JobPriority.Medium, JobPriority.Low})
            {
                highPriorityJob = profile.JobPreferences.FirstOrDefault(p => priorities.GetValueOrDefault(p) == priority);
                if (highPriorityJob.Id != null)
                    break;
            }
        }
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return highPriorityJob.Id == null ? null : _prototypeManager.Index(highPriorityJob);
    }
}
