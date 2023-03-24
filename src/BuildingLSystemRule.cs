using System;
using System.Collections.Generic;
using Vintagestory.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;

using Cairo;

using TCParser;

namespace VillageResearch
{
	public class Rule
	{
		private StatementList statementList = new StatementList();

		private string condition;

		public Rule() {}
		public Rule(StatementList statementList)
		{
			this.statementList = statementList;
		}

		public void AddStatement(Statement statement)
		{
			statementList.AddStatement(statement);
		}

		public void AddCondition(string condition)
		{
			this.condition = condition;
		}

		public bool EvaluateAll(Module module)
		{
			if (!EvaluateCondition(module))
			{
				return false;
			}

			statementList.ExecuteAll(module, module.GetBaseSelection());
			return true;
		}

		public bool EvaluateCondition(Module module)
		{
			return condition == null || MathParser.ParseStringAsBool(condition, new ModuleVariableContext(module));
		}

		/*public bool ExecuteStatement(Module module, ref StatementSelection statementSelection, int statementIndex)
		{
			statementSelection = statementList.ExecuteAt(module, statementSelection, statementIndex);
			return statementIndex < (statementList.StatementCount - 1);
		}*/

		public StatementList GetStatementList()
		{
			return statementList;
		}
	}
}