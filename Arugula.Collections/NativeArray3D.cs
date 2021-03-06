using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Arugula.Collections
{
    /// <summary>
    /// An unmanaged 3D array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length0 = {Length0}, Length1 = {Length1}, Length2 = {Length2}")]
    [DebuggerTypeProxy(typeof(NativeArray3DDebugView<>))]
    public unsafe struct NativeArray3D<T> : INativeDisposable, IEnumerable<T>, IEquatable<NativeArray3D<T>>
        where T : struct
    {
        public struct Enumerator : IEnumerator<T>
        {
            private NativeArray3D<T> m_Array;

            private int m_Index;

            public Enumerator(ref NativeArray3D<T> array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public T Current
            {
                get => m_Array[m_Index];
            }

            object IEnumerator.Current
            {
                get => m_Array[m_Index];
            }

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                m_Index++;
                return m_Index < m_Array.Length;
            }

            public void Reset()
            {
                m_Index = -1;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeArray3D<T>>();

        [BurstDiscard]
        private static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeArray3D<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif

        internal Allocator m_AllocatorLabel;


        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;

        internal int m_Length0;
        internal int m_Length1;
        internal int m_Length2;

        /// <summary>
        /// The length of the array's first dimension.
        /// </summary>
        public int Length0
        {
            get => m_Length0;
        }

        /// <summary>
        /// The length of the array's second dimension.
        /// </summary>
        public int Length1
        {
            get => m_Length1;
        }

        /// <summary>
        /// The length of the array's third dimension.
        /// </summary>
        public int Length2
        {
            get => m_Length2;
        }

        /// <summary>
        /// The total length of the array. Equal to the product of <see cref="Length0"/>, <see cref="Length1"/>, and <see cref="Length2"/>.
        /// </summary>
        public int Length
        {
            get => m_Length0 * m_Length1 * m_Length2;
        }

        public T this[int index0, int index1, int index2]
        {
            get
            {
                CheckElementReadAccess(index0, index1, index2);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, (index0 * m_Length1 + index1) * m_Length2 + index2);
            }
            [WriteAccessRequired]
            set
            {
                CheckElementWriteAccess(index0, index1, index2);
                UnsafeUtility.WriteArrayElement(m_Buffer, (index0 * m_Length1 + index1) * m_Length2 + index2, value);
            }
        }

        internal T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                if (index >= 0 && index < Length)
                {
                    return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                }
                throw new System.IndexOutOfRangeException();
            }
        }

        public bool IsCreated
        {
            get => m_Buffer != null;
        }

        /// <summary>
        /// Creates a copy of <paramref name="array"/>.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="allocator"></param>
        public NativeArray3D(T[,,] array, Allocator allocator)
        {
            Allocate(array.GetLength(0), array.GetLength(1), array.GetLength(2), allocator, out this);
            Copy(array, this);
        }

        /// <summary>
        /// Creates a copy of <paramref name="array"/>.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="allocator"></param>
        public NativeArray3D(NativeArray3D<T> array, Allocator allocator)
        {
            Allocate(array.m_Length0, array.m_Length1, array.m_Length2, allocator, out this);
            Copy(array, this);
        }

        /// <summary>
        /// Creates a 3D array with dimensions [<paramref name="length0"/>, <paramref name="length1"/>, <paramref name="length2"/>].
        /// </summary>
        /// <param name="length0">The length of the array's first dimension.</param>
        /// <param name="length1">The length of the array's second dimension.</param>
        /// <param name="length2">The length of the array's third dimension.</param>
        /// <param name="allocator"></param>
        /// <param name="options">Whether to clear the array or leave it uninitialized.</param>
        public NativeArray3D(int length0, int length1, int length2, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length0, length1, length2, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_Buffer, Length * (long)UnsafeUtility.SizeOf<T>());
            }
        }

        private static void Allocate(int length0, int length1, int length2, Allocator allocator, out NativeArray3D<T> array)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(allocator);
            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new System.NotSupportedException($"type {typeof(T)} must be an unmanaged type.");
            }
