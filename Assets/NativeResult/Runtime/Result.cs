using System;

namespace Unity.Collections {
    using LowLevel.Unsafe;

    /// <summary>
    /// A simple wrapper over an allocation that allows results to be passed
    /// into an out of jobs.  Must be allocated and disposed of, just like
    /// NativeArray.
    /// </summary>
    [NativeContainer]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    public struct Result<T>
        where T : struct {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction]
        private IntPtr m_Buffer;
        private Allocator m_AllocatorLabel;

        /// <summary>
        /// Allocates a new Result using a given allocator.
        /// The result will use the default value of type T as its
        /// initial value.
        /// </summary>
        public Result(Allocator allocator) : this(allocator, default(T)) { }

        /// <summary>
        /// Allocates a new Result with a given initial value, and a given
        /// allocator.
        /// </summary>
        public Result(T value, Allocator allocator) : this(allocator, value) { }

        private Result(Allocator allocator, T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<T>()) {
                throw new ArgumentException($"{typeof(T)} used in Result<{typeof(T)}> must be blittable");
            }
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, allocator);
#endif

            unsafe {
                m_Buffer = (IntPtr)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), allocator);
            }
            m_AllocatorLabel = allocator;

            Value = value;
        }

        /// <summary>
        /// Gets or sets the value of this result.
        /// 
        /// Getting the value requires read access.
        /// Setting the value requires write access;
        /// </summary>
        public T Value {
            get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                unsafe {
                    return UnsafeUtility.ReadArrayElement<T>((void*)m_Buffer, 0);
                }
            }
            set {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                unsafe {
                    UnsafeUtility.WriteArrayElement((void*)m_Buffer, 0, value);
                }
            }
        }

        /// <summary>
        /// Returns whether or not this result has been created or not.  Will return false
        /// if the result haas already been disposed.
        /// </summary>
        public bool IsCreated {
            get {
                return m_Buffer != IntPtr.Zero;
            }
        }

        public void Dispose() {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            unsafe {
                UnsafeUtility.Free((void*)m_Buffer, m_AllocatorLabel);
                m_Buffer = IntPtr.Zero;
            }
        }

        public static implicit operator T(Result<T> result) {
            return result.Value;
        }
    }
}
