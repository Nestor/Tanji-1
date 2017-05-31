using System;
using System.Windows;
using System.Collections.Generic;

using Tanji.Network;
using Tanji.Services;
using Tanji.Windows.Logger;
using Tanji.Services.Modules;
using Tanji.Services.Connection;

using Tangine.Habbo;
using Tangine.Modules;
using Tangine.Network;

namespace Tanji
{
    public partial class App : Application, IMaster
    {
        private readonly List<IHaltable> _haltables;
        private readonly List<ISynchronizer> _synchronizers;
        private readonly SortedList<int, IReceiver> _receivers;

        IHConnection IInstaller.Connection
        {
            get { return Connection; }
        }
        public HConnection Connection { get; }

        public HGame Game { get; set; }
        public HGameData GameData { get; set; }

        public static IMaster Master { get; private set; }

        public App()
        {
            _haltables = new List<IHaltable>();
            _synchronizers = new List<ISynchronizer>();
            _receivers = new SortedList<int, IReceiver>();

            GameData = new HGameData();
            Connection = new HConnection();

            Connection.Connected += Connected;
            Connection.DataOutgoing += HandleData;
            Connection.DataIncoming += HandleData;
            Connection.Disconnected += Disconnected;
        }

        public void AddHaltable(IHaltable haltable)
        {
            _haltables.Add(haltable);
        }
        public void AddReceiver(IReceiver receiver)
        {
            int rank = -1;
            switch (receiver.GetType().Name)
            {
                case nameof(ModulesViewModel): rank = 0; break;
                case nameof(ConnectionViewModel): rank = 1; break;

                case nameof(LoggerViewModel): rank = 10; break;

                default:
                throw new ArgumentException("Unrecognized receiver object.", nameof(receiver));
            }
            _receivers.Add(rank, receiver);
        }
        public void AddSynchronizer(ISynchronizer synchronizer)
        {
            _synchronizers.Add(synchronizer);
        }

        public void Synchronize(HGame game)
        {
            foreach (ISynchronizer synchronizer in _synchronizers)
            {
                synchronizer.Synchronize(game);
            }
        }
        public void Synchronize(HGameData gameData)
        {
            foreach (ISynchronizer synchronizer in _synchronizers)
            {
                synchronizer.Synchronize(gameData);
            }
        }

        private void Connected(object sender, EventArgs e)
        {
            foreach (IHaltable haltable in _haltables)
            {
                haltable.Dispatcher.Invoke(haltable.Restore);
            }
        }
        private void Disconnected(object sender, EventArgs e)
        {
            foreach (IHaltable haltable in _haltables)
            {
                haltable.Dispatcher.Invoke(haltable.Halt);
            }
        }
        private void HandleData(object sender, DataInterceptedEventArgs e)
        {
            if (_receivers.Count == 0) return;
            foreach (IReceiver receiver in _receivers.Values)
            {
                if (!receiver.IsReceiving) continue;
                if (e.IsOutgoing)
                {
                    receiver.HandleOutgoing(e);
                }
                else
                {
                    receiver.HandleIncoming(e);
                }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Master = this;
            base.OnStartup(e);
        }

        public static void Display(Exception exception, string header = null)
        {
            string messsage = header;
            if (!string.IsNullOrWhiteSpace(messsage))
            {
                messsage += "\r\n\r\nException: ";
            }
            MessageBox.Show((messsage + exception), "Tanji - Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}