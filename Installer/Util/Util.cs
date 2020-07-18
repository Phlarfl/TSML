using Mono.Cecil;
using Mono.Collections.Generic;

namespace Installer
{
    public class Util
    {
        public static T GetDefinition<T>(Collection<T> collection, string name, bool full) where T : IMemberDefinition
        {
            foreach (var item in collection)
                if (full ? item.FullName == name : item.Name == name)
                    return item;
            return default;
        }
    }
}
