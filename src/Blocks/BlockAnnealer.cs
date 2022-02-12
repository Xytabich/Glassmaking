using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public class BlockAnnealer : HeatedBlockBase
	{
		public ModelTransform smokeTransform;
		public ModelTransform contentTransform;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if(api.Side == EnumAppSide.Client)
			{
				smokeTransform = Attributes?["smokeTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
				contentTransform = Attributes?["contentTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
			}
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			BlockEntityAnnealer be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityAnnealer;
			if(be != null)
			{
				if(be.TryInteract(byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot))
				{
					if(world.Side == EnumAppSide.Client)
					{
						(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					}
					return true;
				}
			}
			return false;
		}
	}
}