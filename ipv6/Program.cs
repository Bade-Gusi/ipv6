namespace ipv6
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            bool checkMode = args.Contains("--check") || args.Contains("-c");
            Application.Run(new Form1(checkMode));
        }
    }
}