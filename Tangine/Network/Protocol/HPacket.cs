﻿using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tangine.Network.Protocol
{
    [DebuggerDisplay("Id: {Id} | {ToString()}")]
    public abstract class HPacket
    {
        private byte[] _toBytesCache;
        private string _toStringCache;
        private readonly List<byte> _body;

        private static readonly Regex _structurePattern;

        private int _position;
        public int Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private ushort _id;
        public ushort Id
        {
            get { return _id; }
            set
            {
                if (_id == value) return;
                _id = value;

                if (_toBytesCache != null || _toStringCache != null)
                {
                    byte[] idData = Resolver.GetBytes(value);
                    if (_toBytesCache != null)
                    {
                        Resolver.PlaceBytes(idData, _toBytesCache, Resolver.IdPosition);
                    }
                    if (_toStringCache != null)
                    {
                        char[] characters = _toStringCache.ToCharArray();
                        characters[Resolver.IdPosition] = (char)idData[0];
                        characters[Resolver.IdPosition + 1] = (char)idData[1];
                        _toStringCache = new string(characters);
                    }
                }
            }
        }

        public HEncoding Resolver { get; }
        public int BodyLength => _body.Count;
        public int ReadableBytes => GetReadableBytes(Position);

        static HPacket()
        {
            _structurePattern = new Regex(@"{(?<kind>id|i|s|b|d|u):(?<value>[^}]*)\}", RegexOptions.IgnoreCase);
        }
        public HPacket(HEncoding resolver)
        {
            _body = new List<byte>();

            Resolver = resolver;
        }
        public HPacket(HEncoding resolver, IList<byte> data)
            : this(resolver)
        {
            _body.AddRange(resolver.GetBody(data));

            _toBytesCache = new byte[data.Count];
            data.CopyTo(_toBytesCache, 0);

            Id = resolver.GetId(data);
        }

        public int ReadInt32()
        {
            return ReadInt32(ref _position);
        }
        public int ReadInt32(int position)
        {
            return ReadInt32(ref position);
        }
        public virtual int ReadInt32(ref int position)
        {
            int value = Resolver.ReadInt32(_body, position);
            position += Resolver.GetSize(value);
            return value;
        }

        public string ReadUTF8()
        {
            return ReadUTF8(ref _position);
        }
        public string ReadUTF8(int position)
        {
            return ReadUTF8(ref position);
        }
        public virtual string ReadUTF8(ref int position)
        {
            string value = Resolver.ReadUTF8(_body, position);
            position += Resolver.GetSize(value);
            return value;
        }

        public bool ReadBoolean()
        {
            return ReadBoolean(ref _position);
        }
        public bool ReadBoolean(int position)
        {
            return ReadBoolean(ref position);
        }
        public virtual bool ReadBoolean(ref int position)
        {
            bool value = Resolver.ReadBoolean(_body, position);
            position += Resolver.GetSize(value);
            return value;
        }

        public ushort ReadUInt16()
        {
            return ReadUInt16(ref _position);
        }
        public ushort ReadUInt16(int position)
        {
            return ReadUInt16(ref position);
        }
        public virtual ushort ReadUInt16(ref int position)
        {
            ushort value = Resolver.ReadUInt16(_body, position);
            position += Resolver.GetSize(value);
            return value;
        }

        public double ReadDouble()
        {
            return ReadDouble(ref _position);
        }
        public double ReadDouble(int position)
        {
            return ReadDouble(ref position);
        }
        public virtual double ReadDouble(ref int position)
        {
            double value = Resolver.ReadDouble(_body, position);
            position += Resolver.GetSize(value);
            return value;
        }

        public byte ReadByte()
        {
            return ReadByte(ref _position);
        }
        public byte ReadByte(int position)
        {
            return ReadByte(ref position);
        }
        public virtual byte ReadByte(ref int position)
        {
            return _body[position++];
        }

        public byte[] ReadBytes(int length)
        {
            return ReadBytes(length, ref _position);
        }
        public byte[] ReadBytes(int length, int position)
        {
            return ReadBytes(length, ref position);
        }
        public virtual byte[] ReadBytes(int length, ref int position)
        {
            var chunk = new byte[length];
            for (int i = 0; i < length; i++)
            {
                chunk[i] = _body[position++];
            }
            return chunk;
        }

        private void ResetCache()
        {
            _toBytesCache = null;
            _toStringCache = null;
        }
        public int GetReadableBytes(int position)
        {
            return (_body.Count - position);
        }

        protected virtual byte[] AsBytes()
        {
            return Resolver.Construct(Id, _body);
        }
        protected virtual string AsString()
        {
            string result = Encoding.Default.GetString(ToBytes());
            for (int i = 0; i <= 13; i++)
            {
                result = result.Replace(((char)i).ToString(),
                    ("[" + i + "]"));
            }
            return result;
        }

        public byte[] ToBytes()
        {
            if (_toBytesCache != null)
            {
                return _toBytesCache;
            }
            return (_toBytesCache = AsBytes());
        }
        public override string ToString()
        {
            if (_toStringCache != null)
            {
                return _toStringCache;
            }
            return (_toStringCache = AsString());
        }

        public static byte[] ToBytes(HEncoding resolver, string signature)
        {
            for (int i = 0; i <= 13; i++)
            {
                signature = signature.Replace(("[" + i + "]"),
                    ((char)i).ToString());
            }

            int openBraceIndex = signature.IndexOf('{');
            if (openBraceIndex != -1)
            {
                MatchCollection matches = _structurePattern.Matches(signature, openBraceIndex);

                var values = new List<object>(matches.Count);
                var replacements = new Dictionary<string, string>(matches.Count);
                foreach (Match match in matches)
                {
                    string value = match.Groups["value"].Value;
                    string kind = match.Groups["kind"].Value.ToLower();
                    switch (kind)
                    {
                        case "id":
                        {
                            break;
                        }
                        case "i": values.Add(int.Parse(value)); break;
                        case "s": values.Add(value); break;
                        case "b":
                        {
                            byte bValue = 0;
                            value = value.Trim().ToLower();
                            if (!byte.TryParse(value, out bValue))
                            {
                                values.Add(value == "true");
                            }
                            else values.Add(bValue);
                            break;
                        }
                        case "d": values.Add(double.Parse(value)); break;
                        case "u": values.Add(ushort.Parse(value)); break;
                    }
                }
            }
            return Encoding.Default.GetBytes(signature);
        }
    }
}