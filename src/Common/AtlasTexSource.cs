using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Common
{
	public class AtlasTexSource : ITexPositionSource
	{
		public Size2i AtlasSize => atlas.Size;

		private ICoreClientAPI capi;
		private ITextureAtlasAPI atlas;
		private CollectibleObject collectible;
		private Shape shape;

		public AtlasTexSource(ICoreClientAPI capi, ITextureAtlasAPI atlas)
		{
			this.capi = capi;
			this.atlas = atlas;
		}

		public TextureAtlasPosition this[string textureCode]
		{
			get
			{
				var textures = collectible is Item item ? item.Textures : (collectible as Block).Textures;
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
					shape?.Textures.TryGetValue(textureCode, out texturePath);
				}

				// Prio 4: The code is the path
				if(texturePath == null)
				{
					texturePath = new AssetLocation(textureCode);
				}

				return GetOrCreateTexPos(texturePath);
			}
		}

		public void Init(CollectibleObject collectible, Shape shape)
		{
			this.collectible = collectible;
			this.shape = shape;
		}

		private TextureAtlasPosition GetOrCreateTexPos(AssetLocation texturePath)
		{
			TextureAtlasPosition texpos = atlas[texturePath];

			if(texpos == null)
			{
				IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if(texAsset != null)
				{
					BitmapRef bmp = texAsset.ToBitmap(capi);
					atlas.InsertTextureCached(texturePath, bmp, out _, out texpos);
				}
				else
				{
					texpos = atlas.UnknownTexturePosition;
					capi.World.Logger.Warning("Item {0} defined texture {1}, but no such texture was found.", collectible.Code, texturePath);
				}
			}

			return texpos;
		}
	}
}