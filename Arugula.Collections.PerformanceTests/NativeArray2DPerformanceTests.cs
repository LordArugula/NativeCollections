using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Arugula.Collections.PerformanceTests
{
    internal class NativeArray2DPerformanceTests
    {
        const int width = 1 << 10;
        const int height = 1 << 10;

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_CopyFrom()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] source = new int[width, height];

            Measure.Method(() =>
            {
                array.CopyFrom(source);
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_CopyTo()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                array.CopyTo(dest);
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void ManagedArray2D_Performance_Write()
        {
            int[,] array = new int[width, height];

            Measure.Method(() =>
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        array[x, y] = x * y;
                    }
                }
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_Write()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        array[x, y] = x * y;
                    }
                }
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_WriteInJob()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                new WriteToArrayJob
                {
                    array = array
                }.Schedule().Complete();
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_BurstedWriteInJob()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                new BurstedWriteToArrayJob
                {
                    array = array
                }.Schedule().Complete();
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_WriteInParallelJob()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                new WriteToArrayParallelForJob
                {
                    array = array
                }.Schedule(width, width).Complete();
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_BurstedWriteInParallelJob()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                new BurstedWriteToArrayParallelForJob
                {
                    array = array
                }.Schedule(width, width).Complete();
            })
                .WarmupCount(100)
                .MeasurementCount(1000)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_BurstedWriteInParallelForBatchJob()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                new BurstedWriteToArrayParallelForBatchJob
                {
                    array = array
                }.ScheduleBatch(width * height, height).Complete();
            })
                .WarmupCount(100)
                .MeasurementCount(100)
                .Run();

            array.Dispose();
        }

        [Test, Performance, Category("Performance")]
        public void NativeArray2D_Performance_WriteInParallelForBatchJob()
        {
            NativeArray2D<int> array = new NativeArray2D<int>(width, height, Unity.Collections.Allocator.TempJob);

            int[,] dest = new int[width, height];

            Measure.Method(() =>
            {
                new WriteToArrayParallelForBatchJob
                {
                    array = array
                }.ScheduleBatch(width * height, height).Complete();
            })
                .WarmupCount(100)
                .MeasurementCount(100)
                .Run();

            array.Dispose();
        }

        public struct WriteToArrayJob : IJob
        {
            [WriteOnly]
            public NativeArray2D<int> array;

            public void Execute()
            {
                for (int x = 0; x < array.Length0; x++)
                {
                    for (int y = 0; y < array.Length1; y++)
                    {
                        array[x, y] = x * y;
                    }
                }
            }
        }

        [BurstCompile]
        public struct BurstedWriteToArrayJob : IJob
        {
            [WriteOnly]
            public NativeArray2D<int> array;

            public void Execute()
            {
                for (int x = 0; x < array.Length0; x++)
                {
                    for (int y = 0; y < array.Length1; y++)
                    {
                        array[x, y] = x * y;
                    }
                }
            }
        }

        public struct WriteToArrayParallelForJob : IJobParallelFor
        {
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray2D<int> array;

            public void Execute(int x)
            {
                for (int y = 0; y < array.Length1; y++)
                {
                    array[x, y] = x * y;
                }
            }
        }

        [BurstCompile]
        public struct BurstedWriteToArrayParallelForJob : IJobParallelFor
        {
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray2D<int> array;

            public void Execute(int x)
            {
                for (int y = 0; y < array.Length1; y++)
                {
                    array[x, y] = x * y;
                }
            }
        }

        public struct WriteToArrayParallelForBatchJob : IJobParallelForBatch
        {
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray2D<int> array;

            public void Execute(int startIndex, int count)
            {
                int x = startIndex / array.Length1;
                for (int y = 0; y < count; y++)
                {
                    array[x, y] = x * y;
                }
            }
        }

        [BurstCompile]
        public struct BurstedWriteToArrayParallelForBatchJob : IJobParallelForBatch
        {
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray2D<int> array;

            public void Execute(int startIndex, int count)
            {
                int x = startIndex / array.Length1;
                for (int y = 0; y < count; y++)
                {
                    array[x, y] = x * y;
                }
            }
        }
    }
}
