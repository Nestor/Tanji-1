using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

using Tanji.Helpers;
using Tanji.Services.Modules.Models;

using Tangine.Modules;
using Tangine.Network;

namespace Tanji.Services.Modules
{
    public class ModulesViewModel : ObservableObject, IReceiver, IHaltable
    {
        private ModuleInfo[] _safeModules;

        private readonly List<string> _hashBlacklist;
        private readonly OpenFileDialog _installModuleDialog;

        private static readonly Type _iModuleType;
        private static readonly Dictionary<string, ModuleInfo> _moduleCache;

        public Command InstallCommand { get; }
        public Command UninstallCommand { get; }

        public DirectoryInfo ModulesDirectory { get; }
        public DirectoryInfo DependenciesDirectory { get; }
        public ObservableCollection<ModuleInfo> Modules { get; }

        private ModuleInfo _selectedModule;
        public ModuleInfo SelectedModule
        {
            get { return _selectedModule; }
            set
            {
                _selectedModule = value;
                RaiseOnPropertyChanged();
            }
        }

        public int RemoteModulePort { get; } = TService.REMOTE_MODULE_PORT;

        static ModulesViewModel()
        {
            _iModuleType = typeof(IModule);
            _moduleCache = new Dictionary<string, ModuleInfo>();
        }
        public ModulesViewModel()
        {
            _safeModules = new ModuleInfo[0];
            _hashBlacklist = new List<string>();

            _installModuleDialog = new OpenFileDialog();
            _installModuleDialog.Title = "Tanji - Install Module";
            _installModuleDialog.Filter = ".NET Assembly (*.dll, *.exe)|*.dll; *.exe|Dynamic Link Library (*.dll)|*.dll|Executable (*.exe)|*.exe";

            InstallCommand = new Command(Install);
            UninstallCommand = new Command(Uninstall, CanUninstall);

            Modules = new ObservableCollection<ModuleInfo>();
            Modules.CollectionChanged += Modules_CollectionChanged;

            if (App.Master != null) // Stop it from attemping to create these directories in the designer.
            {
                ModulesDirectory = Directory.CreateDirectory("Installed Modules");
                DependenciesDirectory = ModulesDirectory.CreateSubdirectory("Dependencies");

                LoadModules();
            }
        }

        private void Install(object obj)
        {
            var modulePath = (obj as string);
            _installModuleDialog.FileName = string.Empty;
            if (string.IsNullOrWhiteSpace(modulePath) &&
                _installModuleDialog.ShowDialog() != DialogResult.Cancel)
            {
                modulePath = _installModuleDialog.FileName;
            }
            if (string.IsNullOrWhiteSpace(modulePath)) return;

            // Check if the file was blacklisted based on its MD5 hash, if so, do not attempt to install.
            string hash = GetFileHash(modulePath);
            if (_hashBlacklist.Contains(hash)) return;

            // Check if this module is already installed.
            ModuleInfo module = GetModule(hash);
            if (module != null)
            {
                SelectedModule = module;
                module.UserInterface?.BringToFront();
                return;
            }

            // Do not remove from, or empty the module cache.
            // There may be a case where a previously uninstalled module will be be reinstalled in the same session.
            if (!_moduleCache.TryGetValue(hash, out module))
            {
                // Load it through memory, do not feed a local file path/stream(don't want to lock the file).
                module = new ModuleInfo(Assembly_Resolve);
                module.Assembly = Assembly.Load(File.ReadAllBytes(modulePath));

                module.Hash = hash;
                module.PropertyChanged += Module_PropertyChanged;

                // Copy the required dependencies, since utilizing 'ExportedTypes' will attempt to load them when enumerating.
                CopyDependencies(modulePath, module.Assembly);
                try
                {
                    AppDomain.CurrentDomain.AssemblyResolve += Assembly_Resolve;
                    foreach (Type type in module.Assembly.ExportedTypes)
                    {
                        if (!_iModuleType.IsAssignableFrom(type)) continue;
                        module.Type = type;

                        var moduleAtt = type.GetCustomAttribute<ModuleAttribute>();
                        if (moduleAtt == null)
                        {
                            // Module attribute is required.
                            _hashBlacklist.Add(hash);
                            return;
                        }
                        module.Name = moduleAtt.Name;
                        module.Description = moduleAtt.Description;

                        var authorAtts = type.GetCustomAttributes<AuthorAttribute>();
                        module.Authors.AddRange(authorAtts);

                        // Only add it to the cache if this is a valid module.
                        _moduleCache.Add(hash, module);
                        break;
                    }
                }
                finally { AppDomain.CurrentDomain.AssemblyResolve -= Assembly_Resolve; }
            }

            string installPath = CopyFile(modulePath, hash);
            module.Path = installPath; // This property already might have been set from a previous installation, but it wouldn't hurt to re-set the value.
            Modules.Add(module);
        }
        private void Uninstall(object obj)
        {
            if (File.Exists(SelectedModule.Path))
            {
                File.Delete(SelectedModule.Path);
            }
            SelectedModule.Dispose();
            Modules.Remove(SelectedModule);

            SelectedModule = null;
        }
        private bool CanUninstall(object obj)
        {
            return (SelectedModule != null);
        }

