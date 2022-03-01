using GlassMaking.Blocks.Multiblock;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

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
	}
}