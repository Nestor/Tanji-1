using System;

namespace Tangine.Modules
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DataCaptureAttribute : Attribute
    {
        public string Hash { get; }

        public ushort Id { get; }
        public bool IsOutgoing { get; }

        public DataCaptureAttribute(string hash)
        {
            Hash = hash;
        }
        public DataCaptureAttribute(ushort id, bool isOutgoing)
        {
            Id = id;
            IsOutgoing = isOutgoing;
        }
    }
}