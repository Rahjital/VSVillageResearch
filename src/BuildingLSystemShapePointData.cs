using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public struct ShapePointData
	{
		public BlockPos blockPos;
		public Vec3f rotation;

		public ShapePointData(BlockPos blockPos, Vec3f rotation)
		{
			this.blockPos = blockPos;
			this.rotation = rotation;
		}
	}
}