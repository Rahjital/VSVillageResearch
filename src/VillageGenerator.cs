using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;

using Cairo;

namespace VillageResearch
{
	/// <summary>
	/// Super basic example on how to read/set blocks in the game
	/// </summary>
	public class VillageGenerator
	{
		public class VillageStructurePlot
		{
			public Vec2i start = Vec2i.Zero;
			public Vec2i end = Vec2i.Zero;

			public int type;
		}

		ModSystemCore core;

		ICoreServerAPI sapi;

		public VillageGenerator(ModSystemCore core, ICoreServerAPI sapi)
		{
			this.core = core;
			this.sapi = sapi;
		}

		int radius = 128;

		int[] height = new int[256*256];
		int[] terrainType = new int[256*256];
		int[] edgeDistance = new int[256 * 256];
		int[] waterDistance = new int[256 * 256];

		List<VillageStructurePlot> acceptedPlots = new List<VillageStructurePlot>(); 
		Queue<VillageStructurePlot> proposedPlots = new Queue<VillageStructurePlot>();

		Vec2i startPosition = Vec2i.Zero;

		// Edge distance Breadth First Search
		//Vec2i[] neighbours = {new Vec2i(1, 0), new Vec2i(0, 1), new Vec2i(-1, 0), new Vec2i(0, -1)}; - searching only edge neighbours
		Vec2i[] neighbours = {new Vec2i(1, 0), new Vec2i(1, 1), new Vec2i(0, 1), new Vec2i(-1, 1), new Vec2i(-1, 0), 
			new Vec2i(-1, -1), new Vec2i(0, -1), new Vec2i(1, -1)}; // - searching both edge and corner neighbours

		public int EdgeMapBFS(int[] data, System.Func<Vec2i, Vec2i, bool> edgeCondition, bool fromEdges)
		{
			for (int z = 0; z < 256; z++)
			{
				for (int x = 0; x < 256; x++)
				{
					data[x + (256 * z)] = 0;
				}
			}

			bool[] visited = new bool[256 * 256];

			Queue<Vec2i> frontier = new Queue<Vec2i>();

			// First pass - identify edges
			for (int z = 0; z < 256; z++)
			{
				for (int x = 0; x < 256; x++)
				{
					int index = x + (256 * z);

					Vec2i currentPos = new Vec2i(x, z);

					if (fromEdges && (x == 0 || z == 0 || x == 255 || z == 255))
					{
						visited[index] = true;
						frontier.Enqueue(currentPos);
						continue;
					}

					foreach (Vec2i neighbour in neighbours)
					{
						int neighbourX = x + neighbour.X;
						int neighbourZ = z + neighbour.Y;

						if (!fromEdges && (neighbourX < 0 || neighbourZ < 0 || neighbourX > 255 || neighbourZ > 255))
						{
							continue;
						}

						int neighbourIndex = neighbourX + (256 * neighbourZ);

						Vec2i neighbourPos = new Vec2i(neighbourX, neighbourZ);

						if (edgeCondition(currentPos, neighbourPos))
						{
							visited[index] = true;
							frontier.Enqueue(new Vec2i(x, z));
							break;
						}
					}
				}
			}

			// Second pass - find distance from edges
			return BreadthFirstSearch(data, frontier, edgeCondition, visited);
		}

		public int BreadthFirstSearch(int[] data, Queue<Vec2i> frontier, System.Func<Vec2i, Vec2i, bool> edgeCondition, bool[] visited = null)
		{
			if (visited == null)
			{
				visited = new bool[256 * 256];
			}

			int maxDistance = 0;

			// Second pass - find distance from edges
			while (frontier.Count > 0)
			{
				Vec2i currentPos = frontier.Dequeue();
				int index = currentPos.X + (256 * currentPos.Y);

				foreach (Vec2i neighbour in neighbours)
				{
					int neighbourX = currentPos.X + neighbour.X;
					int neighbourZ = currentPos.Y + neighbour.Y;

					Vec2i neighbourPos = new Vec2i(neighbourX, neighbourZ);

					if (neighbourX >= 0 && neighbourZ >= 0 && neighbourX < 256 && neighbourZ < 256)
					{
						int neighbourIndex = neighbourX + (256 * neighbourZ);

						if (!visited[neighbourIndex] && edgeCondition != null && !edgeCondition(currentPos, neighbourPos))
						{
							data[neighbourIndex] = data[index] + 1;
							visited[neighbourIndex] = true;
							frontier.Enqueue(new Vec2i(neighbourX, neighbourZ));

							maxDistance = Math.Max(maxDistance, data[neighbourIndex]);
						}
					}
				}
			}

			return maxDistance;
		}

