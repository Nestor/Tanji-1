using System;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Tanji.Helpers;

using Tangine.Modules;

namespace Tanji.Services.Modules.Models
{
    public class ModuleInfo : ObservableObject
    {
        private readonly ResolveEventHandler _resolverHandler;

        private const string DISPOSE_STATE_ACTION = "Dispose";
        private const string INITIALIZE_STATE_ACTION = "Initialize";

        public Type Type { get; set; }
        public Assembly Assembly { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<AuthorAttribute> Authors { get; }

        public string Path { get; set; }
        public string Hash { get; set; }

        public Form UserInterface { get; set; }
        public bool IsInitialized => (Instance != null);

        private string _nextStateAction = INITIALIZE_STATE_ACTION;
        public string NextStateAction
        {
            get { return _nextStateAction; }
            set
            {
                _nextStateAction = value;
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

        public ModuleInfo(ResolveEventHandler assemblyResolver)
        {
            _resolverHandler = assemblyResolver;

            Authors = new List<AuthorAttribute>();
            ToggleStateCommand = new Command(ToggleState);
        }

        public void Dispose()
        {
            if (UserInterface != null)
            {
                UserInterface.FormClosed -= UserInterface_Closed;
                UserInterface.Close();
                UserInterface = null;
            }
            else if (Instance != null)
            {
                Instance.Dispose();
            }

            Instance = null;
            NextStateAction = INITIALIZE_STATE_ACTION;
        }
        public void Initialize()
        {
            if (Instance != null)
            {
                UserInterface?.BringToFront();
                return;
            }
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += _resolverHandler;
                Instance = (IModule)FormatterServices.GetUninitializedObject(Type);
                Instance.Installer = App.Master;

                ConstructorInfo moduleConstructor = Type.GetConstructor(Type.EmptyTypes);
                moduleConstructor.Invoke(Instance, null);

                if (App.Master.Connection.IsConnected)
                {
                    Instance.Synchronize(App.Master.Game);
                    Instance.Synchronize(App.Master.GameData);
                }

                UserInterface = (Instance as Form);
                if (UserInterface != null)
                {
                    UserInterface.Show();
                    UserInterface.FormClosed += UserInterface_Closed;
                }
            }
            catch { Dispose(); }
            finally
            {
                if (Instance != null)
                {
                    NextStateAction = DISPOSE_STATE_ACTION;
                }
                AppDomain.CurrentDomain.AssemblyResolve -= _resolverHandler;
            }
        }
        public void ToggleState(object obj)
        {
            switch (NextStateAction)
            {
                case INITIALIZE_STATE_ACTION:
                {
                    Initialize();
                    NextStateAction = DISPOSE_STATE_ACTION;
                    break;
                }
                case DISPOSE_STATE_ACTION:
                {
                    Dispose();
                    NextStateAction = INITIALIZE_STATE_ACTION;
                    break;
                }
            }
        }

        private void UserInterface_Closed(object sender, FormClosedEventArgs e)
        {
            UserInterface.FormClosed -= UserInterface_Closed;
            UserInterface = null;
            Dispose();
        }
    }
}