using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Plugable.io
{
    public sealed class PluginsManager : IDisposable
    {
        #region Variables
        private readonly IList<IProtocolProvider> _providers;
        #endregion

        #region Properties
        public IList<IProtocolProvider> Providers
        {
            get { return _providers; }
        }
        #endregion

        #region Events
        public EventHandler<Events.LogEventArgs> LogEvent { get; set; }
        #endregion

        #region Constructor
        public PluginsManager()
        {
            _providers = new List<IProtocolProvider>();
        }

        public PluginsManager(IList<IProtocolProvider> providers, string[] args)
            : this()
        {
            Use(providers, args);
        }
        #endregion

        #region Use
        public void Use(IProtocolProvider provider)
        {
            _providers.Add(provider);
        }

        public void Use(IList<IProtocolProvider> providers, string[] args)
        {
            if (providers != null)
            {
                foreach (var plugin in providers)
                {
                    if (plugin != null)
                    {
                        Use(plugin);
                        try
                        {
                            plugin.Initialize(args, this);
                        }
                        catch (FileNotFoundException ex)
                        {
                            var assemblyFile = (ex.FileName.Contains(','))
                                ? ex.FileName.Substring(0, ex.FileName.IndexOf(','))
                                : ex.FileName;

                            var current = Directory
                              .GetFiles(Path.Combine(Environment.CurrentDirectory, "extensions"))
                              .Where(path => Regex.IsMatch(Path.GetFileName(path), plugin.getName() + "_(.*).zip"))
                              .FirstOrDefault();

                            Plugable.io.Extensions.ResolveAssembly(current, plugin, this, args);
                        }
                    }
                }
            }
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Stop();
        }
        #endregion

        #region Start
        public void Start()
        {
            foreach (var provider in _providers)
            {
                provider.Start();
            }
        }
        #endregion

        #region Stop
        public void Stop()
        {
            foreach (var provider in _providers)
            {
                provider.Stop();
            }
        }
        #endregion

        #region EventLog
        public void EventLog(object sender, Events.LogEventArgs args)
        {
            LogEvent?.Invoke(sender, args);
        }
        #endregion

        #region GetProvider
        public IProtocolProvider GetProvider(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                try
                {
                    if (_providers != null)
                        return _providers.FirstOrDefault(s => s.getName().Equals(name));
                }
                catch (Exception e) { Extensions.Error(e.Message); }
            }

            return null;
        }
        #endregion

        #region MessageProvider
        public void MessageProvider(string name, dynamic message,
            [CallerMemberName] string callingMethod = null,
            [CallerFilePath] string callingFilePath = null,
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            IProtocolProvider provider = GetProvider(name);
            if (provider != null)
            {
                try
                {
                    provider.Message(new
                    {
                        Data = message,
                        Method = callingMethod,
                        File = Path.GetFileNameWithoutExtension(callingFilePath),
                        Line = callingFileLineNumber
                    });
                }
                catch (Exception e) { Extensions.Error(e.Message); }
            }
        }
        #endregion

    }
}
