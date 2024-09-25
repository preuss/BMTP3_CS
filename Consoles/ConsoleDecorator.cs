using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BMTP3_CS.Consoles {
	internal class ConsoleDecorator : TextWriter {
		private TextWriter originalConsoleStream;
		private TextWriter? messageFileWriter;

		public override Encoding Encoding => originalConsoleStream.Encoding;

		public ConsoleDecorator(TextWriter consoleTextWriter, string? messageFile = null) {
			originalConsoleStream = consoleTextWriter;
			messageFileWriter = messageFile != null ? new StreamWriter(messageFile, true, Encoding.UTF8) { AutoFlush = true } : null;
		}

		public override void Write(char value) {
			originalConsoleStream.Write(value);
			if(messageFileWriter != null) {
				messageFileWriter.Write(value);
			}
		}
		public override void WriteLine(string? value) {
			originalConsoleStream.WriteLine(value);

			string currentTime = DateTime.Now.ToString("yyyyMMddTHHmm:ss") + "." + DateTime.Now.Millisecond.ToString().PadLeft(3, '0');
			string msg = $"{currentTime}: {value}";

			// Fire event here with value
			if(messageFileWriter != null) {
				messageFileWriter.WriteLine(msg);
			}
		}

		public static TextWriter SetupConsole(string? messageFile = null) {
			ConsoleDecorator consoleDecorator = new ConsoleDecorator(Console.Out, messageFile);
			Console.SetOut(consoleDecorator);
			return consoleDecorator;
		}

		public static void RestoreConsole() {
			Console.SetOut(Console.Out);
		}
	}
}
