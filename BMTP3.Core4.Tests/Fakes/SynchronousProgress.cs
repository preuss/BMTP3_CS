namespace BMTP3.Core4.Tests.Fakes;

internal sealed class SynchronousProgress<T> : IProgress<T>
{
	private readonly List<T> _target;
	public SynchronousProgress(List<T> target) => _target = target;
	public void Report(T value) => _target.Add(value);
}
