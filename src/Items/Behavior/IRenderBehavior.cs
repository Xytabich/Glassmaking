using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.Items.Behavior
{
	public interface IRenderBehavior
	{
		void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo, ref EnumHandling handling);
	}
}