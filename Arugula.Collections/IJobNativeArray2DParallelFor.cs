using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Arugula.Collections
{
    public interface IJobNativeArray2DParallelFor
    {
        void Execute(int index1, int index2);
    }
    public static class IJobNativeArray2DParallelForExtensions
    {
        internal struct JobNativeArray2DParallelForProducer<T> where T : struct, IJobNativeArray2DParallelFor
        {
            static IntPtr s_JobReflectionData;

            public static IntPtr Initialize()
            {
                if (s_JobReflectionData == IntPtr.Zero)
                {
#if UNITY_2020_2_OR_NEWER
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T), (ExecuteJobFunction)Execute);
#else
                    s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T),
                        JobType.ParallelFor, (ExecuteJobFunction)Execute);
#endif
                }

                return s_JobReflectionData;
            }

            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int begin, out int end))
                    {
                        return;
                    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
#endif

                    int height = end - begin;
                    int x = begin / height;
                    for (int y = 0; y < height; y++)
                    {
                        jobData.Execute(x, y);
                    }
                }
            }
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, int width, int height, JobHandle dependsOn = new JobHandle()) where T : struct, IJobNativeArray2DParallelFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobNativeArray2DParallelForProducer<T>.Initialize(), dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, width * height, height);
        }

        unsafe public static void Run<T>(this T jobData, int width, int height) where T : struct, IJobNativeArray2DParallelFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobNativeArray2DParallelForProducer<T>.Initialize(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, width * height, height);
        }
    }
}
