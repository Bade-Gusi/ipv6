using System.Text;

namespace ipv6
{
    internal static class Program
    {
        private static Mutex? _singleInstance;

        [STAThread]
        static void Main(string[] args)
        {
            bool checkMode = args.Contains("--check") || args.Contains("-c");

            _singleInstance = new Mutex(true, @"Local\IPv6Manager_SingleInstance", out bool isFirst);
            if (!isFirst)
            {
                if (!checkMode)
                    MessageBox.Show("IPv6 管理工具已经在运行了。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _singleInstance.Dispose();
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => LogCrash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash(e.ExceptionObject as Exception);

            try
            {
                Application.Run(new Form1(checkMode));
            }
            catch (Exception ex)
            {
                LogCrash(ex);
            }
            finally
            {
                try { _singleInstance.ReleaseMutex(); } catch { }
                _singleInstance.Dispose();
            }
        }

        private static void LogCrash(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "IPv6Manager");
                Directory.CreateDirectory(dir);
                File.AppendAllText(
                    Path.Combine(dir, "crash.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n",
                    Encoding.UTF8);
            }
            catch { }

            try
            {
                MessageBox.Show(
                    "程序发生未处理的错误：\n" + ex.Message +
                    "\n\n日志已写入 %LOCALAPPDATA%\\IPv6Manager\\crash.log",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