        public ModuleInfo GetModule(string hash)
        {
            return Modules.SingleOrDefault(m => m.Hash == hash);
        }
        public IEnumerable<ModuleInfo> GetInitializedModules()
        {
            return Modules.Where(m => m.IsInitialized);
        }

        private void LoadModules()
        {
            foreach (FileSystemInfo fileSysInfo in ModulesDirectory.EnumerateFiles("*.*"))
            {
                string extension = fileSysInfo.Extension.ToLower();
                if (extension == ".exe" || extension == ".dll")
                {
                    try { Install(fileSysInfo.FullName); }
                    catch (Exception ex)
                    {
                        App.Display(ex, "Failed to install the assembly as a module.\r\nFile: " + fileSysInfo.Name);
                    }
                }
            }
        }
        private string GetFileHash(string path)
        {
            using (var md5 = MD5.Create())
            using (var fileStream = File.OpenRead(path))
            {
                return BitConverter.ToString(md5.ComputeHash(fileStream))
                    .Replace("-", string.Empty).ToLower();
            }
        }
        private string CopyFile(string path, string uniqueId)
        {
            path = Path.GetFullPath(path);
            string fileExt = Path.GetExtension(path);
            string fileName = Path.GetFileNameWithoutExtension(path);

            string copiedFilePath = path;
            string fileNameSuffix = $"({uniqueId}){fileExt}";
            if (!path.EndsWith(fileNameSuffix))
            {
                copiedFilePath = Path.Combine(ModulesDirectory.FullName, fileName + fileNameSuffix);
                if (!File.Exists(copiedFilePath))
                {
                    File.Copy(path, copiedFilePath, true);
                }
            }
            return copiedFilePath;
        }
        private void CopyDependencies(string filePath, Assembly fileAsm)
        {
            AssemblyName[] references = fileAsm.GetReferencedAssemblies();
            var fileReferences = new Dictionary<string, AssemblyName>(references.Length);
            foreach (AssemblyName reference in references)
            {
                fileReferences[reference.Name] = reference;
            }

            string[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .ToArray();

            var sourceDirectory = new DirectoryInfo(Path.GetDirectoryName(filePath));
            IEnumerable<string> missingAssemblies = fileReferences.Keys.Except(loadedAssemblies);
            foreach (string missingAssembly in missingAssemblies)
            {
                string assemblyName = fileReferences[missingAssembly].FullName;
                FileSystemInfo dependencyFile = GetDependencyFile(DependenciesDirectory, assemblyName);
                if (dependencyFile == null)
                {
                    dependencyFile = GetDependencyFile(sourceDirectory, assemblyName);
                    if (dependencyFile != null)
                    {
                        string installDependencyPath = Path.Combine(
                           DependenciesDirectory.FullName, dependencyFile.Name);

                        File.Copy(dependencyFile.FullName, installDependencyPath, true);
                    }
                }
            }
        }
        private FileSystemInfo GetDependencyFile(DirectoryInfo directory, string dependencyName)
        {
            FileSystemInfo[] libraries = directory.GetFileSystemInfos("*.dll");
            foreach (FileSystemInfo library in libraries)
            {
                string libraryName = AssemblyName.GetAssemblyName(library.FullName).FullName;
                if (libraryName == dependencyName)
                {
                    return library;
                }
            }
            return null;
        }

        private Assembly Assembly_Resolve(object sender, ResolveEventArgs e)
        {
            FileSystemInfo dependencyFile = GetDependencyFile(DependenciesDirectory, e.Name);
            if (dependencyFile != null)
            {
                return Assembly.Load(File.ReadAllBytes(dependencyFile.FullName));
            }
            return null;
        }
        private void Module_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ModuleInfo.Instance):
                {
                    IsReceiving = (GetInitializedModules().Count() > 0);
                    break;
                }
            }
        }
        private void Modules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _safeModules = Modules.ToArray();
        }

        #region IHaltable Implementation
        public void Halt()
        { }
        public void Restore()
        { }
        #endregion
        #region IReceiver Implementation
        public bool IsReceiving { get; private set; }
        public void HandleOutgoing(DataInterceptedEventArgs e)
        {
            if (_safeModules.Length == 0) return;
            foreach (ModuleInfo module in _safeModules)
            {
                if (module.Instance != null)
                {
                    try { module.Instance?.HandleOutgoing(e); }
                    catch
                    {
                        // TODO: Display informative error to user with last read values/packet.
                        e.Restore();
                    }
                }
            }
        }
        public void HandleIncoming(DataInterceptedEventArgs e)
        {
            if (_safeModules.Length == 0) return;
            foreach (ModuleInfo module in _safeModules)
            {
                if (module.Instance != null)
                {
                    try { module.Instance?.HandleIncoming(e); }
                    catch
                    {
                        // TODO: Display informative error to user with last read values/packet.
                        e.Restore();
                    }
                }
            }
        }
        #endregion
    }
}