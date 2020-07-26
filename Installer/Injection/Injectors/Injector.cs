using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Installer.Injection.Injectors
{
    public abstract class Injector
    {
        public abstract void Process(Assembly assembly, AssemblyDefinition assemblyDefinition);

        protected void InsertEventHandlerBefore(Assembly asm, AssemblyDefinition asmDef, InjectionMethod im, string eventType, Type[] eventArgumentTypes)
        {
            InsertEventHandler(true, asm, asmDef, im, eventType, eventArgumentTypes);
        }

        protected void InsertEventHandlerAfter(Assembly asm, AssemblyDefinition asmDef, InjectionMethod im, string eventType, Type[] eventArgumentTypes)
        {
            InsertEventHandler(false, asm, asmDef, im, eventType, eventArgumentTypes);
        }

        private void InsertEventHandler(bool before, Assembly asm, AssemblyDefinition asmDef, InjectionMethod im, string eventType, Type[] eventArgumentTypes)
        {
            ModuleDefinition module = asmDef.MainModule;
            MethodReference mrHandleEvent = module.ImportReference(asm.GetType("TSML.Event.EventHandler").GetMethod("OnEvent", new Type[] { asm.GetType("TSML.Event.Event") }));

            MethodReference eventCtor = module.ImportReference(asm.GetType(eventType).GetConstructor(eventArgumentTypes));

            Mono.Cecil.Cil.MethodBody body = im.MethodDef.Body;
            ILProcessor proc = body.GetILProcessor();

            Instruction target = body.Instructions[before ? 0 : body.Instructions.Count - 1];
            List<Instruction> insns = new List<Instruction>();

            // Load arguments, index 0 being the object itself
            for (int i = 0; i < eventArgumentTypes.Length; i++)
                insns.Add(proc.Create(OpCodes.Ldarg, i));

            insns.Add(proc.Create(OpCodes.Newobj, eventCtor));
            insns.Add(proc.Create(OpCodes.Call, mrHandleEvent));

            foreach (Instruction insn in insns)
                proc.InsertBefore(target, insn);
        }
    }
}
