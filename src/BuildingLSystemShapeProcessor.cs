using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace VillageResearch
{
	public enum Shape
	{
		Box,
		Prism
	}

	public static class ShapeProcessor
	{
		public static IEnumerable<ShapePointData> IterateBlocks(StatementSelection selection)
		{
			switch (selection.shape)
			{
				case Shape.Box: return ShapeBox.IterateBlocks(selection);
				case Shape.Prism: return ShapePrism.IterateBlocks(selection);
				default: throw new Exception($"Trying to iterate blocks in unknown shape {selection.shape}");
			}
		}

		public static IEnumerable<StatementSelection> SelectSubshapes(StatementSelection selection, string subshapeType)
		{
			switch (selection.shape)
			{
				case Shape.Box: return ShapeBox.SelectSubshapes(selection, subshapeType);
				case Shape.Prism: return ShapePrism.SelectSubshapes(selection, subshapeType);
				default: throw new Exception($"Trying to select subshape {subshapeType} in unknown shape {selection.shape}");
			}
		}
	}
}