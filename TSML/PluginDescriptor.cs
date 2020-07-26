using System;

namespace TSML
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginDescriptor : Attribute
    {
        public string Name { get; set; }
        public string[] Authors { get; set; }
        public int[] Version { get; set; }

        public PluginDescriptor(string name, string[] authors, int[] version)
        {
            Name = name;
            Authors = authors;
            Version = version;
        }
    }
}
