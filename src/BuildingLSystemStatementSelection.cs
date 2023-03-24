using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public struct StatementSelection
	{
		public Shape shape;

		public Vec3f position;
		public float[] rotation;
		public Vec3f size;

		public StatementSelection(Shape shape, Vec3f position, float[] rotation, Vec3f size)
		{
			this.shape = shape;

			this.position = position;
			this.rotation = rotation;
			this.size = size;
		}
	}
}