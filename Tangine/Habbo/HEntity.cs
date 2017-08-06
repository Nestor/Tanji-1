using System;
using System.Globalization;
using System.Collections.Generic;

using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public class HEntity : HData
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public HPoint Tile { get; set; }
        public string Name { get; set; }
        public string Motto { get; set; }
        public HGender Gender { get; set; }
        public string FigureId { get; set; }
        public string FavoriteGroup { get; set; }
        public HEntityAction LastUpdate { get; private set; }

        public HEntity(HPacket packet)
        {
            Id = packet.ReadInt32();
            Name = packet.ReadUTF8();
            Motto = packet.ReadUTF8();
            FigureId = packet.ReadUTF8();
            Index = packet.ReadInt32();

            Tile = new HPoint(packet.ReadInt32(), packet.ReadInt32(),
                double.Parse(packet.ReadUTF8(), CultureInfo.InvariantCulture));

            packet.ReadInt32();
            int type = packet.ReadInt32();

            switch (type)
            {
                case 1:
                {
                    Gender = (HGender)packet.ReadUTF8().ToLower()[0];
                    packet.ReadInt32();
                    packet.ReadInt32();
                    FavoriteGroup = packet.ReadUTF8();
                    packet.ReadUTF8();
                    packet.ReadInt32();
                    packet.ReadBoolean();
                    break;
                }
                case 2:
                {
                    packet.ReadInt32();
                    packet.ReadInt32();
                    packet.ReadUTF8();
                    packet.ReadInt32();
                    packet.ReadBoolean();
                    packet.ReadBoolean();
                    packet.ReadBoolean();
                    packet.ReadBoolean();
                    packet.ReadBoolean();
                    packet.ReadBoolean();
                    packet.ReadInt32();
                    packet.ReadUTF8();
                    break;
                }
                case 4:
                {
                    packet.ReadUTF8();
                    packet.ReadInt32();
                    packet.ReadUTF8();
                    for (int j = packet.ReadInt32(); j > 0; j--)
                    {
                        packet.ReadUInt16();
                    }
                    break;
                }
            }
        }

        public void Update(HEntityAction action)
        {
            if (!TryUpdate(action))
            {
                throw new ArgumentException("Entity index does not match.", nameof(action));
            }
        }
        public bool TryUpdate(HEntityAction action)
        {
            if (Index != action.Index) return false;

            Tile = action.Tile;
            LastUpdate = action;
            return true;
        }

        public static IEnumerable<HEntity> Parse(HPacket packet)
        {
            int entityCount = packet.ReadInt32();
            for (int i = 0; i < entityCount; i++)
            {
                yield return new HEntity(packet);
            }
        }
    }
}