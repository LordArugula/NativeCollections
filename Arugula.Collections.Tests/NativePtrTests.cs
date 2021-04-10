using NUnit.Framework;
using Unity.Burst;
using Unity.Jobs;

namespace Arugula.Collections.Tests
{
    internal class NativePtrTests
    {
        [Test]
        public void ReadWrite()
        {
            NativePtr<float> floatPtrA = new NativePtr<float>(Unity.Collections.Allocator.TempJob);
            NativePtr<float> floatPtrB = new NativePtr<float>(Unity.Collections.Allocator.TempJob);

            const float valueA = 5f;
            const float valueB = 10f;

            floatPtrA.Value = valueA;
            new WriteFromPtrToPtrJob
            {
                floatPtrInput = floatPtrA,
                floatPtrOutput = floatPtrB,
            }.Run();

            Assert.AreEqual(floatPtrB.Value, valueA);

            floatPtrA.Value = valueA;

            Assert.AreEqual(floatPtrA.Value, valueA);

            new WriteToPtrJob
            {
                floatPtr = floatPtrA,
                value = valueB
            }.Run();

            Assert.AreEqual(floatPtrA.Value, valueB);

            floatPtrA.Dispose();
            floatPtrB.Dispose();
        }

        [Test]
        public void Dispose()
        {
            NativePtr<float> ptr = new NativePtr<float>(Unity.Collections.Allocator.TempJob);

            Assert.IsTrue(ptr.IsCreated);
            ptr.Dispose();

            Assert.IsFalse(ptr.IsCreated);
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                ptr.Dispose();
            });

            ptr = new NativePtr<float>(Unity.Collections.Allocator.TempJob);
            var jobHandle = new WriteToPtrJob
            {
                floatPtr = ptr,
                value = 0f
            }.Schedule();
            jobHandle = ptr.Dispose(jobHandle);
            jobHandle.Complete();

            Assert.IsFalse(ptr.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                ptr.Value = 3;
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
