using NUnit.Framework;

namespace Unity.Collections {
  using Jobs;

  public class ResultTests {

    [Test]
    public void DefaultResultIsInvalid() {
      Result<int> result = default(Result<int>);

      Assert.That(result.IsCreated, Is.False); ;
    }

    [Test]
    public void ResultAllocateDispose([Values(Allocator.Persistent,
                                            Allocator.Temp,
                                            Allocator.TempJob)]
                                    Allocator allocator) {
      Result<int> result = new Result<int>(allocator);

      Assert.That(result.IsCreated, Is.True);

      result.Dispose();

      Assert.That(result.IsCreated, Is.False);
    }

    [Test]
    public void ResultSetGetMainThread() {
      Result<int> result = new Result<int>(Allocator.Temp);

      Assert.That(result.Value, Is.EqualTo(default(int)));

      result.Value = 23;

      Assert.That(result.Value, Is.EqualTo(23));

      result.Dispose();
    }

    [Test]
    public void ResultSetGetFromJob() {
      const int SRC_VAL = 23;
      const int DST_VAL = 0;

      Result<int> src = new Result<int>(Allocator.TempJob);
      Result<int> dst = new Result<int>(Allocator.TempJob);

      src.Value = SRC_VAL;
      dst.Value = DST_VAL;

      new SetGetJob() {
        src = src,
        dst = dst
      }.Schedule().Complete();

      Assert.That(src.Value, Is.EqualTo(SRC_VAL));
      Assert.That(dst.Value, Is.EqualTo(SRC_VAL));

      src.Dispose();
      dst.Dispose();
    }

    [Test]
    public void ConcurrentWritesThrows() {
      Result<int> result = new Result<int>(Allocator.TempJob);

      var writeJob = new WriteToResultJob() {
        result = result
      }.Schedule();

      Assert.That(() => {
        new WriteToResultJob() {
          result = result
        }.Schedule();
      }, Throws.InvalidOperationException);

      writeJob.Complete();
      result.Dispose();
    }

    [Test]
    public void SupportsConcurrentReads() {
      const int SRC_VAL = 23;
      const int DST_VAL = 0;

      Result<int> src = new Result<int>(Allocator.TempJob);
      Result<int> dst1 = new Result<int>(Allocator.TempJob);
      Result<int> dst2 = new Result<int>(Allocator.TempJob);

      src.Value = SRC_VAL;
      dst1.Value = DST_VAL;
      dst2.Value = DST_VAL;

      var job1 = new SetGetJob() {
        src = src,
        dst = dst1
      }.Schedule();

      var job2 = new SetGetJob() {
        src = src,
        dst = dst2
      }.Schedule();

      job1.Complete();
      job2.Complete();

      Assert.That(src.Value, Is.EqualTo(SRC_VAL));
      Assert.That(dst1.Value, Is.EqualTo(SRC_VAL));
      Assert.That(dst2.Value, Is.EqualTo(SRC_VAL));

      src.Dispose();
      dst1.Dispose();
      dst2.Dispose();
    }

    [Test]
    public void SupportsDeallocateOnJobCompletion() {
      Result<int> result = new Result<int>(Allocator.TempJob);

      new DeallocateResultJob() {
        result = result
      }.Schedule().Complete();

      Assert.That(() => {
        result.Value = 5;
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

    private struct TrySetReadonlyJob : IJob {
      [ReadOnly]
      public Result<int> result;

      public void Execute() {
        result.Value = 0;
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
      [WriteOnly]
      public Result<int> result;

      public void Execute() {
        result.Value = 0;
      }
    }

    private struct DeallocateResultJob : IJob {
      [DeallocateOnJobCompletion]
      public Result<int> result;

      public void Execute() { }
    }
  }
}
