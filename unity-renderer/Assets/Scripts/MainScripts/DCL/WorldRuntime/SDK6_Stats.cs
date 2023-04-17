using Unity.Profiling;

namespace MainScripts.DCL.WorldRuntime.KernelCommunication.WebSocketCommunication
{
    public class SDK6_Stats
    {
        public static readonly ProfilerCounterValue<float> messagesTotalSize =
            new (ProfilerCategory.Scripts, "Chunks Total Size",
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);

        public static readonly ProfilerCounterValue<int> sentMessagesAmount =
            new (ProfilerCategory.Scripts, "Chunks Amount",
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);


        public static readonly ProfilerCounterValue<int> precessedMessages =
            new (ProfilerCategory.Scripts, "Processed Messages Amount",
                ProfilerMarkerDataUnit.Count,
                ProfilerCounterOptions.FlushOnEndOfFrame | ProfilerCounterOptions.ResetToZeroOnFlush);
    }
}
