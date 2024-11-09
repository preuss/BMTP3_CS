namespace BMTP3.Core.Configuration {
	public class CancellationTokenGenerator : IDisposable {
		private readonly CancellationTokenSource _cancellationTokenSource;
		public CancellationTokenGenerator(CancellationTokenSource cancellationTokenSource) {
			_cancellationTokenSource = cancellationTokenSource;
		}
		public CancellationToken NewToken() => _cancellationTokenSource.Token;
		public CancellationTokenSource GetCancellationTokenSource() => _cancellationTokenSource;
		public void Dispose() {
			_cancellationTokenSource.Dispose();
		}
	}
}
