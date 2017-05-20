using System;
using System.Reflection;

using Tangine.Network;
using Tangine.Network.Protocol;

namespace Tangine.Modules
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class DataCaptureAttribute : Attribute
    {
        public ushort? Id { get; }
        public string Hash { get; }

        public abstract bool IsOutgoing { get; }

        internal MethodInfo Method { get; set; }

        public DataCaptureAttribute(ushort id)
        {
            Id = id;
        }
        public DataCaptureAttribute(string hash)
        {
            Hash = hash;
        }

        internal void Invoke(object container, DataInterceptedEventArgs args)
        {
            object result = Method?.Invoke(
                container, CreateValues(args, Method.GetParameters()));

            var replacement = (result as HPacket);
            if (replacement != null)
            {
                args.Packet = replacement;
            }
        }

        private object[] CreateValues(DataInterceptedEventArgs args, ParameterInfo[] parameters)
        {
            var values = new object[parameters.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int position = 0;
                ParameterInfo parameter = parameters[i];
                switch (Type.GetTypeCode(parameter.ParameterType))
                {
                    case TypeCode.Int32:
                    values[i] = args.Packet.ReadInt32(ref position);
                    break;

                    case TypeCode.UInt16:
                    {
                        if (parameter.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                        {
                            values[i] = args.Packet.Id;
                        }
                        else
                        {
                            values[i] = args.Packet.ReadUInt16(ref position);
                        }
                        break;
                    }

                    case TypeCode.Boolean:
                    values[i] = args.Packet.ReadBoolean(ref position);
                    break;

                    case TypeCode.Byte:
                    values[i] = args.Packet.ReadByte(ref position);
                    break;

                    case TypeCode.String:
                    values[i] = args.Packet.ReadUTF8(ref position);
                    break;

                    case TypeCode.Double:
                    values[i] = args.Packet.ReadDouble(ref position);
                    break;

                    case TypeCode.Object:
                    {
                        if (parameter.ParameterType == typeof(DataInterceptedEventArgs))
                        {
                            values[i] = args;
                        }
                        else if (parameter.ParameterType == typeof(byte[]))
                        {
                            int length = args.Packet.ReadInt32(ref position);
                            values[i] = args.Packet.ReadBytes(length, ref position);
                        }
                        break;
                    }
                }
            }
            return values;
        }
    }
}