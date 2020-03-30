
namespace Unity.Collections {

    /// <summary>
    /// Represents a comutative operation on a type T that
    /// can combine with itself.  An example of such an operation
    /// would be an addition operation on type int.
    /// </summary>
    public interface IComutativeOp<T>
        where T : struct {

        /// <summary>
        /// Return a value such that when combined with
        /// another value, does not modify it.
        /// </summary>
        void GetIdentity(out T identity);

        /// <summary>
        /// Modify a current value using another given value.
        /// </summary>
        void Combine(ref T curr, ref T value);
    }
}
