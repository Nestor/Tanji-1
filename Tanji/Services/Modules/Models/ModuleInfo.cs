using System;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Tanji.Helpers;

using Tangine.Modules;
using System.Windows;

namespace Tanji.Services.Modules.Models
{
    public class ModuleInfo : ObservableObject
    {
        private readonly ResolveEventHandler _resolverHandler;

        private const string DISPOSED_STATE = "Disposed";
        private const string INITIALIZED_STATE = "Initialized";

        public Type Type { get; set; }
        public Version Version { get; set; } = new Version(1, 0, 0, 0);
        public Assembly Assembly { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public List<AuthorAttribute> Authors { get; }

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

        public ModuleInfo(ResolveEventHandler assemblyResolver)
        {
            _resolverHandler = assemblyResolver;

            Authors = new List<AuthorAttribute>();
            ToggleStateCommand = new Command(ToggleState);
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
                AppDomain.CurrentDomain.AssemblyResolve -= _resolverHandler;
            }
        }
        public void ToggleState(object obj)
        {
            switch (CurrentState)
            {
                case INITIALIZED_STATE:
                {
                    Dispose();
                    CurrentState = DISPOSED_STATE;
                    break;
                }
                case DISPOSED_STATE:
                {
                    Initialize();
                    CurrentState = INITIALIZED_STATE;
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
    }
}