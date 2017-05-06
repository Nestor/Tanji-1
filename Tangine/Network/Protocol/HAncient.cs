using System.Collections.Generic;

namespace Tangine.Network.Protocol
{
    public class HAncient : HPacket
    {
        public HAncient(bool isOutgoing)
            : base(GetResolver(isOutgoing))
        { }
        public HAncient(bool isOutgoing, IList<byte> data)
            : base(GetResolver(isOutgoing), data)
        { }
        public HAncient(bool isOutgoing, ushort id, params object[] values)
            : this(isOutgoing, Construct(isOutgoing, id, values))
        { }

        protected override byte[] AsBytes()
        {
            return null;
        }

        private static HEncoding GetResolver(bool isOutgoing)
        {
            return (isOutgoing ? HEncoding.WedgieOut : HEncoding.WedgieIn);
        }
        public static byte[] Construct(bool isOutgoing, ushort id, params object[] values)
        {
            return GetResolver(isOutgoing).Construct(id, values);
        }
    }
}