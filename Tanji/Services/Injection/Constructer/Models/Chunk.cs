using System;
using Tanji.Helpers;

namespace Tanji.Services.Injection.Constructer.Models
{
    public class Chunk : ObservableObject
    {
        private string _value = null;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                RaiseOnPropertyChanged();
            }
        }

        public Chunk()
        {
            Value = Guid.NewGuid().ToString();
        }
    }
}