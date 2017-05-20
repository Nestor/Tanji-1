using System;
using System.Threading.Tasks;

using Tangine.Habbo;
using Tangine.Network.Protocol;

namespace Tangine.Network
{
    /// <summary>
    /// Represents an intercepted message that will be returned to the caller with blocking/replacing information.
    /// </summary>
    public class DataInterceptedEventArgs : EventArgs
    {
        private readonly byte[] _ogData;
        private readonly string _ogString;
        private readonly object _continueLock;
        private readonly Func<Task> _continuation;
        private readonly DataInterceptedEventArgs _args;
        private readonly Func<HPacket, Task<int>> _transmitter;

        public int Step { get; }
        public bool IsOutgoing { get; }
        public DateTime Timestamp { get; }
        public MessageItem MessageType { get; set; }

        public bool IsOriginal
        {
            get { return Packet.ToString().Equals(_ogString); }
        }
        public bool IsContinuable
        {
            get { return (_continuation != null && !HasContinued); }
        }

        private bool _isBlocked;
        public bool IsBlocked
        {
            get { return (_args?.IsBlocked ?? _isBlocked); }
            set
            {
                if (_args != null)
                {
                    _args.IsBlocked = value;
                }
                _isBlocked = value;
            }
        }

        private HPacket _packet;
        public HPacket Packet
        {
            get { return (_args?.Packet ?? _packet); }
            set
            {
                if (_args != null)
                {
                    _args.Packet = value;
                }
                _packet = value;
            }
        }

        private bool _wasRelayed;
        public bool WasRelayed
        {
            get { return (_args?.WasRelayed ?? _wasRelayed); }
            private set
            {
                if (_args != null)
                {
                    _args.WasRelayed = value;
                }
                _wasRelayed = value;
            }
        }

        private bool _hasContinued;
        public bool HasContinued
        {
            get { return (_args?.HasContinued ?? _hasContinued); }
            private set
            {
                if (_args != null)
                {
                    _args.HasContinued = value;
                }
                _hasContinued = value;
            }
        }

        public DataInterceptedEventArgs(DataInterceptedEventArgs args)
        {
            _args = args;
            _ogData = args._ogData;
            _ogString = args._ogString;
            _transmitter = args._transmitter;
            _continuation = args._continuation;
            _continueLock = args._continueLock;

            Step = args.Step;
            Timestamp = args.Timestamp;
            IsOutgoing = args.IsOutgoing;
            MessageType = args.MessageType;
        }
        public DataInterceptedEventArgs(HPacket packet, int step, bool isOutgoing)
        {
            _ogData = packet.ToBytes();
            _ogString = packet.ToString();

            Step = step;
            Packet = packet;
            IsOutgoing = isOutgoing;
            Timestamp = DateTime.Now;
        }
        public DataInterceptedEventArgs(HPacket packet, int step, bool isOutgoing, Func<Task> continuation)
            : this(packet, step, isOutgoing)
        {
            _continueLock = new object();
            _continuation = continuation;
        }
        public DataInterceptedEventArgs(HPacket packet, int step, bool isOutgoing, Func<Task> continuation, Func<HPacket, Task<int>> transmitter)
            : this(packet, step, isOutgoing, continuation)
        {
            _transmitter = transmitter;
        }

        public void Continue()
        {
            Continue(false);
        }
        public void Continue(bool relay)
        {
            if (IsContinuable)
            {
                lock (_continueLock)
                {
                    if (relay)
                    {
                        _transmitter(Packet);
                        WasRelayed = true;
                    }

                    _continuation();
                    HasContinued = true;
                }
            }
        }

        /// <summary>
        /// Restores the intercepted data to its initial form, before it was replaced/modified.
        /// </summary>
        public void Restore()
        {
            if (!IsOriginal)
            {
                Packet = Packet.Resolver.CreatePacket(_ogData);
            }
        }
    }
}