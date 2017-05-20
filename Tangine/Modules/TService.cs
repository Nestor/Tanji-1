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
        private readonly Dictionary<ushort, DataCaptureAttribute> _outCallbacks, _inCallbacks;
        private const BindingFlags CALLBACK_FLAGS = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        public const int REMOTE_MODULE_PORT = 8055;

        public IInstaller Installer { get; set; }

        public TService()
            : this(null)
        { }
        internal TService(object container)
        {
            _inCallbacks = new Dictionary<ushort, DataCaptureAttribute>();
            _outCallbacks = new Dictionary<ushort, DataCaptureAttribute>();

            _container = (container ?? this);
            _containerType = container.GetType();
            _captureAtts = new List<DataCaptureAttribute>();

            foreach (MethodInfo callback in _containerType.GetMethods((CALLBACK_FLAGS)))
            {
                var dataCaptureAtt = callback.GetCustomAttribute<DataCaptureAttribute>();
                if (dataCaptureAtt != null)
                {
                    dataCaptureAtt.Method = callback;
                    if (dataCaptureAtt.Id != null)
                    {
                        (dataCaptureAtt.IsOutgoing ? _outCallbacks : _inCallbacks)
                            .Add((ushort)dataCaptureAtt.Id, dataCaptureAtt);
                    }
                    else _captureAtts.Add(dataCaptureAtt);
                }
            }
        }
        
        public virtual void Synchronize(HGame game)
        {
            foreach (DataCaptureAttribute captureAtt in _captureAtts)
            {
                if (string.IsNullOrWhiteSpace(captureAtt.Hash)) continue;

                List<MessageItem> messages = null;
                if (game.Messages.TryGetValue(captureAtt.Hash, out messages))
                {
                    (captureAtt.IsOutgoing ? _outCallbacks : _inCallbacks)
                        .Add(messages[0].Id, captureAtt);
                }
            }
        }
        public virtual void Synchronize(HGameData gameData)
        { }

        public virtual void HandleOutgoing(DataInterceptedEventArgs e)
        {
            if (_outCallbacks.Count > 0)
            {
                DataCaptureAttribute captureAtt = null;
                if (_outCallbacks.TryGetValue(e.Packet.Id, out captureAtt))
                {
                    captureAtt.Invoke(_container, e);
                }
            }
        }
        public virtual void HandleIncoming(DataInterceptedEventArgs e)
        {
            if (_inCallbacks.Count > 0)
            {
                DataCaptureAttribute captureAtt = null;
                if (_inCallbacks.TryGetValue(e.Packet.Id, out captureAtt))
                {
                    captureAtt.Invoke(_container, e);
                }
            }
        }

        public virtual void Dispose()
        { }
    }
}