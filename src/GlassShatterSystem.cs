using GlassMaking.Common;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GlassMaking
{
	/// <summary>
	/// Server-side driver that shatters raw (un-annealed) glass carried by players once it cools below the shatter threshold.
	/// </summary>
	public class GlassShatterSystem : ModSystem
	{
		private static readonly AssetLocation BreakSound = new AssetLocation("game", "sounds/block/glass");

		private ICoreServerAPI sapi = default!;

		public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

		public override void StartServerSide(ICoreServerAPI api)
		{
			sapi = api;
			api.Event.RegisterGameTickListener(OnServerTick, 1000);
		}

		private void OnServerTick(float dt)
		{
			foreach(var player in sapi.World.AllOnlinePlayers)
			{
				if(player is not IServerPlayer sp || sp.ConnectionState != EnumClientState.Playing) continue;
				var invManager = player.InventoryManager;
				if(invManager == null) continue;

				ScanInventory(sp, invManager.GetOwnInventory(GlobalConstants.hotBarInvClassName));
				ScanInventory(sp, invManager.GetOwnInventory(GlobalConstants.backpackInvClassName));
			}
		}

		private void ScanInventory(IServerPlayer player, IInventory? inv)
		{
			if(inv == null) return;
			for(int i = 0; i < inv.Count; i++)
			{
				var slot = inv[i];
				if(slot.Empty) continue;
				var stack = slot.Itemstack;
				if(!GlassShatter.ShouldShatter(sapi.World, stack)) continue;

				bool first = true;
				foreach(var shardStack in GlassShatter.CreateShardStacks(sapi.World, stack))
				{
					if(first)
					{
						slot.Itemstack = shardStack;
						slot.MarkDirty();
						first = false;
					}
					else if(!player.InventoryManager.TryGiveItemstack(shardStack, true))
					{
						sapi.World.SpawnItemEntity(shardStack, player.Entity.Pos.XYZ);
					}
				}
				if(first)
				{
					slot.Itemstack = null;
					slot.MarkDirty();
				}

				sapi.World.PlaySoundAt(BreakSound, player.Entity, null, true, 16f);
			}
		}
	}
}
