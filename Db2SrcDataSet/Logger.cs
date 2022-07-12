using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Db2Source
{
    public class Logger
    {
        public static readonly Logger Default = new Logger();
        public string LogDir { get; }
        private string _lockPath;
        public Logger()
        {
            LogDir = Path.Combine(Db2SourceContext.AppDataDir, "Log");
            _lockPath = Path.Combine(LogDir, "log_lock");
        }
        public Logger(string logDirectory)
        {
            LogDir = Path.Combine(Db2SourceContext.AppDataDir, logDirectory);
            _lockPath = Path.Combine(LogDir, "log_lock");
        }
        //private object _logLock = new object();

        public void Log(string message)
        {
            LogWriter writer = new LogWriter(this, message);
            writer.Execute();
        }

        internal class LogWriter
        {
            private Logger _logger;
            //private static readonly string LogDir = Path.Combine(Db2SourceContext.AppDataDir, "Log");
            //private static readonly string LockPath = Path.Combine(Db2SourceContext.AppDataDir, "log_lock");
            private string LogPath;
            private string Message;
            internal LogWriter(Logger logger, string message)
            {
                _logger = logger;
                DateTime dt = DateTime.Now;
                LogPath = Path.Combine(_logger.LogDir, string.Format("Log{0:yyyyMMdd}.txt", dt));
                Message = string.Format("[{0:HH:mm:ss}] {1}", dt, message);
            }

            private void DoExecute()
            {
                FileStream lockStream = null;
                while (lockStream == null)
                {
                    try
                    {
                        lockStream = new FileStream(_logger._lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(0);
                    }
                    catch { throw; }
                }
                try
                {
                    Directory.CreateDirectory(_logger.LogDir);
                    using (StreamWriter writer = new StreamWriter(LogPath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(Message);
                        writer.Flush();
                    }
                }
                finally
                {
                    lockStream.Close();
                    lockStream.Dispose();
                }
            }

            internal void Execute()
            {
                Task t = Task.Run(new Action(DoExecute));
            }
        }
    }
}
