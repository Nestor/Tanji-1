#define DEBUG_CONSTRUCTER

using System.Collections.ObjectModel;

using Tanji.Helpers;
using Tanji.Services.Injection.Constructer.Models;

namespace Tanji.Services.Injection.Constructer
{
    public class ConstructerViewModel : ObservableObject
    {
        public ObservableCollection<Chunk> Chunks { get; }

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

        public ConstructerViewModel()
        {
            Chunks = new ObservableCollection<Chunk>();

#if DEBUG_CONSTRUCTER
            for (int i = 0; i < 10; i++)
            {
                Chunks.Add(new Chunk());
            }
#endif
        }
    }
}