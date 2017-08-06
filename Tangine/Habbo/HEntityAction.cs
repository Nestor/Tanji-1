using System;
using System.Globalization;
using System.Collections.Generic;

using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public class HEntityAction
    {
        public int Index { get; set; }
        public bool IsEmpowered { get; set; }

        public HPoint Tile { get; set; }
        public HPoint MovingTo { get; set; }

        public HSign Sign { get; set; }
        public HStance Stance { get; set; }
        public HAction Action { get; set; }
        public HDirection HeadFacing { get; set; }
        public HDirection BodyFacing { get; set; }

        public HEntityAction(HPacket packet)
        {
            Index = packet.ReadInt32();

            Tile = new HPoint(packet.ReadInt32(), packet.ReadInt32(),
                double.Parse(packet.ReadUTF8(), CultureInfo.InvariantCulture));

            HeadFacing = (HDirection)packet.ReadInt32();
            BodyFacing = (HDirection)packet.ReadInt32();

            string[] actionData = packet.ReadUTF8()
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string actionInfo in actionData)
            {
                string[] actionValues = actionInfo.Split(' ');

                if (actionValues.Length < 2) continue;
                if (string.IsNullOrWhiteSpace(actionValues[0])) continue;

                switch (actionValues[0])
                {
                    case "flatctrl":
                    {
                        IsEmpowered = true;
                        break;
                    }
                    case "mv":
                    {
                        string[] values = actionValues[1].Split(',');
                        if (values.Length >= 3)
                        {
                            Tile = new HPoint(int.Parse(values[0]), int.Parse(values[1]),
                                double.Parse(values[2], CultureInfo.InvariantCulture));
                        }
                        Action = HAction.Move;
                        break;
                    }
                    case "sit":
                    {
                        Action = HAction.Sit;
                        Stance = HStance.Sit;
                        break;
                    }
                    case "lay":
                    {
                        Action = HAction.Lay;
                        Stance = HStance.Lay;
                        break;
                    }
                    case "sign":
                    {
                        Sign = (HSign)int.Parse(actionValues[1]);
                        Action = HAction.Sign;
                        break;
                    }
                }
            }
        }

        public static IEnumerable<HEntityAction> Parse(HPacket packet)
        {
            int entityActionCount = packet.ReadInt32();
            for (int i = 0; i < entityActionCount; i++)
            {
                yield return new HEntityAction(packet);
            }
        }
    }
}