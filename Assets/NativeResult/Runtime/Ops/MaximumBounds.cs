using UnityEngine;

namespace Unity.Collections {

    public struct MaximumBounds : IComutativeOp<Bounds> {

        public void Combine(ref Bounds curr, ref Bounds value) {
            if (curr.extents.x < 0) {
                curr = value;
            } else if (value.extents.x >= 0) {
                curr.Encapsulate(value);
            }
        }

        public void GetIdentity(out Bounds identity) {
            identity = new Bounds(new Vector3(0, 0, 0), new Vector3(-1, -1, -1));
        }
    }
}
