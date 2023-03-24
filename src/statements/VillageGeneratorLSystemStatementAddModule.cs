using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

using TCParser;

namespace VillageResearch
{
    public class StatementAddModule : Statement
    {
        private string moduleName;

        public StatementAddModule(string moduleName)
        {
            this.moduleName = moduleName;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            Module newModule = new Module(moduleName, selection.position, selection.size, selection.rotation, module.GetVariablesCopy());

            module.Grammar.InsertModule(module, newModule);

            return selection;
        }
    }
}