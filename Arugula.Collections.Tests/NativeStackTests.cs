using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Arugula.Collections.Tests
{
    internal class NativeStackTests
    {
        [Test]
        public void IsEmpty()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            Assert.AreEqual(0, stack.Count);
            Assert.IsTrue(stack.IsEmpty);

            stack.Push(0);
            stack.Pop();

            Assert.AreEqual(0, stack.Count);
            Assert.IsTrue(stack.IsEmpty);

            stack.Push(1);
            stack.Push(2);
            stack.Clear();

            Assert.AreEqual(0, stack.Count);
            Assert.IsTrue(stack.IsEmpty);

            stack.Dispose();
        }

        [Test]
        public void Dispose()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            Assert.IsTrue(stack.IsCreated);

            stack.Dispose();
            Assert.IsFalse(stack.IsCreated);
            
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                stack.Dispose();
            });

            stack = new NativeStack<int>(Allocator.TempJob);
            Assert.IsTrue(stack.IsCreated);

            stack.Dispose(default).Complete();
            Assert.IsFalse(stack.IsCreated);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                stack.Dispose();
            });

        }

        [Test]
        public void Push()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            stack.Push(5);
            Assert.AreEqual(5, stack.Peek());
            Assert.AreEqual(1, stack.Count);

            stack.Push(3);
            Assert.AreEqual(3, stack.Peek());
            Assert.AreEqual(2, stack.Count);
            
            stack.Push(4);
            Assert.AreEqual(4, stack.Peek());
            Assert.AreEqual(3, stack.Count);

            stack.Dispose();
        }

        [Test]
        public void Pop()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                stack.Pop();
            });

            stack.Push(5);
            stack.Push(3);
            stack.Push(4);

            Assert.AreEqual(4, stack.Pop());
            Assert.AreEqual(2, stack.Count);

            Assert.AreEqual(3, stack.Pop());
            Assert.AreEqual(1, stack.Count);

            Assert.AreEqual(5, stack.Pop());
            Assert.AreEqual(0, stack.Count);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                stack.Pop();
            });

            stack.Dispose();
        }

        [Test]
        public void Peek()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            Assert.Throws<System.InvalidOperationException>(() =>
            {
                stack.Peek();
            });

            stack.Push(5);
            Assert.AreEqual(5, stack.Peek());
            stack.Push(3);
            Assert.AreEqual(3, stack.Peek());
            stack.Push(4);
            Assert.AreEqual(4, stack.Peek());

            stack.Dispose();
        }

        [Test]
        public void Contains()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            for (int i = 0; i < 10; i++)
            {
                stack.Push(i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(stack.Contains(i));
            }

            Assert.IsFalse(stack.Contains(15));
            Assert.IsFalse(stack.Contains(-1));
            Assert.IsFalse(stack.Contains(41));

            stack.Dispose();
        }

        [Test]
        public void ToArray()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            for (int i = 0; i < 10; i++)
            {
                stack.Push(i);
            }

            var array = stack.ToArray();
            Assert.AreEqual(stack.Count, array.Length);

            for (int i = 10 - 1; i >= 0; i--)
            {
                Assert.AreEqual(array[i], stack.Pop());
            }


            stack.Dispose();
        }

        [Test]
        public void ToNativeArray()
        {
            var stack = new NativeStack<int>(Allocator.Temp);

            for (int i = 0; i < 10; i++)
            {
                stack.Push(i);
            }

            var array = stack.ToArray(Allocator.Temp);
            Assert.AreEqual(stack.Count, array.Length);

            for (int i = 10 - 1; i >= 0; i--)
            {
                Assert.AreEqual(array[i], stack.Pop());
            }

            stack.Dispose();
            array.Dispose();
        }

        [Test]
        public void WriteJob()
        {
            var stack = new NativeStack<int>(Allocator.TempJob);

            new WriteToStackJob
            {
                stack = stack
            }.Schedule().Complete();

            Assert.AreEqual(0, stack.Pop());

            stack.Dispose();
        }

        [Test]
        public void WriteNoSafetyRestrictionsJob()
        {
            var stack = new NativeStack<int>(Allocator.TempJob);

            const int length = 10;
            new WriteToStackNoSafetyRestrictionsJob
            {
                stack = stack
            }.Schedule(length, default).Complete();

            for (int i = length - 1; i >= 0; i--)
            {
                Assert.AreEqual(i, stack.Pop());
            }

            stack.Dispose();
        }

        [Test]
        public void WriteParallelWriterJob()
        {
            var stack = new NativeStack<int>(Allocator.TempJob);

            const int length = 10;
            new WriteToStackParallelWriterJob
            {
                stack = stack.AsParallelWriter()
            }.Schedule(length, default).Complete();

            for (int i = length - 1; i >= 0; i--)
            {
                Assert.AreEqual(i, stack.Pop());
            }

            stack.Dispose();
        }

        [Test]
        public void DisposeAfterJobCompletion()
        {
            var stack = new NativeStack<int>(Allocator.TempJob);

            stack.Push(1);
            stack.Dispose(default);

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                stack.Dispose();
            });
        }

        [BurstCompile]
        public struct WriteToStackJob : IJob
        {
            public NativeStack<int> stack;

            public void Execute()
            {
                stack.Push(0);
            }
        }

        [BurstCompile]
        public struct WriteToStackParallelWriterJob : IJobFor
        {
            public NativeStack<int>.ParallelWriter stack;

            public void Execute(int index)
            {
                stack.PushNoResize(index);
            }
        }


        [BurstCompile]
        public struct WriteToStackNoSafetyRestrictionsJob : IJobFor
        {
            [NativeDisableContainerSafetyRestriction] public NativeStack<int> stack;

            public void Execute(int index)
            {
                stack.Push(index);
            }
        }

        [BurstCompile]
        public struct DisposeAfterJobCompletionJob : IJob
        {
            [DeallocateOnJobCompletion]
            public NativeStack<int> stack;

            public void Execute()
            {
                stack.Peek();
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
                var stack = new NativeStack<ManagedStructTest>(10, Unity.Collections.Allocator.Temp);

                stack.Dispose();
            });
        }

        [Test]
        public void NativeStackDisposesInJob()
        {
            Assert.DoesNotThrow(() =>
            {
                var stack = new NativeStack<int>(10, Unity.Collections.Allocator.TempJob);
                Assert.IsTrue(stack.IsCreated);

                stack.Dispose(default).Complete();

                Assert.IsFalse(stack.IsCreated);
            });
        }
    }
}
