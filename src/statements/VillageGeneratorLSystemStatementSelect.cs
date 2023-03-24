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
    public class StatementSelect : Statement
    {
        private string subshapeType;
        private StatementList statementList;

        public StatementSelect(string subshapeType, StatementList statementList)
        {
            this.subshapeType = subshapeType;
            this.statementList = statementList;
        }

        public override StatementSelection Execute(Module module, StatementSelection selection)
        {
            foreach (StatementSelection subshapeSelection in ShapeProcessor.SelectSubshapes(selection, subshapeType))
            {
                module.Grammar.AddStatementListData(module, statementList, subshapeSelection);
            }

            return selection;
        }
    }
}