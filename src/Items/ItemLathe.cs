using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GlassMaking.Items
{
	public class ItemLathe : ItemWorkbenchTool, IContainedMeshSource
	{
		public override string GetToolCode(IWorldAccessor world, ItemStack itemStack)
		{
			return LatheToolBehavior.CODE;
		}

		public override WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity)
		{
			return new LatheToolBehavior(blockentity, GetToolBoundingBoxes(world, itemStack));
		}

		public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
		{
			return null;
		}

		public string GetMeshCacheKey(ItemStack itemstack)
		{
			return null;
		}
	}
}