using System;
using System.IO;
using System.Collections;
using System.Text;

using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public static class MathHelper
	{
		public enum Axis
		{
			X,
			Y,
			Z
		}

		public static float[] RotateMatrix(float[] originalMatrix, Axis axis, float degrees)
		{
			float rads = degrees * GameMath.DEG2RAD;

			float[] rotationMatrix;

			switch (axis)
			{
				// X and Z axes are switched around from Wikipedia (VS and other game engines use X and Z for horizontal and Y for vertical, 
				// unlike the wiki which uses X and Y for horizonal and Z for vertical; this switches X and Z rotation axes round)
				case Axis.Z: // pitch
					rotationMatrix = new float[] {
						(float)Math.Cos(rads), 0f, (float)Math.Sin(rads),
						0f, 1f, 0f,
						-(float)Math.Sin(rads), 0f, (float)Math.Cos(rads)
					};
					break;
				case Axis.Y: // yaw
					rotationMatrix = new float[] {
						(float)Math.Cos(rads), -(float)Math.Sin(rads), 0f,
						(float)Math.Sin(rads), (float)Math.Cos(rads), 0f,
						0f, 0f, 1f 
					};
					break;
				case Axis.X: // roll
					rotationMatrix = new float[] {
						1f, 0f, 0f,
						0f, (float)Math.Cos(rads), -(float)Math.Sin(rads),
						0f, (float)Math.Sin(rads), (float)Math.Cos(rads)
					};
					break;
				default:
					throw new Exception("Unknown rotation axis in RotateMatrix");
			}

			return Mat3f.Multiply(new float[9], rotationMatrix, originalMatrix);
		}

		public static Vec3f RotateVector(Vec3f vec, float[] rotationMatrix)
		{
			return new Vec3f(
				rotationMatrix[0] * vec.X + rotationMatrix[1] * vec.Z + rotationMatrix[2] * vec.Y,
				rotationMatrix[6] * vec.X + rotationMatrix[7] * vec.Z + rotationMatrix[8] * vec.Y,
				rotationMatrix[3] * vec.X + rotationMatrix[4] * vec.Z + rotationMatrix[5] * vec.Y
			);
		}
	}
}
