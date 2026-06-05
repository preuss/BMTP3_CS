namespace BMTP3.Core4.Tests.Engine;

public class BackupEngineCancellationTests
{
    [Fact]
    public void LinkedTokenSource_CallerCancellation_CancelsLinkedToken()
    {
        using CancellationTokenSource callerCts = new();
        CancellationToken callerToken = callerCts.Token;

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        CancellationToken linkedToken = linkedCts.Token;

        Assert.False(linkedToken.IsCancellationRequested);
        callerCts.Cancel();
        Assert.True(linkedToken.IsCancellationRequested);
    }

    [Fact]
    public void LinkedTokenSource_DirectCancellation_CancelsLinkedToken()
    {
        using CancellationTokenSource callerCts = new();
        CancellationToken callerToken = callerCts.Token;

        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        CancellationToken linkedToken = linkedCts.Token;

        Assert.False(linkedToken.IsCancellationRequested);
        linkedCts.Cancel();
        Assert.True(linkedToken.IsCancellationRequested);
    }

    [Fact]
    public void BackupEnginePattern_ReassignToken_ReflectsDirectCancellation()
    {
        using CancellationTokenSource callerCts = new();
        CancellationToken cancellationToken = callerCts.Token;

        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cancellationTokenSource.Token;

        Assert.False(cancellationToken.IsCancellationRequested);

        cancellationTokenSource.Cancel();
        Assert.True(cancellationToken.IsCancellationRequested);
    }

    [Fact]
    public void BackupEnginePattern_ReassignToken_ReflectsCallerCancellation()
    {
        using CancellationTokenSource callerCts = new();
        CancellationToken cancellationToken = callerCts.Token;

        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cancellationTokenSource.Token;

        Assert.False(cancellationToken.IsCancellationRequested);

        callerCts.Cancel();
        Assert.True(cancellationToken.IsCancellationRequested);
    }

    [Fact]
    public void BackupEnginePattern_ThrowIfCancellationRequested_ThrowsOnCancel()
    {
        using CancellationTokenSource callerCts = new();
        CancellationToken cancellationToken = callerCts.Token;

        using CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cancellationTokenSource.Token;

        cancellationTokenSource.Cancel();

        Assert.Throws<OperationCanceledException>(() => cancellationToken.ThrowIfCancellationRequested());
    }
}
