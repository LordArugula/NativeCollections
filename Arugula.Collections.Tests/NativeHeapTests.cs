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
            var heap = new NativeHeap<int>(0, Allocator.Temp);
            Assert.IsTrue(heap.IsEmpty);

            heap.Push(0);
            heap.Push(10);
            Assert.IsFalse(heap.IsEmpty);

            heap.Pop();
            Assert.IsFalse(heap.IsEmpty);

            heap.Clear();
            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        [Test]
        public void Push()
        {
            var heap = new NativeHeap<int>(Allocator.Temp);

            heap.Push(5);
            Assert.IsTrue(heap.Count == 1);

            heap.Push(10);
            Assert.IsTrue(heap.Count == 2);
            Assert.AreEqual(5, heap.Peek());

            heap.Push(3);
            Assert.IsTrue(heap.Count == 3);
            Assert.AreEqual(3, heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void Pop()
        {
            var heap = new NativeHeap<int>(Allocator.Temp);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                heap.Pop();
            });

            heap.Push(6);
            heap.Push(1);
            heap.Push(4);
            heap.Push(2);
            heap.Push(5);

            Assert.AreEqual(1, heap.Pop());
            Assert.IsFalse(heap.IsEmpty);
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());
            Assert.AreEqual(6, heap.Pop());

            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        [Test]
        public void CreateFromArray()
        {
            var array = new int[] { 1, 5, 2, 3, 4 };
            var heap = new NativeHeap<int>(array, Allocator.Temp);

            Assert.AreEqual(array.Length, heap.Count);
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            heap.Dispose();

            var nativeArray = new NativeArray<int>(new int[] { 1, 5, 2, 3, 4 }, Allocator.Temp);
            heap = new NativeHeap<int>(nativeArray, Allocator.Temp);

            Assert.AreEqual(array.Length, heap.Count);
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());

            heap.Dispose();
            nativeArray.Dispose();
        }

        [Test]
        public void Peek()
        {
            var heap = new NativeHeap<int>(Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => heap.Peek());

            heap.Push(10);
            Assert.AreEqual(10, heap.Peek());

            heap.Push(3);
            Assert.AreEqual(3, heap.Peek());

            heap.Push(5);
            Assert.AreEqual(3, heap.Peek());

            heap.Pop();
            Assert.AreEqual(5, heap.Peek());

            heap.Pop();
            Assert.AreEqual(10, heap.Peek());

            heap.Pop();
            Assert.Throws<InvalidOperationException>(() => heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushPop()
        {
            var heap = new NativeHeap<int>(Allocator.Temp);

            Assert.DoesNotThrow(() => heap.PushPop(3));
            Assert.IsTrue(heap.Count == 0);

            heap.Push(2);
            heap.Push(4);
            heap.Push(6);

            Assert.AreEqual(1, heap.PushPop(1));
            Assert.IsTrue(heap.Count == 3);
            Assert.AreEqual(2, heap.Peek());

            Assert.AreEqual(2, heap.PushPop(7));
            Assert.IsTrue(heap.Count == 3);
            Assert.AreEqual(4, heap.Peek());

            Assert.AreEqual(4, heap.PushPop(5));

            Assert.AreEqual(5, heap.Pop());
            Assert.AreEqual(6, heap.Pop());
            Assert.AreEqual(7, heap.Pop());
            heap.Dispose();
        }

        [Test]
        public void Replace()
        {
            var heap = new NativeHeap<int>(Allocator.Temp);

            Assert.Throws<IndexOutOfRangeException>(() => heap.Replace(1));

            heap.Push(3);
            heap.Push(1);
            heap.Push(4);
            heap.Push(5);
            heap.Push(2);

            Assert.AreEqual(1, heap.Replace(7));
            Assert.AreEqual(2, heap.Peek());
            Assert.IsTrue(heap.Count == 5);

            Assert.AreEqual(2, heap.Replace(1));
            Assert.AreEqual(1, heap.Peek());
            Assert.IsTrue(heap.Count == 5);

            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(3, heap.Pop());
            Assert.AreEqual(4, heap.Pop());
            Assert.AreEqual(5, heap.Pop());
            Assert.AreEqual(7, heap.Pop());

            heap.Dispose();
        }

        [Test]
        public void ToArray()
        {
            var input = new int[] { 10, 8, 12, 13, 9 };
            var heap = new NativeHeap<int>(input, Allocator.Temp);

            int[] array = heap.ToArray();
            Assert.AreEqual(array.Length, heap.Count);

            NativeArray<int> nativeArray = heap.ToArray(Allocator.Temp);
            Assert.AreEqual(nativeArray.Length, heap.Count);

            for (int i = 0; i < input.Length; i++)
            {
                Assert.That(() =>
                {
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (array[j] == input[i]) return true;
                    }
                    return false;
                });

                Assert.That(() =>
                {
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (nativeArray[j] == input[i]) return true;
                    }
                    return false;
                });
            }

            nativeArray.Dispose();
            heap.Dispose();

            heap = new NativeHeap<int>(Allocator.Temp);

            array = heap.ToArray();
            Assert.AreEqual(0, array.Length);

            nativeArray = heap.ToArray(Allocator.Temp);
            Assert.AreEqual(0, array.Length);

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

        public struct ManagedStructTest : IComparable<ManagedStructTest>, IEquatable<ManagedStructTest>
        {
            public string managedVar;

            public int CompareTo(ManagedStructTest other)
            {
                return managedVar.CompareTo(other.managedVar);
            }

            public bool Equals(ManagedStructTest other)
            {
                return managedVar.Equals(other.managedVar);
            }
        }

        [Test]
        public void ThrowsIfTypeIsNotUnmanaged()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var heap = new NativeHeap<ManagedStructTest>(10, Unity.Collections.Allocator.Temp);

                heap.Dispose();
            });
        }

        [Test]
        public void Dispose()
        {
            var heap = new NativeHeap<int>(Allocator.Temp);

            Assert.IsTrue(heap.IsCreated);
            heap.Dispose();
            
            Assert.IsFalse(heap.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose();
            });
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose(default);
            });

            heap = new NativeHeap<int>(Allocator.TempJob);
            Assert.IsTrue(heap.IsCreated);

            heap.Dispose(new JobHandle());
            Assert.IsFalse(heap.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose();
            });
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose(default);
            });
        }
    }

    internal class NativeHeapTValueTPriorityTests
    {
        [Test]
        public void IsEmpty()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);
            Assert.IsTrue(heap.IsEmpty);

            heap.Push(0, 0);
            heap.Push(10, 10);
            Assert.IsFalse(heap.IsEmpty);

            heap.Pop();
            Assert.IsFalse(heap.IsEmpty);

            heap.Clear();
            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        [Test]
        public void Push()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);

            heap.Push(5, 5);
            Assert.IsTrue(heap.Count == 1);

            heap.Push(10, 10);
            Assert.IsTrue(heap.Count == 2);
            Assert.AreEqual(5, heap.Peek().value);

            heap.Push(3, 3);
            Assert.IsTrue(heap.Count == 3);
            Assert.AreEqual(3, heap.Peek().value);

            heap.Dispose();
        }

        [Test]
        public void Pop()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                heap.Pop();
            });

            heap.Push(6, 6);
            heap.Push(1, 1);
            heap.Push(4, 4);
            heap.Push(2, 2);
            heap.Push(5, 5);

            Assert.AreEqual(1, heap.Pop().value);
            Assert.IsFalse(heap.IsEmpty);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);
            Assert.AreEqual(6, heap.Pop().value);

            Assert.IsTrue(heap.IsEmpty);

            heap.Dispose();
        }

        [Test]
        public void CreateFromArray()
        {
            var valuesArray = new int[] { 1, 5, 2, 3, 4 };
            var prioritiesArray = new int[] { 1, 5, 2, 3, 4 };
            var heap = new NativeHeap<int, int>(valuesArray, prioritiesArray, Allocator.Temp);

            Assert.AreEqual(valuesArray.Length, heap.Count);
            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();

            var valuesNativeArray = new NativeArray<int>(new int[] { 1, 5, 2, 3, 4 }, Allocator.Temp);
            var priorityNativeArray = new NativeArray<int>(new int[] { 1, 5, 2, 3, 4 }, Allocator.Temp);
            heap = new NativeHeap<int, int>(valuesNativeArray, priorityNativeArray, Allocator.Temp);

            Assert.AreEqual(valuesArray.Length, heap.Count);
            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(2, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);

            heap.Dispose();
            valuesNativeArray.Dispose();
            priorityNativeArray.Dispose();
        }

        [Test]
        public void Peek()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);

            Assert.Throws<InvalidOperationException>(() => heap.Peek());

            heap.Push(10, 10);
            Assert.AreEqual(10, heap.Peek().value);

            heap.Push(3, 3);
            Assert.AreEqual(3, heap.Peek().value);

            heap.Push(5, 5);
            Assert.AreEqual(3, heap.Peek().value);

            heap.Pop();
            Assert.AreEqual(5, heap.Peek().value);

            heap.Pop();
            Assert.AreEqual(10, heap.Peek().value);

            heap.Pop();
            Assert.Throws<InvalidOperationException>(() => heap.Peek());

            heap.Dispose();
        }

        [Test]
        public void PushPop()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);

            Assert.DoesNotThrow(() => heap.PushPop(3, 3));
            Assert.IsTrue(heap.Count == 0);

            heap.Push(2, 2);
            heap.Push(4, 4);
            heap.Push(6, 6);

            Assert.AreEqual(1, heap.PushPop(1, 1).value);
            Assert.IsTrue(heap.Count == 3);
            Assert.AreEqual(2, heap.Peek().value);

            Assert.AreEqual(2, heap.PushPop(7, 7).value);
            Assert.IsTrue(heap.Count == 3);
            Assert.AreEqual(4, heap.Peek().value);

            Assert.AreEqual(4, heap.PushPop(5, 5).value);

            Assert.AreEqual(5, heap.Pop().value);
            Assert.AreEqual(6, heap.Pop().value);
            Assert.AreEqual(7, heap.Pop().value);
            heap.Dispose();
        }

        [Test]
        public void Replace()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);

            Assert.Throws<IndexOutOfRangeException>(() => heap.Replace(1, 1));

            heap.Push(3, 3);
            heap.Push(1, 1);
            heap.Push(4, 4);
            heap.Push(5, 5);
            heap.Push(2, 2);

            Assert.AreEqual(1, heap.Replace(7, 7));
            Assert.AreEqual(2, heap.Peek().value);
            Assert.IsTrue(heap.Count == 5);

            Assert.AreEqual(2, heap.Replace(1, 1));
            Assert.AreEqual(1, heap.Peek().value);
            Assert.IsTrue(heap.Count == 5);

            Assert.AreEqual(1, heap.Pop().value);
            Assert.AreEqual(3, heap.Pop().value);
            Assert.AreEqual(4, heap.Pop().value);
            Assert.AreEqual(5, heap.Pop().value);
            Assert.AreEqual(7, heap.Pop().value);

            heap.Dispose();
        }

        [Test]
        public void ToArray()
        {
            var values = new int[] { 10, 8, 12, 13, 9 };
            var priorities = new int[] { 10, 8, 12, 13, 9 };
            var heap = new NativeHeap<int, int>(values, priorities, Allocator.Temp);

            HeapNode<int, int>[] array = heap.ToArray();
            Assert.AreEqual(array.Length, heap.Count);

            NativeArray<HeapNode<int, int>> nativeArray = heap.ToArray(Allocator.Temp);
            Assert.AreEqual(nativeArray.Length, heap.Count);

            for (int i = 0; i < values.Length; i++)
            {
                Assert.That(() =>
                {
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (array[j].value == values[i]) return true;
                    }
                    return false;
                });

                Assert.That(() =>
                {
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (nativeArray[j].value == values[i]) return true;
                    }
                    return false;
                });
            }

            nativeArray.Dispose();
            heap.Dispose();

            heap = new NativeHeap<int, int>(Allocator.Temp);

            array = heap.ToArray();
            Assert.AreEqual(0, array.Length);

            nativeArray = heap.ToArray(Allocator.Temp);
            Assert.AreEqual(0, array.Length);

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

        public struct ManagedStructTest : IComparable<ManagedStructTest>, IEquatable<ManagedStructTest>
        {
            public string managedVar;

            public int CompareTo(ManagedStructTest other)
            {
                return managedVar.CompareTo(other.managedVar);
            }

            public bool Equals(ManagedStructTest other)
            {
                return managedVar.Equals(other.managedVar);
            }
        }

        [Test]
        public void ThrowsIfTypeIsNotUnmanaged()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                var heap = new NativeHeap<ManagedStructTest>(10, Unity.Collections.Allocator.Temp);

                heap.Dispose();
            });
        }

        [Test]
        public void Dispose()
        {
            var heap = new NativeHeap<int, int>(Allocator.Temp);

            Assert.IsTrue(heap.IsCreated);
            heap.Dispose();

            Assert.IsFalse(heap.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose();
            });
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose(default);
            });

            heap = new NativeHeap<int, int>(Allocator.TempJob);
            Assert.IsTrue(heap.IsCreated);

            heap.Dispose(default);
            Assert.IsFalse(heap.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose();
            });
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                heap.Dispose(default);
            });
        }
    }
}
