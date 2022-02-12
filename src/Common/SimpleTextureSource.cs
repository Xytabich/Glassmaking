using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GlassMaking.Common
{
	public class SimpleTextureSource : ITexPositionSource
	{
		private static TextureAtlasPosition dummy = new TextureAtlasPosition() { x1 = 0, x2 = 1, y1 = 0, y2 = 1 };

		public Size2i AtlasSize { get; } = new Size2i(1, 1);

		public TextureAtlasPosition this[string textureCode] => dummy;
	}
}