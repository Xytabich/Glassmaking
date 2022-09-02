using GlassMaking.Common;
using GlassMaking.ItemRender;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
	internal class GlassWorkpieceRenderer : IItemRenderer<GlassWorkpieceRenderer.Data>, ITexPositionSource
	{
		public Size2i AtlasSize => curAtlas.Size;

		private string code;
		private int step;

		private CachedMeshRefs.RefHandle meshRefHandle = default;

		private ICoreClientAPI capi;
		private ITextureAtlasAPI curAtlas;
		private IDictionary<string, CompositeTexture> nowTesselatingTextures;
		private Shape nowTesselatingShape;

		public virtual TextureAtlasPosition this[string textureCode]
		{
			get
			{
				AssetLocation texturePath = null;

				if(nowTesselatingTextures != null && nowTesselatingTextures.TryGetValue(textureCode, out var comp))
				{
					texturePath = comp.Base;
				}

				if(texturePath == null && !nowTesselatingShape.Textures.TryGetValue(textureCode, out texturePath))
				{
					texturePath = new AssetLocation(textureCode);
				}

				return GetOrCreateTexPos(capi, texturePath);
			}
		}

		public void UpdateIfChanged(ICoreClientAPI capi, ItemStack itemStack, Data data)
		{
			if(meshRefHandle.isValid && code == data.code && step == data.step)
			{
				meshRefHandle.Postpone();
				return;
			}

			this.code = data.code;
			this.step = data.step;

			var meshRefCache = capi.ModLoader.GetModSystem<GlassMakingMod>().meshRefCache;
			var key = new WorkpieceMeshKey(data.recipe.Code, data.step);
			if(!meshRefCache.TryGetMeshRef(key, out meshRefHandle))
			{
				var mesh = GenMesh(capi, data.recipe, data.step, capi.ItemTextureAtlas);

				var meshRef = capi.Render.UploadMesh(mesh);
				meshRefHandle = meshRefCache.SetMeshRef(key, meshRef);
			}
		}

		public void SetRenderInfo(ICoreClientAPI capi, ItemStack itemStack, ref ItemRenderInfo renderInfo)
		{
			renderInfo.ModelRef = meshRefHandle.meshRef;
			renderInfo.CullFaces = false;
		}

		public void Dispose()
		{
		}

		private MeshData GenMesh(ICoreClientAPI capi, WorkbenchRecipe recipe, int step, ITextureAtlasAPI targetAtlas)
		{
			this.capi = capi;
			curAtlas = targetAtlas;
			nowTesselatingTextures = null;
			CompositeShape shape = null;
			for(int i = step - 1; i >= 0; i--)
			{
				if(nowTesselatingTextures == null)
				{
					nowTesselatingTextures = recipe.Steps[i].Textures;
				}
				if(shape == null)
				{
					shape = recipe.Steps[i].Shape;
					if(shape != null) break;
				}
			}

			MeshData meshdata;
			if(shape == null)
			{
				var stack = recipe.Input.ResolvedItemstack;
				if(stack.Class == EnumItemClass.Block)
				{
					nowTesselatingTextures = stack.Block.Textures;
					nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Block.Shape.Base);
					capi.Tesselator.TesselateShape("glassmaking:glass-workpiece", nowTesselatingShape, out meshdata, this);
				}
				else
				{
					capi.Tesselator.TesselateItem(stack.Item, out meshdata);
				}
			}
			else
			{
				var shapesCache = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:workbenchrecipeshapes", () => new Dictionary<AssetLocation, Shape>());
				if(!shapesCache.TryGetValue(shape.Base, out nowTesselatingShape))
				{
					nowTesselatingShape = capi.Assets.TryGet(new AssetLocation(shape.Base.Domain, "shapes/" + shape.Base.Path + ".json")).ToObject<Shape>();
					shapesCache[shape.Base] = nowTesselatingShape;
				}

				capi.Tesselator.TesselateShape("glassmaking:glass-workpiece", nowTesselatingShape, out meshdata, this);
			}

			nowTesselatingShape = null;
			nowTesselatingTextures = null;
			curAtlas = null;
			this.capi = null;

			return meshdata;
		}

		private TextureAtlasPosition GetOrCreateTexPos(ICoreClientAPI capi, AssetLocation texturePath)
		{
			TextureAtlasPosition texpos = curAtlas[texturePath];

			if(texpos == null)
			{
				IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if(texAsset != null)
				{
					BitmapRef bmp = texAsset.ToBitmap(capi);
					curAtlas.InsertTextureCached(texturePath, bmp, out _, out texpos);
				}
				else
				{
					texpos = curAtlas.UnknownTexturePosition;
					capi.World.Logger.Warning("Workpiece defined texture {0}, not no such texture found.", texturePath);
				}
			}

			return texpos;
		}

		internal struct Data
		{
			public string code;
			public int step;
			public WorkbenchRecipe recipe;

			public Data(string code, int step, WorkbenchRecipe recipe)
			{
				this.code = code;
				this.step = step;
				this.recipe = recipe;
			}
		}

		private struct WorkpieceMeshKey
		{
			private AssetLocation code;
			private int step;

			public WorkpieceMeshKey(AssetLocation code, int step)
			{
				this.code = code;
				this.step = step;
			}

			public override bool Equals(object obj)
			{
				return obj is WorkpieceMeshKey key &&
					   EqualityComparer<AssetLocation>.Default.Equals(code, key.code) &&
					   step == key.step;
			}

			public override int GetHashCode()
			{
				int hashCode = -1973448413;
				hashCode = hashCode * -1521134295 + EqualityComparer<AssetLocation>.Default.GetHashCode(code);
				hashCode = hashCode * -1521134295 + step.GetHashCode();
				return hashCode;
			}
		}
	}
}