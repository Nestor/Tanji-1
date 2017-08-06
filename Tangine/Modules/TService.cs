﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

using Tangine.Habbo;
using Tangine.Network;
using Tangine.Network.Protocol;

namespace Tangine.Modules
{
    public class TService : IModule
    {
        private readonly TService _parent;
        private readonly IModule _container;
        private readonly List<DataCaptureAttribute> _unknownDataAttributes;
        private readonly Dictionary<ushort, List<DataCaptureAttribute>> _outDataAttributes, _inDataAttributes;
        private const BindingFlags TYPE_FLAGS = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        public const int REMOTE_MODULE_PORT = 8055;

        public virtual bool IsStandalone { get; }

        private IInstaller _installer;
        public virtual IInstaller Installer
        {
            get => (_parent?.Installer ?? _installer);
            set => _installer = value;
        }

        public HGame Game => Installer.Game;
        public HGameData GameData => Installer.GameData;
        public IHConnection Connection => Installer.Connection;

        protected TService()
            : this(null)
        { }
        protected TService(TService parent)
            : this(null, parent)
        { }
        protected internal TService(IModule container)
            : this(container, null)
        { }
        private TService(IModule container, TService parent)
        {
            _parent = parent;
            _container = (container ?? this);
            _unknownDataAttributes = (parent?._unknownDataAttributes ?? new List<DataCaptureAttribute>());
            _inDataAttributes = (parent?._inDataAttributes ?? new Dictionary<ushort, List<DataCaptureAttribute>>());
            _outDataAttributes = (parent?._outDataAttributes ?? new Dictionary<ushort, List<DataCaptureAttribute>>());

            Installer = _container.Installer;
            IsStandalone = (parent != null ? false : _container.IsStandalone);
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime) return;

            foreach (MethodInfo method in GetTypes(_container.GetType()).SelectMany(t => t.GetMethods(TYPE_FLAGS)))
            {
                foreach (var dataCaptureAtt in method.GetCustomAttributes<DataCaptureAttribute>())
                {
                    if (dataCaptureAtt == null) continue;

                    dataCaptureAtt.Method = method;
                    if (_unknownDataAttributes.Any(dca => dca.Equals(dataCaptureAtt))) continue;

                    dataCaptureAtt.Target = _container;
                    if (dataCaptureAtt.Id != null)
                    {
                        AddCallback(dataCaptureAtt, (ushort)dataCaptureAtt.Id);
                    }
                    else _unknownDataAttributes.Add(dataCaptureAtt);
                }
            }

            if (!IsStandalone) return;
            while (true)
            {
                HNode installerNode = HNode.ConnectNewAsync("127.0.0.1", REMOTE_MODULE_PORT).Result;
                if (installerNode != null)
                {
                    installerNode.InFormat = HFormat.EvaWire;
                    installerNode.OutFormat = HFormat.EvaWire;

                    // TODO: Gather info about current assembly.
                    var infoPacketOut = new EvaWirePacket(0);
                    infoPacketOut.Write("1.0.0.0");
                    infoPacketOut.Write("Remote Module Name");
                    infoPacketOut.Write("Remote Module Description");
                    infoPacketOut.Write(0);

                    installerNode.SendPacketAsync(infoPacketOut).Wait();
                    Installer = _container.Installer = new DummyInstaller(_container, installerNode);
                    break;
                }
                else if (MessageBox.Show("Failed to connect to the remote installer, would you like to try again?",
                    "Tangine - Alert!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    Environment.Exit(0);
                }
            }
        }

        public virtual void Synchronize(HGame game)
        {
            foreach (PropertyInfo property in GetTypes(_container.GetType()).SelectMany(t => t.GetProperties(TYPE_FLAGS)))
            {
                var messageIdAtt = property.GetCustomAttribute<MessageIdAttribute>();
                if (string.IsNullOrWhiteSpace(messageIdAtt?.Hash)) continue;

                ushort id = game.GetMessageIds(messageIdAtt.Hash).First();
                property.SetValue(_container, id);
            }

            foreach (DataCaptureAttribute dataCaptureAtt in _unknownDataAttributes)
            {
                if (string.IsNullOrWhiteSpace(dataCaptureAtt.Hash)) continue;

                ushort id = game.GetMessageIds(dataCaptureAtt.Hash).First();
                AddCallback(dataCaptureAtt, id);
            }
        }
        public virtual void Synchronize(HGameData gameData)
        { }

        public virtual void HandleIncoming(DataInterceptedEventArgs e) => HandleData(_inDataAttributes, e);
        public virtual void HandleOutgoing(DataInterceptedEventArgs e) => HandleData(_outDataAttributes, e);
        private void HandleData(IDictionary<ushort, List<DataCaptureAttribute>> callbacks, DataInterceptedEventArgs e)
        {
            if (callbacks.TryGetValue(e.Packet.Id, out List<DataCaptureAttribute> attributes))
            {
                foreach (DataCaptureAttribute attribute in attributes)
                {
                    e.Packet.Position = 0;
                    attribute.Invoke(e);
                }
            }
        }

        private IEnumerable<Type> GetTypes(Type container)
        {
            do
            {
                yield return container;
            }
            while ((container = container.BaseType) != null);
        }
        private void AddCallback(DataCaptureAttribute attribute, ushort id)
        {
            Dictionary<ushort, List<DataCaptureAttribute>> callbacks =
                (attribute.IsOutgoing ? _outDataAttributes : _inDataAttributes);

            if (!callbacks.TryGetValue(id, out List<DataCaptureAttribute> attributes))
            {
                attributes = new List<DataCaptureAttribute>();
                callbacks.Add(id, attributes);
            }
            attributes.Add(attribute);
        }

