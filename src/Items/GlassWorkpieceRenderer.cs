using GlassMaking.ItemRender;
using System.Collections.Generic;
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
		private Dictionary<string, CompositeTexture> nowTesselatingTextures;
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
			if(meshRef != null && code == data.code && step == data.step)
			{
				return;
			}

			this.code = data.code;
			this.step = data.step;

			var mesh = GenMesh(capi, data.recipe, data.step, capi.ItemTextureAtlas);

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
			if(shape != null)
			{
				nowTesselatingShape = capi.Assets.TryGet(new AssetLocation(shape.Base.Domain, "shapes/" + shape.Base.Path + ".json")).ToObject<Shape>();
			}

			MeshData meshdata;
			capi.Tesselator.TesselateShape("glassmaking:glass-workpiece", nowTesselatingShape, out meshdata, this);

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
	}
}