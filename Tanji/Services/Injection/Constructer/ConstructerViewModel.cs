using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Collections.Specialized;

using Microsoft.Win32;

using Tanji.Helpers;
using Tanji.Services.Injection.Constructer.Models;

namespace Tanji.Services.Injection.Constructer
{
    public class ConstructerViewModel : ObservableObject
    {
        private readonly SaveFileDialog _saveChunksDialog;
        private readonly OpenFileDialog _loadChunksDialog;

        private const byte MAX_CHUNKS = byte.MaxValue;

        private ushort _id;
        public ushort Id
        {
            get => _id;
            set
            {
                _id = value;
                RaiseOnPropertyChanged();
                RaiseOnPropertyChanged(nameof(Signature));
            }
        }

        private byte _quantity = 1;
        public byte Quantity
        {
            get => _quantity;
            set
            {
                if (value == 0)
                {
                    value = 1;
                }

                _quantity = value;
                RaiseOnPropertyChanged();
            }
        }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                RaiseOnPropertyChanged();
            }
        }

        public string Signature
        {
            get => ("{id:" + Id + "}" + string.Join(string.Empty, Chunks));
        }

        public Command CopyCommand { get; }
        public Command SaveCommand { get; }
        public Command LoadCommand { get; }
        public Command ClearCommand { get; }
        public Command<Type> WriteCommand { get; }
        public ObservableRangeCollection<Chunk> Chunks { get; }

        public ConstructerViewModel()
        {
            _saveChunksDialog = new SaveFileDialog
            {
                DefaultExt = "chks",
                Title = "Tanji - Save Chunks",
                Filter = "Chunks (*.chks)|*.chks"
            };
            _loadChunksDialog = new OpenFileDialog
            {
                Title = "Tanji - Load Chunks",
                Filter = "Chunks (*.chks)|*.chks"
            };

            CopyCommand = new Command(Copy);
            SaveCommand = new Command(Save, CanSave);
            LoadCommand = new Command(Load);

            ClearCommand = new Command(Clear, CanClear);
            WriteCommand = new Command<Type>(Write, CanWrite);

            Chunks = new ObservableRangeCollection<Chunk>();
            Chunks.CollectionChanged += Chunks_CollectionChanged;
        }

        private void Chunk_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Chunk.Value)) return;
            RaiseOnPropertyChanged(nameof(Chunks));
        }
        private void Chunks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Chunk chunk in e.NewItems)
                {
                    chunk.PropertyChanged += Chunk_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Chunk chunk in e.OldItems)
                {
                    chunk.PropertyChanged -= Chunk_PropertyChanged;
                }
            }
            RaiseOnPropertyChanged(nameof(Chunks));
            RaiseOnPropertyChanged(nameof(Signature));
        }

        public void Write(Type targetType)
        {
            object value = Convert(Value, targetType);
            for (int i = 0; i < Quantity; i++)
            {
                Chunks.Add(new Chunk(this, value));
            }
        }
        private bool CanWrite(Type targetType)
        {
            if ((Quantity + Chunks.Count) > MAX_CHUNKS)
            {
                return false;
            }
            return (Convert(Value, targetType) != null);
        }

        private void Clear(object obj)
        {
            Chunks.Clear();
        }
        private bool CanClear(object obj)
        {
            return (Chunks.Count > 0);
        }

        private void Copy(object obj)
        {
            Clipboard.SetText(Signature);
        }
        private void Load(object obj)
        {
            _loadChunksDialog.FileName = string.Empty;
            if (_loadChunksDialog.ShowDialog() ?? false)
            {
                using (var chksStream = File.OpenRead(_loadChunksDialog.FileName))
                using (var chksReader = new BinaryReader(chksStream))
                {
                    Id = chksReader.ReadUInt16();
                    int chunkCount = chksReader.ReadInt32();

                    Chunks.Clear();
                    for (int i = 0; i < chunkCount; i++)
                    {
                        object value = null;
                        var code = (TypeCode)chksReader.ReadByte();
                        switch (code)
                        {
                            case TypeCode.String:
                            value = chksReader.ReadString();
                            break;

                            case TypeCode.Byte:
                            value = chksReader.ReadByte();
                            break;

                            case TypeCode.Int32:
                            value = chksReader.ReadInt32();
                            break;

                            case TypeCode.Boolean:
                            value = chksReader.ReadBoolean();
                            break;

                            case TypeCode.UInt16:
                            value = chksReader.ReadUInt16();
                            break;

                            case TypeCode.Double:
                            value = chksReader.ReadDouble();
                            break;
                        }
                        Chunks.Add(new Chunk(this, value));
                    }
                }
            }
        }

        private void Save(object obj)
        {
            _saveChunksDialog.FileName = string.Empty;
            if (_saveChunksDialog.ShowDialog() ?? false)
            {
                using (var chksStream = File.Open(_saveChunksDialog.FileName, FileMode.Create))
                using (var chksWriter = new BinaryWriter(chksStream))
                {
                    chksWriter.Write(Id);
                    chksWriter.Write(Chunks.Count);
                    foreach (Chunk chunk in Chunks)
                    {
                        var code = Type.GetTypeCode(chunk.Type);
                        chksWriter.Write((byte)code);
                        switch (code)
                        {
                            case TypeCode.String:
                            chksWriter.Write((string)chunk.Value);
                            break;
                            case TypeCode.Byte:
                            chksWriter.Write((byte)chunk.Value);
                            break;
                            case TypeCode.Int32:
                            chksWriter.Write((int)chunk.Value);
                            break;
                            case TypeCode.Boolean:
                            chksWriter.Write((bool)chunk.Value);
                            break;
                            case TypeCode.UInt16:
                            chksWriter.Write((ushort)chunk.Value);
                            break;
                            case TypeCode.Double:
                            chksWriter.Write((double)chunk.Value);
                            break;
                        }
                    }
                }
            }
        }
        private bool CanSave(object obj)
        {
            return (Chunks.Count > 0);
        }

        private object Convert(string input, Type targetType)
        {
            switch (Type.GetTypeCode(targetType))
            {
                case TypeCode.String: return input;
                case TypeCode.Byte:
                {
                    byte bValue = 0;
                    if (byte.TryParse(input, out bValue))
                    {
                        return bValue;
                    }
                    break;
                }
                case TypeCode.Int32:
                {
                    int iValue = 0;
                    if (int.TryParse(input, out iValue))
                    {
                        return iValue;
                    }
                    break;
                }
                case TypeCode.Boolean:
                {
                    bool bValue = false;
                    if (bool.TryParse(input, out bValue))
                    {
                        return bValue;
                    }
                    break;
                }
                case TypeCode.UInt16:
                {
                    ushort uValue = 0;
                    if (ushort.TryParse(input, out uValue))
                    {
                        return uValue;
                    }
                    break;
                }
                case TypeCode.Double:
                {
                    double dValue = 0;
                    if (double.TryParse(input, out dValue))
                    {
                        return dValue;
                    }
                    break;
                }
            }
            return null;
        }
    }
}