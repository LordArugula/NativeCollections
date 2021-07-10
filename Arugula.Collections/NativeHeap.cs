using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Arugula.Collections
{
    [NativeContainer]
    internal unsafe struct NativeHeapDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeList* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            UnsafeList.Destroy(m_Buffer);
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

    /// <summary>
    /// An unmanaged, minimum heap.
    /// </summary>
    /// <remarks>
    /// Does not support parallel writing.
    /// </remarks>
    /// <typeparam name="TValue">The type of the elements in the container.</typeparam>
    /// <typeparam name="TPriority">The type of the elements used to determine priority.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeHeapDebugView<,>))]
    public unsafe struct NativeHeap<TValue, TPriority>
        where TValue : struct, IEquatable<TValue>
        where TPriority : struct, IComparable<TPriority>
    {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeHeap<TValue, TPriority>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeHeap<TValue, TPriority>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList* m_Buffer;

        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Buffer->Length;
            }
        }

        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Buffer->Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                CheckCapacityInRange(value, m_Buffer->Length);
                m_Buffer->SetCapacity<HeapNode<TValue, TPriority>>(value);
            }
        }

        public bool IsCreated
        {
            get => m_Buffer != null;
        }

        public bool IsEmpty
        {
            get => !IsCreated || m_Buffer->IsEmpty;
        }

        private HeapNode<TValue, TPriority> this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_Buffer->Length);
                return UnsafeUtility.ReadArrayElement<HeapNode<TValue, TPriority>>(m_Buffer->Ptr, index);
            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_Buffer->Length);
                UnsafeUtility.WriteArrayElement(m_Buffer->Ptr, index, value);
            }
        }

        public NativeHeap(Allocator allocator) : this(1, allocator, 2)
        {
        }

        /// <summary>
        /// Creates a heap with an initial capacity. 
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="allocator"></param>
        public NativeHeap(int initialCapacity, Allocator allocator) : this(initialCapacity, allocator, 2)
        {
        }

        private NativeHeap(int initialCapacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            var totalSize = UnsafeUtility.SizeOf<HeapNode<TValue, TPriority>>() * (long)initialCapacity;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(allocator);
            CheckInitialCapacity(initialCapacity);
            if (!UnsafeUtility.IsUnmanaged<TValue>())
            {
                throw new System.NotSupportedException($"type {typeof(TValue)} must be an unmanaged type.");
            }

            if (!UnsafeUtility.IsUnmanaged<TPriority>())
            {
                throw new System.NotSupportedException($"type {typeof(TPriority)} must be an unmanaged type.");
            }

            CheckTotalSize(initialCapacity, totalSize);

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            m_Buffer = UnsafeList.Create(UnsafeUtility.SizeOf<HeapNode<TValue, TPriority>>(),
                                              UnsafeUtility.AlignOf<HeapNode<TValue, TPriority>>(),
                                              initialCapacity,
                                              allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Creates a heap from an array containing values and an array containing priorities.
        /// </summary>
        /// <param name="values">An array of values.</param>
        /// <param name="priorities">An array of priorities.</param>
        /// <param name="allocator"></param>
        public NativeHeap(TValue[] values, TPriority[] priorities, Allocator allocator) : this(values.Length, allocator)
        {
            if (values.Length != priorities.Length)
            {
                throw new System.ArgumentException($"{nameof(values)} and {nameof(priorities)} arrays must have the same length.");
            }

            for (int i = 0; i < values.Length; i++)
            {
                Push(values[i], priorities[i]);
            }
        }

        /// <summary>
        /// Creates a heap from an <see cref="NativeArray{TValue}"/> containing values and an <see cref="NativeArray{TPriority}"/> containing priorities.
        /// </summary>
        /// <param name="values">An array of values.</param>
        /// <param name="priorities">An array of priorities.</param>
        /// <param name="allocator"></param>
        public NativeHeap(NativeArray<TValue> values, NativeArray<TPriority> priorities, Allocator allocator) : this(values.Length, allocator)
        {
            if (values.Length != priorities.Length)
            {
                throw new System.ArgumentException($"{nameof(values)} and {nameof(priorities)} arrays must have the same length.");
            }

            for (int i = 0; i < values.Length; i++)
            {
                Push(values[i], priorities[i]);
            }
        }

        /// <summary>
        /// Creates a heap from an array containing values and their priorities.
        /// </summary>
        /// <param name="valuePriorityPairs">An array of value and priority pairs.</param>
        /// <param name="allocator"></param>
        public NativeHeap(HeapNode<TValue, TPriority>[] valuePriorityPairs, Allocator allocator) : this(valuePriorityPairs.Length, allocator)
        {
            for (int i = 0; i < valuePriorityPairs.Length; i++)
            {
                Push(valuePriorityPairs[i].value, valuePriorityPairs[i].priority);
            }
        }

        /// <summary>
        /// Creates a heap from a <see cref="NativeArray{HeapNode}"/> containing values and their priorities.
        /// </summary>
        /// <param name="valuePriorityPairs">A NativeArray of value and priority pairs.</param>
        /// <param name="allocator"></param>
        public NativeHeap(NativeArray<HeapNode<TValue, TPriority>> valuePriorityPairs, Allocator allocator) : this(valuePriorityPairs.Length, allocator)
        {
            for (int i = 0; i < valuePriorityPairs.Length; i++)
            {
                Push(valuePriorityPairs[i].value, valuePriorityPairs[i].priority);
            }
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the heap.
        /// </summary>
        /// <param name="value"></param>
        public void Push(TValue value, TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Buffer->Add(new HeapNode<TValue, TPriority>(value, priority));

            ShiftUp(Count - 1);
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the heap without resizing the heap.
        /// </summary>
        /// <param name="value"></param>
        public void PushNoResize(TValue value, TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_Buffer->AddNoResize(new HeapNode<TValue, TPriority>(value, priority));

            ShiftUp(Count - 1);
        }

        /// <summary>
        /// Removes the root of the heap and returns it.
        /// </summary>
        /// <returns></returns>
        public HeapNode<TValue, TPriority> Pop()
        {
            if (!TryPop(out TValue value, out TPriority priority))
            {
                ThrowEmpty();
            }
            return new HeapNode<TValue, TPriority>(value, priority);
        }

        /// <summary>
        /// Returns the root of the heap, without removing it.
        /// </summary>
        /// <returns></returns>
        public HeapNode<TValue, TPriority> Peek()
        {
            if (!TryPeek(out TValue value, out TPriority priority))
            {
                ThrowEmpty();
            }

            return new HeapNode<TValue, TPriority>(value, priority);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void ThrowEmpty()
        {
            throw new System.InvalidOperationException("Trying to read from an empty heap.");
        }

        /// <summary>
        /// Returns whether <paramref name="value"/> is in the heap.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Contains(TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            for (int i = 0; i < Count; i++)
            {
                if (this[i].value.Equals(value))
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
        public HeapNode<TValue, TPriority> PushPop(TValue value, TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            if (IsEmpty || this[0].priority.CompareTo(priority) >= 0)
            {
                return new HeapNode<TValue, TPriority>(value, priority);
            }

            var pair = this[0];
            this[0] = new HeapNode<TValue, TPriority>(value, priority);
            ShiftDown(0);
            return pair;

        }

        /// <summary>
        /// Removes the root element and returns it, then adds <paramref name="value"/>. 
        /// More efficient than calling <see cref="Pop"/> then <see cref="Push(T)"/>.
        /// </summary>
        /// <param name="value"></param>
        public TValue Replace(TValue value, TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            HeapNode<TValue, TPriority> node = this[0];
            this[0] = new HeapNode<TValue, TPriority>(value, priority);
            ShiftDown(0);

            return node.value;
        }

        /// <summary>
        /// Tries to remove the root of the heap and returns it.
        /// </summary>
        /// <param name="value">Contains the element that was removed 
        /// or a default object if the operation failed.</param>
        /// <returns>True if an element was removed from the heap.</returns>
        public bool TryPop(out TValue value, out TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            if (Count == 0)
            {
                value = default;
                priority = default;
                return false;
            }

            value = this[0].value;
            priority = this[0].priority;

            m_Buffer->RemoveAtSwapBack<HeapNode<TValue, TPriority>>(0);
            ShiftDown(0);

            return true;
        }

        /// <summary>
        /// Tries to return the root of the heap, without removing it.
        /// </summary>
        /// <param name="value">Contains the element at the root of the heap 
        /// or a default object if the operation failed.</param>
        /// <returns>True if an element was returned.</returns>
        public bool TryPeek(out TValue value, out TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (Count == 0)
            {
                value = default;
                priority = default;
                return false;
            }

            value = this[0].value;
            priority = this[0].priority;

            return true;
        }

        /// <summary>
        /// Updates the priority of the first element with <paramref name="value"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="priority"></param>
        public void UpdatePriority(TValue value, TPriority priority)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].value.Equals(value))
                {
                    var element = this[i];
                    element.priority = priority;
                    this[i] = element;
                    if (priority.CompareTo(this[i / 2].priority) < 1)
                    {
                        ShiftUp(i);
                    }
                    else
                    {
                        ShiftDown(i);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all values from the heap.
        /// </summary>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Buffer->Clear();
        }

        public HeapNode<TValue, TPriority>[] ToArray()
        {
            return AsArray().ToArray();
        }

        public NativeArray<HeapNode<TValue, TPriority>> ToArray(Allocator allocator)
        {
            return new NativeArray<HeapNode<TValue, TPriority>>(AsArray(), allocator);
        }

        private void ShiftUp(int i)
        {
            int p = (i - 1) / 2;
            while (i > 0 && this[i].priority.CompareTo(this[p].priority) < 0)
            {
                Swap(i, p);
                i = p;
                p = (i - 1) / 2;
            }
        }

        private void ShiftDown(int i)
        {
            do
            {
                int j = -1;
                int right = (i + 1) * 2;
                int left = i * 2 + 1;

                if (right < Count && this[right].priority.CompareTo(this[i].priority) < 0)
                {
                    j = this[left].priority.CompareTo(this[right].priority) < 0 ? left : right;
                }
                else
                {
                    j = left < Count && this[left].priority.CompareTo(this[i].priority) < 0 ? left : j;
                }

                if (j >= 0) Swap(i, j);
                i = j;
            } while (i >= 0);
        }

        private void Swap(int a, int b)
        {
            HeapNode<TValue, TPriority> x = this[a];
            this[a] = this[b];
            this[b] = x;
        }

        /// <summary>
        /// Creates a <see cref="NativeArray{T}"/> that references the same backing list data.
        /// </summary>
        /// <returns></returns>
        private NativeArray<HeapNode<TValue, TPriority>> AsArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var arraySafety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<HeapNode<TValue, TPriority>>(m_Buffer->Ptr, m_Buffer->Length, Allocator.None);

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
            UnsafeList.Destroy(m_Buffer);
            m_Buffer = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_Buffer == null)
            {
                throw new ObjectDisposedException("The NativeArray2D is already disposed.");
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif
            var jobHandle = new NativeHeapDisposeJob
            {
                Data = new NativeHeapDispose
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
    }

    internal unsafe sealed class NativeHeapDebugView<TValue, TPriority>
        where TValue : struct, IEquatable<TValue>
        where TPriority : struct, IComparable<TPriority>
    {
        private readonly NativeHeap<TValue, TPriority> m_Heap;

        public NativeHeapDebugView(NativeHeap<TValue, TPriority> heap)
        {
            m_Heap = heap;
        }

        public HeapNode<TValue, TPriority>[] Elements
        {
            get => m_Heap.ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HeapNode<TValue, TPriority>
        where TValue : struct, IEquatable<TValue>
        where TPriority : struct, IComparable<TPriority>
    {
        public TValue value;
        public TPriority priority;

        public HeapNode(TValue item1, TPriority item2)
        {
            value = item1;
            priority = item2;
        }
    }
}
