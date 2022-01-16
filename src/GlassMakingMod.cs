﻿using GlassMaking.Blocks;
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

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("glassmaking:glassworkpipe", typeof(ItemGlassworkPipe));
            api.RegisterItemClass("glassmaking:glassblend", typeof(ItemGlassBlend));

            api.RegisterBlockClass("glassmaking:firebox", typeof(BlockFirebox));
            api.RegisterBlockClass("glassmaking:temperingoven", typeof(BlockTemperingOven));
            api.RegisterBlockClass("glassmaking:smeltery", typeof(BlockGlassSmeltery));
            api.RegisterBlockClass("glassmaking:glassmold", typeof(BlockGlassBlowingMold));
            api.RegisterBlockClass("glassmaking:workbench", typeof(BlockWorkbench));
            api.RegisterBlockClass("Horizontal2BMultiblock", typeof(BlockHorizontal2BMultiblock));

            api.RegisterBlockEntityClass("glassmaking:firebox", typeof(BlockEntityFirebox));
            api.RegisterBlockEntityClass("glassmaking:temperingoven", typeof(BlockEntityTemperingOven));
            api.RegisterBlockEntityClass("glassmaking:smeltery", typeof(BlockEntityGlassSmeltery));
            api.RegisterBlockEntityClass("glassmaking:glassmold", typeof(BlockEntityGlassBlowingMold));
            api.RegisterBlockEntityClass("glassmaking:workbench", typeof(BlockEntityWorkbench));

            api.RegisterBlockBehaviorClass("Horizontal2BMultiblock", typeof(BlockBehaviorHorizontal2BMultiblock));

            api.RegisterCollectibleBehaviorClass("glassmaking:gbt-shears", typeof(ShearsTool));
            api.RegisterCollectibleBehaviorClass("glassmaking:gbt-blowing", typeof(BlowingTool));
            api.RegisterCollectibleBehaviorClass("glassmaking:gbt-glassintake", typeof(GlassIntakeTool));

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

        public GlassBlowingRecipe GetGlassBlowingRecipe(string code)
        {
            if(glassblowingRecipes.Pairs.TryGetValue(code.ToLowerInvariant(), out var recipe))
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