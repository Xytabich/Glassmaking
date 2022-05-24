using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Blocks
{
	public class BlockEntityGlassBlowingMold : BlockEntity, IEntityGlassBlowingMold, ITexPositionSource
	{
		private static AssetLocation openSound = new AssetLocation("sounds/block/vesselopen");
		private static AssetLocation closeSound = new AssetLocation("sounds/block/vesselclose");

		public Size2i AtlasSize => capi.BlockTextureAtlas.Size;
		public string AttributeTransformCode => "onDisplayTransform";

		public bool CanTakeItem => contents != null && (splittable ? (!hasContentsTransform || Block.Variant["state"] == "opened") : hasContentsTransform);
		public bool CanBeFilled => contents == null && (!splittable || Block.Variant["state"] != "opened");

		private CollectibleObject nowTesselatingObj;
		private Shape nowTesselatingShape;
		private ICoreClientAPI capi;

		private bool splittable = false;
		private ItemStack contents = null;

		private bool hasContentsTransform = false;
		private ModelTransform contentsTransform = null;
		private MeshData contentsMesh = null;

		public TextureAtlasPosition this[string textureCode]
		{
			get
			{
				Dictionary<string, CompositeTexture> textures = nowTesselatingObj is Item item ? item.Textures : ((Block)nowTesselatingObj).Textures;
				AssetLocation texturePath = null;
				CompositeTexture tex;

				// Prio 1: Get from collectible textures
				if(textures.TryGetValue(textureCode, out tex))
				{
					texturePath = tex.Baked.BakedName;
				}

				// Prio 2: Get from collectible textures, use "all" code
				if(texturePath == null && textures.TryGetValue("all", out tex))
				{
					texturePath = tex.Baked.BakedName;
				}

				// Prio 3: Get from currently tesselating shape
				if(texturePath == null)
				{
					nowTesselatingShape?.Textures.TryGetValue(textureCode, out texturePath);
				}

				// Prio 4: The code is the path
				if(texturePath == null)
				{
					texturePath = new AssetLocation(textureCode);
				}

				return GetOrCreateTexPos(texturePath);
			}
		}

		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			splittable = Block.Variant.ContainsKey("state");
			capi = api as ICoreClientAPI;
			if(Block.Attributes?.KeyExists("contentsTransform") == true)
			{
				hasContentsTransform = true;
				if(Api.Side == EnumAppSide.Client)
				{
					contentsTransform = Block.Attributes["contentsTransform"].AsObject<ModelTransform>();
				}
			}
			if(contents != null)
			{
				contents.ResolveBlockOrItem(Api.World);
				if(Api.Side == EnumAppSide.Client)
				{
					UpdateMesh();
				}
			}
		}

		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			base.GetBlockInfo(forPlayer, dsc);
			if(contents != null)
			{
				dsc.AppendLine(Lang.Get("Contents: {0}x{1}", contents.StackSize, contents.GetName()));
			}
		}

		public bool OnInteract(IWorldAccessor world, IPlayer byPlayer)
		{
			if(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null)
			{
				bool isOpened = splittable && Block.Variant["state"] == "opened";
				if(hasContentsTransform && (!splittable || isOpened) && contents != null)
				{
					if(!byPlayer.Entity.TryGiveItemStack(contents))
					{
						byPlayer.Entity.World.SpawnItemEntity(contents, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
					}
					contents = null;
					MarkDirty(true);
				}
				else if(splittable)
				{
					world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(Block.CodeWithVariant("state", isOpened ? "closed" : "opened")).Id, Pos);
					world.PlaySoundAt(isOpened ? closeSound : openSound, Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, byPlayer, true, 16f);
					if(Api.Side == EnumAppSide.Server && !isOpened && !hasContentsTransform && contents != null)
					{
						if(!byPlayer.Entity.TryGiveItemStack(contents))
						{
							byPlayer.Entity.World.SpawnItemEntity(contents, byPlayer.Entity.Pos.XYZ.Add(0.0, 0.5, 0.0));
						}
						contents = null;
						MarkDirty(true);
					}
				}
				return true;
			}
			return false;
		}

		public bool CanReceiveGlass(string[] layersCode, int[] layersAmount, out float fillTime)
		{
			if(contents != null || splittable && Block.Variant["state"] == "opened")
			{
				fillTime = 0;
				return false;
			}

			int index = FindRecipeIndex(layersCode, layersAmount);
			if(index < 0)
			{
				fillTime = 0;
				return false;
			}

			fillTime = ((BlockGlassBlowingMold)Block).Recipes[index].FillTime;
			return true;
		}

		public void TakeGlass(EntityAgent byEntity, string[] layersCode, int[] layersAmount)
		{
			int index = FindRecipeIndex(layersCode, layersAmount);
			var recipe = ((BlockGlassBlowingMold)Block).Recipes[index];

			var layers = recipe.Recipe;
			int layerIndex = layersCode.Length - 1;
			for(int i = layers.Length - 1; i >= 0; i--)
			{
				layersAmount[layerIndex] -= layers[i].Amount;
			}
			if(Api.Side == EnumAppSide.Server)
			{
				var item = recipe.Output.ResolvedItemstack;
				if(splittable || hasContentsTransform)
				{
					contents = item.Clone();
					MarkDirty(true);
				}
				else
				{
					if(!byEntity.TryGiveItemStack(item))
					{
						byEntity.World.SpawnItemEntity(item, byEntity.Pos.XYZ.Add(0.0, 0.5, 0.0));
					}
				}
			}
		}

		public ItemStack[] GetDropItems()
		{
			if(contents != null) return new ItemStack[] { contents.Clone() };
			return null;
		}

		public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
		{
			if(hasContentsTransform && Block.Variant["state"] == "opened" && contentsMesh != null)
			{
				mesher.AddMeshData(contentsMesh);
			}
			return base.OnTesselation(mesher, tessThreadTesselator);
		}

		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetItemstack("contents", contents);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
		{
			base.FromTreeAttributes(tree, worldAccessForResolve);
			contents = tree.GetItemstack("contents");
			if(Api?.World != null)
			{
				if(contents != null)
				{
					contents.ResolveBlockOrItem(Api.World);
				}
				if(Api.Side == EnumAppSide.Client)
				{
					UpdateMesh();
				}
			}
		}

		public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
		{
			base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed);
			Utils.FixIdMappingOrClear(ref contents, oldBlockIdMapping, oldItemIdMapping, worldForNewMappings);
		}

		public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
		{
			base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
			contents?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(contents), blockIdMapping, itemIdMapping);
		}

		private int FindRecipeIndex(string[] layersCode, int[] layersAmount)
		{
			var recipes = ((BlockGlassBlowingMold)Block).Recipes;
			for(int i = 0; i < recipes.Length; i++)
			{
				if(IsSuitableRecipe(recipes[i], layersCode, layersAmount))
				{
					return i;
				}
			}
			return -1;
		}

		private bool IsSuitableRecipe(BlowingMoldRecipe recipe, string[] layersCode, int[] layersAmount)
		{
			var layers = recipe.Recipe;
			if(layersCode.Length < layers.Length)
			{
				return false;
			}

			int layerIndex = layersCode.Length - 1;
			for(int i = layers.Length - 1; i >= 0; i--)
			{
				if(!string.Equals(layers[i].Code.ToShortString(), layersCode[layerIndex], StringComparison.InvariantCulture)) return false;
				if(!layers[i].IsSuitable(layersAmount[layerIndex])) return false;
				layerIndex--;
			}

			return true;
		}

		private void UpdateMesh()
		{
			if(hasContentsTransform)
			{
				if(contents == null) contentsMesh = null;
				else contentsMesh = GenMesh(contents);
			}
		}

		private MeshData GenMesh(ItemStack stack)
		{
			MeshData mesh;
			if(stack.Class == EnumItemClass.Block)
			{
				mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
			}
			else
			{
				nowTesselatingObj = stack.Collectible;
				nowTesselatingShape = null;
				if(stack.Item.Shape != null)
				{
					nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
				}
				capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

				mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
			}

			mesh.ModelTransform(contentsTransform);

			return mesh;
		}

		private TextureAtlasPosition GetOrCreateTexPos(AssetLocation texturePath)
		{
			TextureAtlasPosition texpos = capi.BlockTextureAtlas[texturePath];

			if(texpos == null)
			{
				IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if(texAsset != null)
				{
					BitmapRef bmp = texAsset.ToBitmap(capi);
					capi.BlockTextureAtlas.InsertTextureCached(texturePath, bmp, out _, out texpos);
				}
				else
				{
					texpos = capi.BlockTextureAtlas.UnknownTexturePosition;
					capi.World.Logger.Warning("For render in block " + Block.Code + ", item {0} defined texture {1}, not no such texture found.", nowTesselatingObj.Code, texturePath);
				}
			}

			return texpos;
		}
	}
}