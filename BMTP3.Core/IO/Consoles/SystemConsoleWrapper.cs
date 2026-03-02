using System.Runtime.Versioning;
using System.Text;

namespace BMTP3.Core.IO.Consoles {
	[SupportedOSPlatform("windows")]
	internal class SystemConsoleWrapper : IConsole {
		TextReader IConsole.In => Console.In;
		TextWriter IConsole.Out => Console.Out;
		TextWriter IConsole.Error => Console.Error;
		bool IConsole.KeyAvailable => Console.KeyAvailable;
		bool IConsole.IsInputRedirected => Console.IsInputRedirected;
		bool IConsole.IsOutputRedirected => Console.IsOutputRedirected;
		bool IConsole.IsErrorRedirected => Console.IsErrorRedirected;
		int IConsole.CursorSize { get => Console.CursorSize; set => Console.CursorSize = value; }
		int IConsole.BufferWidth { get => Console.BufferWidth; set => Console.BufferWidth = value; }
		int IConsole.BufferHeight { get => Console.BufferHeight; set => Console.BufferHeight = value; }
		int IConsole.WindowLeft { get => Console.WindowLeft; set => Console.WindowLeft = value; }
		int IConsole.WindowTop { get => Console.WindowTop; set => Console.WindowTop = value; }
		int IConsole.WindowWidth { get => Console.WindowWidth; set => Console.WindowWidth = value; }
		int IConsole.WindowHeight { get => Console.WindowHeight; set => Console.WindowHeight = value; }
		int IConsole.LargestWindowWidth => Console.LargestWindowWidth;
		int IConsole.LargestWindowHeight => Console.LargestWindowHeight;
		bool IConsole.CursorVisible { get => Console.CursorVisible; set => Console.CursorVisible = value; }
		int IConsole.CursorLeft { get => Console.CursorLeft; set => Console.CursorLeft = value; }
		int IConsole.CursorTop { get => Console.CursorTop; set => Console.CursorTop = value; }
		bool IConsole.NumberLock => Console.NumberLock;
		bool IConsole.CapsLock => Console.CapsLock;
		ConsoleColor IConsole.BackgroundColor { get => Console.BackgroundColor; set => Console.BackgroundColor = value; }
		ConsoleColor IConsole.ForegroundColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }
		string IConsole.Title { get => Console.Title; set => Console.Title = value; }
		bool IConsole.TreatControlCAsInput { get => Console.TreatControlCAsInput; set => Console.TreatControlCAsInput = value; }
		Stream IConsole.OpenStandardInput() => Console.OpenStandardInput();
		Stream IConsole.OpenStandardInput(int bufferSize) => Console.OpenStandardInput(bufferSize);
		Stream IConsole.OpenStandardOutput() => Console.OpenStandardOutput();
		Stream IConsole.OpenStandardOutput(int bufferSize) => Console.OpenStandardOutput(bufferSize);
		Stream IConsole.OpenStandardError() => Console.OpenStandardError();
		Stream IConsole.OpenStandardError(int bufferSize) => Console.OpenStandardError(bufferSize);
		void IConsole.SetIn(TextReader newIn) => Console.SetIn(newIn);
		void IConsole.SetOut(TextWriter newOut) => Console.SetOut(newOut);
		void IConsole.SetError(TextWriter newError) => Console.SetError(newError);
		int IConsole.Read() => Console.Read();
		string? IConsole.ReadLine() => Console.ReadLine();
		void IConsole.SetBufferSize(int width, int height) => Console.SetBufferSize(width, height);
		void IConsole.SetWindowPosition(int left, int top) => Console.SetWindowPosition(left, top);
		void IConsole.SetWindowSize(int width, int height) => Console.SetWindowSize(width, height);
		(int Left, int Top) IConsole.GetCursorPosition() => Console.GetCursorPosition();
		void IConsole.ResetColor() => Console.ResetColor();
		ConsoleKeyInfo IConsole.ReadKey() => Console.ReadKey();
		ConsoleKeyInfo IConsole.ReadKey(bool intercept) => Console.ReadKey(intercept);
		void IConsole.Beep() => Console.Beep();
		void IConsole.Beep(int frequency, int duration) => Console.Beep(frequency, duration);
		void IConsole.MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop) => Console.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop);
		void IConsole.MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor) => Console.MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, sourceChar, sourceForeColor, sourceBackColor);
		void IConsole.Clear() => Console.Clear();
		void IConsole.SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);
		Encoding IConsole.InputEncoding { get => Console.InputEncoding; set => Console.InputEncoding = value; }
		Encoding IConsole.OutputEncoding { get => Console.OutputEncoding; set => Console.OutputEncoding = value; }
		public void WriteLine() {
			Console.WriteLine();
		}
		public void WriteLine(string value) {
			Console.WriteLine(value);
		}
		public void WriteLine(int value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, int value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(uint value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, uint value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(long value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, long value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(ulong value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, ulong value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(float value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, float value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(double value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, double value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(decimal value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, decimal value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(bool value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, bool value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(char value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, char value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(char[] value) {
			Console.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, char[] value) {
			Console.WriteLine(string.Format(provider, "{0}", value));
		}
		public void WriteLine(string format, params object[] args) {
			Console.WriteLine(format, args);
		}
		public void WriteLine(IFormatProvider provider, string format, params object[] args) {
			Console.WriteLine(string.Format(provider, format, args));
		}
	}
}