using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Installer
{
    public class InjectionMethod
    {
        public ModuleDefinition Module { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public TypeDefinition TypeDef { get; set; }
        public MethodDefinition MethodDef { get; set; }

        public InjectionMethod(ModuleDefinition module, string className, string methodName)
        {
            Module = module;
            ClassName = className;
            MethodName = methodName;

            TypeDef = Util.GetDefinition(Module.Types, ClassName, true);
            MethodDef = TypeDef == null ? null : Util.GetDefinition(TypeDef.Methods, MethodName, false);
        }
    }
}
