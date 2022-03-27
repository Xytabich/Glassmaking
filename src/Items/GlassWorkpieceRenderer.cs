using GlassMaking.ItemRender;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	internal class GlassWorkpieceRenderer : IItemRenderer<GlassWorkpieceRenderer.Data>, ITexPositionSource
	{
		public Size2i AtlasSize => curAtlas.Size;

		private string code;
		private int step;

		private MeshRef meshRef = null;

		private ICoreClientAPI capi;
		private ITextureAtlasAPI curAtlas;
		private Shape nowTesselatingShape;

		public virtual TextureAtlasPosition this[string textureCode]
		{
			get
			{
				if(!nowTesselatingShape.Textures.TryGetValue(textureCode, out var texturePath))
				{
					texturePath = new AssetLocation(textureCode);
				}

				return GetOrCreateTexPos(capi, texturePath);
			}
		}

		public void UpdateIfChanged(ICoreClientAPI capi, ItemStack itemStack, Data data)
		{
			if(meshRef != null && code == data.code && step == data.step)
			{
				return;
			}

			this.code = data.code;
			this.step = data.step;

			var mesh = GenMesh(capi, itemStack, data.recipe.Steps[data.step].Shape, capi.ItemTextureAtlas);

			if(meshRef != null) meshRef.Dispose();
			meshRef = capi.Render.UploadMesh(mesh);
		}

		public void SetRenderInfo(ICoreClientAPI capi, ItemStack itemStack, ref ItemRenderInfo renderInfo)
		{
			renderInfo.ModelRef = meshRef;
			renderInfo.CullFaces = false;
		}

		public void Dispose()
		{
			if(meshRef != null)
			{
				meshRef.Dispose();
				meshRef = null;
			}
		}

		private MeshData GenMesh(ICoreClientAPI capi, ItemStack itemstack, CompositeShape shape, ITextureAtlasAPI targetAtlas)
		{
			this.capi = capi;
			curAtlas = targetAtlas;
			nowTesselatingShape = capi.TesselatorManager.GetCachedShape(shape.Base);

			MeshData meshdata;
			capi.Tesselator.TesselateItem(itemstack.Item, out meshdata, this);

			nowTesselatingShape = null;
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
	}
}