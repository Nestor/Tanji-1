using System;
using System.Reflection;

namespace Tangine.Modules
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class DataCaptureAttribute : Attribute
    {
        public ushort Id { get; }
        public string Hash { get; }

        public bool IsOutgoing { get; }

        internal MethodInfo Method { get; set; }

        public DataCaptureAttribute(string hash, bool isOutgoing)
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