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
	public abstract class Statement
	{
		public int SourceLine {get; private set;} = -1;
		private StatementSelection selection;

		public abstract StatementSelection Execute(Module module, StatementSelection selection);

		public void SetSelection(StatementSelection selection)
		{
			this.selection = selection;
		}

		public void SetSourceLine(int line)
		{
			SourceLine = line;
		}
	}
}