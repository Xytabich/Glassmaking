using GlassMaking.Workbench;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	public abstract class ItemWorkbenchTool : Item, IWorkbenchTool
	{
		protected Cuboidf[] toolBoundingBoxes;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			toolBoundingBoxes = Array.ConvertAll(Attributes?["workbenchToolBounds"].AsObject<RotatableCube[]>(), c => c.RotatedCopy());
		}

		public abstract string GetToolCode(IWorldAccessor world, ItemStack itemStack);

		public virtual Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes;
		}

		public abstract WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity);
	}
}