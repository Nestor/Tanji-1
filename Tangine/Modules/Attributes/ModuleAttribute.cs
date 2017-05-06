using System;

namespace Tangine.Modules
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ModuleAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public ModuleAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}