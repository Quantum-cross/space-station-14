using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestCharacterInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;

    public CharacterInfoEvent(NetEntity netEntity, string jobTitle, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing)
    {
        NetEntity = netEntity;
        JobTitle = jobTitle;
        Objectives = objectives;
        Briefing = briefing;
    }
}

[Serializable, NetSerializable]
public sealed class ObjectiveUpdateEvent : EntityEventArgs
{
    public readonly int ObjectiveIdx;
    public readonly float ObjectiveProgress;

    public ObjectiveUpdateEvent(int objectiveIdx, float objectiveProgress)
    {
        ObjectiveIdx = objectiveIdx;
        ObjectiveProgress = objectiveProgress;
    }
}
