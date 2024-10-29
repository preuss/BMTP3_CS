using System.Text;

namespace BMTP3.Core.IO.Consoles {
	public interface IConsole {
		TextReader In { get; }
		TextWriter Out { get; }
		TextWriter Error { get; }
		Encoding InputEncoding { get; set; }
		Encoding OutputEncoding { get; set; }
		bool KeyAvailable { get; }
		bool IsInputRedirected { get; }
		bool IsOutputRedirected { get; }
		bool IsErrorRedirected { get; }
		int CursorSize { get; set; }
		int BufferWidth { get; set; }
		int BufferHeight { get; set; }
		int WindowLeft { get; set; }
		int WindowTop { get; set; }
		int WindowWidth { get; set; }
		int WindowHeight { get; set; }
		int LargestWindowWidth { get; }
		int LargestWindowHeight { get; }
		bool CursorVisible { get; set; }
		int CursorLeft { get; set; }
		int CursorTop { get; set; }
		bool NumberLock { get; }
		bool CapsLock { get; }
		ConsoleColor BackgroundColor { get; set; }
		ConsoleColor ForegroundColor { get; set; }
		string Title { get; set; }
		event ConsoleCancelEventHandler? CancelKeyPress { add { Console.CancelKeyPress += value; } remove { Console.CancelKeyPress -= value; } }
		bool TreatControlCAsInput { get; set; }

		Stream OpenStandardInput();
		Stream OpenStandardInput(int bufferSize);
		Stream OpenStandardOutput();
		Stream OpenStandardOutput(int bufferSize);
		Stream OpenStandardError();
		Stream OpenStandardError(int bufferSize);
		void SetIn(TextReader newIn);
		void SetOut(TextWriter newOut);
		void SetError(TextWriter newError);
		int Read();
		string? ReadLine();
		void SetBufferSize(int width, int height);
		void SetWindowPosition(int left, int top);
		void SetWindowSize(int width, int height);
		(int Left, int Top) GetCursorPosition();
		void ResetColor();
		ConsoleKeyInfo ReadKey();
		ConsoleKeyInfo ReadKey(bool intercept);
		void Beep();
		void Beep(int frequency, int duration);
		void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop);
		void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor);
		void Clear();
		void SetCursorPosition(int left, int top);
		void WriteLine();
		void WriteLine(string value);
		void WriteLine(int value);
		void WriteLine(IFormatProvider provider, int value);
		void WriteLine(uint value);
		void WriteLine(IFormatProvider provider, uint value);
		void WriteLine(long value);
		void WriteLine(IFormatProvider provider, long value);
		void WriteLine(ulong value);
		void WriteLine(IFormatProvider provider, ulong value);
		void WriteLine(float value);
		void WriteLine(IFormatProvider provider, float value);
		void WriteLine(double value);
		void WriteLine(IFormatProvider provider, double value);
		void WriteLine(decimal value);
		void WriteLine(IFormatProvider provider, decimal value);
		void WriteLine(bool value);
		void WriteLine(IFormatProvider provider, bool value);
		void WriteLine(char value);
		void WriteLine(IFormatProvider provider, char value);
		void WriteLine(char[] value);
		void WriteLine(IFormatProvider provider, char[] value);
		void WriteLine(string format, params object[] args);
		void WriteLine(IFormatProvider provider, string format, params object[] args);
	}
}