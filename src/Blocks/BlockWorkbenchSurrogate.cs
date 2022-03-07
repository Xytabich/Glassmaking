using GlassMaking.Blocks.Multiblock;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockWorkbenchSurrogate : BlockHorizontalStructure
	{
		public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
		{
			pos = GetMainBlockPosition(pos);
			var be = blockAccessor.GetBlockEntity(pos) as BlockEntityWorkbench;
			if(be != null)
			{
				var boxes = be.GetSelectionBoxes();
				if(isSurrogate)
				{
					boxes = (Cuboidf[])boxes.Clone();
					for(int i = 1; i < boxes.Length; i++)
					{
						boxes[i] = boxes[i].OffsetCopy(mainOffset.X, mainOffset.Y, mainOffset.Z);
					}
				}
				return boxes;
			}
			return base.GetSelectionBoxes(blockAccessor, pos);
		}
	}
}