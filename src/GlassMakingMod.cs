using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using GlassMaking.Items;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace GlassMaking
{
    public class GlassMakingMod : ModSystem
    {
        private ICoreServerAPI sapi;
        private RecipeRegistryDictionary<GlassBlowingRecipe> glassblowingRecipes;
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

            RegisterGlassBlowingTool("glassintake", new GlassIntakeTool());
            RegisterGlassBlowingTool("blowing", new BlowingTool());

            glassblowingRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<GlassBlowingRecipe>>("glassblowing");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            base.StartServerSide(api);

            api.Event.SaveGameLoaded += OnSaveGameLoaded;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            api.Input.RegisterHotKey("itemrecipeselect", Lang.Get("Select Item Recipe"), GlKeys.F, HotkeyType.GUIOrOtherControls);
            api.Gui.RegisterDialog(new GuiDialogItemRecipeSelector(api));
        }

        public void RegisterGlassBlowingTool(string code, IGlassBlowingTool tool)
        {
            tools.Add(code, tool);
        }

        public GlassBlowingRecipe GetGlassBlowingRecipe(string code)
        {
            if(glassblowingRecipes.Pairs.TryGetValue(code, out var recipe))
            {
                return recipe;
            }
            return null;
        }

        public GlassBlowingRecipe GetGlassBlowingRecipe(AssetLocation code)
        {
            if(glassblowingRecipes.Pairs.TryGetValue(code.ToShortString(), out var recipe))
            {
                return recipe;
            }
            return null;
        }

        public IReadOnlyDictionary<string, GlassBlowingRecipe> GetGlassBlowingRecipes()
        {
            return glassblowingRecipes.Pairs;
        }

        public IGlassBlowingTool GetGlassBlowingTool(string code)
        {
            if(tools.TryGetValue(code, out var tool))
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
            r.recipeId = glassblowingRecipes.Recipes.Count;
            glassblowingRecipes.AddRecipe(r);
        }
    }
}