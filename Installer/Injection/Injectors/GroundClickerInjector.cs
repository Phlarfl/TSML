using Mono.Cecil;
using System;
using System.Reflection;

namespace Installer.Injection.Injectors
{
    public class GroundClickerInjector : Injector
    {
        public override void Process(Assembly assembly, AssemblyDefinition assemblyDefinition)
        {
            var moduleDefinition = assemblyDefinition.MainModule;
            var method = new InjectionMethod(moduleDefinition, "Placemaker.GroundClicker", "AddClick");
            InsertEventHandlerBefore(assembly, assemblyDefinition, method, "TSML.Event.EventGroundClickerAddClick", new Type[] { assembly.GetType("Placemaker.GroundClicker") });
        }
    }
}