#endif

            if (length0 <= 0)
                throw new ArgumentOutOfRangeException(nameof(length0), "Length0 must be >= 0.");

            if (length1 <= 0)
                throw new ArgumentOutOfRangeException(nameof(length1), "Length1 must be >= 0.");

            if (length2 <= 0)
                throw new ArgumentOutOfRangeException(nameof(length1), "Length2 must be >= 0.");

            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length0 * (long)length1 * (long)length2;

            if (totalSize > int.MaxValue)
                throw new InvalidOperationException($"Length0 * Length1 * Length2 * sizeof({typeof(T)}) cannot exceed {int.MaxValue} bytes.");

            array = new NativeArray3D<T>()
            {
                m_Buffer = UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<T>(), allocator),
                m_Length0 = length0,
                m_Length1 = length1,
                m_Length2 = length2,
                m_AllocatorLabel = allocator
            };

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 2, allocator);
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref array.m_Safety, s_staticSafetyId.Data);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementReadAccess(int index0, int index1, int index2)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (!InRange(index0, index1, index2))
            {
                throw new IndexOutOfRangeException();
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementWriteAccess(int index0, int index1, int index2)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            if (!InRange(index0, index1, index2))
            {
                throw new IndexOutOfRangeException();
            }
        }

        public bool InRange(int index0, int index1, int index2)
        {
            return (index0 >= 0 && index0 < m_Length0) 
                && (index1 >= 0 && index1 < m_Length1)
                && (index2 >= 0 && index2 < m_Length2);
        }

        [WriteAccessRequired]
        public void CopyFrom(NativeArray3D<T> src)
        {
            Copy(src, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(T[,,] dst)
        {
            Copy(dst, this);
        }

        public void CopyTo(NativeArray3D<T> dst)
        {
            Copy(this, dst);
        }

        public void CopyTo(T[,,] dst)
        {
            Copy(this, dst);
        }

        public T[,,] ToArray()
        {
            var array = new T[m_Length0, m_Length1, m_Length2];
            Copy(this, array);
            return array;
        }

        public static void Copy(NativeArray3D<T> src, NativeArray3D<T> dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif
            if (src.m_Length0 != dst.m_Length0 || src.m_Length1 != dst.m_Length1 || src.m_Length2 != dst.m_Length2)
            {
                throw new ArgumentException("Source and destination must have the same dimensions.");
            }

            UnsafeUtility.MemCpy(dst.m_Buffer, src.m_Buffer, src.Length * UnsafeUtility.SizeOf<T>());
        }

        public static void Copy(NativeArray3D<T> src, T[,,] dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
#endif
            if (src.m_Length0 != dst.GetLength(0) || src.m_Length1 != dst.GetLength(1))
            {
                throw new ArgumentException("Source and destination must have the same dimensions.");
            }

            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(addr.ToPointer(),
                                 src.m_Buffer,
                                 src.Length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        public static void Copy(T[,,] src, NativeArray3D<T> dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif
            if (src.GetLength(0) != dst.m_Length0 || src.GetLength(1) != dst.m_Length1 || src.GetLength(2) != dst.m_Length2)
            {
                throw new ArgumentException("Source and destination must have the same dimensions.");
            }

            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(dst.m_Buffer,
                                 addr.ToPointer(),
                                 src.Length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        /// <summary>
        /// Flattens the array into a one-dimensional managed array.
        /// </summary>
        public T[] Flatten()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var dst = new T[Length];
            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(addr.ToPointer(),
                                 m_Buffer,
                                 Length * UnsafeUtility.SizeOf<T>());

            handle.Free();
            return dst;
        }

        /// <summary>
        /// Flattens the array into a one-dimensional NativeArray.
        /// </summary>
        public NativeArray<T> Flatten(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var dst = new NativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(dst.GetUnsafePtr(), m_Buffer, Length * UnsafeUtility.SizeOf<T>());
            return dst;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(m_AllocatorLabel);

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);

            m_Buffer = null;
            m_Length0 = m_Length1 = m_Length2 = 0;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_Buffer == null)
            {
                throw new ObjectDisposedException("The NativeArray3D is already disposed.");
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(m_AllocatorLabel);

            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

            var jobHandle = new NativeArray3DDisposeJob
            {
                Data = new NativeArray3DDispose
                {
                    m_Buffer = m_Buffer,
                    m_AllocatorLabel = m_AllocatorLabel,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety
#endif
                }
            }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
            m_Buffer = null;
            m_Length0 = m_Length1 = m_Length2 = 0;

            return jobHandle;
        }

        public bool Equals(NativeArray3D<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Length0 == other.m_Length0 
                && m_Length1 == other.m_Length1 && m_Length2 == other.m_Length2;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocator(Allocator allocator)
        {
            // Native allocation is only valid for Temp, Job and Persistent.
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
        }
    }

    internal unsafe sealed class NativeArray3DDebugView<T> where T : struct
    {
        private readonly NativeArray3D<T> m_Array;

        public NativeArray3DDebugView(NativeArray3D<T> array)
        {
            m_Array = array;
        }

        public T[,,] Items
        {
            get => m_Array.ToArray();
        }
    }

    [NativeContainer]
    internal unsafe struct NativeArray3DDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;

        internal Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
        }
    }

    [BurstCompile]
    struct NativeArray3DDisposeJob : IJob
    {
        public NativeArray3DDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}
