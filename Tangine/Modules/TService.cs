using System;
using System.Reflection;

using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public class TService : IModule
    {
        private readonly Type _containerType;

        public IInstaller Installer { get; set; }

        public TService()
            : this(null)
        { }
        public TService(object container)
        {
            _containerType = (container?.GetType() ?? GetType());

            foreach (MethodInfo callback in _containerType
                .GetMethods((BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)))
            {
                var dataCaptureAtt = callback.GetCustomAttribute<DataCaptureAttribute>();
                if (dataCaptureAtt != null)
                {
                    
                }
            }
        }

        public virtual void ModifyGame(HGame game)
        { }
        public virtual void ModifyGameData(HGameData gameData)
        { }

        public virtual void HandleOutgoing(DataInterceptedEventArgs e)
        { }
        public virtual void HandleIncoming(DataInterceptedEventArgs e)
        { }

        public virtual void Dispose()
        { }
    }
}