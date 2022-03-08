using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.ItemRender
{
	public interface IItemRenderer<T> : IDisposable where T : struct
	{
		void UpdateIfChanged(ICoreClientAPI capi, ItemStack itemStack, T data);

		void SetRenderInfo(ICoreClientAPI capi, ItemStack itemStack, ref ItemRenderInfo renderInfo);
	}
}