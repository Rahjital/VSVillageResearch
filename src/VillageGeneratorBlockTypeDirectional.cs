using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public class BlockTypeDirectional : BlockType
	{
		public enum Direction
		{
			X_Plus,
			X_Minus,
			Y_Plus,
			Y_Minus,
			Z_Plus,
			Z_Minus
		}

		private int leftBlockId = -1;
		private int rightBlockId = -1;
		private int upBlockId = -1;
		private int downBlockId = -1;
		private int forwardBlockId = -1;
		private int backwardBlockId = -1;

		private static Vec3f left;
		private static Vec3f right;
		private static Vec3f up;
		private static Vec3f down;
		private static Vec3f forward;
		private static Vec3f backward;

		public BlockTypeDirectional()
		{
			if (forward == null)
			{
				left = new Vec3f(-1f, 0f, 0f);
				right = new Vec3f(1f, 0f, 0f);
				up = new Vec3f(0f, 1f, 0f);
				down = new Vec3f(0f, -1f, 0f);
				forward = new Vec3f(0f, 0f, -1f);
				backward = new Vec3f(0f, 0f, 1f);
			}
		}

		public void SetBlock(Direction direction, int blockId)
		{
			switch (direction)
			{
				case Direction.X_Plus: rightBlockId = blockId; break;
				case Direction.X_Minus: leftBlockId = blockId; break;
				case Direction.Y_Plus: upBlockId = blockId; break;
				case Direction.Y_Minus: downBlockId = blockId; break;
				case Direction.Z_Plus: backwardBlockId = blockId; break;
				case Direction.Z_Minus: forwardBlockId = blockId; break;
			}
		}

		public void SetBlock(Direction direction, string blockName)
		{
			SetBlock(direction, BlockPalette.GetBlockIdByName(blockName));
		}

		public override int GetBlockExpression(ShapePointData shapePointData)
		{
			int resultId = -1;
			float lastDT = float.MinValue;

			if (leftBlockId >= 0 && left.Dot(shapePointData.rotation) > lastDT)
			{
				lastDT = left.Dot(shapePointData.rotation);
				resultId = leftBlockId;
			}

			if (rightBlockId >= 0 && right.Dot(shapePointData.rotation) > lastDT)
			{
				lastDT = right.Dot(shapePointData.rotation);
				resultId = rightBlockId;
			}

			if (upBlockId >= 0 && up.Dot(shapePointData.rotation) > lastDT)
			{
				lastDT = up.Dot(shapePointData.rotation);
				resultId = upBlockId;
			}

			if (downBlockId >= 0 && down.Dot(shapePointData.rotation) > lastDT)
			{
				lastDT = down.Dot(shapePointData.rotation);
				resultId = downBlockId;
			}

			if (forwardBlockId >= 0 && forward.Dot(shapePointData.rotation) > lastDT)
			{
				lastDT = forward.Dot(shapePointData.rotation);
				resultId = forwardBlockId;
			}

			if (backwardBlockId >= 0 && backward.Dot(shapePointData.rotation) > lastDT)
			{
				lastDT = backward.Dot(shapePointData.rotation);
				resultId = backwardBlockId;
			}

			if (resultId < 0)
			{
				throw new Exception("BlockTypeDirectional could not select any valid block!");
			}

			return resultId;   
		}
	}
}