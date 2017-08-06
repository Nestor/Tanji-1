using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public abstract class HData
    {
        protected void ReadData(HPacket packet, int category)
        {
            switch (category & 0xFF)
            {
                case 0:
                {
                    packet.ReadUTF8();
                    break;
                }
                case 1: /* MapStuffData */
                {
                    int count = packet.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        packet.ReadUTF8();
                        packet.ReadUTF8();
                    }
                    break;
                }
                case 2: /* StringArrayStuffData */
                {
                    int count = packet.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        packet.ReadUTF8();
                    }
                    break;
                }
                case 3:
                {
                    packet.ReadUTF8();
                    packet.ReadInt32();
                    break;
                }
                case 5: /* IntArrayStuffData */
                {
                    int count = packet.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        packet.ReadInt32();
                    }
                    break;
                }
                case 6: /* HighScoreStuffData */
                {
                    packet.ReadUTF8();
                    packet.ReadInt32();
                    packet.ReadInt32();

                    int count = packet.ReadInt32();
                    for (int j = 0; j < count; j++)
                    {
                        int score = packet.ReadInt32();
                        int subCount = packet.ReadInt32();
                        for (int k = 0; k < subCount; k++)
                        {
                            packet.ReadUTF8();
                        }
                    }
                    break;
                }
                case 7:
                {
                    packet.ReadUTF8();
                    packet.ReadInt32();
                    packet.ReadInt32();
                    break;
                }
            }
            if (((category & 0xFF00) & 0x100) > 0)
            {
                packet.ReadInt32();
                packet.ReadInt32();
            }
        }
    }
}