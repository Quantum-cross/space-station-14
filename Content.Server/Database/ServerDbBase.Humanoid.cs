using System.Linq;
using System.Text.Json;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    /// <param name="profile">
    ///     <summary>
    ///     Convert a database type HumanoidProfile to a content type HumanoidCharacterProfile
    ///     </summary>
    /// </param>
    /// <returns></returns>
    private static HumanoidCharacterProfile ConvertToHumanoidCharacter(HumanoidProfile profile)
    {
        var jobs = profile.Jobs.Select(j => new ProtoId<JobPrototype>(j.JobName)).ToHashSet();
        var antags = profile.Antags.Select(a => new ProtoId<AntagPrototype>(a.AntagName));
        var traits = profile.Traits.Select(t => new ProtoId<TraitPrototype>(t.TraitName));

        var sex = Sex.Male;
        if (Enum.TryParse<Sex>(profile.Sex, true, out var sexVal))
            sex = sexVal;

        var spawnPriority = (SpawnPriorityPreference) profile.SpawnPriority;

        var gender = sex == Sex.Male ? Gender.Male : Gender.Female;
        if (Enum.TryParse<Gender>(profile.Gender, true, out var genderVal))
            gender = genderVal;

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        var markingsRaw = profile.Markings?.Deserialize<List<string>>();

        List<Marking> markings = new();
        if (markingsRaw != null)
        {
            foreach (var marking in markingsRaw)
            {
                var parsed = Marking.ParseFromDbString(marking);

                if (parsed is null) continue;

                markings.Add(parsed);
            }
        }

        var loadouts = new Dictionary<string, RoleLoadout>();

        foreach (var role in profile.Loadouts)
        {
            var loadout = new RoleLoadout(role.RoleName)
            {
                EntityName = role.EntityName,
            };

            foreach (var group in role.Groups)
            {
                var groupLoadouts = loadout.SelectedLoadouts.GetOrNew(group.GroupName);
                foreach (var profLoadout in group.Loadouts)
                {
                    groupLoadouts.Add(new Loadout()
                    {
                        Prototype = profLoadout.LoadoutName,
                    });
                }
            }

            loadouts[role.RoleName] = loadout;
        }

        return new HumanoidCharacterProfile(
            profile.CharacterName,
            profile.FlavorText,
            profile.Species,
            profile.Age,
            sex,
            gender,
            new HumanoidCharacterAppearance
            (
                profile.HairName,
                Color.FromHex(profile.HairColor),
                profile.FacialHairName,
                Color.FromHex(profile.FacialHairColor),
                Color.FromHex(profile.EyeColor),
                Color.FromHex(profile.SkinColor),
                markings
            ),
            spawnPriority,
            jobs,
            antags.ToHashSet(),
            traits.ToHashSet(),
            loadouts,
            profile.Enabled
        );
    }

    private static BaseProfile ConvertHumanoidToDatabaseProfile(HumanoidCharacterProfile humanoid, int slot, BaseProfile? profile = null)
    {
        if(profile is not HumanoidProfile humanoidProfile)
        {
            humanoidProfile = new HumanoidProfile();
            profile = humanoidProfile;
        }
        var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;
        List<string> markingStrings = new();
        foreach (var marking in appearance.Markings)
        {
            markingStrings.Add(marking.ToString());
        }
        var markings = JsonSerializer.SerializeToDocument(markingStrings);

        humanoidProfile.CharacterName = humanoid.Name;
        humanoidProfile.FlavorText = humanoid.FlavorText;
        humanoidProfile.Species = humanoid.Species;
        humanoidProfile.Age = humanoid.Age;
        humanoidProfile.Sex = humanoid.Sex.ToString();
        humanoidProfile.Gender = humanoid.Gender.ToString();
        humanoidProfile.HairName = appearance.HairStyleId;
        humanoidProfile.HairColor = appearance.HairColor.ToHex();
        humanoidProfile.FacialHairName = appearance.FacialHairStyleId;
        humanoidProfile.FacialHairColor = appearance.FacialHairColor.ToHex();
        humanoidProfile.EyeColor = appearance.EyeColor.ToHex();
        humanoidProfile.SkinColor = appearance.SkinColor.ToHex();
        humanoidProfile.SpawnPriority = (int) humanoid.SpawnPriority;
        humanoidProfile.Markings = markings;
        humanoidProfile.Slot = slot;
        humanoidProfile.Enabled = humanoid.Enabled;

        humanoidProfile.Jobs.Clear();
        humanoidProfile.Jobs.AddRange(
            humanoid.JobPreferences
                .Select(j => new Job {JobName = j})
        );

        humanoidProfile.Antags.Clear();
        humanoidProfile.Antags.AddRange(
            humanoid.AntagPreferences
                .Select(a => new Antag {AntagName = a})
        );

        humanoidProfile.Traits.Clear();
        humanoidProfile.Traits.AddRange(
            humanoid.TraitPreferences
                    .Select(t => new Trait {TraitName = t})
        );

        humanoidProfile.Loadouts.Clear();

        foreach (var (role, loadouts) in humanoid.Loadouts)
        {
            var dz = new ProfileRoleLoadout()
            {
                RoleName = role,
                EntityName = loadouts.EntityName ?? string.Empty,
            };

            foreach (var (group, groupLoadouts) in loadouts.SelectedLoadouts)
            {
                var profileGroup = new ProfileLoadoutGroup()
                {
                    GroupName = group,
                };

                foreach (var loadout in groupLoadouts)
                {
                    profileGroup.Loadouts.Add(new ProfileLoadout()
                    {
                        LoadoutName = loadout.Prototype,
                    });
                }

                dz.Groups.Add(profileGroup);
            }

            humanoidProfile.Loadouts.Add(dz);
        }

        return profile;
    }
}
