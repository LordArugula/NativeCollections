using NUnit.Framework;
using Unity.Burst;
using Unity.Jobs;

namespace Arugula.Collections.Tests
{
    internal class NativePtrTests
    {
        [Test]
        public void PtrStoresData()
        {
            NativePtr<float> floatPtr = new NativePtr<float>(Unity.Collections.Allocator.TempJob);

            const float value = 5f;
            floatPtr.Value = value;

            Assert.AreEqual(floatPtr.Value, value);
            floatPtr.Dispose();
        }

        [Test]
        public void PtrStoresDataAfterJob()
        {
            NativePtr<float> floatPtr = new NativePtr<float>(Unity.Collections.Allocator.TempJob);

            const float value = 5f;
            new WriteToPtrJob
            {
                floatPtr = floatPtr,
                value = value
            }.Run();

            Assert.AreEqual(floatPtr.Value, value);
            floatPtr.Dispose();
        }

        [Test]
        public void WriteFromPtrToSecondPtr()
        {
            NativePtr<float> floatPtrA = new NativePtr<float>(Unity.Collections.Allocator.TempJob);
            NativePtr<float> floatPtrB = new NativePtr<float>(Unity.Collections.Allocator.TempJob);

            const float value = 5f;
            floatPtrA.Value = value;
            new WriteFromPtrToPtrJob
            {
                floatPtrInput = floatPtrA,
                floatPtrOutput = floatPtrB,
            }.Run();

            Assert.AreEqual(floatPtrB.Value, value);
            floatPtrA.Dispose();
            floatPtrB.Dispose();
        }

        [Test]
        public void NativePtrDisposeJob()
        {
            Assert.DoesNotThrow(() =>
            {
                NativePtr<float> ptr = new NativePtr<float>(Unity.Collections.Allocator.TempJob);

                var jobHandle = new WriteToPtrJob
                {
                    floatPtr = ptr,
                    value = 0f
                }.Schedule();
                jobHandle = ptr.Dispose(jobHandle);
                jobHandle.Complete();

                Assert.IsTrue(ptr.IsCreated == false);
            });
        }

        [BurstCompile]
        public struct WriteToPtrJob : IJob
        {
            public NativePtr<float> floatPtr;
            public float value;

            public void Execute()
            {
                floatPtr.Value = value;
            }
        }

        [BurstCompile]
        public struct WriteFromPtrToPtrJob : IJob
        {
            public NativePtr<float> floatPtrInput;
            public NativePtr<float> floatPtrOutput;

            public void Execute()
            {
                floatPtrOutput.Value = floatPtrInput.Value;
            }
        }

        public struct ManagedStructTest
        {
            public string managedVar;
        }

        [Test]
        public void ThrowsIfTypeIsNotUnmanaged()
        {
            Assert.Throws<System.NotSupportedException>(() =>
            {
                var ptr = new NativePtr<ManagedStructTest>(Unity.Collections.Allocator.Temp);

                ptr.Dispose();
            });
        }
    }
}
