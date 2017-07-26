using System.Collections.Generic;

using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public class HItem
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public int RoomId { get; set; }
        public int Category { get; set; }
        public string SlotId { get; set; }
        public int SecondsToExpiration { get; set; }
        public bool HasRentPeriodStarted { get; set; }

        public HItem(int id, int typeId, int category,
            int secondsToExpiration, bool hasRentPeriodStarted, int roomId)
        {
            Id = id;
            TypeId = typeId;
            Category = category;
            SecondsToExpiration = secondsToExpiration;
            HasRentPeriodStarted = hasRentPeriodStarted;
            RoomId = roomId;
        }

        public static List<HItem> Parse(HPacket packet)
        {
            packet.ReadInt32();
            packet.ReadInt32();

            int itemCount = packet.ReadInt32();
            var itemList = new List<HItem>(itemCount);

            for (int i = 0; i < itemList.Capacity; i++)
            {
                packet.ReadInt32();
                string s1 = packet.ReadUTF8();

                int id = packet.ReadInt32();
                int typeId = packet.ReadInt32();
                packet.ReadInt32();

                int category = packet.ReadInt32();
                HStuffData.ReadStuffData(category, packet);

                packet.ReadBoolean();
                packet.ReadBoolean();
                packet.ReadBoolean();
                packet.ReadBoolean();
                int secondsToExpiration = packet.ReadInt32();

                bool hasRentPeriodStarted = packet.ReadBoolean();
                int roomId = packet.ReadInt32();

                var item = new HItem(id, typeId, category,
                    secondsToExpiration, hasRentPeriodStarted, roomId);

                if (s1 == "S")
                {
                    item.SlotId = packet.ReadUTF8();
                    packet.ReadInt32();
                }
                itemList.Add(item);
            }
            return itemList;
        }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, {nameof(TypeId)}: {TypeId}, " +
                $"{nameof(RoomId)}: {RoomId}, {nameof(Category)}: {Category}, {nameof(SlotId)}: {SlotId}, " +
                $"{nameof(SecondsToExpiration)}: {SecondsToExpiration}, {nameof(HasRentPeriodStarted)}: {HasRentPeriodStarted}";
        }
    }
}