        public virtual void Dispose()
        { }

        private class DummyInstaller : IInstaller, IHConnection
        {
            private readonly IModule _module;
            private readonly HNode _installerNode;
            private readonly Dictionary<ushort, Action<HPacket>> _moduleEvents;
            private readonly Dictionary<DataInterceptedEventArgs, string> _dataIdentifiers;

            HNode IHConnection.Local
            {
                get { throw new NotSupportedException("Local node not accessible during remote debugging."); }
            }
            HNode IHConnection.Remote
            {
                get { throw new NotSupportedException("Remote node not accessible during remote debugging."); }
            }

            public HGame Game { get; set; }
            public HGameData GameData { get; }
            public IHConnection Connection => this;

            public DummyInstaller(IModule module, HNode installerNode)
            {
                _module = module;
                _installerNode = installerNode;
                _dataIdentifiers = new Dictionary<DataInterceptedEventArgs, string>();

                _moduleEvents = new Dictionary<ushort, Action<HPacket>>
                {
                    [1] = HandleData,
                    [3] = HandleGameSynchronize,
                    [4] = HandleGameDataSynchronize
                };

                GameData = new HGameData();
                Task handleInstallerDataTask = HandleInstallerDataAsync();
            }

            private void HandleData(HPacket packet)
            {
                string identifier = packet.ReadUTF8();

                int step = packet.ReadInt32();
                bool isOutgoing = packet.ReadBoolean();
                var format = HFormat.GetFormat(packet.ReadUTF8());
                bool canContinue = packet.ReadBoolean();

                int ogDataLength = packet.ReadInt32();
                byte[] ogData = packet.ReadBytes(ogDataLength);

                var args = new DataInterceptedEventArgs(format.CreatePacket(ogData), step, isOutgoing, ContinueAsync);
                _dataIdentifiers.Add(args, identifier);

                bool isOriginal = packet.ReadBoolean();
                if (!isOriginal)
                {
                    int packetLength = packet.ReadInt32();
                    byte[] packetData = packet.ReadBytes(packetLength);
                    args.Packet = format.CreatePacket(packetData);
                }

                if (isOutgoing)
                {
                    _module.HandleOutgoing(args);
                }
                else
                {
                    _module.HandleIncoming(args);
                }

                if (!args.WasRelayed)
                {
                    HPacket handledDataPacket = CreateHandledDataPacket(args, false);
                    _installerNode.SendPacketAsync(handledDataPacket);
                }
            }
            private void HandleGameSynchronize(HPacket packet)
            {
                string path = packet.ReadUTF8();
                Game = new HGame(File.ReadAllBytes(path));
                Game.Location = path;

                Game.Disassemble();
                Game.GenerateMessageHashes();

                _module.Synchronize(Game);
            }
            private void HandleGameDataSynchronize(HPacket packet)
            {
                GameData.Source = packet.ReadUTF8();
                _module.Synchronize(GameData);
            }

            private async Task HandleInstallerDataAsync()
            {
                try
                {
                    HPacket packet = await _installerNode.ReceivePacketAsync().ConfigureAwait(false);
                    if (packet == null) Environment.Exit(0);

                    // Continue listening for further data.
                    Task handleInstallerDataTask = HandleInstallerDataAsync();

                    Action<HPacket> handler = null;
                    if (_moduleEvents.TryGetValue(packet.Id, out handler))
                    {
                        handler(packet);
                    }
                }
                catch { Environment.Exit(0); }
            }
            private async Task ContinueAsync(DataInterceptedEventArgs args)
            {
                HPacket handledDataPacket = CreateHandledDataPacket(args, true);
                await _installerNode.SendPacketAsync(handledDataPacket).ConfigureAwait(false);
            }

            public Task<int> SendToClientAsync(byte[] data)
            {
                return _installerNode.SendPacketAsync(2, false, data.Length, data);
            }
            public Task<int> SendToClientAsync(HPacket packet)
            {
                return SendToClientAsync(packet.ToBytes());
            }
            public Task<int> SendToClientAsync(ushort id, params object[] values)
            {
                return SendToClientAsync(EvaWirePacket.Construct(id, values));
            }

            public Task<int> SendToServerAsync(byte[] data)
            {
                return _installerNode.SendPacketAsync(2, true, data.Length, data);
            }
            public Task<int> SendToServerAsync(HPacket packet)
            {
                return SendToServerAsync(packet.ToBytes());
            }
            public Task<int> SendToServerAsync(ushort id, params object[] values)
            {
                return SendToServerAsync(EvaWirePacket.Construct(id, values));
            }

            private HPacket CreateHandledDataPacket(DataInterceptedEventArgs args, bool isContinuing)
            {
                var handledDataPacket = new EvaWirePacket(1);
                handledDataPacket.Write(_dataIdentifiers[args]);

                handledDataPacket.Write(isContinuing);
                if (isContinuing)
                {
                    handledDataPacket.Write(args.WasRelayed);
                }
                else
                {
                    byte[] packetData = args.Packet.ToBytes();
                    handledDataPacket.Write(packetData.Length);
                    handledDataPacket.Write(packetData);
                    handledDataPacket.Write(args.IsBlocked);
                }
                return handledDataPacket;
            }
        }
    }
}