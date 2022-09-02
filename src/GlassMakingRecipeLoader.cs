using GlassMaking.Common;
using Vintagestory.API.Common;
using Vintagestory.ServerMods;

namespace GlassMaking
{
	internal class GlassMakingRecipeLoader : ModSystem
	{
		private ICoreAPI api = null;

		internal RecipeRegistryDictionary<GlassBlowingRecipe> glassblowingRecipes;
		internal RecipeRegistryDictionary<WorkbenchRecipe> workbenchRecipes;

		public override double ExecuteOrder()
		{
			return 1.1;
		}

		public override void Start(ICoreAPI api)
		{
			this.api = api;
			base.Start(api);

			glassblowingRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<GlassBlowingRecipe>>("glassblowing");
			workbenchRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<WorkbenchRecipe>>("glassworkbench");
		}

		public override void AssetsLoaded(ICoreAPI api)
		{
			if(api.Side == EnumAppSide.Server)
			{
				var recLoader = api.ModLoader.GetModSystem<RecipeLoader>();
				recLoader.LoadRecipes<GlassBlowingRecipe>("glassblowing recipe", "recipes/glassblowing", RegisterGlassblowingRecipe);
				recLoader.LoadRecipes<WorkbenchRecipe>("glassworkbench recipe", "recipes/glassworkbench", RegisterWorkbenchRecipe);
			}
		}

		private void RegisterGlassblowingRecipe(GlassBlowingRecipe r)
		{
			r.RecipeId = glassblowingRecipes.Recipes.Count;
			if(!glassblowingRecipes.AddRecipe(r))
			{
				api.Logger.Error("Unable to add glassblowing recipe {0} with output {1} as a similar recipe has already been added", r.Code, r.Output.Code);
			}
		}

		private void RegisterWorkbenchRecipe(WorkbenchRecipe r)
		{
			r.RecipeId = workbenchRecipes.Recipes.Count;
			if(!workbenchRecipes.AddRecipe(r))
			{
				api.Logger.Error("Unable to add workbench recipe {0} with output {1} as a similar recipe has already been added", r.Code, r.Output.Code);
			}
		}
	}
}