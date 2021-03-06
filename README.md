WebProt
======

![](/images/version.svg) ![](/images/license.svg)

A light plugin manager that loads libraries from a compressed file. 


## Clone with examples
```sh
git clone --recurse-submodules https://github.com/fmwasekaga/WebProt.git
```

| Example | Source |
| ------ | ------ |
| WebProt.WebHttp.Provider | [Plugins/WebProt.WebHttp.Provider](https://github.com/fmwasekaga/WebProt.WebHttp.Provider) |
| WebProt.WebSocket.Provider | [Plugins/WebProt.WebSocket.Provider](https://github.com/fmwasekaga/WebProt.WebSocket.Provider) |
| WebProt.Provider.Plugin.Console | [Plugins/WebProt.Provider.Plugin.Console](https://github.com/fmwasekaga/WebProt.Provider.Plugin.Console) |
| WebProt.Provider.Plugin.Ping | [Plugins/WebProt.Provider.Plugin.Ping](https://github.com/fmwasekaga/WebProt.Provider.Plugin.Ping) |

## Example
This loads all .zip plugins from an "extension" directory
```c#
 var server = new PluginsManager(
              System.IO.Path.Combine(Environment.CurrentDirectory, "extensions")
                   .GetPlugable(typeof(Program).Assembly.GetName().Name)
			.OfType<IProtocolProvider>().ToList(), args);
              server.Start();
```

IPlugable is needed to be recognized by the plugin manager as a plugin and IProtocolProvider
is used to initialize it

```c#
using Plugable.io;
using Plugable.io.Interfaces;
using System;
using System.Reflection;

namespace Extensions
{
    public class Plugin : IPlugable, IProtocolProvider
    {
        public void Initialize(string[] args, PluginsManager server)
        {
            if (args.Length > 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            }
        }

        public string getName()
        {
            return GetType().Assembly.GetName().Name;
        }

        public string getVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }

        public void Message(dynamic message)
        {

        }

        public Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            return null;
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
           
        }
    }
}
```