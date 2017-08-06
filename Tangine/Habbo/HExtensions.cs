using System;

namespace Tangine.Habbo
{
    public static class HExtensions
    {
        private static readonly Random _rng;

        static HExtensions()
        {
            _rng = new Random();
        }

        public static HDirection ToLeft(this HDirection facing)
        {
            return (HDirection)(((int)facing - 1) & 7);
        }
        public static HDirection ToRight(this HDirection facing)
        {
            return (HDirection)(((int)facing + 1) & 7);
        }

        public static HSign GetRandomSign()
        {
            return (HSign)_rng.Next(0, 19);
        }
        public static HTheme GetRandomTheme()
        {
            return (HTheme)((_rng.Next(1, 7) + 2) & 7);
        }
    }
}