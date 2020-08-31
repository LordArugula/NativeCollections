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
    /// An unmanaged 2D array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length0 = {Length0}, Length1 = {Length1}")]
    [DebuggerTypeProxy(typeof(NativeArray2DDebugView<>))]
    public unsafe struct NativeArray2D<T> : IDisposable, IEnumerable<T>, IEquatable<NativeArray2D<T>>
        where T : struct
    {
        public struct Enumerator : IEnumerator<T>
        {
            private NativeArray2D<T> m_Array;

            private int m_Index0;
            private int m_Index1;

            public Enumerator(ref NativeArray2D<T> array)
            {
                m_Array = array;
                m_Index0 = 0;
                m_Index1 = -1;
            }

            public T Current
            {
                get => m_Array[m_Index0, m_Index1];
            }

            object IEnumerator.Current
            {
                get => m_Array[m_Index0, m_Index1];
            }

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                m_Index1++;
                if (m_Index1 >= m_Array.Length1)
                {
                    m_Index1 = 0;
                    m_Index0++;
                    return m_Index0 < m_Array.Length0;
                }
                return true;
            }

            public void Reset()
            {
                m_Index0 = 0;
                m_Index1 = -1;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeArray2D<T>>();

        [BurstDiscard]
        static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeArray2D<T>>();
        }

        [NativeSetClassTypeToNullOnSchedule]
        internal DisposeSentinel m_DisposeSentinel;
#endif

        internal Allocator m_AllocatorLabel;


        [NativeDisableUnsafePtrRestriction]
        internal void* m_Buffer;

        internal int m_Length0;
        internal int m_Length1;

        public int Length0
        {
            get => m_Length0;
        }

        public int Length1
        {
            get => m_Length1;
        }

        public int Length
        {
            get => m_Length0 * m_Length1;
        }

        public T this[int index0, int index1]
        {
            get
            {
                CheckElementReadAccess(index0, index1);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index0 * m_Length1 + index1);
            }
            set
            {
                CheckElementWriteAccess(index0, index1);
                UnsafeUtility.WriteArrayElement(m_Buffer, index0 * m_Length1 + index1, value);
            }
        }

        public bool IsCreated
        {
            get => m_Buffer != null;
        }

        public NativeArray2D(T[,] array, Allocator allocator)
        {
            Allocate(array.GetLength(0), array.GetLength(1), allocator, out this);
            Copy(array, this);
        }

        public NativeArray2D(NativeArray2D<T> array, Allocator allocator)
        {
            Allocate(array.m_Length0, array.m_Length1, allocator, out this);
            Copy(array, this);
        }

        public NativeArray2D(int length0, int length1, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length0, length1, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(m_Buffer, Length * (long)UnsafeUtility.SizeOf<T>());
            }
        }

        private static void Allocate(int length0, int length1, Allocator allocator, out NativeArray2D<T> array)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocator(allocator);
            CollectionHelper.CheckIsUnmanaged<T>();

            if (length0 <= 0)
                throw new ArgumentOutOfRangeException(nameof(length0), "Length0 must be >= 0.");

            if (length1 <= 0)
                throw new ArgumentOutOfRangeException(nameof(length1), "Length1 must be >= 0.");

            long totalSize = UnsafeUtility.SizeOf<T>() * (long)length0 * (long)length1;

            if (totalSize > int.MaxValue)
                throw new InvalidOperationException($"Length0 * Length1 * sizeof(T) cannot exceed {int.MaxValue} bytes.");
#endif

            array = new NativeArray2D<T>()
            {
                m_Buffer = UnsafeUtility.Malloc(totalSize, UnsafeUtility.AlignOf<T>(), allocator),
                m_Length0 = length0,
                m_Length1 = length1,
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

        private void CheckElementReadAccess(int index0, int index1)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

            if (index0 < 0 || index0 >= m_Length0)
            {
                throw new IndexOutOfRangeException($"Index {index0} is out of range (must be between 0 and {m_Length0 - 1}).");
            }

            if (index1 < 0 || index1 >= m_Length1)
            {
                throw new IndexOutOfRangeException($"Index {index1} is out of range (must be between 0 and {m_Length1 - 1}).");
            }
        }

        private void CheckElementWriteAccess(int index0, int index1)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            if (index0 < 0 || index0 >= m_Length0)
            {
                throw new IndexOutOfRangeException($"Index {index0} is out of range (must be between 0 and {m_Length0 - 1}).");
            }

            if (index1 < 0 || index1 >= m_Length1)
            {
                throw new IndexOutOfRangeException($"Index {index1} is out of range (must be between 0 and {m_Length1 - 1}).");
            }
        }

        [WriteAccessRequired]
        public void CopyFrom(NativeArray2D<T> src)
        {
            Copy(src, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(T[,] src)
        {
            Copy(src, this);
        }

        public void CopyTo(NativeArray2D<T> dst)
        {
            Copy(this, dst);
        }

        public void CopyTo(T[,] dst)
        {
            Copy(this, dst);
        }

        public T[,] ToArray()
        {
            var array = new T[m_Length0, m_Length1];
            Copy(this, array);
            return array;
        }

        public static void Copy(NativeArray2D<T> src, NativeArray2D<T> dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif
            if (src.m_Length0 != dst.m_Length0 || src.m_Length1 != dst.m_Length1)
            {
                throw new ArgumentException("Source and destination must have the same lengths.");
            }

            UnsafeUtility.MemCpy(dst.m_Buffer, src.m_Buffer, src.Length * UnsafeUtility.SizeOf<T>());
        }

        public static void Copy(NativeArray2D<T> src, T[,] dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
#endif
            if (src.m_Length0 != dst.GetLength(0) || src.m_Length1 != dst.GetLength(1))
            {
                throw new ArgumentException("Source and destination must have the same lengths.");
            }

            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(addr.ToPointer(),
                                 src.m_Buffer,
                                 src.Length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        public static void Copy(T[,] src, NativeArray2D<T> dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif
            if (src.GetLength(0) != dst.m_Length0 || src.GetLength(1) != dst.m_Length1)
            {
                throw new ArgumentException("Source and destination must have the same lengths.");
            }

            var handle = GCHandle.Alloc(src, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            UnsafeUtility.MemCpy(dst.m_Buffer,
                                 addr.ToPointer(),
                                 src.Length * UnsafeUtility.SizeOf<T>());

            handle.Free();
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS

            CheckAllocator(m_AllocatorLabel);

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);

            m_Buffer = null;
            m_Length0 = m_Length1 = 0;
        }

        public bool Equals(NativeArray2D<T> other)
        {
            return m_Buffer == other.m_Buffer && m_Length0 == other.m_Length0 && m_Length1 == other.m_Length1;
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

    internal unsafe sealed class NativeArray2DDebugView<T> where T : struct
    {
        private readonly NativeArray2D<T> m_Array;

        public NativeArray2DDebugView(NativeArray2D<T> array)
        {
            m_Array = array;
        }

        public T[,] Items
        {
            get => m_Array.ToArray();
        }

        public void* Buffer
        {
            get => m_Array.m_Buffer;
        }
    }
}
