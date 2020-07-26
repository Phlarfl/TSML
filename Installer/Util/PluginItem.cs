namespace Installer.Util
{
    public class PluginItem
    {
        public string Name { get; set; }
        public string[] Authors { get; set; }
        public int[] Version { get; set; }
        public string Download { get; set; }
        public bool Featured { get; set; }
        public Dependency[] Dependencies { get; set; }

        public PluginItem(string name, string[] authors, int[] version, string download, bool featured, Dependency[] dependencies)
        {
            Name = name;
            Authors = authors;
            Version = version;
            Download = download;
            Featured = featured;
            Dependencies = dependencies;
        }

        public override string ToString()
        {
            return $"{Name}{(Version != null ? $" v{string.Join(".", Version)}" : "")}{(Authors != null ? $" By {string.Join(", ", Authors)}" : "")}";
        }
    }

    public class Dependency
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public Dependency(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}
