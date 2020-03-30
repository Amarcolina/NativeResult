using System.Linq;
using NUnit.Framework;

namespace Unity.Collections {
    using Jobs;
    using Jobs.LowLevel.Unsafe;

    public class CombinableResultTests {

        [SetUp]
        public void SetUp() {
            JobsUtility.JobDebuggerEnabled = true;
        }

        [Test]
        public void DefaultResultIsInvalid() {
            Result<int, Sum> result = default(Result<int, Sum>);

            Assert.That(result.IsCreated, Is.False);
        }

        [Test]
        public void ResultAllocateDispose([Values(Allocator.Persistent, Allocator.Temp, Allocator.TempJob)] Allocator allocator) {
            Result<int, Sum> result = new Result<int, Sum>(allocator);

            try {
                Assert.That(result.IsCreated, Is.True);
            } finally {
                result.Dispose();
            }

            Assert.That(result.IsCreated, Is.False);
        }

        [Test]
        public void TestDoubleFree() {
            Result<int, Sum> result = new Result<int, Sum>(Allocator.Temp);

            result.Dispose();

            Assert.That(() => {
                result.Dispose();
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestMainThreadCombine() {
            Result<int, Sum> sum = new Result<int, Sum>(Allocator.Temp);

            try {
                int referenceSum = 0;
                for (int i = 0; i < 1000; i++) {
                    int value = UnityEngine.Random.Range(-1000, 1000);

                    referenceSum += value;
                    sum.Write(value);
                }

                Assert.That(sum.Value, Is.EqualTo(referenceSum));
            } finally {
                sum.Dispose();
            }
        }

        [Test]
        public void TestJobParallelFor() {
            NativeArray<int> values = new NativeArray<int>(1000, Allocator.TempJob);
            Result<int, Sum> sum = new Result<int, Sum>(Allocator.TempJob);

            try {
                for (int i = 0; i < values.Length; i++) {
                    values[i] = UnityEngine.Random.Range(-1000, 1000);
                }

                new SumValuesJob() {
                    values = values,
                    sum = sum
                }.Schedule(values.Length, 32).Complete();

                Assert.That(sum.Value, Is.EqualTo(values.Sum()));
            } finally {
                values.Dispose();
                sum.Dispose();
            }
        }

        [Test]
        public void ConcurrentReadWriteThrows() {
            Result<int, Sum> result = new Result<int, Sum>(Allocator.TempJob);

            try {
                var writeJob = new WriteToResultJob() {
                    result = result
                }.Schedule();

                Assert.That(() => {
                    int tmp = result.Value;
                }, Throws.InvalidOperationException);

                Assert.That(() => {
                    result.Write(5);
                }, Throws.InvalidOperationException);

                Assert.That(() => {
                    new WriteToResultJob() {
                        result = result
                    }.Schedule();
                }, Throws.InvalidOperationException);

                writeJob.Complete();
            } finally {
                result.Dispose();
            }
        }

        [Test]
        public void SupportsDeallocateOnJobCompletion() {
            Result<int, Sum> result = new Result<int, Sum>(Allocator.TempJob);

            new DeallocateResultJob() {
                result = result
            }.Schedule().Complete();

            Assert.That(() => {
                result.Reset();
            }, Throws.InvalidOperationException);
        }

        private struct SetGetJob : IJob {
            [ReadOnly]
            public Result<int> src;
            [WriteOnly]
            public Result<int> dst;

            public void Execute() {
                dst.Value = src.Value;
            }
        }

        private struct SumValuesJob : IJobParallelFor {
            public NativeArray<int> values;
            public Result<int, Sum>.Concurrent sum;

            public void Execute(int index) {
                sum.Write(values[index]);
            }
        }

        private struct TryGetWriteonlyJob : IJob {
            [WriteOnly]
            public Result<int> result;

            public void Execute() {
                int value = result.Value;
            }
        }

        private struct WriteToResultJob : IJob {
            public Result<int, Sum>.Concurrent result;

            public void Execute() {
                result.Write(0);
            }
        }

        private struct DeallocateResultJob : IJob {
            [DeallocateOnJobCompletion]
            public Result<int, Sum> result;

            public void Execute() { }
        }
    }
}
