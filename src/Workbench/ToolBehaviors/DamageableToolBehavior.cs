using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class DamageableToolBehavior : SimpleToolBehavior
	{
		public DamageableToolBehavior(string toolCode, BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(toolCode, blockentity, boundingBoxes)
		{
		}

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			int cost = recipe.Steps[step].Tools[ToolCode]["toolDurabilityCost"].AsInt(1);
			if(Slot.Itemstack.Attributes.GetInt("durability", Slot.Itemstack.Collectible.GetDurability(Slot.Itemstack)) < cost)
			{
				return false;
			}
			return base.OnUseStart(world, byPlayer, blockSel, recipe, step);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			int cost = recipe.Steps[step].Tools[ToolCode]["toolDurabilityCost"].AsInt(1);
			if(Slot.Itemstack.Attributes.GetInt("durability", Slot.Itemstack.Collectible.GetDurability(Slot.Itemstack)) < cost)
			{
				return false;
			}
			return base.OnUseStep(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			if(world.Api.Side == EnumAppSide.Server)
			{
				int cost = recipe.Steps[step].Tools[ToolCode]["toolDurabilityCost"].AsInt(1);
				Slot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, Slot, cost);
			}
			base.OnUseComplete(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}
	}
}