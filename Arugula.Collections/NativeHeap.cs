using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Arugula.Collections
{
    /// <summary>
    /// An unmanaged, minimum heap.
    /// </summary>
    /// <remarks>
    /// Does not support parallel writing.
    /// </remarks>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeHeapDebugView<>))]
    public unsafe struct NativeHeap<T> : INativeDisposable
        where T : struct, IComparable<T>, IEquatable<T>
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
                m_Buffer->SetCapacity<T>(value);
            }
        }

        public bool IsEmpty
        {
            get => m_Buffer == null || m_Buffer->IsEmpty;
        }

        private T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_Buffer->Length);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer->Ptr, index);
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
            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new System.NotSupportedException($"type {typeof(T)} must be an unmanaged type.");
            }

            CheckTotalSize(initialCapacity, totalSize);

            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
            m_Buffer = UnsafeList.Create(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), initialCapacity, allocator);

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
        public void Push(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Buffer->Add(value);

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
            m_Buffer->AddNoResize(value);

            ShiftUp(Count - 1);
        }

        /// <summary>
        /// Removes the root of the heap and returns it.
        /// </summary>
        /// <returns></returns>
        public T Pop()
        {
            if (!TryPop(out T element))
            {
                ThrowEmpty();
            }
            return element;
        }

        /// <summary>
        /// Returns the root of the heap, without removing it.
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            if (!TryPeek(out T element))
            {
                ThrowEmpty();
            }

            return element;
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
        /// Tries to remove the root of the heap and returns it.
        /// </summary>
        /// <param name="element">Contains the element that was removed 
        /// or a default object if the operation failed.</param>
        /// <returns>True if an element was removed from the heap.</returns>
        public bool TryPop(out T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            if (Count == 0)
            {
                element = default;
                return false;
            }

            element = this[0];
            m_Buffer->RemoveAtSwapBack<T>(0);
            ShiftDown(0);

            return true;
        }

        /// <summary>
        /// Tries to return the root of the heap, without removing it.
        /// </summary>
        /// <param name="element">Contains the element at the root of the heap 
        /// or a default object if the operation failed.</param>
        /// <returns>True if an element was returned.</returns>
        public bool TryPeek(out T element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (Count == 0)
            {
                element = default;
                return false;
            }

            element = this[0];
            return true;
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

        private void ShiftDown(int i)
        {
            do
            {
                int j = -1;
                int right = (i + 1) * 2;
                int left = i * 2 + 1;

                if (right < Count && this[right].CompareTo(this[i]) < 0)
                {
                    j = this[left].CompareTo(this[right]) < 0 ? left : right;
                }
                else
                {
                    j = left < Count && this[left].CompareTo(this[i]) < 0 ? left : j;
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
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_Buffer->Ptr, m_Buffer->Length, Allocator.None);

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

    internal unsafe sealed class NativeHeapDebugView<T>
        where T : struct, IComparable<T>, IEquatable<T>
    {
        private readonly NativeHeap<T> m_Heap;

        public NativeHeapDebugView(NativeHeap<T> heap)
        {
            m_Heap = heap;
        }

        public T[] Elements
        {
            get => m_Heap.ToArray();
        }
    }

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
                m_Buffer->SetCapacity<(TValue value, TPriority priority)>(value);
            }
        }

        public bool IsEmpty
        {
            get => m_Buffer == null || m_Buffer->IsEmpty;
        }

        private (TValue value, TPriority priority) this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                CheckIndexInRange(index, m_Buffer->Length);
                return UnsafeUtility.ReadArrayElement<(TValue value, TPriority priority)>(m_Buffer->Ptr, index);
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
        /// Creates values heap with an initial capacity. 
        /// </summary>
        /// <param name="initialCapacity"></param>
        /// <param name="allocator"></param>
        public NativeHeap(int initialCapacity, Allocator allocator) : this(initialCapacity, allocator, 2)
        {
        }

        private NativeHeap(int initialCapacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            var totalSize = UnsafeUtility.SizeOf<(TValue value, TPriority priority)>() * (long)initialCapacity;

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
            m_Buffer = UnsafeList.Create(UnsafeUtility.SizeOf<(TValue value, TPriority priority)>(),
                                              UnsafeUtility.AlignOf<(TValue value, TPriority priority)>(),
                                              initialCapacity,
                                              allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        /// <summary>
        /// Creates values heap from an array.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="allocator"></param>
        public NativeHeap((TValue value, TPriority priority)[] values, Allocator allocator) : this(values.Length, allocator)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Push(values[i].value, values[i].priority);
            }
        }

        /// <summary>
        /// Creates values heap from values <see cref="NativeArray{T}"/>.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="allocator"></param>
        public NativeHeap(NativeArray<(TValue value, TPriority priority)> values, Allocator allocator) : this(values.Length, allocator)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Push(values[i].value, values[i].priority);
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
            m_Buffer->Add((value, priority));

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
            m_Buffer->AddNoResize((value, priority));

            ShiftUp(Count - 1);
        }

        /// <summary>
        /// Removes the root of the heap and returns it.
        /// </summary>
        /// <returns></returns>
        public (TValue value, TPriority priority) Pop()
        {
            if (!TryPop(out TValue value, out TPriority priority))
            {
                ThrowEmpty();
            }
            return (value, priority);
        }

        /// <summary>
        /// Returns the root of the heap, without removing it.
        /// </summary>
        /// <returns></returns>
        public (TValue value, TPriority priority) Peek()
        {
            if (!TryPeek(out TValue element, out TPriority priority))
            {
                ThrowEmpty();
            }

            return (element, priority);
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
        public (TValue value, TPriority priority) PushPop(TValue value, TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            if (!IsEmpty && this[0].priority.CompareTo(priority) < 0)
            {
                this[0] = (value, priority);
                ShiftDown(0);
                return this[0];
            }
            return (value, priority);
        }

        /// <summary>
        /// Removes the root element and returns it, then adds <paramref name="value"/>. 
        /// More efficient than calling <see cref="Pop"/> then <see cref="Push(T)"/>.
        /// </summary>
        /// <param name="value"></param>
        public TValue Replace(TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            (TValue _value, TPriority priority) = this[0];
            this[0] = (value, priority);
            ShiftDown(0);

            return _value;
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

            (value, priority) = this[0];
            m_Buffer->RemoveAtSwapBack<(TValue value, TPriority priority)>(0);
            ShiftDown(0);

            return true;
        }

        /// <summary>
        /// Tries to return the root of the heap, without removing it.
        /// </summary>
        /// <param name="element">Contains the element at the root of the heap 
        /// or a default object if the operation failed.</param>
        /// <returns>True if an element was returned.</returns>
        public bool TryPeek(out TValue element, out TPriority priority)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (Count == 0)
            {
                element = default;
                priority = default;
                return false;
            }

            (element, priority) = this[0];
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

        public (TValue value, TPriority priority)[] ToArray()
        {
            return AsArray().ToArray();
        }

        public NativeArray<(TValue value, TPriority priority)> ToNativeArray(Allocator allocator)
        {
            return new NativeArray<(TValue value, TPriority priority)>(AsArray(), allocator);
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
            (TValue value, TPriority priority) x = this[a];
            this[a] = this[b];
            this[b] = x;
        }

        /// <summary>
        /// Creates a <see cref="NativeArray{T}"/> that references the same backing list data.
        /// </summary>
        /// <returns></returns>
        private NativeArray<(TValue value, TPriority priority)> AsArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var arraySafety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<(TValue value, TPriority priority)>(m_Buffer->Ptr, m_Buffer->Length, Allocator.None);

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
}
