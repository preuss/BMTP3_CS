using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Consoles {
	internal class CliConsole {
		public void WriteLine() {
			AnsiConsole.WriteLine();
		}
		public void WriteLine(string value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(int value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, int value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(uint value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, uint value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(long value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, long value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(ulong value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, ulong value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(float value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, float value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(double value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, double value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(decimal value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, decimal value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(bool value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, bool value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(char value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, char value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(char[] value) {
			AnsiConsole.WriteLine(value);
		}
		public void WriteLine(IFormatProvider provider, char[] value) {
			AnsiConsole.WriteLine(provider, value);
		}
		public void WriteLine(string format, params object[] args) {
			AnsiConsole.WriteLine(format, args);
		}
		public void WriteLine(IFormatProvider provider, string format, params object[] args) {
			AnsiConsole.WriteLine(provider, format, args);
		}
	}
}
