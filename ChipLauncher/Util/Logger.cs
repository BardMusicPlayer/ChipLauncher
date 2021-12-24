using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChipLauncher
{
    public class Logger
    {
        public enum Verbosity
        {
            Verbose,
            Debug,
            Info,
            Warning,
            Error
        };

        public static void V(string format, params object[] args) { Instance.Log(Verbosity.Verbose, format, args); }
        public static void D(string format, params object[] args) { Instance.Log(Verbosity.Debug, format, args); }
        public static void I(string format, params object[] args) { Instance.Log(Verbosity.Info, format, args); }
        public static void W(string format, params object[] args) { Instance.Log(Verbosity.Warning, format, args); }
        public static void E(string format, params object[] args) { Instance.Log(Verbosity.Error, format, args); }

        public static void SetMinVerbosity(Verbosity verbosity)
        {
            Instance.MinVerbosity = verbosity;
        }

        private static Logger Instance { get { return __arbitur__.Value; } }
        private static readonly Lazy<Logger> __arbitur__ = new Lazy<Logger>(() => new Logger());

        private Verbosity MinVerbosity;

        protected void Log(Verbosity verbosity, string format, params object[] args)
        {
            format = "[" + verbosity.ToString() + "] - " + format;
            string output = string.Format(format, args);

            if (verbosity >= this.MinVerbosity)
                Console.WriteLine(output);
        }
    }
}
