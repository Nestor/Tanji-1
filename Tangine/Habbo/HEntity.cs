using System.Globalization;
using System.Collections.Generic;

using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public class HEntity : IHEntity
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string Motto { get; set; }
        public string FigureId { get; set; }
        public string FavoriteGroup { get; set; }

        public HPoint Tile { get; set; }
        public HGender Gender { get; set; }

        public HEntity(int id, int index, string name, HPoint tile,
            string motto, HGender gender, string figureId, string favoriteGroup)
        {
            Id = id;
            Index = index;
            Name = name;
            Tile = tile;
            Motto = motto;
            Gender = gender;
            FigureId = figureId;
            FavoriteGroup = favoriteGroup;
        }

        public static IReadOnlyList<HEntity> Parse(HPacket packet)
        {
            int entityCount = packet.ReadInt32();
            var entityList = new List<HEntity>(entityCount);

            for (int i = 0; i < entityList.Capacity; i++)
            {
                int id = packet.ReadInt32();
                string name = packet.ReadUTF8();
                string motto = packet.ReadUTF8();
                string figureId = packet.ReadUTF8();
                int index = packet.ReadInt32();
                int x = packet.ReadInt32();
                int y = packet.ReadInt32();
                var z = double.Parse(packet.ReadUTF8(), CultureInfo.InvariantCulture);

                packet.ReadInt32();
                int type = packet.ReadInt32();

                var gender = HGender.Unisex;
                string favoriteGroup = string.Empty;
                #region Switch: type
                switch (type)
                {
                    case 1:
                    {
                        // TODO: gender = SKore.ToGender(packet.ReadUTF8());
                        packet.ReadInt32();
                        packet.ReadInt32();
                        favoriteGroup = packet.ReadUTF8();
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
                            packet.ReadUInt16();

                        break;
                    }
                }
                #endregion

                var entity = new HEntity(id, index, name, new HPoint(x, y, z), motto, gender, figureId, favoriteGroup);
                entityList.Add(entity);
            }
            return entityList;
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(Index)}: {Index}, {nameof(Name)}: {Name}, " +
                $"{nameof(Tile)}: {Tile}, {nameof(Motto)}: {Motto}, {nameof(Gender)}: {Gender}, " +
                $"{nameof(FigureId)}: {FigureId}, {nameof(FavoriteGroup)}: {FavoriteGroup}";
        }
    }
}