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
            AddClick(assembly, assemblyDefinition, moduleDefinition);
            RemoveClick(assembly, assemblyDefinition, moduleDefinition);
        }

        private void AddClick(Assembly assembly, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition)
        {
            var method = new InjectionMethod(moduleDefinition, "Placemaker.GroundClicker", "AddClick");
            InsertEventHandlerBefore(assembly, assemblyDefinition, method, "TSML.Event.EventGroundClickerAddClick", new Type[] { assembly.GetType("Placemaker.GroundClicker") });
        }

        private void RemoveClick(Assembly assembly, AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition)
        {
            var method = new InjectionMethod(moduleDefinition, "Placemaker.GroundClicker", "RemoveClick");
            InsertEventHandlerBefore(assembly, assemblyDefinition, method, "TSML.Event.EventGroundClickerRemoveClick", new Type[] { assembly.GetType("Placemaker.GroundClicker") });
        }
    }
}
