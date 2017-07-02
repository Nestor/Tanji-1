using Tanji.Helpers;

namespace Tanji.Services.Injection.Constructer.Models
{
    public class Chunk : ObservableObject
    {
        private string _type = null;
        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                RaiseOnPropertyChanged();
            }
        }

        private object _value = null;
        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                RaiseOnPropertyChanged();
            }
        }

        public Command RemoveCommand { get; }

        public Chunk(string type, object value)
        {
            Type = type;
            Value = value;

            RemoveCommand = new Command(Remove);
        }

        private void Remove(object obj)
        {

        }
    }
}