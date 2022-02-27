using GlassMaking.Blocks.Multiblock;
using Vintagestory.API.Common;

namespace GlassMaking.Blocks
{
	public class BlockLargeSmeltery : BlockHorizontalStructurePlanMain
	{
		public ModelTransform smokeTransform;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			if(api.Side == EnumAppSide.Client)
			{
				smokeTransform = Attributes?["smokeTransform"].AsObject<ModelTransform>() ?? ModelTransform.NoTransform;
			}
		}
	}
}