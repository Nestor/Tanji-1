using System;
using System.Reflection;
using System.Collections.Generic;

using Tangine.Habbo;
using Tangine.Network;

namespace Tangine.Modules
{
    public abstract class TService : IModule
    {
        private object _container;
        private readonly Type _containerType;
        private readonly List<DataCaptureAttribute> _captureAtts;
        private const BindingFlags CALLBACK_FLAGS = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        public const int REMOTE_MODULE_PORT = 8055;

        public IInstaller Installer { get; set; }

        public TService()
            : this(null)
        { }
        internal TService(object container)
        {
            _container = (container ?? this);
            _containerType = container.GetType();
            _captureAtts = new List<DataCaptureAttribute>();

            foreach (MethodInfo callback in _containerType.GetMethods((CALLBACK_FLAGS)))
            {
                var dataCaptureAtt = callback.GetCustomAttribute<DataCaptureAttribute>();
                if (dataCaptureAtt != null)
                {
                    dataCaptureAtt.Method = callback;
                    _captureAtts.Add(dataCaptureAtt);
                }
            }
        }

        public virtual void Synchronize(HGame game)
        {
        }
        public virtual void Synchronize(HGameData gameData)
        { }

        public virtual void HandleOutgoing(DataInterceptedEventArgs e)
        { }
        public virtual void HandleIncoming(DataInterceptedEventArgs e)
        { }

        public virtual void Dispose()
        { }
    }
}