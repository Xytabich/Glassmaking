using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking
{
	public static class HandbookItemInfoEvent
	{
		public delegate void OnGetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, HandbookItemInfoSection section, List<RichTextComponentBase> outComponents);

		public static event OnGetHandbookInfo onGetHandbookInfo;

		internal static void GetHandbookInfo(ItemSlot inSlot, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, HandbookItemInfoSection section, List<RichTextComponentBase> components)
		{
			onGetHandbookInfo?.Invoke(inSlot, capi, allStacks, openDetailPageFor, section, components);
		}
	}
}