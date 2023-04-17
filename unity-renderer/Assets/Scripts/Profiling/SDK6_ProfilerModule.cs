using Unity.Profiling;
using Unity.Profiling.Editor;

[System.Serializable]
[ProfilerModuleMetadata("SDK6 Messages")]
public class SDK6_ProfilerModule : ProfilerModule
{
    private static readonly ProfilerCounterDescriptor[] COUNTERS =
        {
            new ("Chunks Total Size", ProfilerCategory.Scripts),
            new ("Chunks Amount", ProfilerCategory.Scripts),
            new ("Processed Messages Amount", ProfilerCategory.Scripts),
        };

    public SDK6_ProfilerModule() : base(COUNTERS) { }
}



