using Xunit.v3;

// Each HTTP fixture hosts its own server; bound test concurrency across machines.
[assembly: Parallelization(MaxThreads = 2)]
