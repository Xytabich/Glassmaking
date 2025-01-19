using GlassMaking.Blocks.Multiblock;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockLargeSmeltery : BlockHorizontalStructurePlanMain, IGlassmeltSourceBlock
	{
		public double processHoursPerUnit;
		public double bubblingProcessMultiplier;

		public ModelTransform SmokeTransform = default!;
		public Vec3i[] HearthOffsets = default!;
		public Vec3i LightOffset = default!;

		protected override void OnStructureLoaded()
		{
			base.OnStructureLoaded();

			if(api.Side == EnumAppSide.Client)
			{
				SmokeTransform = Attributes["smokeTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
			}
			HearthOffsets = new Vec3i[4];
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
							HearthOffsets[index] = -plan.mainOffset;
							index++;
						}
						else if(structure[x, y, z] is BlockHorizontalStructure block && block.Variant.ContainsKey("light"))
						{
							LightOffset = -block.mainOffset;
						}
					}
				}
			}
			processHoursPerUnit = Attributes["hoursPerUnit"].AsDouble();
			bubblingProcessMultiplier = Attributes["bubblingMult"].AsDouble();
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