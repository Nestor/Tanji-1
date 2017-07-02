#define DEBUG_CONSTRUCTER

using System.Collections.ObjectModel;

using Tanji.Helpers;
using Tanji.Services.Injection.Constructer.Models;

namespace Tanji.Services.Injection.Constructer
{
    public class ConstructerViewModel : ObservableObject
    {
        public ObservableCollection<Chunk> Chunks { get; }

        private ushort _id = 0;
        public ushort Id
        {
            get => _id;
            set
            {
                _id = value;
                RaiseOnPropertyChanged();
            }
        }

        private ushort _quantity = 1;
        public ushort Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                RaiseOnPropertyChanged();
            }
        }

        private Chunk _selectedChunk = null;
        public Chunk SelectedChunk
        {
            get => _selectedChunk;
            set
            {
                _selectedChunk = value;
                RaiseOnPropertyChanged();
            }
        }

        public Command RemoveCommand { get; }

        public ConstructerViewModel()
        {
            RemoveCommand = new Command(Remove);
            Chunks = new ObservableCollection<Chunk>();

#if DEBUG_CONSTRUCTER
            Chunks.Add(new Chunk("String", "Random String Value"));
            Chunks.Add(new Chunk("Int32", int.MaxValue));
            Chunks.Add(new Chunk("UInt16", ushort.MaxValue));
            Chunks.Add(new Chunk("Boolean", bool.TrueString));
            Chunks.Add(new Chunk("Byte", byte.MaxValue));
            Chunks.Add(new Chunk("Double", double.MaxValue));
#endif
        }

        private void Remove(object obj)
        {
            Chunks.Remove((Chunk)obj);
        }
    }
}