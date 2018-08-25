## High Concept ##

This small library is for getting results from a job back onto the main thread or into other jobs.  It can do this in one of two ways:

 - Using a single allocated Result<T> that is conceptually a wrapper around a NativeArray<T> with a length of 1.
 
 - Using a smart Result<T, Op> that allows multiple jobs to collectively create a result in parallel.  For example, a Result<int, Sum> would allow an IJobParallelFor to add up a bunch of numbers in parallel.
 
## Result<T> ##

The NativeArray<T> with a length of 1 has been the goto method for getting computed results from a job back onto the main thread.  While completely workable and performant, it leaves a little to be desired when it comes to code clarity.  Using Result<T> instead of a NativeArray<T> of length 1 ensures that nobody (including you!) will ever go and mess with the length of that array accidentally because they didn't know it was an array for storing results.  The type of the data structure enforces the amount of data it holds.

## Result<T, Op> ##

While Result<T> is a simple convinience, Result<T, Op> is the real magic sauce.  Currently if you want multiple threads to contribute to a single result, there are not exactly any built in ways to do that.  If you want to sum up multiple values accross different threads, there isn't really a built-in way that can be accomplished.  Result<T, Op> is a generic data structure that allows multiple threads to contribute to a _single_ result in parallel.

For example, Result<int, Sum> is a container that represents a single integer result.  This container supports the Sum operation, and allows jobs to add values to the result from multiple threads at the same time.  For example, here is a simple job to add up all the values in an array in parallel:


    public struct AddValues : IJobParallelFor {
    	[ReadOnly]
    	public NativeArray<int> values;
    	
    	public Result<int, Sum>.Concurrent sum;
    	
    	public void Execute(int i) {
    		sum.Write(values[i]);
    	}
    }