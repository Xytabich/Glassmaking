using GlassMaking.Blocks;
using GlassMaking.Blocks.Multiblock;
using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using GlassMaking.Handbook;
using GlassMaking.ItemRender;
using GlassMaking.Items;
using GlassMaking.TemporaryMetadata;
using GlassMaking.ToolDescriptors;
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
		internal CachedItemRenderer itemsRenderer;

		private ICoreServerAPI sapi = null;
		private ICoreClientAPI capi = null;
		private RecipeRegistryDictionary<GlassBlowingRecipe> glassblowingRecipes;
		private RecipeRegistryDictionary<WorkbenchRecipe> workbenchRecipes;
		private Dictionary<AssetLocation, GlassTypeVariant> glassTypes;
		private Dictionary<string, IPipeBlowingToolDescriptor> pipeToolDescriptors;

		private List<Block> molds = null;
		private HashSet<AssetLocation> moldsOutput = null;

		private Dictionary<Tuple<EnumItemClass, int>, ItemStack> annealRecipes = null;
		private HashSet<AssetLocation> annealOutputs = null;

		private List<ToolBehaviorDescriptor> descriptors = null;

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
			api.RegisterItemClass("glassmaking:wettable", typeof(ItemWettable));

			api.RegisterBlockClass("glassmaking:firebox", typeof(BlockFirebox));
			api.RegisterBlockClass("glassmaking:annealer", typeof(BlockAnnealer));
			api.RegisterBlockClass("glassmaking:smeltery", typeof(BlockGlassSmeltery));
			api.RegisterBlockClass("glassmaking:glassmold", typeof(BlockGlassBlowingMold));
			api.RegisterBlockClass("glassmaking:workbench", typeof(BlockWorkbench));
			api.RegisterBlockClass("glassmaking:workbenchs", typeof(BlockWorkbenchSurrogate));

			api.RegisterBlockClass("glassmaking:horstruct", typeof(BlockHorizontalStructure));
			api.RegisterBlockClass("glassmaking:larsmelmain", typeof(BlockLargeSmeltery));
			api.RegisterBlockClass("glassmaking:larsmelhearth", typeof(BlockLargeSmelteryHearth));
			api.RegisterBlockClass("glassmaking:structureplan", typeof(BlockHorizontalStructurePlan));

			api.RegisterBlockEntityClass("glassmaking:firebox", typeof(BlockEntityFirebox));
			api.RegisterBlockEntityClass("glassmaking:annealer", typeof(BlockEntityAnnealer));
			api.RegisterBlockEntityClass("glassmaking:smeltery", typeof(BlockEntityGlassSmeltery));
			api.RegisterBlockEntityClass("glassmaking:glassmold", typeof(BlockEntityGlassBlowingMold));
			api.RegisterBlockEntityClass("glassmaking:workbench", typeof(BlockEntityWorkbench));

			api.RegisterBlockEntityClass("glassmaking:larsmelmain", typeof(BlockEntityLargeSmelteryCore));
			api.RegisterBlockEntityClass("glassmaking:larsmelhearth", typeof(BlockEntityLargeSmelteryHearth));

			api.RegisterBlockBehaviorClass("glassmaking:placetable", typeof(BlockBehaviorPlaceTable));

			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-tooluse", typeof(ToolUse));
			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-dryable", typeof(DryableTool));
			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-blowing", typeof(BlowingTool));
			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-glassintake", typeof(GlassIntakeTool));

			api.RegisterCollectibleBehaviorClass("glassmaking:glassblend", typeof(BehaviorGlassBlend));

			glassblowingRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<GlassBlowingRecipe>>("glassblowing");
			//workbenchRecipes = api.RegisterRecipeRegistry<RecipeRegistryDictionary<WorkbenchRecipe>>("glassworkbench");
			workbenchRecipes = new RecipeRegistryDictionary<WorkbenchRecipe>();

			descriptors = new List<ToolBehaviorDescriptor>();
			descriptors.Add(new ToolUseDescriptor(this));
			descriptors.Add(new DryableToolDescriptor(this));
			descriptors.Add(new IntakeToolDescriptor(this));
			descriptors.Add(new BlowingToolDescriptor(this));
		}

		public override void StartServerSide(ICoreServerAPI api)
		{
			sapi = api;
			base.StartServerSide(api);

			api.Event.SaveGameLoaded += OnSaveGameLoadedServer;
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			capi = api;
			base.StartClientSide(api);
			api.Input.RegisterHotKey("itemrecipeselect", Lang.Get("Select Item Recipe"), GlKeys.F, HotkeyType.GUIOrOtherControls);
			api.Gui.RegisterDialog(new GuiDialogItemRecipeSelector(api));

			var pool = api.ModLoader.GetModSystem<TemporaryMetadataSystem>().CreatePool<CachedItemRenderer.RendererContainer>(TimeSpan.FromSeconds(30));
			itemsRenderer = new CachedItemRenderer(pool);

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
			handbookInfoList.Add(new BlowingMoldRecipeInfo());
			handbookInfoList.Add(new BlowingMoldOutputInfo(this));
			handbookInfoList.Add(new GlassblowingRecipeInfo(this));
			handbookInfoList.Add(new AnnealRecipeInfo());
			handbookInfoList.Add(new AnnealOutputInfo(this));

			molds = new List<Block>();
			moldsOutput = new HashSet<AssetLocation>();
			annealRecipes = new Dictionary<Tuple<EnumItemClass, int>, ItemStack>();
			annealOutputs = new HashSet<AssetLocation>();
			api.Event.LevelFinalize += OnClientLevelFinallize;
		}

		public override void Dispose()
		{
			foreach(var descriptor in descriptors)
			{
				descriptor.OnUnloaded();
			}
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

		public void AddPipeToolDescriptor(string tool, IPipeBlowingToolDescriptor descriptor)
		{
			if(pipeToolDescriptors == null) pipeToolDescriptors = new Dictionary<string, IPipeBlowingToolDescriptor>();
			pipeToolDescriptors[tool.ToLowerInvariant()] = descriptor;
		}

		public IPipeBlowingToolDescriptor GetPipeToolDescriptor(string tool)
		{
			if(pipeToolDescriptors != null && pipeToolDescriptors.TryGetValue(tool.ToLowerInvariant(), out var descriptor))
			{
				return descriptor;
			}
			return null;
		}

		public bool TryGetMoldsForItem(CollectibleObject item, out Block[] molds)
		{
			if(moldsOutput.Contains(item.Code))
			{
				List<Block> list = new List<Block>();
				foreach(var block in this.molds)
				{
					var mold = (IGlassBlowingMold)block;
					foreach(var recipe in mold.GetRecipes())
					{
						if(recipe.Output.Code.Equals(item.Code))
						{
							list.Add(block);
						}
					}
				}
				molds = list.ToArray();
				return true;
			}
			molds = null;
			return false;
		}

		public bool TryGetMaterialsForAnneal(ItemStack forOutputItem, out CollectibleObject[] materials)
		{
			if(annealOutputs.Contains(forOutputItem.Collectible.Code))
			{
				List<CollectibleObject> list = new List<CollectibleObject>();
				foreach(var pair in annealRecipes)
				{
					if(pair.Value.Collectible.Equals(pair.Value, forOutputItem, GlobalConstants.IgnoredStackAttributes))
					{
						list.Add((pair.Key.Item1 == EnumItemClass.Block) ? (CollectibleObject)capi.World.GetBlock(pair.Key.Item2) : (CollectibleObject)capi.World.GetItem(pair.Key.Item2));
					}
				}
				materials = list.ToArray();
				return true;
			}
			materials = null;
			return false;
		}

		private void OnClientLevelFinallize()
		{
			foreach(var block in capi.World.Blocks)
			{
				if(block is IGlassBlowingMold mold)
				{
					var recipes = mold.GetRecipes();
					if(recipes != null && recipes.Length > 0)
					{
						molds.Add(block);
						foreach(var recipe in recipes)
						{
							moldsOutput.Add(recipe.Output.Code);
						}
					}
				}
			}
			foreach(var collectible in capi.World.Collectibles)
			{
				if(collectible.Attributes != null && collectible.Attributes.KeyExists("glassmaking:anneal"))
				{
					var properties = collectible.Attributes["glassmaking:anneal"];
					var output = properties["output"].AsObject<JsonItemStack>(null, collectible.Code.Domain);
					if(output.Resolve(capi.World, "recipes collect"))
					{
						var outputItem = output.ResolvedItemstack;
						annealRecipes.Add(new Tuple<EnumItemClass, int>(collectible.ItemClass, collectible.Id), outputItem);
						annealOutputs.Add(outputItem.Collectible.Code);
					}
				}
			}
			foreach(var descriptor in descriptors)
			{
				descriptor.OnLoaded(capi);
			}
		}

		private void OnSaveGameLoadedServer()
		{
			sapi.ModLoader.GetModSystem<RecipeLoader>().LoadRecipes<GlassBlowingRecipe>("glassblowing recipe", "recipes/glassblowing", RegisterGlassblowingRecipe);
			//sapi.ModLoader.GetModSystem<RecipeLoader>().LoadRecipes<WorkbenchRecipe>("glassworkbench recipe", "recipes/glassworkbench", RegisterWorkbenchRecipe);
			foreach(var descriptor in descriptors)
			{
				descriptor.OnLoaded(sapi);
			}
		}

		private void RegisterGlassblowingRecipe(GlassBlowingRecipe r)
		{
			r.RecipeId = glassblowingRecipes.Recipes.Count;
			glassblowingRecipes.AddRecipe(r);
		}

		private void RegisterWorkbenchRecipe(WorkbenchRecipe r)
		{
			r.RecipeId = workbenchRecipes.Recipes.Count;
			workbenchRecipes.AddRecipe(r);
		}
	}
}