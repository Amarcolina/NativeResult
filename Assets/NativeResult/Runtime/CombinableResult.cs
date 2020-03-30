using System;

namespace Unity.Collections {
    using Jobs.LowLevel.Unsafe;
    using LowLevel.Unsafe;

    /// <summary>
    /// A special type of Result that allows the value to be modified by an
    /// specified operation.  The power of this type of Result is that it contains
    /// a Current writer that allows multiple jobs to modify the result concurrently.
    /// 
    /// For example, if you wanted to find the sum of many different values using an
    /// IJobParallelFor, you could this type of result to acomplish that.
    /// 
    /// This result supports any type of operation that inherits from IComutativeOp.
    /// </summary>
    [NativeContainer]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    public struct Result<T, Op>
      where T : struct
      where Op : struct, IComutativeOp<T> {

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#endif

        [NativeDisableUnsafePtrRestriction]
        private IntPtr m_Buffer;
        private Allocator m_AllocatorLabel;

        private Op m_Op;

        /// <summary>
        /// Constructs a new result with an initial value.  You must specify the Allocator
        /// type to use to allocate the memory used by this Result container.
        /// 
        /// You can optionally specify an instance of the Op type to use to combine results
        /// into the final single result.  You only need to specify an instance if the op will
        /// contain specific instance data that is important to performing its operation.
        /// 
        /// For operations that need no context, such as addition, no operation needs to be 
        /// specified, as the result will use the default instance automatically based on the
        /// Op type argument.
        /// </summary>
        public Result(Allocator allocator, Op op = default(Op)) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsBlittable<T>()) {
                throw new ArgumentException($"{typeof(T)} used in Result<{typeof(T)}, {typeof(Op)}> must be blittable");
            }
            if (UnsafeUtility.SizeOf<T>() > JobsUtility.CacheLineSize) {
                throw new ArgumentException($"{typeof(T)} used in Result<{typeof(T)}, { typeof(Op)}> had a size of {UnsafeUtility.SizeOf<T>()} which is greater than the maximum size of {JobsUtility.CacheLineSize}");
            }
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, allocator);
#endif

            unsafe {
                m_Buffer = (IntPtr)UnsafeUtility.Malloc(JobsUtility.CacheLineSize * JobsUtility.MaxJobThreadCount,
                                                        JobsUtility.CacheLineSize,
                                                        allocator);
            }

            m_AllocatorLabel = allocator;
            m_Op = op;

            Reset();
        }

        /// <summary>
        /// Gets the result value of this result container.
        /// 
        /// Note that this operation is more expensive than simply accessing
        /// a field, and so you should always save the result into a local
        /// variable rather than getting the value over and over again when
        /// it could not have changed.
        /// 
        /// This operation requires read access.
        /// </summary>
        public T Value {
            get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

                unsafe {
                    void* ptr = (void*)m_Buffer;

                    T result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
                    for (int i = 1; i < JobsUtility.MaxJobThreadCount; i++) {
                        T element = UnsafeUtility.ReadArrayElementWithStride<T>(ptr, i, JobsUtility.CacheLineSize);
                        m_Op.Combine(ref result, ref element);
                    }

                    return result;
                }
            }
        }

        /// <summary>
        /// Combines the current value stored in this result with a new value.  
        /// 
        /// This operation will use the specific combine operation specified by the 
        /// generic arguments of this class, and the constructor if a custom operator
        /// was defined.
        /// 
        /// This operation requires write access.
        /// </summary>
        public void Write(T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            unsafe {
                void* ptr = (void*)m_Buffer;

                T curr = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
                m_Op.Combine(ref curr, ref value);
                UnsafeUtility.WriteArrayElement(ptr, 0, curr);
            }
        }

        /// <summary>
        /// Resets the value contained within this result to the identity value.
        /// </summary>
        public void Reset() {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            T identity;
            m_Op.GetIdentity(out identity);

            unsafe {
                void* ptr = (void*)m_Buffer;
                for (int i = 0; i < JobsUtility.MaxJobThreadCount; i++) {
                    UnsafeUtility.WriteArrayElementWithStride(ptr, i, JobsUtility.CacheLineSize, identity);
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

        public static implicit operator T(Result<T, Op> result) {
            return result.Value;
        }

        /// <summary>
        /// A concurrent writer for a Result.  This writer allows multiple
        /// jobs to write into the result concurrently.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct Concurrent {

            [NativeDisableUnsafePtrRestriction]
            private IntPtr m_Buffer;

            private Op m_Op;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private AtomicSafetyHandle m_Safety;
#endif

            [NativeSetThreadIndex]
            private int m_ThreadIndex;

            public static implicit operator Concurrent(Result<T, Op> result) {
                Concurrent concurrent;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(result.m_Safety);
                concurrent.m_Safety = result.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref concurrent.m_Safety);
#endif

                concurrent.m_Buffer = result.m_Buffer;
                concurrent.m_Op = result.m_Op;
                concurrent.m_ThreadIndex = 0;
                return concurrent;
            }

            /// <summary>
            /// Combines the current value stored in this result with a new value.  
            /// 
            /// This operation will use the specific combine operation specified by the 
            /// generic arguments of this class, and the constructor if a custom operator
            /// was defined.
            /// 
            /// This operation requires write access.
            /// </summary>
            public void Write(T value) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

                unsafe {
                    void* ptr = (void*)m_Buffer;

                    T curr = UnsafeUtility.ReadArrayElementWithStride<T>(ptr, m_ThreadIndex, JobsUtility.CacheLineSize);
                    m_Op.Combine(ref curr, ref value);
                    UnsafeUtility.WriteArrayElementWithStride(ptr, m_ThreadIndex, JobsUtility.CacheLineSize, curr);
                }
            }
        }
    }
}
