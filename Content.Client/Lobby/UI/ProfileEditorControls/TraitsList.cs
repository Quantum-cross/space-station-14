﻿using System.Linq;
using Content.Client.Lobby.UI.Roles;
using Content.Client.Stylesheets;
using Content.Shared.Preferences;
using Content.Shared.Traits;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed class TraitsList : BoxContainer
{
    private IPrototypeManager _prototypeManager = default!;

    private ProfileEditor? _editor;

    public TraitsList()
    {
        Orientation = LayoutOrientation.Vertical;
    }

    public void Initialize(ProfileEditor editor, IPrototypeManager protoMan)
    {
        _editor = editor;
        _prototypeManager = protoMan;

        RefreshTraits();
    }

    /// <summary>
    /// Refreshes traits selector
    /// </summary>
    public void RefreshTraits()
    {
        DisposeAllChildren();

        var traits = _prototypeManager.EnumeratePrototypes<TraitPrototype>().OrderBy(t => Loc.GetString(t.Name)).ToList();

        if (traits.Count < 1 || _editor?.Profile is not HumanoidCharacterProfile humanoid)
        {
            AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-no-traits"),
                FontColorOverride = Color.Gray,
            });
            return;
        }

        // Setup model
        Dictionary<string, List<string>> traitGroups = new();
        List<string> defaultTraits = new();
        traitGroups.Add(TraitCategoryPrototype.Default, defaultTraits);

        foreach (var trait in traits)
        {
            if (trait.Category == null)
            {
                defaultTraits.Add(trait.ID);
                continue;
            }

            if (!_prototypeManager.HasIndex(trait.Category))
                continue;

            var group = traitGroups.GetOrNew(trait.Category);
            group.Add(trait.ID);
        }

        // Create UI view from model
        foreach (var (categoryId, categoryTraits) in traitGroups)
        {
            TraitCategoryPrototype? category = null;

            if (categoryId != TraitCategoryPrototype.Default)
            {
                category = _prototypeManager.Index<TraitCategoryPrototype>(categoryId);
                // Label
                AddChild(new Label
                {
                    Text = Loc.GetString(category.Name),
                    Margin = new Thickness(0, 10, 0, 0),
                    StyleClasses = { StyleBase.StyleClassLabelHeading },
                });
            }

            List<TraitPreferenceSelector?> selectors = new();
            var selectionCount = 0;

            foreach (var traitProto in categoryTraits)
            {
                var trait = _prototypeManager.Index<TraitPrototype>(traitProto);
                var selector = new TraitPreferenceSelector(trait);

                selector.Preference = humanoid.TraitPreferences.Contains(trait.ID);
                if (selector.Preference)
                    selectionCount += trait.Cost;

                selector.PreferenceChanged += preference => HandlePreferenceChanged(trait, preference);

                selectors.Add(selector);
            }

            // Selection counter
            if (category is { MaxTraitPoints: >= 0 })
            {
                AddChild(new Label
                {
                    Text = Loc.GetString("humanoid-profile-editor-trait-count-hint", ("current", selectionCount) ,("max", category.MaxTraitPoints)),
                    FontColorOverride = Color.Gray
                });
            }

            foreach (var selector in selectors)
            {
                if (selector == null)
                    continue;

                if (category is { MaxTraitPoints: >= 0 } &&
                    selector.Cost + selectionCount > category.MaxTraitPoints)
                {
                    selector.Checkbox.Label.FontColorOverride = Color.Red;
                }

                AddChild(selector);
            }
        }

        _editor.Profile = humanoid;
    }

    private void HandlePreferenceChanged(TraitPrototype trait, bool preference)
    {
        if (_editor?.Profile is not HumanoidCharacterProfile humanoid)
            return;

        _editor.Profile = humanoid.WithTraitPreference(trait.ID, _prototypeManager, preference);
        _editor?.SetDirty();
        RefreshTraits(); // If too many traits are selected, they will be reset to the real value.
    }
}
