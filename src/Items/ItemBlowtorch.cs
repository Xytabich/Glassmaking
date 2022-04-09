using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	public class ItemBlowtorch : StrictLiquidContainer, IWorkbenchTool
	{
		public override bool IsTopOpened => false;
		public override bool CanDrinkFrom => false;
		public override bool AllowHeldLiquidTransfer => true;

		protected Cuboidf[] toolBoundingBoxes = null;

		public Cuboidf[] GetToolBoundingBoxes(IWorldAccessor world, ItemStack itemStack)
		{
			return toolBoundingBoxes ?? (toolBoundingBoxes = Attributes?["workbenchToolBounds"].AsObject<Cuboidf[]>());
		}

		public WorkbenchToolBehavior CreateToolBehavior(IWorldAccessor world, ItemStack itemStack, BlockEntity blockentity)
		{
			return new BlowtorchToolBehavior(blockentity, GetToolBoundingBoxes(world, itemStack));
		}

		public string GetToolCode(IWorldAccessor world, ItemStack itemStack)
		{
			return BlowtorchToolBehavior.CODE;
		}

		public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
		{
			// disallow drink
			return null;
		}
	}
}