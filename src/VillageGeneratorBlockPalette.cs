using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public abstract class BlockType
	{
		public abstract int GetBlockExpression(ShapePointData shapePointData);
	}

	public static class BlockPalette
	{
		private static Dictionary<int, BlockType> palette = new Dictionary<int, BlockType>();
		private static int currentId = 0;

		private static ICoreAPI api;

		public static void Init(ICoreAPI api)
		{
			BlockPalette.api = api;
		}

		public static int RegisterBlock(BlockType type)
		{
			palette[currentId] = type;

			return currentId++;
		}

		public static int GetBlockExpression(int paletteId, ShapePointData shapePointData)
		{
			if (!palette.ContainsKey(paletteId))
			{
				throw new Exception($"Palette does not have slot {paletteId} occupied");
			}

			return palette[paletteId].GetBlockExpression(shapePointData);
		}

		public static int GetBlockIdByName(string blockName)
		{
			//return (api as ICoreServerAPI).WorldManager.GetBlockId(new AssetLocation(blockName));
			return api.World.SearchBlocks(new AssetLocation(blockName))[0].Id;
		}
	}
}