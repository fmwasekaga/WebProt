using Plugable.io;
using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace WebProt
{
    class Program : ServiceBase
    {
        private readonly PluginsManager server = new PluginsManager();

        static void Main(string[] args)
        {
            var service = new Program();
            if (Environment.UserInteractive)
            {
                service.OnStart(args);

                ManualResetEvent exitMre = new ManualResetEvent(false);
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    service.OnStop();

                    e.Cancel = true;
                    exitMre.Set();
                };

                exitMre.WaitOne();
            }
            else
            {
                Run(service);
            }
           
        }

        protected override void OnStart(string[] args)
        {
            #region Initialize Http Server
            Environment.SetEnvironmentVariable("MONO_TLS_PROVIDER", "btls");
            
            server.Use(
                System.IO.Path.Combine(Environment.CurrentDirectory, "extensions")
                    .GetPlugable(typeof(Program).Assembly.GetName().Name).OfType<IProtocolProvider>().ToList(), args);
            server.Start();
            Console.WriteLine("----------------------------------------------------------------------------");

            #endregion
        }

        protected override void OnStop()
        {
            server.Dispose();
        }
    }
}
