using Microsoft.Extensions.Logging;
using ZLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMTP3_CS.Consoles;

namespace BMTP3_CS {
	internal static class LogManager {
		private static ILoggerFactory logLoggerFactory = default!;
		private static ILogger globalLogger = default!;

		private static ILoggerFactory messageLoggerFactory = default!;
		private static TextWriter messageWriter = default!;

		public static void Initialize() {
			Initialize("Global");
		}

		/// <summary> Blank Method which will force constructor of static class </summary>
		public static void Initialize(string globalCategoryName = "Global", LogLevel logLevel = LogLevel.Trace, bool logToConsole = false) {
			logLoggerFactory = initializeLogFactory("Log.log", logLevel, logToConsole);
			LogManager.globalLogger = logLoggerFactory.CreateLogger(globalCategoryName);
			messageLoggerFactory = initializeMessageFactory();
			messageWriter = initializeMessageWriter("Message.txt");
		}

		private static ILoggerFactory initializeLogFactory(string logFilePath, LogLevel logLevel, bool logToConsole) {
			return LoggerFactory.Create(logging => {
				logging.SetMinimumLevel(LogLevel.Trace);
				logging.AddZLoggerFile(logFilePath, options => {
					options.UsePlainTextFormatter(formatter => {
						//formatter.SetPrefixFormatter($"{0:utc-longdate} [{1:short}]", (template, info) => template.Format(info.Timestamp, info.LogLevel));
						formatter.SetPrefixFormatter($"{0:longdate} [{1:short}]", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
						formatter.SetSuffixFormatter($" ({0})", (in MessageTemplate template, in LogInfo info) => template.Format(info.Category));
						formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));

						//formatter.SetPrefixFormatter($"{info.Timestamp:utc-longdate} [{info.LogLevel:short}]", (template, info) => template.Format(info.Timestamp, info.LogLevel));
					});
				});
				//logging.AddZLoggerFile(logFilePath);
				if(logToConsole) {
					logging.AddZLoggerConsole(options => {
						options.UsePlainTextFormatter(formatter => {
							//formatter.SetPrefixFormatter($"{0:utc-longdate} [{1:short}]", (template, info) => template.Format(info.Timestamp, info.LogLevel));
							formatter.SetPrefixFormatter($"{0:utc-longdate} [{1:short}]", (in MessageTemplate template, in LogInfo info) => template.Format(info.Timestamp, info.LogLevel));
							formatter.SetSuffixFormatter($" ({0})", (in MessageTemplate template, in LogInfo info) => template.Format(info.Category));
							formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));

							//formatter.SetPrefixFormatter($"{info.Timestamp:utc-longdate} [{info.LogLevel:short}]", (template, info) => template.Format(info.Timestamp, info.LogLevel));
						});
					});
				}
			});
		}

		private static ILoggerFactory initializeMessageFactory(string? messageFile = null) {
			return LoggerFactory.Create(logging => {
				logging.SetMinimumLevel(LogLevel.Trace);
				logging.AddZLoggerConsole();
				if(messageFile != null) {
					logging.AddZLoggerFile(messageFile);
				}
			});
		}

		private static TextWriter initializeMessageWriter(string? messageFile = null) {
			return ConsoleDecorator.SetupConsole(messageFile);
			//return Console.Out;
		}

		public static ILogger Logger => globalLogger;

		// standard LoggerFactory caches logger per category so no need to cache in this manager
		public static ILogger<T> GetLogger<T>() where T : class => logLoggerFactory.CreateLogger<T>();
		public static ILogger GetLogger(string categoryName) => logLoggerFactory.CreateLogger(categoryName);

		public static ILogger GetMessageLogger<T>() where T : class => messageLoggerFactory.CreateLogger<T>();

		public static ILogger GetMessageLogger(string categoryName) => messageLoggerFactory.CreateLogger(categoryName);

		public static TextWriter GetMessageWriter() => messageWriter;
	}
}
