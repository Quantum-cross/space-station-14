using System.Linq;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Guidebook;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed class AntagList : BoxContainer
{
    private IPrototypeManager _prototypeManager = default!;
    private IEntityManager _entManager = default!;
    private JobRequirementsManager _requirements = default!;

    private ProfileEditor? _editor;

    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    public AntagList()
    {
        Orientation = LayoutOrientation.Vertical;
    }

    public void Initialize(ProfileEditor editor, IPrototypeManager protoMan, IEntityManager entMan, JobRequirementsManager jobMan)
    {
        _editor = editor;
        _prototypeManager = protoMan;
        _entManager = entMan;
        _requirements = jobMan;
        RefreshAntags();
    }

    public void RefreshAntags()
    {
        DisposeAllChildren();

        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        var items = new[]
        {
            ("humanoid-profile-editor-antag-preference-yes-button", 0),
            ("humanoid-profile-editor-antag-preference-no-button", 1),
        };

        foreach (var antag in _prototypeManager.EnumeratePrototypes<AntagPrototype>().OrderBy(a => Loc.GetString(a.Name)))
        {
            if (!antag.SetPreference)
                continue;

            var antagContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
            };

            var selector = new RequirementsSelector()
            {
                Margin = new Thickness(3f, 3f, 3f, 0f),
            };
            selector.OnOpenGuidebook += OnOpenGuidebook;

            var title = Loc.GetString(antag.Name);
            var description = Loc.GetString(antag.Objective);
            selector.Setup(items, title, 250, description, guides: antag.Guides);
            selector.Select(humanoid.AntagPreferences.Contains(antag.ID) == true ? 0 : 1);

            var requirements = _entManager.System<SharedRoleSystem>().GetAntagRequirement(antag);
            if (!_requirements.CheckRoleRequirements(requirements, humanoid, out var reason))
            {
                selector.LockRequirements(reason);
                humanoid = humanoid.WithAntagPreference(antag.ID, false);
                _editor.SetDirty();
            }
            else
            {
                selector.UnlockRequirements();
            }

            selector.OnSelected += preference => HandleAntagSelected(antag, preference);

            antagContainer.AddChild(selector);

            antagContainer.AddChild(new Button()
            {
                Disabled = true,
                Text = Loc.GetString("loadout-window"),
                HorizontalAlignment = HAlignment.Right,
                Margin = new Thickness(3f, 0f, 0f, 0f),
            });

            AddChild(antagContainer);
        }

        _editor.Profile = humanoid;
    }

    private void HandleAntagSelected(AntagPrototype antag, int preference)
    {
        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        _editor.Profile = humanoid.WithAntagPreference(antag.ID, preference == 0);
        _editor.PreviewPanel.ReloadPreview();
        _editor?.SetDirty();
    }
}
