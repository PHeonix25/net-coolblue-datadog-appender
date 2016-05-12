using System;

using log4net;
using log4net.Config;

namespace DataDogLog4NetAppender.Console
{
    class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            var repo = LogManager.GetRepository();
            var appenders = repo.GetAppenders();
            System.Console.Write(appenders.Length);

            _log.Debug("This is a debug message");
            _log.Info("This is an info message");
            _log.Warn("This is a warning message");
            _log.Error("This is just a simple error");
            try
            {
                int zero = 0;
                int a = 1 / zero;
            }
            catch(Exception ex)
            {
                _log.Error("This is an error with an exception", ex);
            }
        }
    }
}