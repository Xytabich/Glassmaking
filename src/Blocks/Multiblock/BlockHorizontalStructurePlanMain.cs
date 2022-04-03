using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Multiblock
{
	public class BlockHorizontalStructurePlanMain : BlockHorizontalStructure
	{
		public int requiredSurrogates = 0;

		protected override void OnStructureLoaded()
		{
			base.OnStructureLoaded();

			int sx = structure.GetLength(0), sy = structure.GetLength(1), sz = structure.GetLength(2);
			for(int x = 0; x < sx; x++)
			{
				for(int y = 0; y < sy; y++)
				{
					for(int z = 0; z < sz; z++)
					{
						if(structure[x, y, z] is BlockHorizontalStructurePlan)
						{
							if(!(structure[x, y, z] is IStructurePlanOptionalBlock))
							{
								requiredSurrogates++;
							}
						}
					}
				}
			}
		}

		public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
		{
			return base.GetHeldItemName(OnPickBlock(world, pos));
		}

		public override string GetHeldItemName(ItemStack itemStack)
		{
			return Lang.Get("glassmaking:{0} (Plan)", base.GetHeldItemName(itemStack));
		}
	}
}