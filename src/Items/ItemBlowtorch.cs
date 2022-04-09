using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	public class ItemBlowtorch : StrictLiquidContainer, IWorkbenchTool
	{
		public override bool IsTopOpened => false;
		public override bool CanDrinkFrom => false;

		protected Cuboidf[] toolBoundingBoxes;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			toolBoundingBoxes = Attributes?["workbenchToolBounds"].AsObject<Cuboidf[]>();
		}

		public Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes;
		}

		public WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity)
		{
			return new BlowtorchToolBehavior(blockentity, GetToolBoundingBoxes(world, itemStack));
		}

		public string GetToolCode(IWorldAccessor world, ItemStack itemStack)
		{
			return BlowtorchToolBehavior.CODE;
		}
	}
}