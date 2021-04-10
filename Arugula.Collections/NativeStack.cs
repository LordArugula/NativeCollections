using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Arugula.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeStackData
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList* m_StackData;

        internal int m_Version;

        internal Allocator m_AllocatorLabel;

        public unsafe static void Allocate<T>(int initialCapacity, Allocator allocator, out NativeStackData* data)
            where T : struct
        {
            data = (NativeStackData*)UnsafeUtility.Malloc(sizeof(NativeStackData),
                                                          UnsafeUtility.AlignOf<NativeStackData>(),
                                                          allocator);

            data->m_StackData = UnsafeList.Create(UnsafeUtility.SizeOf<T>(),
                                                  UnsafeUtility.AlignOf<T>(),
                                                  initialCapacity,
                                                  allocator);

            //data->m_NextIndices = UnsafeList.Create(sizeof(int),
            //                                        UnsafeUtility.AlignOf<int>(),
            //                                        initialCapacity,
            //                                        allocator);

            data->m_Version = 1;
            //data->m_HeadIndex = -1;
            data->m_AllocatorLabel = allocator;
        }

        public unsafe static void Deallocate(NativeStackData* data)
        {
            UnsafeList.Destroy(data->m_StackData);

            UnsafeUtility.Free(data, data->m_AllocatorLabel);
        }
    }

    /// <summary>
    /// An unmanaged stack.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeStackDebugView<>))]
    public unsafe struct NativeStack<T> : INativeDisposable, IEnumerable<T>
where T : struct
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeStack<T>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeStack<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction]
        internal NativeStackData* m_Buffer;

        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Buffer->m_StackData->Length;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return m_Buffer == null || m_Buffer->m_StackData == null || m_Buffer->m_StackData->IsEmpty;
            }
        }

        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Buffer->m_StackData->Capacity;
            }
        }

        public bool IsCreated
        {
            get => m_Buffer != null && m_Buffer->m_StackData != null && m_Buffer->m_StackData->IsCreated;
        }

        public NativeStack(int initialCapacity, Allocator allocator) : this(initialCapacity, allocator, 2)
        {

        }

        public NativeStack(Allocator allocator) : this(16, allocator, 2)
        {

        }

        private NativeStack(int initialCapacity, Allocator allocator, int disposeSentinelStackDepth)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(allocator);
            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new System.NotSupportedException($"type {typeof(T)} must be an unmanaged type.");
            }

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);

            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            NativeStackData.Allocate<T>(initialCapacity, allocator, out m_Buffer);
        }

        public T Peek()
        {
            if (!TryPeek(out T element))
            {
                ThrowEmpty();
            }
            return element;
        }

        public T Pop()
        {
            if (!TryPop(out T element))
            {
                ThrowEmpty();
            }
            return element;
        }

        public void Push(T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            m_Buffer->m_StackData->Add(element);
            //m_Buffer->m_NextIndices->Add(0);
            //m_Buffer->m_HeadIndex++;
            m_Buffer->m_Version++;
        }

        public void PushNoResize(T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            m_Buffer->m_StackData->AddNoResize(element);
            //m_Buffer->m_NextIndices->AddNoResize(0);
            //m_Buffer->m_HeadIndex++;
            m_Buffer->m_Version++;
        }

        public bool TryPeek(out T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            //int index = m_Buffer->m_HeadIndex;
            if (Count > 0)
            {
                element = UnsafeUtility.ReadArrayElement<T>(m_Buffer->m_StackData->Ptr, Count - 1);
                return true;
            }
            element = default;
            return false;
        }

        public bool TryPop(out T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            //int index = m_Buffer->m_HeadIndex;
            if (Count > 0)
            {
                element = UnsafeUtility.ReadArrayElement<T>(m_Buffer->m_StackData->Ptr, Count - 1);
                m_Buffer->m_StackData->RemoveAt<T>(Count - 1);
                //m_Buffer->m_NextIndices->RemoveAt<T>(index);
                //m_Buffer->m_HeadIndex--;
                m_Buffer->m_Version++;
                return true;
            }
            element = default;
            return false;
        }

        public bool Contains(T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            for (int i = 0; i < Count; i++)
            {
                if (UnsafeUtility.ReadArrayElement<T>(m_Buffer->m_StackData->Ptr, i).Equals(element))
                    return true;
            }
            return false;
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Buffer->m_StackData->Clear();
            //m_Buffer->m_NextIndices->Clear();
            //m_Buffer->m_HeadIndex = -1;
            m_Buffer->m_Version++;
        }

        public T[] ToArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var dstArray = new T[Count];

            var handle = GCHandle.Alloc(dstArray, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(addr.ToPointer(), m_Buffer->m_StackData->Ptr, Count * UnsafeUtility.SizeOf<T>());

            return dstArray;
        }

        public NativeArray<T> ToArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var dstArray = new NativeArray<T>(Count, allocator);

            UnsafeUtility.MemCpy(dstArray.GetUnsafePtr(), m_Buffer->m_StackData->Ptr, Count * UnsafeUtility.SizeOf<T>());

            return dstArray;
        }

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            NativeStackData.Deallocate(m_Buffer);
            m_Buffer = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = new NativeStackDisposeJob
            {
                Data = new NativeStackDispose
                {
                    m_Buffer = m_Buffer,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety
#endif
                }
            }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
            m_Buffer = null;

            return jobHandle;
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        /// <returns>Parallel writer instance.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            writer.m_Safety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref writer.m_Safety);
#endif
            writer.m_Buffer = m_Buffer;
            writer.m_ThreadIndex = 0;

            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal NativeStackData* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            public void PushNoResize(T value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                var idx = Interlocked.Increment(ref m_Buffer->m_StackData->Length) - 1;
                CheckSufficientCapacity(m_Buffer->m_StackData->Capacity, idx + 1);

                UnsafeUtility.WriteArrayElement(m_Buffer->m_StackData->Ptr, idx, value);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocator(Allocator allocator)
        {
            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckInitialCapacity(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be >= 0");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckTotalSize(int initialCapacity, long totalSize)
        {
            // Make sure we cannot allocate more than int.MaxValue (2,147,483,647 bytes)
            // because the underlying UnsafeUtility.Malloc is expecting a int.
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), $"Capacity * sizeof({typeof(T)}) cannot exceed {int.MaxValue} bytes");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
                throw new Exception($"Length {length} exceeds capacity Capacity {capacity}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCapacityInRange(int value, int length)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");

            if ((uint)value < (uint)length)
                throw new ArgumentOutOfRangeException($"Value {value} is out of range in NativeStack of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ThrowEmpty()
        {
            throw new System.InvalidOperationException("Trying to read from an empty stack.");
        }
    }

    internal unsafe sealed class NativeStackDebugView<T> where T : struct
    {
        private readonly NativeStack<T> m_Stack;

        public NativeStackDebugView(NativeStack<T> stack)
        {
            m_Stack = stack;
        }

        public T[] Elements
        {
            get => m_Stack.ToArray();
        }
    }

    [NativeContainer]
    internal unsafe struct NativeStackDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public NativeStackData* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif
        public void Dispose()
        {
            NativeStackData.Deallocate(m_Buffer);
        }
    }

    [BurstCompile]
    internal unsafe struct NativeStackDisposeJob : IJob
    {
        internal NativeStackDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}
