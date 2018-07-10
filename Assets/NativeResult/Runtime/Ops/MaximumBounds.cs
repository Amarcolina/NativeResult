using UnityEngine;

namespace Unity.Collections {

  public struct MaximumBounds : IComutativeOp<Bounds> {

    public void Combine(ref Bounds curr, ref Bounds value) {
      curr.Encapsulate(value);
    }

    public void GetIdentity(out Bounds identity) {
      identity = new Bounds(new Vector3(0, 0, 0), new Vector3(float.MinValue, float.MinValue, float.MinValue));
    }
  }
}
