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
    public class StatementSetVariable : Statement
    {
        private string varName;
        private string assignment;

        public StatementSetVariable(string varName, string assignment)
        {
            this.varName = varName;
            this.assignment = assignment;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            module.SetVariable(varName, MathParser.ParseString(assignment, module.variableContext));

            return selection;
        }
    }
}