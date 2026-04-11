using System.Diagnostics;
using BMTP3.Core2.BackupNew.Engine.Resilience;

namespace BMTP3.Core2.Tests.Resilience;

public class ExponentialBackoffRetryPolicyTests
{
	// ---------------------------------------------------------------
	// Succeeds on first attempt
	// ---------------------------------------------------------------

	[Fact]
	public async Task ExecuteAsync_SucceedsOnFirstAttempt_ReturnsResultWithoutRetrying()
	{
		ExponentialBackoffRetryPolicy policy = new(3, 0);
		int callCount = 0;

		int result = await policy.ExecuteAsync(() =>
		{
			callCount++;
			return Task.FromResult(42);
		}, CancellationToken.None);

		Assert.Equal(42, result);
		Assert.Equal(1, callCount);
	}

	// ---------------------------------------------------------------
	// Succeeds after failures
	// ---------------------------------------------------------------

	[Fact]
	public async Task ExecuteAsync_FailsTwiceThenSucceeds_ReturnsResultOnThirdAttempt()
	{
		ExponentialBackoffRetryPolicy policy = new(3, 0);
		int callCount = 0;

		string result = await policy.ExecuteAsync(() =>
		{
			callCount++;
			if (callCount < 3)
			{
				throw new InvalidOperationException($"Simulated failure #{callCount}");
			}

			return Task.FromResult("ok");
		}, CancellationToken.None);

		Assert.Equal("ok", result);
		Assert.Equal(3, callCount);
	}

	// ---------------------------------------------------------------
	// Exhausts all attempts and rethrows
	// ---------------------------------------------------------------

	[Fact]
	public async Task ExecuteAsync_ExhaustsAllAttempts_RethrowsLastException()
	{
		ExponentialBackoffRetryPolicy policy = new(3, 0);
		int callCount = 0;

		InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			await policy.ExecuteAsync<int>(() =>
			{
				callCount++;
				throw new InvalidOperationException($"fail #{callCount}");
			}, CancellationToken.None);
		});

		Assert.Equal(3, callCount);
		// The last exception message should reference the third attempt
		Assert.Contains("3", ex.Message);
	}

	// ---------------------------------------------------------------
	// CancellationToken cancels mid-retry
	// ---------------------------------------------------------------

	[Fact]
	public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		// Use a non-zero base delay so cancellation can bite during Task.Delay
		ExponentialBackoffRetryPolicy policy = new(5, 500);
		using CancellationTokenSource cts = new();

		// Cancel as soon as the first attempt fails so the delay is cancelled
		int callCount = 0;

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
		{
			await policy.ExecuteAsync<int>(() =>
			{
				callCount++;
				// Cancel after first failure to trigger the Task.Delay cancellation path
				cts.Cancel();
				throw new InvalidOperationException("force retry");
			}, cts.Token);
		});

		// Only one real attempt should have been made before cancellation
		Assert.Equal(1, callCount);
	}

	// ---------------------------------------------------------------
	// Delay grows exponentially
	// ---------------------------------------------------------------

	[Fact]
	public async Task ExecuteAsync_ExponentialBackoff_TotalElapsedGrowsWithRetries()
	{
		// Use a small but measurable base delay to avoid flakiness
		const int baseMs = 20;
		// 3 attempts: attempt 1 fails → delay 20ms, attempt 2 fails → delay 40ms, attempt 3 succeeds
		// Minimum expected elapsed ≈ 60ms (20 + 40), use a conservative lower bound of 50ms
		ExponentialBackoffRetryPolicy policy = new(3, baseMs);
		int callCount = 0;

		Stopwatch sw = Stopwatch.StartNew();

		await policy.ExecuteAsync(() =>
		{
			callCount++;
			if (callCount < 3)
			{
				throw new InvalidOperationException("retry me");
			}

			return Task.FromResult(true);
		}, CancellationToken.None);

		sw.Stop();

		// base * 2^0 + base * 2^1 = 20 + 40 = 60ms minimum
		// Allow generous margin for CI environments
		Assert.True(sw.ElapsedMilliseconds >= 50,
			$"Expected >= 50ms elapsed due to exponential back-off, but got {sw.ElapsedMilliseconds}ms");
	}

	// ---------------------------------------------------------------
	// Constructor validation
	// ---------------------------------------------------------------

	[Fact]
	public void Constructor_MaxAttemptsLessThanOne_ThrowsArgumentOutOfRangeException()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new ExponentialBackoffRetryPolicy(0, 100));
	}

	[Fact]
	public void Constructor_NegativeBaseBackoff_ThrowsArgumentOutOfRangeException()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new ExponentialBackoffRetryPolicy(3, -1));
	}
}