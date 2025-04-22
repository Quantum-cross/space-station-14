
using Content.Shared.Preferences;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static BorgCharacterProfile ConvertToBorgCharacter(BorgProfile borg)
    {
        return new BorgCharacterProfile(borg.CharacterName, (SpawnPriorityPreference)borg.SpawnPriority);
    }

    private static BaseProfile ConvertBorgToDatabaseProfile(BorgCharacterProfile borg, int slot, BaseProfile? profile)
    {
        if(profile is not BorgProfile borgProfile)
        {
            borgProfile = new BorgProfile();
            profile = borgProfile;
        }
        // var appearance = (BorgCharacterAppearance) borg.CharacterAppearance;
        borgProfile.CharacterName = borg.Name;
        borgProfile.SpawnPriority = (int) borg.SpawnPriority;
        borgProfile.Slot = slot;
        borgProfile.Enabled = borg.Enabled;
        return profile;
    }
}
