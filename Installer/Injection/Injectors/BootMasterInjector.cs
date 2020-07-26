using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Reflection;

namespace Installer.Injection.Injectors
{
    public class BootMasterInjector : Injector
    {
        public override void Process(Assembly assembly, AssemblyDefinition assemblyDefinition)
        {
            var moduleDefinition = assemblyDefinition.MainModule;
            var method = new InjectionMethod(moduleDefinition, "Placemaker.BootMaster", "OnEnable");
            var methodCall = moduleDefinition.ImportReference(assembly.GetType("TSML.Core").GetMethod("Init", new Type[] { }));
            method.MethodDef.Body.Instructions.Insert(3, method.MethodDef.Body.GetILProcessor().Create(OpCodes.Call, methodCall));
        }
    }
}
