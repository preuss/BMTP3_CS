using BMTP3.Core4.Models;
using System.Text;

namespace BMTP3.Core4.Tests.Fakes;

internal sealed class FakeContent : IContent
{
	private readonly byte[] _data;

	public FakeContent(string text)
	{
		_data = Encoding.UTF8.GetBytes(text);
		Length = (ulong)_data.Length;
	}

	public FakeContent(byte[] data)
	{
		_data = data;
		Length = (ulong)_data.Length;
	}

	public ulong Length { get; }

	public void Dispose() { }

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	public Stream OpenRead() => new MemoryStream(_data);

	public Task<Stream> OpenReadAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		return Task.FromResult<Stream>(new MemoryStream(_data));
	}
}
