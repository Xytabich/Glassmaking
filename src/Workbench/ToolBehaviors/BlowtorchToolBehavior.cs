using System.Runtime.CompilerServices;
using GlassMaking.Items;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Workbench.ToolBehaviors
{
	public class BlowtorchToolBehavior : SimpleToolBehavior
	{
		public const string CODE = "blowtorch";

		public BlowtorchToolBehavior(BlockEntity blockentity, Cuboidf[] boundingBoxes) : base(CODE, blockentity, boundingBoxes)
		{
		}

		public override bool OnUseStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			var useTime = recipe.Steps[step].UseTime;
			if(useTime.HasValue && Slot.Itemstack!.Item is ItemBlowtorch item)
			{
				if(GetTemparature(recipe.Steps[step]) <= item.flameTemperature)
				{
					isUsing = true;
					var useLitres = item.consumptionPerSecond;
					return item.GetCurrentLitres(Slot.Itemstack) >= useLitres * useTime.Value;
				}
				else return false;
			}
			return base.OnUseStart(world, byPlayer, blockSel, recipe, step);
		}

		public override bool OnUseStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			var useTime = recipe.Steps[step].UseTime;
			if(useTime.HasValue && Slot.Itemstack!.Item is ItemBlowtorch item)
			{
				if(GetTemparature(recipe.Steps[step]) <= item.flameTemperature)
				{
					var useLitres = item.consumptionPerSecond;
					return item.GetCurrentLitres(Slot.Itemstack) >= useLitres * useTime.Value;
				}
				else return false;
			}
			return base.OnUseStep(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		public override void OnUseComplete(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, WorkbenchRecipe recipe, int step)
		{
			isUsing = false;
			var useTime = recipe.Steps[step].UseTime;
			if(useTime.HasValue && Slot.Itemstack!.Item is ItemBlowtorch item)
			{
				var useLitres = item.consumptionPerSecond;
				item.TryTakeLiquid(Slot.Itemstack, useLitres * useTime.Value);
				Slot.MarkDirty();
			}
			base.OnUseComplete(secondsUsed, world, byPlayer, blockSel, recipe, step);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetTemparature(WorkbenchRecipeStep step)
		{
			return step.Tools[CODE]!.RecipeAttributes!["temperature"].AsFloat();
		}
	}
}