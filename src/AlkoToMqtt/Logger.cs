using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlkoToMqtt {

    enum LogLevel {
        Fine = 1,
        Debug = 2,
        Info = 3,
        Error = 4
    }

    internal static class Logger {

        public static LogLevel Level = LogLevel.Debug;

        public static void WriteFine(string msg, params object[] p) {
            Write(LogLevel.Fine, msg, p);
        }

        public static void WriteDebug(string msg, params object[] p) {
            Write(LogLevel.Debug, msg, p);
        }

        public static void WriteInfo(string msg, params object[] p) {
            Write(LogLevel.Info, msg, p);
        }

        public static void WriteError(string msg, params object[] p) {
            Write(LogLevel.Error, msg, p);
        }

        private static void Write(LogLevel level, string msg, params object[] p) {
            if (level < Level) {
                return;
            }
            var actualMessage = string.Format(msg, p);
            Console.WriteLine("{0} [{1,-5}] {2}", DateTime.Now, level, actualMessage);
        }
    }
}
