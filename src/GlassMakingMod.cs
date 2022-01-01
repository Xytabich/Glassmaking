using GlassMaking.Blocks;
using GlassMaking.Items;
using GlassMaking.Tools;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace GlassMaking
{
    public class GlassMakingMod : ModSystem
    {
        private ICoreServerAPI sapi;
        private List<GlassBlowingRecipe> glassblowingRecipes;
        private Dictionary<string, IGlassBlowingTool> tools = new Dictionary<string, IGlassBlowingTool>();

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("glassmaking:glassworkpipe", typeof(ItemGlassworkPipe));
            api.RegisterItemClass("glassmaking:glassblend", typeof(ItemGlassBlend));

            api.RegisterBlockClass("glassmaking:firebox", typeof(BlockFirebox));
            api.RegisterBlockClass("glassmaking:glasssmeltery", typeof(BlockGlassSmeltery));
            api.RegisterBlockClass("glassmaking:glassworktable", typeof(BlockGlassworktable));
            api.RegisterBlockClass("Horizontal2BMultiblockSurrogate", typeof(BlockHorizontal2BMultiblockSurrogate));
            api.RegisterBlockClass("Horizontal2BMultiblockMain", typeof(BlockHorizontal2BMultiblockMain));

            api.RegisterBlockEntityClass("glassmaking:firebox", typeof(BlockEntityFirebox));
            api.RegisterBlockEntityClass("glassmaking:glasssmeltery", typeof(BlockEntityGlassSmeltery));
            api.RegisterBlockEntityClass("glassmaking:glassmold", typeof(BlockEntityGlassBlowingMold));
            api.RegisterBlockEntityClass("glassmaking:glassworktable", typeof(BlockEntityGlassworktable));

            api.RegisterBlockBehaviorClass("Horizontal2BMultiblock", typeof(BlockBehaviorHorizontal2BMultiblock));

            RegisterGlassBlowingTool(new AssetLocation("glasspipe"), new GlasspipeBlowingTool());

            glassblowingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<GlassBlowingRecipe>>("glassblowing").Recipes;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            base.StartServerSide(api);

            api.Event.SaveGameLoaded += OnSaveGameLoaded;
        }

        public void RegisterGlassBlowingTool(AssetLocation code, IGlassBlowingTool tool)
        {
            tools.Add(code.ToShortString(), tool);
        }

        public IGlassBlowingTool GetGlassBlowingTool(AssetLocation code)
        {
            if(tools.TryGetValue(code.ToShortString(), out var tool))
            {
                return tool;
            }
            return null;
        }

        private void OnSaveGameLoaded()
        {
            sapi.ModLoader.GetModSystem<RecipeLoader>().LoadRecipes<GlassBlowingRecipe>("glassblowing recipe", "recipes/glassblowing", RegisterGlassblowingRecipe);
        }

        private void RegisterGlassblowingRecipe(GlassBlowingRecipe r)
        {
            r.recipeId = glassblowingRecipes.Count;//TODO: resolve
            glassblowingRecipes.Add(r);
        }
    }
}