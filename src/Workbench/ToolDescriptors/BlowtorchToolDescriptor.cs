using GlassMaking.Workbench.ToolBehaviors;
using Vintagestory.API.Common;

namespace GlassMaking.Workbench.ToolDescriptors
{
	public class BlowtorchToolDescriptor : ItemToolDescriptor
	{
		public BlowtorchToolDescriptor() : base(BlowtorchToolBehavior.CODE)
		{
		}

		public override bool ResolveIngredient(IWorldAccessor world, WorkbenchRecipe recipe, CraftingRecipeIngredient? ingredient, string sourceForErrorLogging)
		{
			if(!base.ResolveIngredient(world, recipe, ingredient, sourceForErrorLogging))
			{
				return false;
			}
			if(!(ingredient?.RecipeAttributes?.KeyExists("temperature") ?? false))
			{
				world.Logger.Log(EnumLogType.Warning, "The temperature must be specified");
				return false;
			}
			return true;
		}
	}
}