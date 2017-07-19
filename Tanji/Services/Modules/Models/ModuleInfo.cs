using System;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Tanji.Helpers;

using Tangine.Habbo;
using Tangine.Modules;
using Tangine.Network;
using Tangine.Network.Protocol;

namespace Tanji.Services.Modules.Models
{
    public class ModuleInfo : ObservableObject
    {
        private readonly ResolveEventHandler _assemblyResolver;

        private const string DISPOSED_STATE = "Disposed";
        private const string INITIALIZED_STATE = "Initialized";

        public Dictionary<string, TaskCompletionSource<HPacket>> DataAwaiters { get; }

        public Type Type { get; set; }
        public Version Version { get; set; } = new Version(1, 0, 0, 0);
        public Assembly Assembly { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<AuthorAttribute> Authors { get; }

        public HNode Node { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }

        public Form FormUI { get; set; }
        public Window WindowUI { get; set; }
        public bool IsInitialized => (Instance != null);

        private string _currentState = DISPOSED_STATE;
        public string CurrentState
        {
            get { return _currentState; }
            set
            {
                _currentState = value;
                RaiseOnPropertyChanged();
            }
        }

        private IModule _instance;
        public IModule Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                RaiseOnPropertyChanged();
            }
        }

        public Command ToggleStateCommand { get; }

        private ModuleInfo()
        {
            Authors = new List<AuthorAttribute>();
            ToggleStateCommand = new Command(ToggleState);
            DataAwaiters = new Dictionary<string, TaskCompletionSource<HPacket>>();
        }
        public ModuleInfo(HNode node)
            : this()
        {
            Node = node;
        }
        public ModuleInfo(ResolveEventHandler assemblyResolver)
            : this()
        {
            _assemblyResolver = assemblyResolver;
        }

        public void Dispose()
        {
            if (FormUI != null)
            {
                FormUI.FormClosed -= UserInterface_Closed;
                FormUI.Close();
                FormUI = null;
            }
            else if (Instance != null)
            {
                Instance.Dispose();
            }

            IEnumerable<TaskCompletionSource<HPacket>> handledDataSources = DataAwaiters.Values.ToArray();
            foreach (TaskCompletionSource<HPacket> handledDataSource in handledDataSources)
            {
                handledDataSource.SetResult(null);
            }

            Instance = null;
            CurrentState = DISPOSED_STATE;
        }
        public void Initialize()
        {
            if (Instance != null)
            {
                FormUI?.BringToFront();
                WindowUI?.Activate();
                return;
            }
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += _assemblyResolver;
                if (Type != null)
                {
                    Instance = (IModule)FormatterServices.GetUninitializedObject(Type);
                }
                else Instance = new DummyModule(this);

                Instance.Installer = App.Master;
                if (Type != null)
                {
                    ConstructorInfo moduleConstructor = Type.GetConstructor(Type.EmptyTypes);
                    moduleConstructor.Invoke(Instance, null);
                }

                if (App.Master.Connection.IsConnected)
                {
                    Instance.Synchronize(App.Master.Game);
                    Instance.Synchronize(App.Master.GameData);
                }

                FormUI = (Instance as Form);
                if (FormUI != null)
                {
                    FormUI.Show();
                    FormUI.FormClosed += UserInterface_Closed;
                }
                else
                {
                    WindowUI = (Instance as Window);
                    if (WindowUI != null)
                    {
                        WindowUI.Show();
                        WindowUI.Closed += UserInterface_Closed;
                    }
                }
            }
            catch { Dispose(); }
            finally
            {
                if (Instance != null)
                {
                    CurrentState = INITIALIZED_STATE;
                }
                AppDomain.CurrentDomain.AssemblyResolve -= _assemblyResolver;
            }
        }
        public void ToggleState(object obj)
        {
            switch (CurrentState)
            {
                case INITIALIZED_STATE:
                {
                    Dispose();
                    break;
                }
                case DISPOSED_STATE:
                {
                    Initialize();
                    break;
                }
            }
        }

        private void UserInterface_Closed(object sender, EventArgs e)
        {
            if (FormUI != null)
            {
                FormUI.FormClosed -= UserInterface_Closed;
                FormUI = null;
            }
            if (WindowUI != null)
            {
                WindowUI.Closed -= UserInterface_Closed;
                WindowUI = null;
            }
            Dispose();
        }

        private class DummyModule : IModule
        {
            private readonly ModuleInfo _module;

            public bool IsStandalone => true;
            public IInstaller Installer { get; set; }

            public DummyModule(ModuleInfo module)
            {
                _module = module;
            }

            private void HandleData(DataInterceptedEventArgs e)
            {
                string identifier = e.Timestamp.Ticks.ToString();
                identifier += e.IsOutgoing;
                identifier += e.Step;
                try
                {
                    var interceptedData = new EvaWirePacket(1);
                    interceptedData.Write(identifier);

                    interceptedData.Write(e.Step);
                    interceptedData.Write(e.IsOutgoing);
                    interceptedData.Write(e.Packet.Format.Name);
                    interceptedData.Write(e.IsContinuable && !e.HasContinued);

                    interceptedData.Write(e.GetOriginalData().Length);
                    interceptedData.Write(e.GetOriginalData());

                    interceptedData.Write(e.IsOriginal);
                    if (!e.IsOriginal)
                    {
                        byte[] curPacketData = e.Packet.ToBytes();
                        interceptedData.Write(curPacketData.Length);
                        interceptedData.Write(curPacketData);
                    }

                    _module.DataAwaiters.Add(identifier, new TaskCompletionSource<HPacket>());
                    _module.Node.SendPacketAsync(interceptedData);

                    HPacket handledDataPacket = _module.DataAwaiters[identifier].Task.Result;
                    if (handledDataPacket == null) return;
                    // This packet contains the identifier at the start, although we do not read it here.

                    bool isContinuing = handledDataPacket.ReadBoolean();
                    if (isContinuing)
                    {
                        _module.DataAwaiters[identifier] = new TaskCompletionSource<HPacket>();

                        bool wasRelayed = handledDataPacket.ReadBoolean();
                        e.Continue(wasRelayed);

                        if (wasRelayed) return; // We have nothing else to do here, packet has already been sent/relayed.
                        handledDataPacket = _module.DataAwaiters[identifier].Task.Result;
                        isContinuing = handledDataPacket.ReadBoolean(); // We can ignore this one.
                    }
                    
                    int newPacketLength = handledDataPacket.ReadInt32();
                    byte[] newPacketData = handledDataPacket.ReadBytes(newPacketLength);

                    e.Packet = e.Packet.Format.CreatePacket(newPacketData);
                    e.IsBlocked = handledDataPacket.ReadBoolean();
                }
                finally { _module.DataAwaiters.Remove(identifier); }
            }
            public void HandleOutgoing(DataInterceptedEventArgs e) => HandleData(e);
            public void HandleIncoming(DataInterceptedEventArgs e) => HandleData(e);

            public void Synchronize(HGame game)
            {
                _module.Node.SendPacketAsync(3, game.Location);
            }
            public void Synchronize(HGameData gameData)
            {
                _module.Node.SendPacketAsync(4, gameData.Source);
            }
            
            public void Dispose()
            {
                _module.Node.Dispose();
            }
        }
    }
}