using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Arugula.Collections.Tests
{
    internal class NativeHeapTests
    {
        [Test]
        public void IsEmpty()
        {
            var heap = new NativeHeap<int>(0, Allocator.Persistent);
            Assert.IsTrue(heap.IsEmpty);

            heap.Push(0);
            Assert.IsFalse(heap.IsEmpty);

            heap.Pop();
            Assert.IsTrue(heap.IsEmpty);

            heap.Push(0);
            heap.Clear();
            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        [Test]
        public void Create_HasZeroLength()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);
            Assert.IsTrue(heap.Count == 0);
            heap.Dispose();
        }

        [Test]
        public void PushIncrementsCount()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(5);
            Assert.IsTrue(heap.Count == 1);
            heap.Dispose();
        }

        [Test]
        public void PushOneAndThree_IsSorted()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(1);
            heap.Push(3);
            Assert.AreEqual(1, heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushThreeAndOne_IsSorted()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(1);
            Assert.AreEqual(1, heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushFive_RootIsFive()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(5);
            Assert.AreEqual(5, heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushDuplicate_ContainsDuplicate()
        {
            var heap = new NativeHeap<int>(5, Allocator.Persistent);

            heap.PushNoResize(1);
            heap.PushNoResize(1);

            Assert.AreEqual(2, heap.Count);
            heap.Dispose();
        }


        [Test]
        public void PushDuplicate_RemainsSorted()
        {
            var heap = new NativeHeap<int>(5, Allocator.Persistent);

            heap.PushNoResize(1);
            heap.PushNoResize(3);
            heap.PushNoResize(2);
            heap.PushNoResize(5);
            heap.PushNoResize(1);

            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void PushAndPop_RemainsSorted()
        {
            var heap = new NativeHeap<int>(5, Allocator.Persistent);

            heap.PushNoResize(5);
            heap.PushNoResize(3);
            heap.PushNoResize(1);
            heap.Pop();

            heap.PushNoResize(4);
            heap.PushNoResize(2);

            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void PopRemovesRoot()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(1);
            Assert.AreEqual(1, heap.Pop());
            heap.Dispose();
        }

        [Test]
        public void PopDecrementsCount()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(1);
            Assert.AreEqual(1, heap.Pop());
            heap.Dispose();
        }

        [Test]
        public void Pop_RemainsSorted()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(6);
            heap.Push(1);
            heap.Push(4);
            heap.Push(2);
            heap.Push(5);

            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());
            Assert.AreEqual(6, heap.Pop());
            heap.Dispose();
        }

        [Test]
        public void CreateFromArray_HasSameCount()
        {
            var array = new int[] { 1, 5, 2, 3, 4 };
            var heap = new NativeHeap<int>(array, Allocator.Persistent);

            Assert.AreEqual(array.Length, heap.Count);

            heap.Dispose();
        }

        [Test]
        public void CreateFromArray_IsSorted()
        {
            var array = new int[] { 1, 5, 2, 3, 4 };
            var heap = new NativeHeap<int>(array, Allocator.Persistent);

            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void CreateFromNativeArray_IsSorted()
        {
            var nativeArray = new NativeArray<int>(new int[] { 1, 5, 2, 3, 4 }, Allocator.Persistent);
            var heap = new NativeHeap<int>(nativeArray, Allocator.Persistent);

            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void CreateFromNativeArray_HasSameCount()
        {
            var nativeArray = new NativeArray<int>(new int[] { 1, 5, 2, 3, 4 }, Allocator.Persistent);
            var heap = new NativeHeap<int>(nativeArray, Allocator.Persistent);

            Assert.AreEqual(nativeArray.Length, heap.Count);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void Push12345_Contains12345()
        {
            var heap = new NativeHeap<int>(5, Allocator.Persistent);

            heap.PushNoResize(4);
            heap.PushNoResize(1);
            heap.PushNoResize(3);
            heap.PushNoResize(5);
            heap.PushNoResize(2);

            Assert.IsTrue(heap.Contains(4));
            Assert.IsTrue(heap.Contains(1));
            Assert.IsTrue(heap.Contains(3));
            Assert.IsTrue(heap.Contains(5));
            Assert.IsTrue(heap.Contains(2));
            heap.Dispose();
        }

        [Test]
        public void Push12345_DoesNotContain6()
        {
            var heap = new NativeHeap<int>(5, Allocator.Persistent);

            heap.PushNoResize(4);
            heap.PushNoResize(1);
            heap.PushNoResize(3);
            heap.PushNoResize(5);
            heap.PushNoResize(2);

            Assert.IsFalse(heap.Contains(6));
            heap.Dispose();
        }

        [Test]
        public void PopFromEmptyHeapThrows()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            Assert.Throws<InvalidOperationException>(() => heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void PeekFromEmptyHeapThrows()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            Assert.Throws<InvalidOperationException>(() => heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushPop_DoesNotChangeCount()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.PushPop(5);

            Assert.IsTrue(heap.Count == 1);
            heap.Dispose();
        }

        [Test]
        public void PushPopHigherValue_RemovesRoot()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(2);
            heap.Push(5);

            heap.PushPop(4);

            Assert.IsFalse(heap.Contains(2));
            heap.Dispose();
        }

        [Test]
        public void PushPopLowerValue_RemovesRoot()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(2);
            heap.Push(5);

            heap.PushPop(1);

            Assert.IsFalse(heap.Contains(1));
            heap.Dispose();
        }

        [Test]
        public void PushPopHigherValue_AddsHigherValue()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(2);
            heap.Push(5);

            heap.PushPop(4);

            Assert.IsTrue(heap.Contains(4));
            heap.Dispose();
        }

        [Test]
        public void PushPopLowerValueDoesNotChangeHeap()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(2);
            heap.Push(5);

            heap.PushPop(1);

            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void PushPopEmptyHeapDoesNotThrow()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            Assert.DoesNotThrow(() => heap.PushPop(1));

            heap.Dispose();
        }

        [Test]
        public void Replace_DoesNotChangeCount()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Replace(5);

            Assert.IsTrue(heap.Count == 1);
            heap.Dispose();
        }

        [Test]
        public void Replace_RemovesRoot()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Replace(5);

            Assert.IsFalse(heap.Contains(3));
            heap.Dispose();
        }

        [Test]
        public void Replace_AddsReplacement()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Replace(5);

            Assert.IsTrue(heap.Contains(5));
            heap.Dispose();
        }

        [Test]
        public void ReplaceWithHigherValue_IsSorted()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(3);
            heap.Push(1);
            heap.Push(4);
            heap.Push(5);
            heap.Push(2);

            heap.Replace(7);

            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());
            Assert.AreEqual(7, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void ReplaceWithLowerValue_IsSorted()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(10);
            heap.Push(8);
            heap.Push(12);
            heap.Push(13);
            heap.Push(9);
            heap.Replace(3);

            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(9, heap.Pop());
            Assert.AreEqual(10, heap.Pop());
            Assert.AreEqual(12, heap.Pop());
            Assert.AreEqual(13, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void ReplaceWithMedianValue_IsSorted()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(10);
            heap.Push(8);
            heap.Push(12);
            heap.Push(13);
            heap.Push(9);
            heap.Replace(11);

            Assert.AreEqual(9, heap.Pop());
            Assert.AreEqual(10, heap.Pop());
            Assert.AreEqual(11, heap.Pop());
            Assert.AreEqual(12, heap.Pop());
            Assert.AreEqual(13, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void ReplaceEmptyHeapThrows()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            Assert.Throws<IndexOutOfRangeException>(() => heap.Replace(1));

            heap.Dispose();
        }

        [Test]
        public void ToArrayReturnsArrayWithSameCount()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(10);
            heap.Push(8);
            heap.Push(12);
            heap.Push(13);
            heap.Push(9);

            int[] array = heap.ToArray();
            Assert.AreEqual(array.Length, heap.Count);

            heap.Dispose();
        }

        [Test]
        public void ToArrayReturnsArrayWithSameElements()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(10);
            heap.Push(8);
            heap.Push(12);
            heap.Push(13);
            heap.Push(9);

            int[] array = heap.ToArray();
            Assert.AreEqual(array.Length, heap.Count);

            heap.Dispose();
        }

        [Test]
        public void EmptyHeapToArrayReturnsEmptyArray()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            int[] array = heap.ToArray();
            Assert.AreEqual(0, array.Length);

            heap.Dispose();
        }

        [Test]
        public void ToNativeArrayReturnsNativeArrayWithSameCount()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            heap.Push(10);
            heap.Push(8);
            heap.Push(12);
            heap.Push(13);
            heap.Push(9);

            NativeArray<int> nativeArray = heap.ToNativeArray(Allocator.Persistent);
            Assert.AreEqual(nativeArray.Length, heap.Count);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void EmptyHeapToNativeArrayReturnsEmptyNativeArray()
        {
            var heap = new NativeHeap<int>(Allocator.Persistent);

            NativeArray<int> nativeArray = heap.ToNativeArray(Allocator.Persistent);
            Assert.AreEqual(0, nativeArray.Length);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void HeapWriteInJob()
        {
            var heap = new NativeHeap<int>(Allocator.TempJob);

            new HeapWriteJob
            {
                heap = heap
            }.Schedule().Complete();

            Assert.IsTrue(heap.Count != 0);
            Assert.AreEqual(5, heap.Peek());
            heap.Dispose();
        }

        public struct HeapWriteJob : IJob
        {
            public NativeHeap<int> heap;

            public void Execute()
            {
                heap.Push(5);
            }
        }
    }

    internal class NativeHeapTValueTPriorityTests
    {
        [Test]
        public void IsEmpty()
        {
            var heap = new NativeHeap<int, int>(0, Allocator.Persistent);
            Assert.IsTrue(heap.IsEmpty);

            heap.Push(0, 0);
            Assert.IsFalse(heap.IsEmpty);

            heap.Pop();
            Assert.IsTrue(heap.IsEmpty);

            heap.Push(0, 0);
            heap.Clear();
            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        [Test]
        public void Create_HasZeroLength()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);
            Assert.IsTrue(heap.Count == 0);
            heap.Dispose();
        }

        [Test]
        public void PushIncrementsCount()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(5, 5);
            Assert.IsTrue(heap.Count == 1);
            heap.Dispose();
        }

        [Test]
        public void PushOneAndThree_IsSorted()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(1, 1);
            heap.Push(3, 3);
            Assert.AreEqual(1, heap.Peek().value);

            heap.Dispose();
        }

        [Test]
        public void PushThreeAndOne_IsSorted()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(1, 1);
            Assert.AreEqual(1, heap.Peek().value);

            heap.Dispose();
        }

        [Test]
        public void PushFive_RootIsFive()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(5, 5);
            Assert.AreEqual(5, heap.Peek().value);

            heap.Dispose();
        }

        [Test]
        public void PushDuplicate_ContainsDuplicate()
        {
            var heap = new NativeHeap<int, int>(5, Allocator.Persistent);

            heap.PushNoResize(1, 1);
            heap.PushNoResize(1, 1);

            Assert.AreEqual(2, heap.Count);
            heap.Dispose();
        }


        [Test]
        public void PushDuplicate_RemainsSorted()
        {
            var heap = new NativeHeap<int, int>(5, Allocator.Persistent);

            heap.PushNoResize(1, 1);
            heap.PushNoResize(3, 3);
            heap.PushNoResize(2, 2);
            heap.PushNoResize(5, 5);
            heap.PushNoResize(1, 1);

            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void PushAndPop_RemainsSorted()
        {
            var heap = new NativeHeap<int, int>(5, Allocator.Persistent);

            heap.PushNoResize(5, 5);
            heap.PushNoResize(3, 3);
            heap.PushNoResize(1, 1);
            heap.Pop();

            heap.PushNoResize(4, 4);
            heap.PushNoResize(2, 2);

            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void PopRemovesRoot()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(1, 1);
            Assert.AreEqual(1, heap.Pop().value);
            heap.Dispose();
        }

        [Test]
        public void PopDecrementsCount()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(1, 1);
            Assert.AreEqual(1, heap.Pop().value);
            heap.Dispose();
        }

        [Test]
        public void Pop_RemainsSorted()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(6, 6);
            heap.Push(1, 1);
            heap.Push(4, 4);
            heap.Push(2, 2);
            heap.Push(5, 5);

            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);
            Assert.AreEqual(6, heap.Pop().value);
            heap.Dispose();
        }

        [Test]
        public void CreateFromArray_HasSameCount()
        {
            var array = new (int, int)[] { (1, 1), (5, 5), (2, 2), (3, 3), (4, 4) };
            var heap = new NativeHeap<int, int>(array, Allocator.Persistent);

            Assert.AreEqual(array.Length, heap.Count);

            heap.Dispose();
        }

        [Test]
        public void CreateFromArray_IsSorted()
        {
            var array = new (int, int)[] { (1, 1), (5, 5), (2, 2), (3, 3), (4, 4) };
            var heap = new NativeHeap<int, int>(array, Allocator.Persistent);

            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void CreateFromNativeArray_IsSorted()
        {
            var nativeArray = new NativeArray<(int, int)>(new (int, int)[] { (1, 1), (5, 5), (2, 2), (3, 3), (4, 4) }, Allocator.Persistent);
            var heap = new NativeHeap<int, int>(nativeArray, Allocator.Persistent);

            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void CreateFromNativeArray_HasSameCount()
        {
            var nativeArray = new NativeArray<(int, int)>(new (int, int)[] { (1, 1), (5, 5), (2, 2), (3, 3), (4, 4) }, Allocator.Persistent);
            var heap = new NativeHeap<int, int>(nativeArray, Allocator.Persistent);

            Assert.AreEqual(nativeArray.Length, heap.Count);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void Push12345_Contains12345()
        {
            var heap = new NativeHeap<int, int>(5, Allocator.Persistent);

            heap.PushNoResize(4, 4);
            heap.PushNoResize(1, 1);
            heap.PushNoResize(3, 3);
            heap.PushNoResize(5, 5);
            heap.PushNoResize(2, 2);

            Assert.IsTrue(heap.Contains(4));
            Assert.IsTrue(heap.Contains(1));
            Assert.IsTrue(heap.Contains(3));
            Assert.IsTrue(heap.Contains(5));
            Assert.IsTrue(heap.Contains(2));
            heap.Dispose();
        }

        [Test]
        public void Push12345_DoesNotContain6()
        {
            var heap = new NativeHeap<int, int>(5, Allocator.Persistent);

            heap.PushNoResize(4, 4);
            heap.PushNoResize(1, 1);
            heap.PushNoResize(3, 3);
            heap.PushNoResize(5, 5);
            heap.PushNoResize(2, 2);

            Assert.IsFalse(heap.Contains(6));
            heap.Dispose();
        }

        [Test]
        public void PopFromEmptyHeapThrows()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            Assert.Throws<InvalidOperationException>(() => heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void PeekFromEmptyHeapThrows()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            Assert.Throws<InvalidOperationException>(() => heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushPop_DoesNotChangeCount()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.PushPop(5, 5);

            Assert.IsTrue(heap.Count == 1);
            heap.Dispose();
        }

        [Test]
        public void PushPopHigherValue_RemovesRoot()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(2, 2);
            heap.Push(5, 5);

            heap.PushPop(4, 4);

            Assert.IsFalse(heap.Contains(2));
            heap.Dispose();
        }

        [Test]
        public void PushPopLowerValue_RemovesRoot()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(2, 2);
            heap.Push(5, 5);

            heap.PushPop(1, 1);

            Assert.IsFalse(heap.Contains(1));
            heap.Dispose();
        }

        [Test]
        public void PushPopHigherValue_AddsHigherValue()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(2, 2);
            heap.Push(5, 5);

            heap.PushPop(4, 4);

            Assert.IsTrue(heap.Contains(4));
            heap.Dispose();
        }

        [Test]
        public void PushPopLowerValueDoesNotChangeHeap()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(2, 2);
            heap.Push(5, 5);

            heap.PushPop(1, 1);

            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void PushPopEmptyHeapDoesNotThrow()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            Assert.DoesNotThrow(() => heap.PushPop(1, 1));

            heap.Dispose();
        }

        [Test]
        public void Replace_DoesNotChangeCount()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Replace(5);

            Assert.IsTrue(heap.Count == 1);
            heap.Dispose();
        }

        [Test]
        public void Replace_RemovesRoot()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Replace(5);

            Assert.IsFalse(heap.Contains(3));
            heap.Dispose();
        }

        [Test]
        public void Replace_AddsReplacement()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Replace(5);

            Assert.IsTrue(heap.Contains(5));
            heap.Dispose();
        }

        [Test]
        public void ReplaceWithHigherValue_IsSorted()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(3, 3);
            heap.Push(1, 1);
            heap.Push(4, 4);
            heap.Push(5, 5);
            heap.Push(2, 2);

            heap.Replace(7);

            Assert.AreEqual(7, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void ReplaceWithLowerValue_IsSorted()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(10, 10);
            heap.Push(8, 8);
            heap.Push(12, 12);
            heap.Push(13, 13);
            heap.Push(9, 9);
            heap.Replace(3);

            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(9, heap.Pop().value);
            Assert.AreEqual(10, heap.Pop().value);
            Assert.AreEqual(12, heap.Pop().value);
            Assert.AreEqual(13, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void ReplaceWithMedianValue_IsSorted()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(10, 10);
            heap.Push(8, 8);
            heap.Push(12, 12);
            heap.Push(13, 13);
            heap.Push(9, 9);
            heap.Replace(11);

            Assert.AreEqual(11, heap.Pop().value);
            Assert.AreEqual(9, heap.Pop().value);
            Assert.AreEqual(10, heap.Pop().value);
            Assert.AreEqual(12, heap.Pop().value);
            Assert.AreEqual(13, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void ReplaceEmptyHeapThrows()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            Assert.Throws<IndexOutOfRangeException>(() => heap.Replace(1));

            heap.Dispose();
        }

        [Test]
        public void Update()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);
            heap.Push(5, 5);
            heap.Push(2, 2);
            heap.Push(7, 7);
            heap.Push(6, 6);
            heap.Push(4, 4);
            heap.Push(3, 3);

            heap.UpdatePriority(5, 9);

            heap.Pop(); // 2
            heap.Pop(); // 3
            heap.Pop(); // 4
            heap.Pop(); // 6
            heap.Pop(); // 7
            Assert.AreEqual(5, heap.Pop().value); // 5
            heap.Dispose();
        }

        [Test]
        public void ToArrayReturnsArrayWithSameCount()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(10, 10);
            heap.Push(8, 8);
            heap.Push(12, 12);
            heap.Push(13, 13);
            heap.Push(9, 9);

            (int, int)[] array = heap.ToArray();
            Assert.AreEqual(array.Length, heap.Count);

            heap.Dispose();
        }

        [Test]
        public void ToArrayReturnsArrayWithSameElements()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(10, 10);
            heap.Push(8, 8);
            heap.Push(12, 12);
            heap.Push(13, 13);
            heap.Push(9, 9);

            (int, int)[] array = heap.ToArray();
            Assert.AreEqual(array.Length, heap.Count);

            heap.Dispose();
        }

        [Test]
        public void EmptyHeapToArrayReturnsEmptyArray()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            (int, int)[] array = heap.ToArray();
            Assert.AreEqual(0, array.Length);

            heap.Dispose();
        }

        [Test]
        public void ToNativeArrayReturnsNativeArrayWithSameCount()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            heap.Push(10, 10);
            heap.Push(8, 8);
            heap.Push(12, 12);
            heap.Push(13, 13);
            heap.Push(9, 9);

            NativeArray<(int, int)> nativeArray = heap.ToNativeArray(Allocator.Persistent);
            Assert.AreEqual(nativeArray.Length, heap.Count);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void EmptyHeapToNativeArrayReturnsEmptyNativeArray()
        {
            var heap = new NativeHeap<int, int>(Allocator.Persistent);

            NativeArray<(int, int)> nativeArray = heap.ToNativeArray(Allocator.Persistent);
            Assert.AreEqual(0, nativeArray.Length);

            nativeArray.Dispose();
            heap.Dispose();
        }

        [Test]
        public void HeapWriteInJob()
        {
            var heap = new NativeHeap<int, int>(Allocator.TempJob);

            new HeapWriteJob
            {
                heap = heap
            }.Schedule().Complete();

            Assert.IsTrue(heap.Count != 0);
            Assert.AreEqual(5, heap.Peek().value);
            heap.Dispose();
        }

        public struct HeapWriteJob : IJob
        {
            public NativeHeap<int, int> heap;

            public void Execute()
            {
                heap.Push(5, 5);
            }
        }
    }
}
