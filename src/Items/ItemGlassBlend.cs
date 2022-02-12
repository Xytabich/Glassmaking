using GlassMaking.Common;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GlassMaking.Items
{
	public class ItemGlassBlend : Item, IContainedMeshSource, ITexPositionSource
	{
		public Size2i AtlasSize => curAtlas.Size;

		private ITextureAtlasAPI curAtlas;
		private Shape nowTesselatingShape;
		private GlassBlend curBlend;

		public virtual TextureAtlasPosition this[string textureCode]
		{
			get
			{
				AssetLocation texturePath = null;
				CompositeTexture tex;

				// Prio 1: Get from collectible textures
				if(Textures.TryGetValue(textureCode, out tex))
				{
					texturePath = tex.Baked.BakedName;
				}

				if(textureCode == "material" && curBlend != null)
				{
					texturePath = curBlend.Code.Clone().WithPathPrefix("block/glass/");
				}

				// Prio 2: Get from collectible textures, use "all" code
				if(texturePath == null && Textures.TryGetValue("all", out tex))
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

				return getOrCreateTexPos(texturePath);
			}
		}

		public override void OnUnloaded(ICoreAPI api)
		{
			base.OnUnloaded(api);

			Dictionary<string, MeshRef> blendMeshrefs = ObjectCacheUtil.TryGet<Dictionary<string, MeshRef>>(api, "glassmaking:blendMeshRefs");
			if(blendMeshrefs != null)
			{
				foreach(var val in blendMeshrefs.Values)
				{
					val?.Dispose();
				}

				api.ObjectCache.Remove("glassmaking:blendMeshRefs");
			}
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
			GlassBlend blend = GlassBlend.FromJson(inSlot.Itemstack);
			if(blend == null) blend = GlassBlend.FromTreeAttributes(inSlot.Itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null && blend.Amount > 0)
			{
				dsc.AppendLine(Lang.Get("glassmaking:Melts into {0} units of {1} glass", blend.Amount, Lang.Get(GlassBlend.GetBlendNameCode(blend.Code))));
			}
		}

		public override string GetHeldItemName(ItemStack itemStack)
		{
			GlassBlend blend = GlassBlend.FromJson(itemStack);
			if(blend != null)
			{
				return Lang.Get("glassmaking:glassblend", Lang.Get(GlassBlend.GetBlendNameCode(blend.Code)));
			}
			blend = GlassBlend.FromTreeAttributes(itemStack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null)
			{
				return Lang.Get("glassmaking:glassshards", Lang.Get(GlassBlend.GetBlendNameCode(blend.Code)));
			}
			return base.GetHeldItemName(itemStack);
		}

		public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
		{
			GlassBlend blend = GlassBlend.FromTreeAttributes(itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend == null) return;

			Dictionary<string, MeshRef> blendMeshrefs = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:blendMeshRefs", () => new Dictionary<string, MeshRef>());
			string key = blend.Code.ToString();

			MeshRef meshRef;
			if(!blendMeshrefs.TryGetValue(key, out meshRef))
			{
				var mesh = GenMesh(itemstack, capi.ItemTextureAtlas);
				meshRef = mesh == null ? renderinfo.ModelRef : capi.Render.UploadMesh(mesh);
				blendMeshrefs[key] = meshRef;
			}
			renderinfo.ModelRef = meshRef;
		}

		public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
		{
			curAtlas = targetAtlas;
			MeshData mesh = genMesh(api as ICoreClientAPI, itemstack);
			mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
			return mesh;
		}

		public string GetMeshCacheKey(ItemStack itemstack)
		{
			GlassBlend blend = GlassBlend.FromTreeAttributes(itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));
			if(blend != null)
			{
				return "glassmaking:shards|" + blend.Code.ToString();
			}
			return "glassmaking:blend|" + Code.ToString();
		}

		protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
		{
			var capi = api as ICoreClientAPI;
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
					capi.World.Logger.Warning("Item {0} defined texture {1}, not no such texture found.", Code, texturePath);
				}
			}

			return texpos;
		}

		private MeshData genMesh(ICoreClientAPI capi, ItemStack itemstack)
		{
			Shape blendShape = capi.TesselatorManager.GetCachedShape(itemstack.Item.Shape.Base);

			MeshData meshdata;
			curBlend = GlassBlend.FromTreeAttributes(itemstack.Attributes.GetTreeAttribute(GlassBlend.PROPERTY_NAME));

			nowTesselatingShape = blendShape;

			capi.Tesselator.TesselateItem(this, out meshdata, this);

			nowTesselatingShape = null;

			return meshdata;
		}
	}
}