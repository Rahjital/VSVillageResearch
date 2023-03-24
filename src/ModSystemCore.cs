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
	public class ModSystemCore : ModSystem
	{
		public static AssetCategory lsystemCategory = new AssetCategory("lsystem", true, EnumAppSide.Universal);

		[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
		public class NetworkVillageVisData
		{
			public int[] data;
			public string dataType;
		}

		[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
		public class NetworkVillageStartData
		{
			public int radius;
			public int xPos;
			public int zPos;
		}

		// Serverside
		ICoreServerAPI sapi;
		IServerNetworkChannel serverChannel;

		public override void StartServerSide(ICoreServerAPI api)
		{
			sapi = api;

			serverChannel = sapi.Network.RegisterChannel("villageInfo")
				.RegisterMessageType(typeof(NetworkVillageVisData))
				.RegisterMessageType(typeof(NetworkVillageStartData))
			;

			sapi.RegisterCommand("village", "", "Plans and builds a village", CmdGenVillage, Privilege.controlserver);
			sapi.RegisterCommand("build", "", "Tests creating a building through the L-System", CmdGenBuilding, Privilege.controlserver);
		}

		VillageGenerator generator;

		private void CmdGenVillage(IServerPlayer player, int groupId, CmdArgs args)
		{
			if (generator == null)
			{
				generator = new VillageGenerator(this, sapi);
			}

			string arg = args.PopWord() ?? "all";

			BlockPos playerPos = player.Entity.Pos.AsBlockPos;

			serverChannel.BroadcastPacket(new NetworkVillageStartData()
			{
				radius = 128,
				xPos = playerPos.X,
				zPos = playerPos.Y
			});

			generator.GenerateVillage(new Vec2i(playerPos.X, playerPos.Z), arg);
		}

		private void CmdGenBuilding(IServerPlayer player, int groupId, CmdArgs args)
		{
			BuildingLSystem lSystem = new BuildingLSystem(sapi); 
			lSystem.Generate(player.Entity.Pos.AsBlockPos, args.PopWord() ?? "test");
			lSystem.ExecuteFully();
			sapi.ModLoader.GetModSystem<ModSystemBlockSetter>().CommitBlockUpdates(lSystem.PopBlockUpdates());
		}

		public void SendVisData(int[] data, string dataType)
		{
			serverChannel.BroadcastPacket(new NetworkVillageVisData()
			{
				data = data,
				dataType = dataType
			});
		}

		// Clientside
		ICoreClientAPI capi;
		IClientNetworkChannel clientChannel;

		GuiDialogVisData visDataGui;

		public override void StartClientSide(ICoreClientAPI api)
		{
			capi = api;

			clientChannel = capi.Network.RegisterChannel("villageInfo")
				.RegisterMessageType(typeof(NetworkVillageVisData))
				.RegisterMessageType(typeof(NetworkVillageStartData))
				.SetMessageHandler<NetworkVillageVisData>(OnVisDataReceived)
				.SetMessageHandler<NetworkVillageStartData>(OnStartDataReceived)
			;

			visDataGui = new GuiDialogVisData(capi);

			//capi.Input.RegisterHotKey("visdatagui", "Village data visualisation GUI", GlKeys.Tilde, HotkeyType.DevTool);
			capi.Input.SetHotKeyHandler("visdatagui", OnVisDataHotkey);
		}

		public bool OnVisDataHotkey(KeyCombination comb)
		{
			visDataGui.Toggle();

			return true;
		}

		private void OnVisDataReceived(NetworkVillageVisData visDataMessage)
		{
			capi.ShowChatMessage("Vis data received: " + visDataMessage.dataType);

			visDataGui.SetVisData(visDataMessage.dataType, visDataMessage.data);

			visDataGui.TryOpen();
		}

		private void OnStartDataReceived(NetworkVillageStartData startDataMessage)
		{
			capi.ShowChatMessage(String.Format("Starting new village with radius {0} at coords {1}/{2}", startDataMessage.radius, startDataMessage.xPos, startDataMessage.zPos));
		}

		// Data is sent from serverside now, but at least now I know this works
		/*public int[] GetHeightData(ICoreAPI api, int posX, int posZ) 
		{
			int[] heightData = new int[256*256];

			int maxHeight = 0;

			IBlockAccessor blockAccessor = api.World.BlockAccessor;

			for (int z = 0; z < 256; z++)
			{
				for (int x = 0; x < 256; x++)
				{
					int index = x + (256 * z);

					heightData[index] = blockAccessor.GetTerrainMapheightAt(new BlockPos(posX + x, 0, posZ + z));

					maxHeight = Math.Max(maxHeight, heightData[index]);
				}
			}

			capi.ShowChatMessage("Client max height: " + maxHeight);

			return heightData;
		}*/
	}

	public class GuiDialogVisData : GuiDialog
	{
		public override string ToggleKeyCombinationCode => "visdatagui";
		public override bool PrefersUngrabbedMouse => false;

		List<string> visDataTypes = new List<string>();
		string selectedVisDataType = null;

		GuiElementDropDown guiElementVisDataType;
		GuiElementVisData guiElementVisData;

		public GuiDialogVisData(ICoreClientAPI capi) : base(capi)
		{
			SetupDialog();
		}

		public void SetupDialog()
		{
			// Auto-sized dialog at the center of the screen
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithFixedOffset(-400, -300);

			int mapWidth = 256;
			int mapHeight = 256;

			ElementBounds dropdownBounds = ElementBounds.Fixed(0, 20, mapWidth, 30);
			ElementBounds mapBounds = ElementBounds.Fixed(0, 60, mapWidth, mapHeight);

			// Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(mapBounds);

			SingleComposer = capi.Gui.CreateCompo("visdataDialog", dialogBounds)
				.AddShadedDialogBG(bgBounds)
				.AddDialogTitleBar("Village data vis", OnTitleBarClose)
				.BeginChildElements(bgBounds)
					.AddDropDown(new string[] {""}, new string[] {""}, 0, OnDropdownChanged, dropdownBounds, "visTypeElem")
					.AddInteractiveElement(new GuiElementVisData(capi, mapBounds), "visElem")
				.EndChildElements()
				.Compose()
			;

			guiElementVisDataType = SingleComposer.GetDropDown("visTypeElem");

			guiElementVisData = SingleComposer.GetElement("visElem") as GuiElementVisData;
		}

		public void OnDropdownChanged(string code, bool selected)
		{
			guiElementVisData.SelectDataType(code);
			selectedVisDataType = code;
		}

		public void SetVisData(string type, int[] data)
		{
			guiElementVisData.SetVisData(type, data);   

			if (!visDataTypes.Contains(type))
			{
				visDataTypes.Add(type);
				guiElementVisDataType.SetList(visDataTypes.ToArray(), visDataTypes.ToArray());

				if (selectedVisDataType == null)
				{
					guiElementVisDataType.SetSelectedIndex(0);
					guiElementVisData.SelectDataType(type);
					selectedVisDataType = type;
				}
			} 
		}

		private void OnTitleBarClose()
		{
			TryClose();
		}
	}

	public class GuiElementVisData : GuiElement
	{
		LoadedTexture texture;
		ICoreClientAPI capi;

		Dictionary<string, int[]> dataStore = new Dictionary<string, int[]>();
		string selectedDataType = null;

		ElementBounds parentBounds;

		public GuiElementVisData(ICoreClientAPI capi, ElementBounds bounds) : base(capi, bounds)
		{
			this.capi = capi;
			this.parentBounds = bounds;

			texture = new LoadedTexture(capi, 0, 256, 256);
		}

		public void SetVisData(string type, int[] data)
		{
			dataStore[type] = data;

			if (type == selectedDataType)
			{
				Redraw();
			}
		}

		public void SelectDataType(string type)
		{
			if (type == selectedDataType)
			{
				return;
			}

			selectedDataType = type;
			Redraw();
		}

		public void Redraw()
		{
			int[] data = dataStore[selectedDataType];
			int[] pixels = new int[256 * 256];

			// "soil", "rock", "sand", "gravel", "rawclay", "peat", "water"

			if (selectedDataType == "terrainType")
			{
				for (int i = 0; i < data.Length; i++)
				{
					switch (data[i])
					{
						case 0:
							pixels[i] = ColorUtil.ToRgba(255, 0, 255, 0);
							break;
						case 1:
							pixels[i] = ColorUtil.ToRgba(255, 255, 0, 0);
							break;
					}
				}
			}
			else if (selectedDataType == "startScore" || selectedDataType == "plotScore" || selectedDataType == "plotScoreNormalised")
			{
				int greatestIndex = 0;
				int greatestScore = 0;

				for (int i = 0; i < data.Length; i++)
				{
					pixels[i] = ColorUtil.ToRgba(255, data[i], data[i], data[i]);

					if (data[i] > greatestScore)
					{
						greatestScore = data[i];
						greatestIndex = i;
					}
				}

				pixels[greatestIndex] = ColorUtil.ToRgba(255, 0, 0, 255);
			}
			else if (selectedDataType == "overlap")
			{
				for (int i = 0; i < data.Length; i++)
				{
					pixels[i] = data[i] > 0 ? ColorUtil.HsvToRgba((data[i] * 110) % 256, 128, 192, 255) : ColorUtil.ToRgba(255, 0, 0, 0);
				}
			}
			else if (selectedDataType == "blockMap")
			{
				for (int i = 0; i < data.Length; i++)
				{
					if (data[i] > 0) {
						pixels[i] = ColorUtil.ToRgba(255, (data[i] - 1) * 80, 255, (data[i] - 1) * 80);
					} 
					else 
					{
						pixels[i] = data[i] < 0 ? ColorUtil.ToRgba(255, 0, 0, 255) : ColorUtil.ToRgba(255, 0, 0, 0);
					}
				}
			}
			else
			{
				for (int i = 0; i < data.Length; i++)
				{
					pixels[i] = ColorUtil.ToRgba(255, data[i], data[i], data[i]);
				}
			}

			capi.Render.LoadOrUpdateTextureFromRgba(pixels, false, 0, ref texture);

			capi.Render.BindTexture2d(texture.TextureId);
			capi.Render.GlGenerateTex2DMipmaps();
		}

		public override void RenderInteractiveElements(float deltaTime)
		{
			capi.Render.Render2DTexture(
				texture.TextureId,
				(float)parentBounds.absX,
				(float)parentBounds.absY,
				texture.Width,
				texture.Height,
				50
			);
		}
	}
}