namespace BMTP3.Core4.Helpers;

/// <summary>
/// A synchronous <see cref="IProgress{T}"/> that transforms reported values
/// from <typeparamref name="TInner"/> to <typeparamref name="TOuter"/> before
/// forwarding to an inner progress sink.
/// </summary>
/// <remarks>
/// Unlike <see cref="Progress{T}"/>, this class does NOT capture a
/// <see cref="SynchronizationContext"/> and never dispatches via
/// <see cref="ThreadPool"/>. All calls to <see cref="Report"/> are synchronous,
/// making it suitable for use in library code where consistency matters.
/// </remarks>
public sealed class TransformProgress<TInner, TOuter> : IProgress<TInner>
{
	private readonly IProgress<TOuter> _inner;
	private readonly Func<TInner, TOuter> _transform;

	public TransformProgress(IProgress<TOuter> inner, Func<TInner, TOuter> transform)
	{
		ArgumentNullException.ThrowIfNull(inner);
		ArgumentNullException.ThrowIfNull(transform);
		_inner = inner;
		_transform = transform;
	}

	public void Report(TInner value) => _inner.Report(_transform(value));
}

/// <summary>
/// Extension methods for synchronous progress transformations.
/// </summary>
public static class ProgressExtensions
{
	/// <summary>
	/// Creates a synchronous <see cref="IProgress{TInner}"/> that transforms each
	/// reported value via <paramref name="transform"/> and forwards it to
	/// <paramref name="progress"/>.
	/// </summary>
	public static IProgress<TInner> Transform<TInner, TOuter>(
		this IProgress<TOuter> progress,
		Func<TInner, TOuter> transform)
		=> new TransformProgress<TInner, TOuter>(progress, transform);
}
