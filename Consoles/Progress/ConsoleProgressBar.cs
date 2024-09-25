using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace BMTP3_CS.Consoles.Progress {
	/// <summary>
	/// See https://stackoverflow.com/questions/24918768/progress-bar-in-console-application
	/// // Example:
	/// <code>
	///    var total = 100;
	///    IProgressBar progressBar = new ConsoleProgressBar(total);
	///    for (var i = 0; i <= total; i++)
	///    {
	///    progressBar.ShowProgress(i);
	///    Thread.Sleep(50);
	///    }
	///    Thread.Sleep(500);
	///    Console.Clear();
	///    total = 9999;
	///    progressBar = new ConsoleProgressBar(total);
	///    for(var i = 0; i <= total; i++) {
	///    progressBar.ShowProgress(i);
	///    }
	/// </code>
	/// </summary>
	public class ConsoleProgressBar : IProgressBar {
		private const ConsoleColor ForeColor = ConsoleColor.Green;
		private const ConsoleColor BkColor = ConsoleColor.Gray;
		private const int DefaultWidthOfBar = 32;
		private const int TextMarginLeft = 3;

		private readonly int _total;
		private readonly int _widthOfBar;

		public ConsoleProgressBar(int total, int widthOfBar = DefaultWidthOfBar) {
			_total = total;
			_widthOfBar = widthOfBar;
		}

		private bool _intited;
		public void Init() {
			_lastPosition = 0;

			//Draw empty progress bar
			Console.CursorVisible = false;
			Console.CursorLeft = 0;
			Console.Write("["); //start
			Console.CursorLeft = _widthOfBar;
			Console.Write("]"); //end
			Console.CursorLeft = 1;

			//Draw background bar
			for(var position = 1; position < _widthOfBar; position++) //Skip the first position which is "[".
			{
				Console.BackgroundColor = BkColor;
				Console.CursorLeft = position;
				Console.Write(" ");
			}
		}

		public void ShowProgress(int currentCount) {
			if(!_intited) {
				Init();
				_intited = true;
			}
			DrawTextProgressBar(currentCount);
		}

		private int _lastPosition;

		public void DrawTextProgressBar(int currentCount) {
			//Draw current chunk.
			var position = currentCount * _widthOfBar / _total;
			if(position != _lastPosition) {
				_lastPosition = position;
				Console.BackgroundColor = ForeColor;
				Console.CursorLeft = position >= _widthOfBar ? _widthOfBar - 1 : position;
				Console.Write(" ");
			}

			//Draw totals
			Console.CursorLeft = _widthOfBar + TextMarginLeft;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.Write(currentCount + " of " + _total + "    "); //blanks at the end remove any excess
		}
	}

	public interface IProgressBar {
		public void ShowProgress(int currentCount);
	}
}
