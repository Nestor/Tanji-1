using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public interface IInstaller
    {
        HGame Game { get; }
        HGameData GameData { get; }
        IHConnection Connection { get; }
    }
}