namespace Plugable.io
{
    public interface IProtocolProvider
    {
        string getName();

        void Initialize(string[] args, PluginsManager server);

        void Start();

        void Stop();

        void Message(dynamic message);
    }
}
