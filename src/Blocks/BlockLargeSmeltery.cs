using GlassMaking.Blocks.Multiblock;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockLargeSmeltery : BlockHorizontalStructurePlanMain
	{
		public ModelTransform smokeTransform;
		public Vec3i[] hearthOffsets;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if(api.Side == EnumAppSide.Client)
			{
				smokeTransform = Attributes?["smokeTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
			}
			hearthOffsets = new Vec3i[4];
			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			int index = 0;
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] is BlockHorizontalStructurePlan plan && plan.Variant.TryGetValue("sides", out var type) && type == "hearth")
						{
							hearthOffsets[index] = -plan.mainOffset;
							index++;
						}
					}
				}
			}
		}

		public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
		{
			var items = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
			if(items == null) items = new ItemStack[0];
			var be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityLargeSmelteryCore;
			if(be != null) items = items.Append(be.GetDropItems() ?? new ItemStack[0]);
			return items;
		}
	}
}