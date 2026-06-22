namespace BMTP3.Core4.Helpers;

/// <summary>
/// A synchronous <see cref="IProgress{T}"/> that executes an <see cref="Action{T}"/>
/// for each reported value without any <see cref="SynchronizationContext"/> or
/// <see cref="ThreadPool"/> dispatch.
/// </summary>
/// <remarks>
/// Use this when progress reporting involves side-effects or state mutation
/// rather than a pure value transform. For transforms, see <see cref="TransformProgress{TInner,TOuter}"/>.
/// </remarks>
public sealed class ActionProgress<T> : IProgress<T>
{
	private readonly Action<T> _action;

	public ActionProgress(Action<T> action)
		=> _action = action;

	public void Report(T value)
		=> _action(value);
}

/// <summary>
/// Extension methods for <see cref="ActionProgress{T}"/>.
/// </summary>
public static class ActionProgressExtensions
{
	/// <summary>
	/// Wraps an <see cref="Action{T}"/> as a synchronous <see cref="IProgress{T}"/>.
	/// </summary>
	public static IProgress<T> ToProgress<T>(this Action<T> action)
		=> new ActionProgress<T>(action);
}
