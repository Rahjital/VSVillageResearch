using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	// Consider implementing one of the weighted random methods from https://blog.bruce-hill.com/a-faster-weighted-random-choice
	public class BlockTypeRandom : BlockType
	{
		private struct RandomBlock
		{
			public int id;
			public float weight;

			public RandomBlock(int id, float weight)
			{
				this.id = id;
				this.weight = weight;
			}
		}

		private static Random random;

		public bool isSorted = false;
		private List<RandomBlock> randomBlocks = new List<RandomBlock>();

		float weightSum;

		public BlockTypeRandom()
		{
			random = random ?? new Random();
		}

		public void AddRandomBlock(string blockName, float weight)
		{
			AddRandomBlock(BlockPalette.GetBlockIdByName(blockName), weight);
		}

		public void AddRandomBlock(int blockId, float weight)
		{
			randomBlocks.Add(new RandomBlock(blockId, weight));
			weightSum += weight;

			isSorted = false;
		}

		public override int GetBlockExpression(ShapePointData shapePointData)
		{
			if (!isSorted)
			{
				randomBlocks.Sort((b1, b2) => b1.weight.CompareTo(b2.weight));
				isSorted = true;
			}

			float rnd = (float)random.NextDouble() * weightSum;

			foreach (RandomBlock randomBlock in randomBlocks)
			{
				rnd -= randomBlock.weight;

				if (rnd < 0)
				{
					return randomBlock.id;
				}
			}

			return randomBlocks[randomBlocks.Count - 1].id;
		}
	}
}