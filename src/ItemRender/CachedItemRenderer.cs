using GlassMaking.TemporaryMetadata;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.ItemRender
{
	public class CachedItemRenderer
	{
		private int counter = 0;
		private Dictionary<int, RendererContainer> containers = new Dictionary<int, RendererContainer>();

		private ITemporaryMetadataPool<RendererContainer> pool;

		internal CachedItemRenderer(ITemporaryMetadataPool<RendererContainer> pool)
		{
			this.pool = pool;
		}

		public void RenderItem<TRenderer, TData>(ICoreClientAPI capi, ItemStack itemStack, TData data, ref ItemRenderInfo renderInfo)
			where TRenderer : IItemRenderer<TData>, new() where TData : struct
		{
			var idProp = itemStack.TempAttributes.TryGetInt("glassmaking:tmpMeshId");
			RendererContainer container;
			if(idProp.HasValue && containers.TryGetValue(idProp.Value, out container))
			{
				if(container.renderer is TRenderer)
				{
					container.Postpone();
				}
				else
				{
					container.OnRemoved();
					container = null;
				}
			}
			else
			{
				container = null;
			}

			if(container == null)
			{
				int id = counter++;
				container = new RendererContainer(this, id);
				container.tmpHandle = pool.AllocateHandle(container);
				container.renderer = new TRenderer();

				itemStack.TempAttributes.SetInt("glassmaking:tmpMeshId", id);
				containers[id] = container;
			}

			var renderer = (TRenderer)container.renderer;
			renderer.UpdateIfChanged(capi, itemStack, data);
			renderer.SetRenderInfo(capi, itemStack, ref renderInfo);
		}

		public void RemoveRenderer(ItemStack itemStack)
		{
			var idProp = itemStack.TempAttributes.TryGetInt("glassmaking:tmpMeshId");
			if(idProp.HasValue)
			{
				itemStack.TempAttributes.RemoveAttribute("glassmaking:tmpMeshId");
				if(containers.TryGetValue(idProp.Value, out var container))
				{
					containers.Remove(container.id);
					container.OnRemoved();
				}
			}
		}

		internal class RendererContainer : IDisposable
		{
			internal IDisposableHandle tmpHandle;
			internal IDisposable renderer;
			internal int id;

			private CachedItemRenderer manager;

			public RendererContainer(CachedItemRenderer manager, int id)
			{
				this.manager = manager;
				this.id = id;
			}

			public void Postpone()
			{
				tmpHandle.Postpone();
			}

			public void OnRemoved()
			{
				tmpHandle.Dispose();
				renderer.Dispose();
			}

			void IDisposable.Dispose()
			{
				renderer.Dispose();
				manager.containers.Remove(id);
			}
		}
	}
}