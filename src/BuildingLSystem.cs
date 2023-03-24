using System;
using System.Collections.Generic;
using Vintagestory.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using ProtoBuf;

using Cairo;

using TCParser;

namespace VillageResearch
{
	public class BuildingLSystem
	{
		ICoreAPI api;
		
		private Dictionary<BlockPos, int> blockUpdates = new Dictionary<BlockPos, int>();
		public Grammar Grammar {get; private set;}
		public string Script {get; private set;}

		private bool debugging;

		public BuildingLSystem(ICoreAPI api)
		{
			this.api = api;

			BlockPalette.Init(api);
		}

		public void Generate(BlockPos position, string argument, bool debug = false)
		{
			debugging = debug;
			string scriptName = argument;

			switch (argument)
			{
				case "cantor3": scriptName = "cantor"; break;
				case "cantor27": scriptName = "cantor"; break;
				case "selecteven": scriptName = "select"; break;
				case "prism5":
				case "prism5even":
				case "prism7": scriptName = "prism"; break;
				case "prismselect7":
				case "prismselect7even":
				case "prismselect9": scriptName = "prismselect"; break;
				case "zroteven": scriptName = "zrot"; break;
			}

			int radius = 1;
			int sizeOffset = 0;

			switch (argument)
			{
				case "cantor": radius = 4; break;
				case "cantor27": radius = 13; break;
				case "select": radius = 2; break;
				case "selecteven": radius = 2; sizeOffset = 1; break;
				case "rotate": radius = 4; break;
				case "rotate45": radius = 4; break;
				case "rotmove": radius = 4; break;
				case "rotmovevert": radius = 4; break;
				case "directlog": radius = 4; break;
				case "directwalls": radius = 2; break;
				case "prism5": radius = 2; break;
				case "prism5even": radius = 2; sizeOffset = 1; break;
				case "prism7": radius = 3; break;
				case "prismselect": radius = 2; break;
				case "prismselect7": radius = 3; break;
				case "prismselect7even": radius = 3;  sizeOffset = 1; break;
				case "prismselect9": radius = 4; break;
				case "zroteven": sizeOffset = 1; break;
			}

			// Script loading
			AssetLocation scriptLocation = new AssetLocation("villageresearch", $"lsystem/{scriptName}.txt");
			IAsset scriptAsset = api.Assets.TryGet(scriptLocation);

			if (scriptAsset == null)
			{
				(api.Assets as AssetManager).AddExternalAssets(api.Logger);
				scriptAsset = api.Assets.TryGet(scriptLocation);

				if (scriptAsset == null)
				{
					MessagePlayer($"Unknown script {scriptName}");
					return;
				}
			}

			// Doesn't quite work; setting data to null forces Assets.TryGet to reload properly
			//sapi.Assets.Reload(scriptLocation);

			scriptAsset.Data = null;
			scriptAsset = api.Assets.TryGet(scriptLocation);

			if (scriptAsset == null)
			{
				MessagePlayer($"Failed to (re)load script {scriptName}!");
				return;
			}

			// L-system
			Grammar = new Grammar(this);
			Module axiomModule = null;

			Script = scriptAsset.ToText();
			ScriptParser.ParseString(Grammar, Script);

			axiomModule = new Module("axiom", position.AddCopy(-radius, 0, 6), position.AddCopy(radius + sizeOffset, 2, 6 + radius * 2 + sizeOffset));

			int oakBlockSimpleID = BlockPalette.RegisterBlock(new BlockTypeSimple("log-placed-oak-ud"));
			int pineBlockSimpleID = BlockPalette.RegisterBlock(new BlockTypeSimple("log-placed-pine-ud"));
			int mapleBlockSimpleID = BlockPalette.RegisterBlock(new BlockTypeSimple("log-placed-maple-ud"));

			axiomModule.SetVariable("oakWoodSimple", oakBlockSimpleID);
			axiomModule.SetVariable("pineWoodSimple", pineBlockSimpleID);
			axiomModule.SetVariable("mapleWoodSimple", mapleBlockSimpleID);

			BlockTypeRandom randomStone = new BlockTypeRandom();
			randomStone.AddRandomBlock("stonebricks-granite", 0.5f);
			randomStone.AddRandomBlock("cobblestone-granite", 0.5f);

			int randomStoneID = BlockPalette.RegisterBlock(randomStone);
			axiomModule.SetVariable("randomStone", randomStoneID);

			BlockTypeDirectional oakPlankType = new BlockTypeDirectional();
			oakPlankType.SetBlock(BlockTypeDirectional.Direction.X_Plus, "plankslab-oak-north-free");
			oakPlankType.SetBlock(BlockTypeDirectional.Direction.X_Minus, "plankslab-oak-south-free");
			oakPlankType.SetBlock(BlockTypeDirectional.Direction.Y_Plus, "plankslab-oak-up-free");
			oakPlankType.SetBlock(BlockTypeDirectional.Direction.Y_Minus, "plankslab-oak-down-free");
			oakPlankType.SetBlock(BlockTypeDirectional.Direction.Z_Plus, "plankslab-oak-east-free");
			oakPlankType.SetBlock(BlockTypeDirectional.Direction.Z_Minus, "plankslab-oak-west-free");

			int oakPlankBlockID = BlockPalette.RegisterBlock(oakPlankType);
			axiomModule.SetVariable("oakPlank", oakPlankBlockID);

			BlockTypeDirectional oakBlockType = new BlockTypeDirectional();
			oakBlockType.SetBlock(BlockTypeDirectional.Direction.X_Plus, "log-placed-oak-we");
			oakBlockType.SetBlock(BlockTypeDirectional.Direction.X_Minus, "log-placed-oak-we");
			oakBlockType.SetBlock(BlockTypeDirectional.Direction.Y_Plus, "log-placed-oak-ud");
			oakBlockType.SetBlock(BlockTypeDirectional.Direction.Y_Minus, "log-placed-oak-ud");
			oakBlockType.SetBlock(BlockTypeDirectional.Direction.Z_Plus, "log-placed-oak-ns");
			oakBlockType.SetBlock(BlockTypeDirectional.Direction.Z_Minus, "log-placed-oak-ns");

			int oakBlockID = BlockPalette.RegisterBlock(oakBlockType);
			axiomModule.SetVariable("oakLog", oakBlockID);

			BlockTypeDirectional mapleBlockType = new BlockTypeDirectional();
			mapleBlockType.SetBlock(BlockTypeDirectional.Direction.X_Plus, "log-placed-maple-we");
			mapleBlockType.SetBlock(BlockTypeDirectional.Direction.X_Minus, "log-placed-maple-we");
			mapleBlockType.SetBlock(BlockTypeDirectional.Direction.Y_Plus, "log-placed-maple-ud");
			mapleBlockType.SetBlock(BlockTypeDirectional.Direction.Y_Minus, "log-placed-maple-ud");
			mapleBlockType.SetBlock(BlockTypeDirectional.Direction.Z_Plus, "log-placed-maple-ns");
			mapleBlockType.SetBlock(BlockTypeDirectional.Direction.Z_Minus, "log-placed-maple-ns");

			int mapleBlockTypeID = BlockPalette.RegisterBlock(mapleBlockType);
			axiomModule.SetVariable("mapleLog", mapleBlockTypeID);

			BlockTypeDirectional pineBlockType = new BlockTypeDirectional();
			pineBlockType.SetBlock(BlockTypeDirectional.Direction.X_Plus, "log-placed-pine-we");
			pineBlockType.SetBlock(BlockTypeDirectional.Direction.X_Minus, "log-placed-pine-we");
			pineBlockType.SetBlock(BlockTypeDirectional.Direction.Y_Plus, "log-placed-pine-ud");
			pineBlockType.SetBlock(BlockTypeDirectional.Direction.Y_Minus, "log-placed-pine-ud");
			pineBlockType.SetBlock(BlockTypeDirectional.Direction.Z_Plus, "log-placed-pine-ns");
			pineBlockType.SetBlock(BlockTypeDirectional.Direction.Z_Minus, "log-placed-pine-ns");

			int pineBlockTypeID = BlockPalette.RegisterBlock(pineBlockType);
			axiomModule.SetVariable("pineLog", pineBlockTypeID);

			// Putting this here to remember that SetAxiomModule shouldn't be necessary once the LSystem is done
			Grammar.SetAxiomModule(axiomModule);
		}

		public bool TryStepForward()
		{
			bool result = Grammar?.EvaluateNextStatement() ?? false;

			return result;
		}

		public void ExecuteFully()
		{
			Grammar?.Run();
		}

		public void SetBlock(BlockPos blockPos, int blockId)
		{
			blockUpdates.Add(blockPos, blockId);
		}

		public Dictionary<BlockPos, int> PopBlockUpdates()
		{
			Dictionary<BlockPos, int> result = new Dictionary<BlockPos, int>(blockUpdates);

			blockUpdates.Clear();

			return result;
		}

		private void MessagePlayer(string message)
		{
			if (api.Side == EnumAppSide.Server)
			{
				(api as ICoreServerAPI).BroadcastMessageToAllGroups(message, EnumChatType.Notification);
			}
			else
			{
				(api as ICoreClientAPI).ShowChatMessage(message);  
			}
		}
	}
}