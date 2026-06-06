using BMTP3.Core4.Traversal;

namespace BMTP3.Core4.Tests.Traversal;

public class MtpGatekeeperTests
{
	[Fact]
	public async Task ExecuteAsync_RunsAction()
	{
		using MtpGatekeeper gatekeeper = new();
		int result = await gatekeeper.ExecuteAsync(ct => Task.FromResult(42), CancellationToken.None);
		Assert.Equal(42, result);
	}

	[Fact]
	public async Task ExecuteAsync_NonGeneric_RunsAction()
	{
		using MtpGatekeeper gatekeeper = new();
		bool ran = false;
		await gatekeeper.ExecuteAsync(ct => { ran = true; return Task.CompletedTask; }, CancellationToken.None);
		Assert.True(ran);
	}

	[Fact]
	public async Task ExecuteAsync_SerializesConcurrentCalls()
	{
		using MtpGatekeeper gatekeeper = new();
		int concurrent = 0;
		int maxConcurrent = 0;

		Task[] tasks = Enumerable.Range(0, 10).Select(_ =>
			gatekeeper.ExecuteAsync(async ct =>
			{
				int current = Interlocked.Increment(ref concurrent);
				InterlockedExchangeMax(ref maxConcurrent, current);
				await Task.Delay(10, ct);
				Interlocked.Decrement(ref concurrent);
			}, CancellationToken.None)
		).ToArray();

		await Task.WhenAll(tasks);
		Assert.True(maxConcurrent <= 1, $"maxConcurrent was {maxConcurrent}, expected <= 1");
	}

	[Fact]
	public async Task ExecuteAsync_CancelledToken_Throws()
	{
		using MtpGatekeeper gatekeeper = new();
		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<TaskCanceledException>(() =>
			gatekeeper.ExecuteAsync(ct => Task.FromResult(0), cts.Token));
	}

	[Fact]
	public async Task AcquireAsync_ReturnsLease_ThatReleasesWhenDisposed()
	{
		using MtpGatekeeper gatekeeper = new();
		IDisposable lease = await gatekeeper.AcquireAsync(CancellationToken.None);
		lease.Dispose();

		await gatekeeper.ExecuteAsync(ct => Task.CompletedTask, CancellationToken.None);
	}

	[Fact]
	public async Task AcquireAsync_DoubleDispose_Safe()
	{
		using MtpGatekeeper gatekeeper = new();
		IDisposable lease = await gatekeeper.AcquireAsync(CancellationToken.None);
		lease.Dispose();
		lease.Dispose();
	}

	[Fact]
	public async Task AcquireAsync_HoldsLockUntilDisposed()
	{
		using MtpGatekeeper gatekeeper = new();
		IDisposable lease = await gatekeeper.AcquireAsync(CancellationToken.None);

		Task tryExecute = gatekeeper.ExecuteAsync(ct => Task.CompletedTask, CancellationToken.None);
		await Assert.ThrowsAsync<TaskCanceledException>(async () =>
		{
			using CancellationTokenSource cts = new();
			cts.CancelAfter(50);
			await tryExecute.WaitAsync(cts.Token);
		});

		lease.Dispose();
	}

	[Fact]
	public async Task ExecuteAsync_Exception_ReleasesLock()
	{
		using MtpGatekeeper gatekeeper = new();

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			gatekeeper.ExecuteAsync<object>(ct => throw new InvalidOperationException("fail"), CancellationToken.None));

		await gatekeeper.ExecuteAsync(ct => Task.CompletedTask, CancellationToken.None);
	}

	[Fact]
	public async Task AcquireAsync_CancelledToken_Throws()
	{
		using MtpGatekeeper gatekeeper = new();
		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<TaskCanceledException>(() =>
			gatekeeper.AcquireAsync(cts.Token));
	}

	[Fact]
	public async Task AcquireAsync_Timeout_ThrowsTimeoutException()
	{
		using MtpGatekeeper gatekeeper = new();
		IDisposable lease = await gatekeeper.AcquireAsync(CancellationToken.None);

		await Assert.ThrowsAsync<TimeoutException>(() =>
			gatekeeper.AcquireAsync(TimeSpan.FromMilliseconds(1), CancellationToken.None));

		lease.Dispose();
	}

	[Fact]
	public async Task AcquireAsync_Timeout_CancelledExternally_ThrowsTaskCanceled()
	{
		using MtpGatekeeper gatekeeper = new();
		using CancellationTokenSource cts = new();
		cts.Cancel();

		await Assert.ThrowsAsync<TaskCanceledException>(() =>
			gatekeeper.AcquireAsync(TimeSpan.FromSeconds(60), cts.Token));
	}

	[Fact]
	public async Task AcquireAsync_Timeout_CompletesWhenLockAvailable()
	{
		using MtpGatekeeper gatekeeper = new();
		using IDisposable lease = await gatekeeper.AcquireAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
		Assert.NotNull(lease);
	}

	[Fact]
	public async Task ExecuteAsync_AfterDispose_Throws()
	{
		MtpGatekeeper gatekeeper = new();
		gatekeeper.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() =>
			gatekeeper.ExecuteAsync(ct => Task.FromResult(0), CancellationToken.None));
	}

	[Fact]
	public async Task AcquireAsync_AfterDispose_Throws()
	{
		MtpGatekeeper gatekeeper = new();
		gatekeeper.Dispose();

		await Assert.ThrowsAsync<ObjectDisposedException>(() =>
			gatekeeper.AcquireAsync(CancellationToken.None));
	}

	[Fact]
	public void Dispose_Idempotent()
	{
		MtpGatekeeper gatekeeper = new();
		gatekeeper.Dispose();
		gatekeeper.Dispose(); // should not throw
	}

	private static void InterlockedExchangeMax(ref int target, int value)
	{
		int initial;
		do
		{
			initial = target;
		}
		while(initial < value && Interlocked.CompareExchange(ref target, value, initial) != initial);
	}
}