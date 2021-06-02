using Plugable.io;
using System;
using System.Linq;
using System.Threading;

namespace WebProt
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Initialize Http Server
            ManualResetEvent exitMre = new ManualResetEvent(false);
            Environment.SetEnvironmentVariable("MONO_TLS_PROVIDER", "btls");

            var server = new PluginsManager(
                System.IO.Path.Combine(Environment.CurrentDirectory, "extensions")
                    .GetPlugable(typeof(Program).Assembly.GetName().Name).OfType<IProtocolProvider>().ToList(), args);
            server.Start();
            Console.WriteLine("----------------------------------------------------------------------------");

            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                server.Dispose();
                e.Cancel = true;
                exitMre.Set();
            };

            exitMre.WaitOne();
            #endregion
        }
    }
}
