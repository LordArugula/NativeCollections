using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Arugula.Collections
{
    /// <summary>
    /// An unmanaged, minimum heap.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Count}")]
    public unsafe struct NativeHeap<T> : IDisposable, INativeDisposable
        where T : struct, IComparable<T>
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeHeap<T>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeHeap<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList* m_HeapData;

        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_HeapData->Length;
            }
        }

        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_HeapData->Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                CheckCapacityInRange(value, m_HeapData->Length);
                m_HeapData->SetCapacity<T>(value);
            }
        }

        public bool IsEmpty
        {
            get => m_HeapData == null || m_HeapData->IsEmpty;
        }

        private T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_HeapData->Length);
                return UnsafeUtility.ReadArrayElement<T>(m_HeapData->Ptr, index);
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_HeapData->Length);
                UnsafeUtility.WriteArrayElement(m_HeapData->Ptr, index, value);
            }
        }

        public NativeHeap(Allocator allocator) : this(1, allocator, 2)
        {
        }

        /// <summary>
        /// Creates values heap with an initial capacity. 
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="allocator"></param>
        public NativeHeap(int initialCapacity, Allocator allocator) : this(initialCapacity, allocator, 2)
        {
        }

        private NativeHeap(int initialCapacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            var totalSize = UnsafeUtility.SizeOf<T>() * (long)initialCapacity;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(allocator);
            CheckInitialCapacity(initialCapacity);
            CollectionHelper.CheckIsUnmanaged<T>();
            CheckTotalSize(initialCapacity, totalSize);

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            m_HeapData = UnsafeList.Create(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), initialCapacity, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Creates values heap from an array.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="allocator"></param>
        public NativeHeap(T[] values, Allocator allocator) : this(values.Length, allocator)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Push(values[i]);
            }
        }

        /// <summary>
        /// Creates values heap from values <see cref="NativeArray{T}"/>.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="allocator"></param>
        public NativeHeap(NativeArray<T> values, Allocator allocator) : this(values.Length, allocator)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Push(values[i]);
            }
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the heap.
        /// </summary>
        /// <param name="value"></param>
        public void Push(in T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_HeapData->Add(value);

            ShiftUp(Count - 1);
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the heap without resizing the heap.
        /// </summary>
        /// <param name="value"></param>
        public void PushNoResize(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_HeapData->AddNoResize(value);

            ShiftUp(Count - 1);
        }

        /// <summary>
        /// Removes the head of the heap and returns it.
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            T value = this[0];
            m_HeapData->RemoveAtSwapBack<T>(0);

            ShiftDown(0);

            return value;
        }

        /// <summary>
        /// Returns the root of the heap, without removing it.
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return this[0];
        }

        /// <summary>
        /// Returns whether <paramref name="value"/> is in the heap.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Equals(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the heap, 
        /// then removes and returns the root of the resulting heap.
        /// More effecient than calling <see cref="Push(T)"/> then <see cref="Pop"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public T PushPop(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            //T root = value;
            //if (!IsEmpty && m_values[0].CompareTo(value) < 0)
            //{
            //    root = m_values[0];
            //    m_values[0] = value;
            //    ShiftDown(0);
            //}

            if (!IsEmpty && this[0].CompareTo(value) < 0)
            {
                this[0] = value;
                ShiftDown(0);
                return this[0];
            }
            return value;
        }

        /// <summary>
        /// Removes the root element and returns it, then adds <paramref name="value"/>. 
        /// More efficient than calling <see cref="Pop"/> then <see cref="Push(T)"/>.
        /// </summary>
        /// <param name="value"></param>
        public T Replace(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            T root = this[0];
            this[0] = value;
            ShiftDown(0);

            return root;
        }

        /// <summary>
        /// Removes all values from the heap.
        /// </summary>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_HeapData->Clear();
        }

        public T[] ToArray()
        {
            return AsArray().ToArray();
        }

        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            return new NativeArray<T>(AsArray(), allocator);
        }

        [BurstCompile]
        private void ShiftUp(int i)
        {
            int p = (i - 1) / 2;
            while (i > 0 && this[i].CompareTo(this[p]) < 0)
            {
                Swap(i, p);
                i = p;
                p = (i - 1) / 2;
            }
        }

        [BurstCompile]
        private void ShiftDown(int i)
        {
            do
            {
                int j = -1;
                int right = (i + 1) * 2;
                int left = i * 2 + 1;

                if (right < Count && this[right].CompareTo(this[i]) < 0)
                {
                    j = math.select(right,
                                    left,
                                    this[left].CompareTo(this[right]) < 0);
                }
                else
                {
                    j = math.select(j,
                                    left,
                                    left < Count && this[left].CompareTo(this[i]) < 0);
                }

                if (j >= 0) Swap(i, j);
                i = j;
            } while (i >= 0);
        }

        private void Swap(int a, int b)
        {
            T x = this[a];
            this[a] = this[b];
            this[b] = x;
        }

        /// <summary>
        /// Creates a <see cref="NativeArray{T}"/> that references the same backing list data.
        /// </summary>
        /// <returns></returns>
        private NativeArray<T> AsArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var arraySafety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_HeapData->Ptr, m_HeapData->Length, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
            return array;
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
            // TODO: change UnsafeUtility.Malloc to accept a UIntPtr length instead to match C++ API
            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), $"Capacity * sizeof(T) cannot exceed {int.MaxValue} bytes");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckIndexInRange(int value, int length)
        {
            if (value < 0)
                throw new IndexOutOfRangeException($"Value {value} must be positive.");

            if ((uint)value >= (uint)length)
                throw new IndexOutOfRangeException($"Value {value} is out of range in NativeHeap of '{length}' Length.");
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCapacityInRange(int value, int length)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException($"Value {value} must be positive.");

            if ((uint)value < (uint)length)
                throw new ArgumentOutOfRangeException($"Value {value} is out of range in NativeHeap of '{length}' Length.");
        }

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeList.Destroy(m_HeapData);
            m_HeapData = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);

            var jobHandle = new NativeHeapDisposeJob { Data = new NativeHeapDispose { m_HeapData = m_HeapData, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new NativeHeapDisposeJob { Data = new NativeHeapDispose { m_HeapData = m_HeapData } }.Schedule(inputDeps);
#endif
            m_HeapData = null;

            return jobHandle;
        }
    }

    [NativeContainer]
    internal unsafe struct NativeHeapDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList* m_HeapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            UnsafeList.Destroy(m_HeapData);
        }
    }

    [BurstCompile]
    internal unsafe struct NativeHeapDisposeJob : IJob
    {
        internal NativeHeapDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    } 
}
