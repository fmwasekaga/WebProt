using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Plugable.io.Interfaces
{
    public interface IPlugable
    {
        string getName();
        string getVersion();
        Assembly ResolveAssembly(object sender, ResolveEventArgs args);
    }
}
