using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public class BlockAnnealer : HeatedBlockBase
	{
		public ModelTransform SmokeTransform = default!;
		public ModelTransform ContentTransform = default!;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if(api.Side == EnumAppSide.Client)
			{
				SmokeTransform = Attributes?["smokeTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
				ContentTransform = Attributes?["contentTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
			}
		}

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
		{
			if(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityAnnealer be)
			{
				if(be.TryInteract(byPlayer, byPlayer.InventoryManager.ActiveHotbarSlot))
				{
					if(world.Side == EnumAppSide.Client)
					{
						(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					}
					return true;
				}
			}
			return false;
		}
	}
}