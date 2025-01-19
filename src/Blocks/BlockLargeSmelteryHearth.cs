using GlassMaking.Blocks.Multiblock;
using GlassMaking.Common;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockLargeSmelteryHearth : BlockHorizontalStructure
	{
		private WorldInteraction[] interactions = default!;

		protected override void OnStructureLoaded()
		{
			base.OnStructureLoaded();

			if(api.Side != EnumAppSide.Client) return;
			interactions = BlockGlassSmeltery.GetSmelteryInteractions((ICoreClientAPI)api, "glassmaking:blockhelp-largesmeltery", GetMatchingBlends);
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
			ItemStack itemstack = slot.Itemstack;
			if(itemstack != null)
			{
				var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityLargeSmelteryHearth;
				if(be != null)
				{
					if(be.TryAdd(byPlayer, slot, byPlayer.Entity.Controls.Sneak ? (byPlayer.Entity.Controls.Sprint ? 20 : 5) : 1))
					{
						if(world.Side == EnumAppSide.Client)
						{
							(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
						}
						return true;
					}
				}
			}
			return false;
		}

		public override void OnUnloaded(ICoreAPI api)
		{
			base.OnUnloaded(api);
			if(api.Side == EnumAppSide.Client)
			{
				var bathMesh = ObjectCacheUtil.TryGet<MeshRef>(api, "glassmaking:smeltery-shape-" + Variant["side"]);
				if(bathMesh != null)
				{
					bathMesh.Dispose();
					ObjectCacheUtil.Delete(api, "glassmaking:smeltery-shape-" + Variant["side"]);
				}
			}
		}

		public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
		{
			return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
		}

		private ItemStack[]? GetMatchingBlends(WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection)
		{
			if(wi.Itemstacks.Length == 0) return null;
			var be = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as BlockEntityLargeSmelteryHearth;
			if(be == null) return null;
			be.GetGlassFillState(out var canAddAmount, out var code);
			if(code == null) return wi.Itemstacks;
			if(canAddAmount <= 0) return null;
			List<ItemStack> list = new List<ItemStack>();
			foreach(var stack in wi.Itemstacks)
			{
				var blend = GlassBlend.FromJson(stack)!;
				if(blend.Code.Equals(code) && blend.Amount * stack.StackSize <= canAddAmount)
				{
					list.Add(stack);
				}
			}
			return list.ToArray();
		}
	}
}