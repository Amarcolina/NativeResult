using UnityEngine;

namespace Unity.Collections {
    using Mathematics;

    public struct Sum : IComutativeOp<int>,
                        IComutativeOp<int2>,
                        IComutativeOp<int3>,
                        IComutativeOp<int4>,
                        IComutativeOp<half>,
                        IComutativeOp<half2>,
                        IComutativeOp<half3>,
                        IComutativeOp<half4>,
                        IComutativeOp<float>,
                        IComutativeOp<float2>,
                        IComutativeOp<float3>,
                        IComutativeOp<float4>,
                        IComutativeOp<double>,
                        IComutativeOp<Vector2>,
                        IComutativeOp<Vector3>,
                        IComutativeOp<Vector4>,
                        IComutativeOp<Color> {

        public void Combine(ref int curr, ref int value) {
            curr += value;
        }

        public void Combine(ref int2 curr, ref int2 value) {
            curr += value;
        }

        public void Combine(ref int3 curr, ref int3 value) {
            curr += value;
        }

        public void Combine(ref int4 curr, ref int4 value) {
            curr += value;
        }

        public void Combine(ref half curr, ref half value) {
            curr += value;
        }

        public void Combine(ref half2 curr, ref half2 value) {
            curr.x += value.x;
            curr.y += value.y;
        }

        public void Combine(ref half3 curr, ref half3 value) {
            curr.x += value.x;
            curr.y += value.y;
            curr.z += value.z;
        }

        public void Combine(ref half4 curr, ref half4 value) {
            curr.x += value.x;
            curr.y += value.y;
            curr.z += value.z;
            curr.w += value.w;
        }

        public void Combine(ref float curr, ref float value) {
            curr += value;
        }

        public void Combine(ref float2 curr, ref float2 value) {
            curr += value;
        }

        public void Combine(ref float3 curr, ref float3 value) {
            curr += value;
        }

        public void Combine(ref float4 curr, ref float4 value) {
            curr += value;
        }

        public void Combine(ref double curr, ref double value) {
            curr += value;
        }

        public void Combine(ref Vector2 curr, ref Vector2 value) {
            curr += value;
        }

        public void Combine(ref Vector3 curr, ref Vector3 value) {
            curr += value;
        }

        public void Combine(ref Vector4 curr, ref Vector4 value) {
            curr += value;
        }

        public void Combine(ref Color curr, ref Color value) {
            curr += value;
        }

        public void GetIdentity(out int identity) {
            identity = 0;
        }

        public void GetIdentity(out int2 identity) {
            identity = new int2(0, 0);
        }

        public void GetIdentity(out int3 identity) {
            identity = new int3(0, 0, 0);
        }

        public void GetIdentity(out int4 identity) {
            identity = new int4(0, 0, 0, 0);
        }

        public void GetIdentity(out half identity) {
            identity = (half)0;
        }

        public void GetIdentity(out half2 identity) {
            identity = new half2(0);
        }

        public void GetIdentity(out half3 identity) {
            identity = new half3(0);
        }

        public void GetIdentity(out half4 identity) {
            identity = new half4(0);
        }

        public void GetIdentity(out float identity) {
            identity = 0;
        }

        public void GetIdentity(out float2 identity) {
            identity = new float2(0, 0);
        }

        public void GetIdentity(out float3 identity) {
            identity = new float3(0, 0, 0);
        }

        public void GetIdentity(out float4 identity) {
            identity = new float4(0, 0, 0, 0);
        }

        public void GetIdentity(out double identity) {
            identity = 0;
        }

        public void GetIdentity(out Vector2 identity) {
            identity = new Vector2(0, 0);
        }

        public void GetIdentity(out Vector3 identity) {
            identity = new Vector3(0, 0, 0);
        }

        public void GetIdentity(out Vector4 identity) {
            identity = new Vector4(0, 0, 0, 0);
        }

        public void GetIdentity(out Color identity) {
            identity = new Color(0, 0, 0, 0);
        }
    }
}