		// Normalisation, mostly for user convenience
		public int[] Normalise(int[] data, int min, int max)
		{
			int[] normalisedData = new int[data.Length];

			float normaliseFactor = 255f / ((float)max - (float)min);

			for (int z = 0; z < 256; z++)
			{
				for (int x = 0; x < 256; x++)
				{
					int index = x + (256 * z);

					normalisedData[index] = (int)((data[index] - min) * normaliseFactor);
				}
			}

			return normalisedData;
		}

		public void GenerateVillage(Vec2i centerPos, string argument)
		{
			if (argument == "all" || argument == "plan" || argument == "planandstep")
			{
				// Investigate prefetch/catching block accessor
				IBlockAccessor blockAccessor = sapi.World.GetBlockAccessor(true, true, true);

				string[] terrainKinds = new string[] {"soil", "rock", "sand", "gravel", "rawclay", "peat", "water"};

				HashSet<int> heightBlockingIds = new HashSet<int>();
				HashSet<int> waterIds = new HashSet<int>();

				foreach (Block block in sapi.World.Blocks)
				{
					for (int i = 0; i < terrainKinds.Length; i++)
					{
						string kind = terrainKinds[i];

						if (block.Code != null && block.Code.Path.Contains(kind))
						{
							heightBlockingIds.Add(block.Id);

							if (i == 6)
							{
								waterIds.Add(block.Id);
							}

							break;
						}
					}
				}

				// Height data
				// Terrain type
				// 0 = generic ground
				// 1 = water
				int minHeight = int.MaxValue;
				int maxHeight = 0;

				for (int z = 0; z < 256; z++)
				{
					for (int x = 0; x < 256; x++)
					{
						int index = x + (256 * z);

						//height[index] = blockAccessor.GetTerrainMapheightAt(new BlockPos(centerPos.X - radius + x, 0, centerPos.Y - radius + z));
						//height[index] = blockAccessor.GetRainMapHeightAt(new BlockPos(centerPos.X - radius + x, 0, centerPos.Y - radius + z));

						for (int y = blockAccessor.MapSizeY - 1; y > 0; y--)
						{
							Block block = blockAccessor.GetBlock(centerPos.X - radius + x, y, centerPos.Y - radius + z);

							if (heightBlockingIds.Contains(block.Id))
							{
								height[index] = y;
								terrainType[index] = waterIds.Contains(block.Id) ? 1 : 0;
								break;
							}
						}

						minHeight = Math.Min(minHeight, height[index]);
						maxHeight = Math.Max(maxHeight, height[index]);
					}
				}

				core.SendVisData(terrainType, "terrainType");
				core.SendVisData(height, "height");

				// Normalised height for convenience
				int[] heightNormalised = Normalise(height, minHeight, maxHeight);

				core.SendVisData(heightNormalised, "heightNormalised");

				// Edge distance
				int maxEdgeDistance = EdgeMapBFS(edgeDistance, (Vec2i currentPos, Vec2i neighbourPos) => 
				{
					int index = currentPos.X + (256 * currentPos.Y);
					int neighbourIndex = neighbourPos.X + (256 * neighbourPos.Y);

					return Math.Abs(height[index] - height[neighbourIndex]) >= 2 || terrainType[index] != terrainType[neighbourIndex];
				}, true);

				core.SendVisData(edgeDistance, "edgeDistance");

				// Normalised distance, for user convenience
				int[] edgeDistanceNormalised = Normalise(edgeDistance, 0, maxEdgeDistance);

				core.SendVisData(edgeDistanceNormalised, "edgeDistanceNormalised");

				// Distance to water
				int maxWaterDistance = EdgeMapBFS(waterDistance, (Vec2i currentPos, Vec2i neighbourPos) => 
				{
					int index = currentPos.X + (256 * currentPos.Y);
					int neighbourIndex = neighbourPos.X + (256 * neighbourPos.Y);

					return terrainType[index] == 1 || terrainType[index] != terrainType[neighbourIndex];
				}, false);

				core.SendVisData(waterDistance, "waterDistance");

				// Normalised distance, for user convenience
				int[] waterDistanceNormalised = Normalise(waterDistance, 0, maxWaterDistance);

				core.SendVisData(waterDistanceNormalised, "waterDistanceNormalised");

				// Start score - finding the best place to start a settlement
				int[] startScore = new int[256 * 256];

				int maxScore = 0;

				for (int z = 0; z < 256; z++)
				{
					for (int x = 0; x < 256; x++)
					{
						int index = x + (256 * z);

						int localScore = (edgeDistance[index] - 10) - ((int)Math.Pow(Math.Max(waterDistance[index] - 10, 0) * 0.15f, 1.2));

						startScore[index] = Math.Max(localScore, 0);

						if (startScore[index] > maxScore)
						{
							maxScore = startScore[index];
							startPosition.X = x;
							startPosition.Y = z;
						}
					}
				}

				core.SendVisData(startScore, "startScore");

				// Normalised start score, for user convenience
				int[] startScoreNormalised = Normalise(startScore, 0, maxScore);

				core.SendVisData(startScoreNormalised, "startScoreNormalised");
			}

			// =========================
			// -- BUILDING GENERATION --

			Random random = new Random(100);

			// Insert initial segment plan
			if (argument == "all" || argument == "plan" || argument == "planandstep")
			{
				VillageStructurePlot firstPlot = new VillageStructurePlot()
				{
					start = new Vec2i(startPosition.X - 4, startPosition.Y - 4),
					end = new Vec2i(startPosition.X + 4, startPosition.Y + 4),
					type = 1
				};

				proposedPlots.Enqueue(firstPlot);
			}

			int[] blockMap = new int[256 * 256];
			int[] plotScore = new int[256 * 256];
			int maxPlotScore = 0;

			if (argument == "all" || argument == "build" || argument == "step" || argument == "planandstep")
			{
				int currentSegment = 0;
				int structureLimit = (argument == "step" || argument == "planandstep") ? 1 : 25;

				while (proposedPlots.Count > 0 && currentSegment < structureLimit)
				{
					VillageStructurePlot proposedPlot = proposedPlots.Dequeue();

					// Check local constraints
					bool localConstraintsAccepted = true;

					// if accepted:
					if (localConstraintsAccepted)
					{
						// == Add to accepted structure list ==
						acceptedPlots.Add(proposedPlot);

						// == Propose more segments ==

						// data reset
						for (int z = 0; z < 256; z++)
						{
							for (int x = 0; x < 256; x++)
							{
								int index = x + (256 * z);

								blockMap[index] = plotScore[index] = 0;
							}
						}

						maxPlotScore = 0;

						int proposedPlotSizeX = 5 * 2 + 1;
						int proposedPlotSizeZ = 5 * 2 + 1;

						// calculate block map
						// integer division by 2 rounds odd numbers down
						int xDistance = proposedPlotSizeX / 2;
						int zDistance = proposedPlotSizeZ / 2;

						foreach(VillageStructurePlot plot in acceptedPlots)
						{
							// switch between odd and even - disabled for now, all plots are odd size to make things easier
							//int xEnd = proposedPlotSizeX % 2 == 1 ? plot.end.X + xDistance : plot.end.X + xDistance - 1;
							//int zEnd = proposedPlotSizeZ % 2 == 1 ? plot.end.Y + zDistance : plot.end.Y + zDistance - 1;
							int xStart = plot.start.X - xDistance - 1;
							int zStart = plot.start.Y - zDistance - 1;
							int xEnd = plot.end.X + xDistance + 1;
							int zEnd = plot.end.Y + zDistance + 1;

							for (int z = zStart; z <= zEnd; z++)
							{
								for (int x = xStart; x <= xEnd; x++)
								{
									int index = x + (256 * z);

									if (blockMap[index] < 0)
									{
										continue;
									}

									blockMap[index] = (x > (xStart) && z > (zStart) && x < xEnd && z < zEnd) ? -1 : blockMap[index] + 1;
								}
							}
						}
						// </blockmap>

						// plot score map constants
						int edgeAlignmentBonus = 1;
						int cornerAlignmentBonus = 1;
						int adjacencyBonus = 2;
						int spaceBonusMultiplier = 1;

						float proximityBonusFalloff = 0.15f;
						int maxProximityBonus = 20;

						// plot score map
						// bonus spots aligned to edges of existing buildings
						if (edgeAlignmentBonus != 0)
						{
							foreach(VillageStructurePlot plot in acceptedPlots)
							{
								// Top edge
								plotScore[(plot.start.X + xDistance) + (256 * (plot.start.Y - zDistance - 1))] = edgeAlignmentBonus;
								plotScore[(plot.end.X - xDistance) + (256 * (plot.start.Y - zDistance - 1))] = edgeAlignmentBonus;

								// Left edge
								plotScore[(plot.start.X - xDistance - 1) + (256 * (plot.start.Y + zDistance))] = edgeAlignmentBonus;
								plotScore[(plot.start.X - xDistance - 1) + (256 * (plot.end.Y - zDistance))] = edgeAlignmentBonus;

								// Right edge
								plotScore[(plot.end.X + xDistance + 1) + (256 * (plot.start.Y + zDistance))] = edgeAlignmentBonus;
								plotScore[(plot.end.X + xDistance + 1) + (256 * (plot.end.Y - zDistance))] = edgeAlignmentBonus;

								// Bottom edge
								plotScore[(plot.start.X + xDistance) + (256 * (plot.end.Y + zDistance + 1))] = edgeAlignmentBonus;
								plotScore[(plot.end.X - xDistance) + (256 * (plot.end.Y + zDistance + 1))] = edgeAlignmentBonus;
							}
						}

						Vec2i newPlotPos = Vec2i.Zero;

						// for edgeDistance map; possibly may need +1?
						int requiredSpace = Math.Max(xDistance, zDistance);

						for (int z = 0; z < 256; z++)
						{
							for (int x = 0; x < 256; x++)
							{
								int index = x + (256 * z);

								// skip areas blocked by existing plots
								if (blockMap[index] < 0)
								{
									plotScore[index] = 0;
									continue;
								}

								int edgeDistanceScore = edgeDistance[index] - requiredSpace;

								// skip areas where there isn't enough space to place the building
								if (edgeDistanceScore <= 0) 
								{
									plotScore[index] = 0;
									continue;
								}

								// bonus for plots with greater space around
								plotScore[index] += edgeDistanceScore * spaceBonusMultiplier;

								if (blockMap[index] > 0)
								{
									// bonus for plots adjacent to existing ones
									plotScore[index] += adjacencyBonus;

									// bonus for plots nestled in corners of two or more plots (stacks; corner of 3 plots gets twice the bonus, etc.)
									plotScore[index] += (blockMap[index] - 1) * cornerAlignmentBonus;
								}

								if (maxProximityBonus > 0f)
								{
									int distance = int.MaxValue;

									foreach (VillageStructurePlot plot in acceptedPlots)
									{
										int plotXPos = (plot.start.X + plot.end.X) / 2;
										int plotZPos = (plot.start.Y + plot.end.Y) / 2;

										int averageSize = (proposedPlotSizeX + proposedPlotSizeZ) / 2;

										distance = Math.Min(
												Math.Max((int)Math.Sqrt(((plotXPos - x) * (plotXPos - x) + (plotZPos - z) * (plotZPos - z))) - averageSize, 0)
											, distance); 
									}

									plotScore[index] += Math.Max((maxProximityBonus - (int)((float)distance * proximityBonusFalloff)), 0);
								}

								// check the final score
								if (plotScore[index] > maxPlotScore)
								{
									maxPlotScore = plotScore[index];
									newPlotPos.X = x;
									newPlotPos.Y = z;
								}
							}
						}

						VillageStructurePlot newPlot = new VillageStructurePlot()
						{
							start = new Vec2i(newPlotPos.X - xDistance, newPlotPos.Y - zDistance),
							end = new Vec2i(newPlotPos.X + xDistance, newPlotPos.Y + zDistance),
							type = 1
						};

						proposedPlots.Enqueue(newPlot);

						currentSegment++;
					}
				}

				core.SendVisData(plotScore, "plotScore");
				core.SendVisData(Normalise(plotScore, 0, maxPlotScore), "plotScoreNormalised");

				int[] overlap = new int[256 * 256];

				foreach(VillageStructurePlot plot in acceptedPlots)
				{
					for (int z = plot.start.Y; z <= plot.end.Y; z++)
					{
						for (int x = plot.start.X; x <= plot.end.X; x++)
						{
							int index = x + (256 * z);

							overlap[index] += 1;
						}
					}
				}

				core.SendVisData(overlap, "overlap");

				core.SendVisData(blockMap, "blockMap");
			}

			if (argument == "all" || argument == "build")
			{
			}
		}
	}
}