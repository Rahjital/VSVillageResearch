using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public class StatementList
	{
		public int StatementCount => statements.Count;

		private List<Statement> statements = new List<Statement>();

		public StatementList() {}
		public StatementList(params Statement[] statements)
		{
			foreach (Statement statement in statements)
			{
				AddStatement(statement);
			}
		}

		public void AddStatement(Statement statement, int sourceLine = -1)
		{
			statements.Add(statement);

			if (sourceLine > 0)
			{
				statement.SetSourceLine(sourceLine);
			}
		}

		public Statement GetStatementAt(int index)
		{
			return statements[index];
		}

		public void ExecuteAll(Module module, StatementSelection statementSelection)
		{
			foreach (Statement statement in statements)
			{
				statementSelection = statement.Execute(module, statementSelection);
			}
		}

		public StatementSelection ExecuteAt(Module module, StatementSelection statementSelection, int index)
		{
			return statements[index].Execute(module, statementSelection);
		}
	}
}