using nhitomi.Models;

namespace nhitomi.Database
{
    public interface IDbSupportsSnapshot
    {
        SnapshotTarget SnapshotTarget { get; }
    }
}