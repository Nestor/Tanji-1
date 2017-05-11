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
        private readonly Func<HPacket, Task<int>> _transmitter;

        public int Step { get; }
        public bool IsOutgoing { get; }
        public DateTime Timestamp { get; }
        public MessageItem Message { get; set; }

        public bool IsOriginal
        {
            get { return Packet.ToString().Equals(_ogString); }
        }
        public bool IsContinuable
        {
            get { return (_continuation != null && !HasContinued); }
        }
        public bool IsBlocked { get; set; }
        public HPacket Packet { get; set; }

        public bool WasRelayed { get; private set; }
        public bool HasContinued { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataInterceptedEventArgs"/> class.
        /// </summary>
        /// <param name="packet">The intercepted message.</param>
        /// <param name="step">The current count/step/order of the intercepted message.</param>
        public DataInterceptedEventArgs(HPacket packet, int step, bool isOutgoing)
        {
            _ogData = packet.ToBytes();
            _ogString = packet.ToString();

            Step = step;
            Packet = packet;
            IsOutgoing = isOutgoing;
            Timestamp = DateTime.Now;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DataInterceptedEventArgs"/> class.
        /// </summary>
        /// <param name="packet">The intercepted message.</param>
        /// <param name="step">The current count/step/order of the intercepted message.</param>
        /// <param name="continuation">The method that will be called when the user invokes <see cref="Continue"/>.</param>
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