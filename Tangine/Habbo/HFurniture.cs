using System.Globalization;
using System.Collections.Generic;

using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public class HFurniture
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public int OwnerId { get; set; }
        public string OwnerName { get; set; }

        public HPoint Tile { get; set; }
        public HDirection Direction { get; set; }
        
        public HFurniture(int id, int typeId, int ownerId,
            string ownerName, HPoint tile, HDirection direction)
        {
            Id = id;
            TypeId = typeId;
            OwnerId = ownerId;
            OwnerName = ownerName;
            Tile = tile;
            Direction = direction;
        }

        public static List<HFurniture> Parse(HPacket packet)
        {
            int ownersCount = packet.ReadInt32();
            var owners = new Dictionary<int, string>(ownersCount);

            for (int i = 0; i < ownersCount; i++)
            {
                int ownerId = packet.ReadInt32();
                string ownerName = packet.ReadUTF8();

                owners.Add(ownerId, ownerName);
            }

            int furnitureCount = packet.ReadInt32();
            var furnitureList = new List<HFurniture>(furnitureCount);

            for (int i = 0; i < furnitureList.Capacity; i++)
            {
                int id = packet.ReadInt32();
                int typeId = packet.ReadInt32();

                int x = packet.ReadInt32();
                int y = packet.ReadInt32();
                var direction = (HDirection)packet.ReadInt32();
                var z = double.Parse(packet.ReadUTF8(), CultureInfo.InvariantCulture);

                packet.ReadUTF8();
                packet.ReadInt32();

                int category = packet.ReadInt32();
                HStuffData.ReadStuffData(category, packet);

                packet.ReadInt32();
                packet.ReadInt32();

                int ownerId = packet.ReadInt32();
                if (typeId < 0) packet.ReadUTF8();

                var furniture = new HFurniture(id, typeId, ownerId,
                    owners[ownerId], new HPoint(x, y, z), direction);

                furnitureList.Add(furniture);
            }
            return furnitureList;
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(TypeId)}: {TypeId}, " +
                $"{nameof(OwnerId)}: {OwnerId}, {nameof(OwnerName)}: {OwnerName}";
        }
    }
}