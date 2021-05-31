
namespace Plugable.io
{
    public interface IProtocolPlugin
    {
        void Initialize(string[] args, PluginsManager parent, dynamic server);
    }
}
