using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Arugula.Collections.Tests
{
    public class NativePtrTests
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
    }
}
