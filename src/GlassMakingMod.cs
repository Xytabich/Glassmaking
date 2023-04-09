using GlassMaking.Blocks;
using GlassMaking.Blocks.Multiblock;
using GlassMaking.Common;
using GlassMaking.GlassblowingTools;
using GlassMaking.Handbook;
using GlassMaking.ItemRender;
using GlassMaking.Items;
using GlassMaking.Items.Behavior;
using GlassMaking.TemporaryMetadata;
using GlassMaking.ToolDescriptors;
using GlassMaking.Workbench;
using GlassMaking.Workbench.ToolBehaviors;
using GlassMaking.Workbench.ToolDescriptors;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GlassMaking
{
	public class GlassMakingMod : ModSystem
	{
		public const string RECIPE_SELECT_HOTKEY = "itemrecipeselect";

		internal CachedItemRenderer itemsRenderer;
		internal CachedMeshRefs meshRefCache;

		private ICoreAPI api;
		private ICoreClientAPI capi = null;
		private Dictionary<AssetLocation, GlassTypeVariant> glassTypes;
		private Dictionary<string, IPipeBlowingToolDescriptor> pipeToolDescriptors;
		private Dictionary<string, IWorkbenchToolDescriptor> workbenchToolDescriptors;
		private Dictionary<string, WorkbenchToolBehavior> workbenchTools = null;

		private RecipeRegistryDictionary<GlassBlowingRecipe> glassblowingRecipes => recipeLoader.glassblowingRecipes;
		private RecipeRegistryDictionary<WorkbenchRecipe> workbenchRecipes => recipeLoader.workbenchRecipes;

		private List<Block> blowingMolds = null;
		private HashSet<AssetLocation> blowingMoldsOutput = null;

		private List<Block> castingMolds = null;
		private HashSet<AssetLocation> castingMoldsOutput = null;

		private Dictionary<Tuple<EnumItemClass, int>, ItemStack> annealRecipes = null;
		private HashSet<AssetLocation> annealOutputs = null;

		private List<ToolBehaviorDescriptor> descriptors = null;

		private Harmony harmony;

		private GlassMakingRecipeLoader recipeLoader;

		private List<IDisposable> handbookInfoList;

		public override void Start(ICoreAPI api)
		{
			this.api = api;

			base.Start(api);

			recipeLoader = api.ModLoader.GetModSystem<GlassMakingRecipeLoader>();

			api.RegisterItemClass("glassmaking:glassworkpipe", typeof(ItemGlassworkPipe));
			api.RegisterItemClass("glassmaking:glassblend", typeof(ItemGlassBlend));
			api.RegisterItemClass("glassmaking:wettable", typeof(ItemWettable));
			api.RegisterItemClass("glassmaking:glassladle", typeof(ItemGlassLadle));
			api.RegisterItemClass("glassmaking:workpiece", typeof(ItemGlassWorkpiece));
			api.RegisterItemClass("glassmaking:blowtorch", typeof(ItemBlowtorch));
			api.RegisterItemClass("glassmaking:lathe", typeof(ItemLathe));

			api.RegisterBlockClass("glassmaking:firebox", typeof(BlockFirebox));
			api.RegisterBlockClass("glassmaking:annealer", typeof(BlockAnnealer));
			api.RegisterBlockClass("glassmaking:smeltery", typeof(BlockGlassSmeltery));
			api.RegisterBlockClass("glassmaking:glassmold", typeof(BlockGlassBlowingMold));
			api.RegisterBlockClass("glassmaking:castingmold", typeof(BlockGlassCastingMold));
			api.RegisterBlockClass("glassmaking:workbench", typeof(BlockWorkbench));

			api.RegisterBlockClass("glassmaking:horstruct", typeof(BlockHorizontalStructure));
			api.RegisterBlockClass("glassmaking:larsmelmain", typeof(BlockLargeSmeltery));
			api.RegisterBlockClass("glassmaking:larsmelhearth", typeof(BlockLargeSmelteryHearth));
			api.RegisterBlockClass("glassmaking:structureplan", typeof(BlockHorizontalStructurePlan));

			api.RegisterBlockEntityClass("glassmaking:firebox", typeof(BlockEntityFirebox));
			api.RegisterBlockEntityClass("glassmaking:annealer", typeof(BlockEntityAnnealer));
			api.RegisterBlockEntityClass("glassmaking:smeltery", typeof(BlockEntityGlassSmeltery));
			api.RegisterBlockEntityClass("glassmaking:glassmold", typeof(BlockEntityGlassBlowingMold));
			api.RegisterBlockEntityClass("glassmaking:castingmold", typeof(BlockEntityGlassCastingMold));
			api.RegisterBlockEntityClass("glassmaking:workbench", typeof(BlockEntityWorkbench));

			api.RegisterBlockEntityClass("glassmaking:larsmelmain", typeof(BlockEntityLargeSmelteryCore));
			api.RegisterBlockEntityClass("glassmaking:larsmelhearth", typeof(BlockEntityLargeSmelteryHearth));

			api.RegisterBlockBehaviorClass("glassmaking:placetable", typeof(BlockBehaviorPlaceTable));

			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-tooluse", typeof(ToolUse));
			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-dryable", typeof(DryableTool));
			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-blowing", typeof(BlowingTool));
			api.RegisterCollectibleBehaviorClass("glassmaking:gbt-glassintake", typeof(GlassIntakeTool));

			api.RegisterCollectibleBehaviorClass("glassmaking:glassblend", typeof(ItemBehaviorGlassBlend));
			api.RegisterCollectibleBehaviorClass("glassmaking:workbenchtool", typeof(ItemBehaviorWorkbenchTool));

			glassTypes = api.RegisterRecipeRegistry<GlassTypeRegistry>("glassmaking:glasstypes").GlassTypes;

			descriptors = new List<ToolBehaviorDescriptor>();
			descriptors.Add(new ToolUseDescriptor(this));
			descriptors.Add(new DryableToolDescriptor(this));
			descriptors.Add(new IntakeToolDescriptor(this));
			descriptors.Add(new BlowingToolDescriptor(this));

			AddWorkbenchToolBehavior(new ItemUseBehavior(true));
			AddWorkbenchToolBehavior(new ItemUseBehavior(false));
			AddWorkbenchToolBehavior(new LiquidUseBehavior());
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			capi = api;
			base.StartClientSide(api);
			api.Input.RegisterHotKey(RECIPE_SELECT_HOTKEY, Lang.Get("Select Item Recipe"), GlKeys.F, HotkeyType.GUIOrOtherControls);
			api.Gui.RegisterDialog(new GuiDialogItemRecipeSelector(api));

			var tmpMetaSystem = api.ModLoader.GetModSystem<TemporaryMetadataSystem>();
			itemsRenderer = new CachedItemRenderer(tmpMetaSystem.CreatePool<CachedItemRenderer.RendererContainer>(TimeSpan.FromSeconds(30)));
			meshRefCache = new CachedMeshRefs(tmpMetaSystem.CreatePool<CachedMeshRefs.RefContainer>(TimeSpan.FromSeconds(30)));

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
			handbookInfoList.Add(new CastingMoldRecipeInfo());
			handbookInfoList.Add(new CastingMoldOutputInfo(this));
			handbookInfoList.Add(new GlassblowingRecipeInfo(this));
			handbookInfoList.Add(new AnnealRecipeInfo());
			handbookInfoList.Add(new AnnealOutputInfo(this));
			handbookInfoList.Add(new AllowedLiquidsList());
			handbookInfoList.Add(new WorkbenchRecipeInfo(this));
			handbookInfoList.Add(new MultiblockPlanMaterials());

			AddWorkbenchToolDescriptor(ItemUseBehavior.CODE, new ItemUseDescriptor());
			AddWorkbenchToolDescriptor(ItemUseBehavior.OTHER_CODE, new ItemUseDescriptor());
			AddWorkbenchToolDescriptor(LiquidUseBehavior.CODE, new LiquidUseDescriptor());

			blowingMolds = new List<Block>();
			castingMolds = new List<Block>();
			blowingMoldsOutput = new HashSet<AssetLocation>();
			castingMoldsOutput = new HashSet<AssetLocation>();
			annealRecipes = new Dictionary<Tuple<EnumItemClass, int>, ItemStack>();
			annealOutputs = new HashSet<AssetLocation>();
			api.Event.LevelFinalize += OnClientLevelFinallize;
		}

		public override void AssetsLoaded(ICoreAPI api)
		{
			if(api.Side == EnumAppSide.Server)
			{
				foreach(var pair in api.Assets.GetMany<JToken>(api.Logger, "worldproperties/abstract/glasstype.json"))
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
			}
		}

		public override void Dispose()
		{
			foreach(var descriptor in descriptors)
			{
				descriptor.OnUnloaded();
			}
			if(workbenchTools != null)
			{
				foreach(var pair in workbenchTools)
				{
					pair.Value.OnUnloaded();
				}
			}
			if(capi != null)
			{
				AnimUtil.ReleaseResources(capi);
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

		public bool TryFindWorkbenchRecipes(ItemStack ingredient, out WorkbenchRecipe[] recipes)
		{
			List<WorkbenchRecipe> list = null;
			foreach(var recipe in workbenchRecipes.Recipes)
			{
				if(recipe.Input.SatisfiesAsIngredient(ingredient))
				{
					if(list == null) list = new List<WorkbenchRecipe>();
					list.Add(recipe);
				}
			}
			if(list == null)
			{
				recipes = null;
				return false;
			}
			recipes = list.ToArray();
			return true;
		}

		public IReadOnlyDictionary<string, GlassBlowingRecipe> GetGlassBlowingRecipes()
		{
			return glassblowingRecipes.Pairs;
		}

		public IReadOnlyDictionary<string, WorkbenchRecipe> GetWorkbenchRecipes()
		{
			return workbenchRecipes.Pairs;
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

		public void AddWorkbenchToolDescriptor(string tool, IWorkbenchToolDescriptor descriptor)
		{
			if(workbenchToolDescriptors == null) workbenchToolDescriptors = new Dictionary<string, IWorkbenchToolDescriptor>();
			workbenchToolDescriptors[tool.ToLowerInvariant()] = descriptor;
		}

		public IWorkbenchToolDescriptor GetWorkbenchToolDescriptor(string tool)
		{
			if(workbenchToolDescriptors != null && workbenchToolDescriptors.TryGetValue(tool.ToLowerInvariant(), out var descriptor))
			{
				return descriptor;
			}
			return null;
		}

		public void AddWorkbenchToolBehavior(WorkbenchToolBehavior behavior)
		{
			if(workbenchTools == null) workbenchTools = new Dictionary<string, WorkbenchToolBehavior>();
			workbenchTools[behavior.ToolCode.ToLowerInvariant()] = behavior;
		}

		public WorkbenchToolBehavior GetWorkbenchToolBehavior(string tool)
		{
			if(workbenchTools != null && workbenchTools.TryGetValue(tool.ToLowerInvariant(), out var behavior))
			{
				return behavior;
			}
			return null;
		}

		public bool TryGetBlowingMoldsForItem(CollectibleObject item, out Block[] molds)
		{
			if(blowingMoldsOutput.Contains(item.Code))
			{
				List<Block> list = new List<Block>();
				foreach(var block in this.blowingMolds)
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

		public bool TryGetCastingMoldsForItem(CollectibleObject item, out Block[] molds)
		{
			if(castingMoldsOutput.Contains(item.Code))
			{
				List<Block> list = new List<Block>();
				foreach(var block in this.castingMolds)
				{
					var mold = (IGlassCastingMold)block;
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
				if(block is IGlassBlowingMold bmold)
				{
					var recipes = bmold.GetRecipes();
					if(recipes != null && recipes.Length > 0)
					{
						blowingMolds.Add(block);
						foreach(var recipe in recipes)
						{
							blowingMoldsOutput.Add(recipe.Output.Code);
						}
					}
				}
				if(block is IGlassCastingMold cmold)
				{
					var recipes = cmold.GetRecipes();
					if(recipes != null && recipes.Length > 0)
					{
						castingMolds.Add(block);
						foreach(var recipe in recipes)
						{
							castingMoldsOutput.Add(recipe.Output.Code);
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
			InitWorkbenchTools();
		}

		public override void AssetsFinalize(ICoreAPI api)
		{
			if(api.Side == EnumAppSide.Server)
			{
				foreach(var descriptor in descriptors)
				{
					descriptor.OnLoaded(api);
				}
				InitWorkbenchTools();
			}
		}

		private void InitWorkbenchTools()
		{
			if(workbenchTools != null)
			{
				foreach(var pair in workbenchTools)
				{
					pair.Value.OnLoaded(api);
				}
			}
		}
	}
}