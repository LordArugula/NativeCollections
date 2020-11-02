﻿using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

/// <summary>
/// An unmanaged pointer used to store single value outputs from a Job.
/// </summary>
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativePtr<T> : INativeDisposable
    where T : struct
{
    [NativeDisableUnsafePtrRestriction]
    internal void* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal AtomicSafetyHandle m_Safety;
    static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativePtr<T>>();

    [BurstDiscard]
    static void CreateStaticSafetyId()
    {
        s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativePtr<T>>();
    }

    [NativeSetClassTypeToNullOnSchedule]
    DisposeSentinel m_DisposeSentinel;
#endif

    internal Allocator m_AllocatorLabel;

    public bool IsCreated
    {
        get => m_Buffer != null;
    }

    public T Value
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return UnsafeUtility.ReadArrayElement<T>(m_Buffer, 0);
        }
        [WriteAccessRequired]
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif       
            UnsafeUtility.WriteArrayElement(m_Buffer, 0, value);
        }
    }

    public NativePtr(Allocator allocator)
    {
        Allocate(allocator, out this);
    }

    public NativePtr(T value, Allocator allocator)
    {
        Allocate(allocator, out this);
        Value = value;
    }

    internal static void Allocate(Allocator allocator, out NativePtr<T> nativePtr)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!UnsafeUtility.IsUnmanaged<T>())
        {
            throw new System.NotSupportedException($"type {typeof(T)} must be an unmanaged type.");
        }

        if (allocator <= Allocator.None)
            throw new System.ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
#endif
        nativePtr = new NativePtr<T>
        {
            m_AllocatorLabel = allocator,
            m_Buffer = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator)
        };

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out nativePtr.m_Safety, out nativePtr.m_DisposeSentinel, 2, allocator);
        if (s_staticSafetyId.Data == 0)
        {
            CreateStaticSafetyId();
        }
        AtomicSafetyHandle.SetStaticSafetyId(ref nativePtr.m_Safety, s_staticSafetyId.Data);
#endif
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return default;
    }

    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
        UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
    }
}