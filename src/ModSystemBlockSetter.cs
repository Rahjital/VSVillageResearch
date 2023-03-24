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
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class MessageBlockCommit
	{
		public Dictionary<BlockPos, int> Blocks;
	}

	public class ModSystemBlockSetter : ModSystem
	{
		// Serverside
		ICoreServerAPI sapi;
		IServerNetworkChannel serverChannel;

		private IBulkBlockAccessor bulkBlockAccessor;

		public override void StartServerSide(ICoreServerAPI api)
		{
			sapi = api;

			serverChannel = sapi.Network.RegisterChannel("lsystemdebugger")
				.RegisterMessageType(typeof(MessageBlockCommit))
				.SetMessageHandler<MessageBlockCommit>(OnMessageBlockCommit)
			;

			bulkBlockAccessor = api.World.GetBlockAccessorBulkUpdate(true, true) as IBulkBlockAccessor;
		}

		public void OnMessageBlockCommit(IServerPlayer fromPlayer, MessageBlockCommit networkMessage)
		{
			CommitBlockUpdates(networkMessage.Blocks);
		}

		public void CommitBlockUpdates(Dictionary<BlockPos, int> blocks)
		{
			foreach (KeyValuePair<BlockPos, int> blockKV in blocks)
			{
				bulkBlockAccessor.SetBlock(blockKV.Value, blockKV.Key);
			}

			bulkBlockAccessor.Commit();
		}
	}
}