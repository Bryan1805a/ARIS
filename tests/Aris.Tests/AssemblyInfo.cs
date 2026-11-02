using Xunit.Sdk;
using Xunit.v3;

// Integration tests share one local SQL Server database and leave invariant-safe
// fixtures behind. Running collections in parallel causes cross-test deadlocks and
// lets the Golden Invariant observe another fixture mid-write, so this assembly runs
// sequentially. Unit tests remain parallel.
[assembly: Parallelization(Mode = ParallelMode.None)]
