using Tanji.Network;

using Tangine.Habbo;
using Tangine.Modules;

namespace Tanji.Services
{
    public interface IMaster : IInstaller
    {
        new HGame Game { get; set; }
        new HConnection Connection { get; }

        void AddReceiver(IReceiver receiver);
        void AddHaltable(IHaltable haltable);
    }
}