using Mono.Cecil;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    public class Util
    {
        public static T GetDefinition<T>(Collection<T> collection, string name, bool full) where T : IMemberDefinition
        {
            foreach (var item in collection)
                if (full ? item.FullName == name : item.Name == name)
                    return item;
            return default(T);
        }
    }
}
