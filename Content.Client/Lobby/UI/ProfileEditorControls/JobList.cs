using System.Numerics;
using System.Linq;
using Content.Client.Lobby.UI.Loadouts;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Clothing;
using Content.Shared.Guidebook;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed class JobList : BoxContainer
{
    private IPrototypeManager _prototypeManager = default!;
    private IEntityManager _entManager = default!;
    private JobRequirementsManager _requirements = default!;
    private ISharedPlayerManager _playerManager = default!;

    private SpriteSystem _sprite = default!;

    private readonly Dictionary<string, BoxContainer> _jobCategories = new();
    private List<(string, RequirementsSelector)> _jobPreferences = new();

    private ProfileEditor? _editor;

    private LoadoutWindow? _loadoutWindow;

    public JobPrototype? JobOverride { get;  private set; }

    public event Action<List<ProtoId<GuideEntryPrototype>>>? OnOpenGuidebook;

    public JobList()
    {
        Orientation = LayoutOrientation.Vertical;
    }

    public void Initialize(ProfileEditor editor, IPrototypeManager protoMan, IEntityManager entMan, JobRequirementsManager jobMan, ISharedPlayerManager playerMan)
    {
        _editor = editor;
        _prototypeManager = protoMan;
        _entManager = entMan;
        _requirements = jobMan;
        _playerManager = playerMan;
        _sprite = _entManager.System<SpriteSystem>();
        RefreshJobs();
    }

    /// <summary>
    /// Updates selected job preferences to the priority selectors
    /// </summary>
    private void UpdateJobPreferences()
    {
        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        foreach (var (jobId, prioritySelector) in _jobPreferences)
        {
            prioritySelector.Select(humanoid.JobPreferences.Contains(jobId) ? 0 : 1);
        }
    }

    /// <summary>
    /// Refreshes all job selectors.
    /// </summary>
    public void RefreshJobs()
    {
        DisposeAllChildren();
        _jobCategories.Clear();
        _jobPreferences.Clear();

        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        var firstCategory = true;

        // Get all displayed departments
        var departments = new List<DepartmentPrototype>();
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.EditorHidden)
                continue;

            departments.Add(department);
        }

        departments.Sort(DepartmentUIComparer.Instance);

        var items = new[]
        {
            ("humanoid-profile-editor-antag-preference-yes-button", 0),
            ("humanoid-profile-editor-antag-preference-no-button", 1)
        };

        foreach (var department in departments)
        {
            var departmentName = Loc.GetString(department.Name);

            if (!_jobCategories.TryGetValue(department.ID, out var category))
            {
                category = new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Name = department.ID,
                    ToolTip = Loc.GetString("humanoid-profile-editor-jobs-amount-in-department-tooltip",
                        ("departmentName", departmentName))
                };

                if (firstCategory)
                {
                    firstCategory = false;
                }
                else
                {
                    category.AddChild(new Control
                    {
                        MinSize = new Vector2(0, 23),
                    });
                }

                category.AddChild(new PanelContainer
                {
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#464966")},
                    Children =
                    {
                        new Label
                        {
                            Text = Loc.GetString("humanoid-profile-editor-department-jobs-label",
                                ("departmentName", departmentName)),
                            Margin = new Thickness(5f, 0, 0, 0),
                        },
                    },
                });

                _jobCategories[department.ID] = category;
                AddChild(category);
            }

            var jobs = department.Roles.Select(jobId => _prototypeManager.Index(jobId))
                .Where(job => job.SetPreference)
                .ToArray();

            Array.Sort(jobs, JobUIComparer.Instance);

            foreach (var job in jobs)
            {
                var jobContainer = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var selector = new RequirementsSelector()
                {
                    Margin = new Thickness(3f, 3f, 3f, 0f),
                };
                selector.OnOpenGuidebook += OnOpenGuidebook;

                var icon = new TextureRect
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center
                };
                var jobIcon = _prototypeManager.Index(job.Icon);
                icon.Texture = _sprite.Frame0(jobIcon.Icon);
                selector.Setup(items, job.LocalizedName, 200, job.LocalizedDescription, icon, job.Guides);

                if (!_requirements.IsAllowed(job, humanoid, out var reason))
                {
                    selector.LockRequirements(reason);
                    humanoid = humanoid.WithoutJob(job);
                    _editor.SetDirty();
                }
                else
                {
                    selector.UnlockRequirements();
                }

                selector.OnSelected += selection => HandleJobSelected(job, selection);

                var loadoutWindowBtn = new Button()
                {
                    Text = Loc.GetString("loadout-window"),
                    HorizontalAlignment = HAlignment.Right,
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(3f, 3f, 0f, 0f),
                };

                var collection = IoCManager.Instance!;
                var protoManager = collection.Resolve<IPrototypeManager>();

                // If no loadout found then disabled button
                if (!protoManager.TryIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID), out var roleLoadoutProto))
                {
                    loadoutWindowBtn.Disabled = true;
                }
                // else
                else
                {
                    loadoutWindowBtn.OnPressed += args =>
                    {
                        RoleLoadout? loadout = null;

                        // Clone so we don't modify the underlying loadout.
                        humanoid?.Loadouts.TryGetValue(LoadoutSystem.GetJobPrototype(job.ID), out loadout);
                        loadout = loadout?.Clone();

                        if (loadout == null)
                        {
                            loadout = new RoleLoadout(roleLoadoutProto.ID);
                            loadout.SetDefault(humanoid, _playerManager.LocalSession, _prototypeManager);
                        }

                        OpenLoadout(job, loadout, roleLoadoutProto);
                    };
                }

                _jobPreferences.Add((job.ID, selector));
                jobContainer.AddChild(selector);
                jobContainer.AddChild(loadoutWindowBtn);
                category.AddChild(jobContainer);
            }

        }
        _editor.Profile = humanoid;
        UpdateJobPreferences();
        RefreshLoadouts();
    }

    private void HandleJobSelected(JobPrototype job, int selection)
    {
        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        var include = selection == 0;
        _editor.Profile = humanoid.WithJob(job, include);

        UpdateJobPreferences();
        _editor.PreviewPanel.ReloadPreview();
        _editor.SetDirty();
    }


    private void OpenLoadout(JobPrototype? jobProto, RoleLoadout roleLoadout, RoleLoadoutPrototype roleLoadoutProto)
    {
        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        _loadoutWindow?.Dispose();
        _loadoutWindow = null;
        var collection = IoCManager.Instance;

        if (collection == null || _playerManager.LocalSession == null || humanoid == null)
            return;

        JobOverride = jobProto;
        var session = _playerManager.LocalSession;

        _loadoutWindow = new LoadoutWindow(humanoid, roleLoadout, roleLoadoutProto, _playerManager.LocalSession, collection)
        {
            Title = jobProto?.ID + "-loadout",
        };

        // Refresh the buttons etc.
        _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
        _loadoutWindow.OpenCenteredLeft();

        _loadoutWindow.OnNameChanged += name =>
        {
            roleLoadout.EntityName = name;
            humanoid = humanoid.WithLoadout(roleLoadout);
            _editor?.SetDirty();
        };

        _loadoutWindow.OnLoadoutPressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.AddLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            humanoid = humanoid.WithLoadout(roleLoadout);
            _editor?.PreviewPanel.ReloadPreview();
        };

        _loadoutWindow.OnLoadoutUnpressed += (loadoutGroup, loadoutProto) =>
        {
            roleLoadout.RemoveLoadout(loadoutGroup, loadoutProto, _prototypeManager);
            _loadoutWindow.RefreshLoadouts(roleLoadout, session, collection);
            humanoid = humanoid.WithLoadout(roleLoadout);
            _editor?.PreviewPanel.ReloadPreview();
        };

        JobOverride = jobProto;
        _editor?.PreviewPanel.ReloadPreview();

        _loadoutWindow.OnClose += () =>
        {
            JobOverride = null;
            _editor?.PreviewPanel.ReloadPreview();
        };

        UpdateJobPreferences();
    }

    /// <summary>
    /// Refresh all loadouts.
    /// </summary>
    public void RefreshLoadouts()
    {
        _loadoutWindow?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _loadoutWindow?.Dispose();
        _loadoutWindow = null;
    }
}
