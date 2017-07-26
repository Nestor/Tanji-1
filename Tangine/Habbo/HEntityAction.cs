using System;
using System.Globalization;
using System.Collections.Generic;

using Tangine.Network.Protocol;

namespace Tangine.Habbo
{
    public class HEntityAction : IHEntity
    {
        public int Index { get; set; }
        public bool IsEmpowered { get; set; }

        public HPoint Tile { get; set; }
        public HPoint MovingTo { get; set; }

        public HSign Sign { get; set; }
        public HStance Stance { get; set; }
        public HAction LastAction { get; set; }
        public HDirection HeadDirection { get; set; }
        public HDirection BodyDirection { get; set; }

        public HEntityAction(bool isEmpowered, int index, HPoint tile, HPoint movingTo,
            HSign sign, HStance stance, HDirection headDirection, HDirection bodyDirection, HAction lastAction)
        {
            Index = index;
            IsEmpowered = isEmpowered;

            Tile = tile;
            MovingTo = movingTo;

            Sign = sign;
            Stance = stance;

            HeadDirection = headDirection;
            BodyDirection = bodyDirection;

            LastAction = lastAction;
        }

        public static IReadOnlyList<HEntityAction> Parse(HPacket packet)
        {
            int entityActionCount = packet.ReadInt32();
            var entityActionList = new List<HEntityAction>(entityActionCount);

            for (int i = 0; i < entityActionList.Capacity; i++)
            {
                int index = packet.ReadInt32();
                int x = packet.ReadInt32();
                int y = packet.ReadInt32();
                var z = double.Parse(packet.ReadUTF8(), CultureInfo.InvariantCulture);

                var headDirection = (HDirection)packet.ReadInt32();
                var bodyDirection = (HDirection)packet.ReadInt32();

                string actionString = packet.ReadUTF8();
                string[] actionData = actionString.Split(new[] { '/' },
                    StringSplitOptions.RemoveEmptyEntries);

                HSign sign = HSign.One;
                HAction action = HAction.None;
                HStance stance = HStance.Stand;

                double movingToZ = 0.0;
                bool isEmpowered = false;
                int movingToX = 0, movingToY = 0;

                foreach (string actionInfo in actionData)
                {
                    string[] actionValues = actionInfo.Split(' ');

                    if (actionValues.Length < 2) continue;
                    if (string.IsNullOrWhiteSpace(actionValues[0])) continue;
                    #region Switch: actionValues
                    switch (actionValues[0])
                    {
                        case "flatctrl":
                        {
                            isEmpowered = true;
                            break;
                        }
                        case "mv":
                        {
                            string[] movingToValues = actionValues[1].Split(',');
                            if (movingToValues.Length >= 3)
                            {
                                movingToX = int.Parse(movingToValues[0]);
                                movingToY = int.Parse(movingToValues[1]);
                                movingToZ = double.Parse(movingToValues[2], CultureInfo.InvariantCulture);
                            }
                            action = HAction.Move;
                            break;
                        }
                        case "sit":
                        {
                            action = HAction.Sit;
                            stance = HStance.Sit;
                            break;
                        }
                        case "lay":
                        {
                            action = HAction.Lay;
                            stance = HStance.Lay;
                            break;
                        }
                        case "sign":
                        {
                            sign = (HSign)int.Parse(actionValues[1]);
                            action = HAction.Sign;
                            break;
                        }
                    }
                    #endregion
                }

                var entityAction = new HEntityAction(isEmpowered, index, new HPoint(x, y, z),
                    new HPoint(movingToX, movingToY, movingToZ), sign, stance, headDirection, bodyDirection, action);

                entityActionList.Add(entityAction);
            }
            return entityActionList;
        }

        public override string ToString()
        {
            return $"{nameof(IsEmpowered)}: {IsEmpowered}, {nameof(Index)}: {Index}, {nameof(Tile)}: {Tile}, " +
                $"{nameof(MovingTo)}: {MovingTo}, {nameof(HeadDirection)}: {HeadDirection}, " +
                $"{nameof(BodyDirection)}: {BodyDirection}, {nameof(LastAction)}: {LastAction}";
        }
    }
}