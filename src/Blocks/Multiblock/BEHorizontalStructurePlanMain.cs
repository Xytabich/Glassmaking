using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace GlassMaking.Blocks.Multiblock
{
	public class BEHorizontalStructurePlanMain : BlockEntity, IStructurePlanMainBlock
	{
		protected bool isStructureComplete = false;
		protected int structureBuildStage = 0;

		public virtual void OnSurrogateReplaced(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, Block oldBlock, Block newBlock)
		{
			if(!(oldBlock is IStructurePlanOptionalBlock))
			{
				structureBuildStage++;
				isStructureComplete = structureBuildStage >= ((BlockHorizontalStructurePlanMain)Block).requiredSurrogates;
				MarkDirty(true);
			}
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			if(isStructureComplete)
			{
				tree.SetBool("structureComplete", true);
			}
			else
			{
				tree.SetInt("structureStage", structureBuildStage);
			}
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			if(tree.GetBool("structureComplete", false))
			{
				isStructureComplete = true;
			}
			else
			{
				structureBuildStage = tree.GetInt("structureStage", 0);
			}
		}
	}
}