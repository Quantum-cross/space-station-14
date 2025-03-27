using Content.Shared.Access.Components;
using Content.Shared.FingerprintReader;
using Content.Shared.Forensics.Components;

namespace Content.Shared.Access.Systems;

public sealed class AccessSystem : EntitySystem
{
    public bool IsAllowed(EntityUid uid,
        AccessReaderComponent? accessReader = null,
        FingerprintComponent? fingerReader = null)
    {
        Resolve(uid, ref accessReader, ref fingerReader);

        RaiseLocalEvent(new GetAccessOrder());

        if ((accessReader, fingerReader) is (null, null))
            return true;

        return false;
    }
}

public sealed class GetAccessOrder : EntityEventArgs
{
    public SortedSet<AccessOrderEntry> AccessOrder = new ();

}

public sealed class AccessOrderEntry(Type unknown, int priority, bool terminateAfterDenial) : IComparable<AccessOrderEntry>
{
    public Type AccessType = unknown;
    public int Priority = priority;
    public bool TerminateAfterDenial = terminateAfterDenial;
    public int CompareTo(AccessOrderEntry? other)
    {
        return Comparer<int>.Default.Compare(Priority, other?.Priority ?? int.MaxValue);
    }
}
