using System;

using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public interface IModule : IDisposable
    {
        IInstaller Installer { get; set; }

        void ModifyGame(HGame game);
        void ModifyGameData(HGameData gameData);

        void HandleOutgoing(DataInterceptedEventArgs e);
        void HandleIncoming(DataInterceptedEventArgs e);
    }
}