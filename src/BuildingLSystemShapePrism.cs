using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public static class ShapePrism
	{
		public static IEnumerable<ShapePointData> IterateBlocks(StatementSelection selection)
		{
			Vec3f rotation = MathHelper.RotateVector(new Vec3f(0f, 0f, 1f), selection.rotation);

			int yMin = (int)(-selection.size.Y / 2f);
			//int yMax = (int)(selection.size.Y / 2f) - (Math.Round(selection.size.Y) % 2 == 0 ? 1 : 0);
			int yMax = (int)(selection.size.Y / 2f) - (selection.size.Y % 2 == 0 ? 1 : 0);
			int zMin = (int)(-selection.size.Z / 2f);
			//int zMax = (int)(selection.size.Z / 2f) - (Math.Round(selection.size.Z) % 2 == 0 ? 1 : 0);
			int zMax = (int)(selection.size.Z / 2f) - (selection.size.Z % 2 == 0 ? 1 : 0);

			//float finalXRatio = (selection.size.X % 2 == 0 ? 2f : 1f) / selection.size.X;

			// For each Y level
			for (int dy = yMin; dy <= yMax; dy++)
			{
				//float xRatio = finalXRatio + ((float)(yMax - dy) / (float)(yMax - yMin)) * (1f - finalXRatio);

				float xRatio = (float)(yMax - dy) / (float)(yMax - yMin);

				int xCenterDistance = (int)Math.Ceiling((selection.size.X - 1) / 2f * xRatio);
				//newXSize = newXSize > 1 ? newXSize : 1; 

				// Shortening on Z axis as well would turn the prism into a pyramid
				//int xMin = (int)(-selection.size.X / 2f * ratio) + (selection.size.X % 2 == 0 ? 1 : 0);
				//int xMax = (int)Math.Ceiling(selection.size.X / 2f * ratio) + (selection.size.X % 2 == 0 ? 1 : 0);

				int xMin = -xCenterDistance;
				//int xMax = (int)(selection.size.X / 2f * ratio) - (Math.Round(selection.size.X * ratio) % 2 == 0 ? 1 : 0);

				//int xMax = xCenterDistance - (xCenterDistance > 0 && (xCenterDistance * 2) % 2 == 0 ? 1 : 0) + (selection.size.X % 2 == 0 ? 1 : 0);
				int xMax = xCenterDistance + (selection.size.X % 2 == 0 ? 1 : 0);

				for (int dx = xMin; dx <= xMax; dx++)
				{
					for (int dz = zMin; dz <= zMax; dz++)
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
			// slopes, bases (gables?), floor

			if (subshapeType == "slopes" || subshapeType == "all")
			{
				float xDiff = selection.size.X / 2f;
				float yDiff = selection.size.Y;

				float angle = (float)Math.Atan2(yDiff, xDiff) * GameMath.RAD2DEG;

				float width = (float)(Math.Sqrt(xDiff * xDiff + yDiff * yDiff));

				float offset = (int)(selection.size.X / 4f) + ((int)width % 2 == 0 ? 0.5f : 0f);

				yield return new StatementSelection(Shape.Box, 
					selection.position.AddCopy(-offset, 0, 0), 
					MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Z, -angle),
					new Vec3f(width, 1, selection.size.Z));

				yield return new StatementSelection(Shape.Box, 
					selection.position.AddCopy(offset, 0, 0), 
					MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Z, angle),
					new Vec3f(width, 1, selection.size.Z));
			}

			if (subshapeType == "bases" || subshapeType == "all")
			{
				yield return new StatementSelection(Shape.Prism, 
					selection.position.AddCopy(0, 0, selection.size.Z / 2f - (selection.size.Z % 2 == 1 ? 1 : 0)), 
					selection.rotation,
					new Vec3f(selection.size.X, selection.size.Y, 1));

				yield return new StatementSelection(Shape.Prism, 
					selection.position.AddCopy(0, 0, -selection.size.Z / 2f), 
					MathHelper.RotateMatrix(selection.rotation, MathHelper.Axis.Y, 180f),
					new Vec3f(selection.size.X, selection.size.Y, 1));
			}
		}
	}
}