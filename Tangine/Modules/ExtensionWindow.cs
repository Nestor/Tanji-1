using System.Windows;

using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public class ExtensionWindow : Window, IModule
    {
        private readonly TService _service;

        private IInstaller _installer;
        IInstaller IModule.Installer
        {
            get => (_service?.Installer ?? _installer);
            set
            {
                _installer = value;
                if (_service != null)
                {
                    _service.Installer = value;
                }
            }
        }

        public HGame Game => _service.Installer.Game;
        public HGameData GameData => _service.Installer.GameData;
        public IHConnection Connection => _service.Installer.Connection;

        public ExtensionWindow()
            : this(null)
        { }
        public ExtensionWindow(TService service)
        {
            _service = (service ?? new ExtensionService(this));
            _service.Installer = _installer;
        }

        void IModule.Synchronize(HGame game)
        {
            Synchronize(game);
            _service.Synchronize(game);
        }
        public virtual void Synchronize(HGame game)
        { }

        void IModule.Synchronize(HGameData gameData)
        {
            Synchronize(gameData);
            _service.Synchronize(gameData);
        }
        public virtual void Synchronize(HGameData gameData)
        { }

        void IModule.HandleOutgoing(DataInterceptedEventArgs e)
        {
            HandleOutgoing(e);
            _service.HandleOutgoing(e);
        }
        public virtual void HandleOutgoing(DataInterceptedEventArgs e)
        { }

        void IModule.HandleIncoming(DataInterceptedEventArgs e)
        {
            HandleIncoming(e);
            _service.HandleIncoming(e);
        }
        public virtual void HandleIncoming(DataInterceptedEventArgs e)
        { }

        public void Dispose()
        {
            Dispose(true);
        }
        public virtual void Dispose(bool disposing)
        { }

        private sealed class ExtensionService : TService
        {
            public ExtensionService(object container)
                : base(container)
            { }
        }
    }
}