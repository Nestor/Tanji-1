using System.Windows.Forms;

using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public class ExtensionForm : Form, IModule
    {
        private readonly TService _service;

        private IInstaller _installer;
        IInstaller IModule.Installer
        {
            get { return (_service?.Installer ?? _installer); }
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

        public ExtensionForm()
            : this(null)
        { }
        public ExtensionForm(TService service)
        {
            _service = (service ?? new TService(this));
            _service.Installer = _installer;
        }

        void IModule.ModifyGame(HGame game)
        {
            ModifyGame(game);
            _service.ModifyGame(game);
        }
        public virtual void ModifyGame(HGame game)
        { }

        void IModule.ModifyGameData(HGameData gameData)
        {
            ModifyGameData(gameData);
            _service.ModifyGameData(gameData);
        }
        public virtual void ModifyGameData(HGameData gameData)
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
    }
}