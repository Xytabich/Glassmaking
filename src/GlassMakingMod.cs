﻿using GlassMaking.Blocks;
using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using GlassMaking.Handbook;
using GlassMaking.Items;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        private ICoreServerAPI sapi = null;
        private ICoreClientAPI capi = null;
        private RecipeRegistryDictionary<GlassBlowingRecipe> glassblowingRecipes;
        private RecipeRegistryDictionary<WorkbenchRecipe> workbenchRecipes;
        private Dictionary<AssetLocation, GlassTypeVariant> glassTypes;

        private Harmony harmony;

        private List<IDisposable> handbookInfoList;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            glassTypes = new Dictionary<AssetLocation, GlassTypeVariant>();
            var glassTypeProperties = api.Assets.GetMany<JToken>(api.Logger, "worldproperties/abstract/glasstype.json");
            foreach(var pair in glassTypeProperties)
            {
                try
                {
                    var property = pair.Value.ToObject<GlassTypeProperty>(pair.Key.Domain);
                    foreach(var type in property.Variants)
                    {
                        glassTypes[type.Code.Clone()] = type;
                    }
                }
                catch(JsonReaderException ex)
                {
                    api.Logger.Error("Syntax error in json file '{0}': {1}", pair.Key, ex.Message);
                }
            }

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

            api.RegisterBlockBehaviorClass("glassmaking:horizontalmb", typeof(BlockBehaviorHorizontal2BMultiblock));

            api.RegisterCollectibleBehaviorClass("glassmaking:gbt-shears", typeof(ShearsTool));
            api.RegisterCollectibleBehaviorClass("glassmaking:gbt-blowing", typeof(BlowingTool));
            api.RegisterCollectibleBehaviorClass("glassmaking:gbt-glassintake", typeof(GlassIntakeTool));

            api.RegisterCollectibleBehaviorClass("glassmaking:glassblend", typeof(BehaviorGlassBlend));

            glassblowingRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<GlassBlowingRecipe>>("glassblowing");
            workbenchRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<WorkbenchRecipe>>("glassworkbench");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            base.StartServerSide(api);

            api.Event.SaveGameLoaded += OnSaveGameLoadedServer;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            base.StartClientSide(api);
            api.Input.RegisterHotKey("itemrecipeselect", Lang.Get("Select Item Recipe"), GlKeys.F, HotkeyType.GUIOrOtherControls);
            api.Gui.RegisterDialog(new GuiDialogItemRecipeSelector(api));

            try
            {
                harmony = new Harmony("glassmaking");
                harmony.PatchAll(typeof(GlassMakingMod).Assembly);
            }
            catch(Exception e)
            {
                api.Logger.Error(e.Message);
            }

            handbookInfoList = new List<IDisposable>();
            handbookInfoList.Add(new ItemMeltableInfo(this));
            handbookInfoList.Add(new MoldBlowingRecipeInfo());
        }

        public override void Dispose()
        {
            if(capi != null)
            {
                foreach(var info in handbookInfoList)
                {
                    info.Dispose();
                }
                harmony.UnpatchAll("glassmaking");
            }
            base.Dispose();
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

        public WorkbenchRecipe GetWorkbenchRecipe(string code)
        {
            if(workbenchRecipes.Pairs.TryGetValue(code, out var recipe))
            {
                return recipe;
            }
            return null;
        }

        public WorkbenchRecipe GetWorkbenchRecipe(AssetLocation code)
        {
            if(workbenchRecipes.Pairs.TryGetValue(code.ToShortString(), out var recipe))
            {
                return recipe;
            }
            return null;
        }

        public IReadOnlyDictionary<string, GlassBlowingRecipe> GetGlassBlowingRecipes()
        {
            return glassblowingRecipes.Pairs;
        }

        public GlassTypeVariant GetGlassTypeInfo(AssetLocation code)
        {
            if(glassTypes.TryGetValue(code, out var info)) return info;
            return null;
        }

        public IReadOnlyDictionary<AssetLocation, GlassTypeVariant> GetGlassTypes()
        {
            return glassTypes;
        }

        private void OnSaveGameLoadedServer()
        {
            sapi.ModLoader.GetModSystem<RecipeLoader>().LoadRecipes<GlassBlowingRecipe>("glassblowing recipe", "recipes/glassblowing", RegisterGlassblowingRecipe);
            sapi.ModLoader.GetModSystem<RecipeLoader>().LoadRecipes<WorkbenchRecipe>("glassworkbench recipe", "recipes/glassworkbench", RegisterWorkbenchRecipe);
        }

        private void RegisterGlassblowingRecipe(GlassBlowingRecipe r)
        {
            r.recipeId = glassblowingRecipes.Recipes.Count;
            glassblowingRecipes.AddRecipe(r);
        }

        private void RegisterWorkbenchRecipe(WorkbenchRecipe r)
        {
            r.recipeId = workbenchRecipes.Recipes.Count;
            workbenchRecipes.AddRecipe(r);
        }
    }
}