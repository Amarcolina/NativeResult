
namespace Unity.Collections {
    using Mathematics;

    public struct Minimum : IComutativeOp<int>,
                            IComutativeOp<half>,
                            IComutativeOp<float>,
                            IComutativeOp<double> {

        public void Combine(ref int curr, ref int value) {
            curr = value < curr ? value : curr;
        }

        public void Combine(ref half curr, ref half value) {
            curr = value < curr ? value : curr;
        }

        public void Combine(ref float curr, ref float value) {
            curr = value < curr ? value : curr;
        }

        public void Combine(ref double curr, ref double value) {
            curr = value < curr ? value : curr;
        }

        public void GetIdentity(out int identity) {
            identity = int.MaxValue;
        }

        public void GetIdentity(out half identity) {
            identity = (half)half.MaxValue;
        }

        public void GetIdentity(out float identity) {
            identity = float.MaxValue;
        }

        public void GetIdentity(out double identity) {
            identity = double.MaxValue;
        }
    }
}
