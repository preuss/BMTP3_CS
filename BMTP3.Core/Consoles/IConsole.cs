namespace BMTP3.Core.Consoles {
	public interface IConsole {
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