using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public static class HStuffData
    {
        public static void ReadStuffData(int category, HPacket packet)
        {
            switch (category & 255)
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
        }
    }
}