using System.Collections.Generic;

namespace Tangine.Network.Protocol
{
    public class HModern : HPacket
    {
        public HModern()
            : base(HEncoding.BigEndian)
        { }
        public HModern(IList<byte> data)
            : base(HEncoding.BigEndian, data)
        { }
        public HModern(ushort id, params object[] values)
            : this(Construct(id, values))
        { }

        protected override byte[] AsBytes()
        {
            return null;
        }

        public static byte[] ToBytes(string signature)
        {
            return ToBytes(HEncoding.BigEndian, signature);
        }
        public static byte[] Construct(ushort id, params object[] values)
        {
            return HEncoding.BigEndian.Construct(id, values);
        }
    }
}