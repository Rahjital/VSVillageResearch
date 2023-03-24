using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public static class ShapeBox
	{
		public static IEnumerable<ShapePointData> IterateBlocks(StatementSelection selection)
		{
			Vec3f rotation = MathHelper.RotateVector(new Vec3f(0f, 0f, 1f), selection.rotation);

			/*int xMin = (int)(-selection.size.X / 2f) + (selection.size.X % 2 == 0 ? 1 : 0);
			int xMax = (int)Math.Ceiling(selection.size.X / 2f) + (selection.size.X % 2 == 0 ? 1 : 0);
			int yMin = (int)(-selection.size.Y / 2f) + (selection.size.Y % 2 == 0 ? 1 : 0);
			int yMax = (int)Math.Ceiling(selection.size.Y / 2f) + (selection.size.Y % 2 == 0 ? 1 : 0);
			int zMin = (int)(-selection.size.Z / 2f) + (selection.size.Z % 2 == 0 ? 1 : 0);
			int zMax = (int)Math.Ceiling(selection.size.Z / 2f) + (selection.size.Z % 2 == 0 ? 1 : 0);

			for (int dx = xMin; dx < xMax; dx++)
			{
				for (int dz = zMin; dz < zMax; dz++)
				{
					for (int dy = yMin; dy < yMax; dy++)
					{
						Vec3f rotatedPosition = MathHelper.RotateVector(new Vec3f(dx, dy, dz), selection.rotation);

						float rotatedX = selection.rotation[0] * dx + selection.rotation[1] * dz + selection.rotation[2] * dy;
						float rotatedY = selection.rotation[6] * dx + selection.rotation[7] * dz + selection.rotation[8] * dy;
						float rotatedZ = selection.rotation[3] * dx + selection.rotation[4] * dz + selection.rotation[5] * dy;

						int actualX = (int)(selection.position.X + rotatedPosition.X);
						int actualY = (int)(selection.position.Y + rotatedPosition.Y);
						int actualZ = (int)(selection.position.Z + rotatedPosition.Z);

						yield return new ShapePointData(new BlockPos(actualX, actualY, actualZ), rotation);
					}
				}
			}*/

			/*int xMin = (int)(-selection.size.X / 2f) + (selection.size.X % 2 == 0 ? 1 : 0);
			int xMax = (int)(selection.size.X / 2f) + (selection.size.X % 2 == 0 ? 1 : 0);
			int yMin = (int)(-selection.size.Y / 2f) + (selection.size.Y % 2 == 0 ? 1 : 0);
			int yMax = (int)(selection.size.Y / 2f) + (selection.size.Y % 2 == 0 ? 1 : 0);
			int zMin = (int)(-selection.size.Z / 2f) + (selection.size.Z % 2 == 0 ? 1 : 0);
			int zMax = (int)(selection.size.Z / 2f) + (selection.size.Z % 2 == 0 ? 1 : 0);*/

			int xMin = (int)(-selection.size.X / 2f);
			//int xMax = (int)(selection.size.X / 2f) - (Math.Round(selection.size.X) % 2 == 0 ? 1 : 0);
			int xMax = (int)(selection.size.X / 2f) - (selection.size.X % 2 == 0 ? 1 : 0);
			int yMin = (int)(-selection.size.Y / 2f);
			//int yMax = (int)(selection.size.Y / 2f) - (Math.Round(selection.size.Y) % 2 == 0 ? 1 : 0);
			int yMax = (int)(selection.size.Y / 2f) - (selection.size.Y % 2 == 0 ? 1 : 0);
			int zMin = (int)(-selection.size.Z / 2f);
			//int zMax = (int)(selection.size.Z / 2f) - (Math.Round(selection.size.Z) % 2 == 0 ? 1 : 0);
			int zMax = (int)(selection.size.Z / 2f) - (selection.size.Z % 2 == 0 ? 1 : 0);

			for (int dx = xMin; dx <= xMax; dx++)
			{
				for (int dz = zMin; dz <= zMax; dz++)
				{
					for (int dy = yMin; dy <= yMax; dy++)
					{
						Vec3f rotatedPosition = MathHelper.RotateVector(new Vec3f(dx, dy, dz), selection.rotation);

						float rotatedX = selection.rotation[0] * dx + selection.rotation[1] * dz + selection.rotation[2] * dy;
						float rotatedY = selection.rotation[6] * dx + selection.rotation[7] * dz + selection.rotation[8] * dy;
						float rotatedZ = selection.rotation[3] * dx + selection.rotation[4] * dz + selection.rotation[5] * dy;

						int actualX = (int)(selection.position.X + rotatedPosition.X);
						int actualY = (int)(selection.position.Y + rotatedPosition.Y);
						int actualZ = (int)(selection.position.Z + rotatedPosition.Z);

						yield return new ShapePointData(new BlockPos(actualX, actualY, actualZ), rotation);
					}
				}
			}
		}

		// Subshape quirks
		// - sides always occupy the entire surface of the wall, corners included; this means neighbouring sides will intersect
		// shrink some sides by 1 block on the Z and Y axes if this is undesirable
		// - same applies to edges
		public static IEnumerable<StatementSelection> SelectSubshapes(StatementSelection selection, string subshapeType)
		{
			// todo:
			// all_sides, walls (vertical), roof, floor
			// all_edges, vertical_edges, roof_edges, floor_edges
			// corners

			// walls (ie vertical sides of the box)
			if (subshapeType == "walls" || subshapeType == "all_sides")
			{
				float[] perpendicularWallRot = MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Y, 90f);

				yield return new StatementSelection(Shape.Box, 
					selection.position.AddCopy(-selection.size.X / 2, 0, 0), 
					selection.rotation,
					new Vec3f(1f, selection.size.Y, selection.size.Z));

				yield return new StatementSelection(Shape.Box, 
					selection.position.AddCopy(0, 0, -selection.size.Z / 2), 
					MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Y, 90f),
					new Vec3f(1f, selection.size.Y, selection.size.Z));

				yield return new StatementSelection(Shape.Box, 
					selection.position.AddCopy(selection.size.X / 2 - 1f, 0, 0), 
					MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Y, 180f),
					new Vec3f(1f, selection.size.Y, selection.size.Z));

				yield return new StatementSelection(Shape.Box, 
					selection.position.AddCopy(0, 0, selection.size.Z / 2 - 1f), 
					MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Y, 270f),
					new Vec3f(1f, selection.size.Y, selection.size.Z));
			}
		}
	}
}