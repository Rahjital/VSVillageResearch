using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public class BlockTypeSimple : BlockType
	{
		private int blockId;

		public BlockTypeSimple(int blockId)
		{
			this.blockId = blockId;
		}

		public BlockTypeSimple(string blockName)
		{
			this.blockId = BlockPalette.GetBlockIdByName(blockName);
		}

		public override int GetBlockExpression(ShapePointData shapePointData)
		{
			return blockId;   
		}
	}
}