using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.TemporaryMetadata
{
	public class TemporaryMetadataSystem : ModSystem
	{
		private ICoreClientAPI? capi = null;
		private List<ITemporaryMetadataPool>? pools = null;
		private long tickListenerId;

		public override void StartClientSide(ICoreClientAPI api)
		{
			base.StartClientSide(api);
			tickListenerId = api.Event.RegisterGameTickListener(OnTick, 1000);
		}

		public override void Dispose()
		{
			if(capi != null)
			{
				capi.Event.UnregisterGameTickListener(tickListenerId);
				if(pools != null)
				{
					foreach(var pool in pools)
					{
						pool.Dispose();
					}
					pools = null;
				}
			}
			base.Dispose();
		}

		public ITemporaryMetadataPool<T> CreatePool<T>(TimeSpan keepTime) where T : IDisposable
		{
			if(pools == null) pools = new List<ITemporaryMetadataPool>();
			var pool = new TmpPool<T>(keepTime);
			pools.Add(pool);
			return pool;
		}

		private void OnTick(float dt)
		{
			if(pools != null)
			{
				for(int i = pools.Count - 1; i >= 0; i--)
				{
					pools[i].Update();
				}
			}
		}

		private class TmpPool<T> : ITemporaryMetadataPool<T>, ITemporaryMetadataPool where T : IDisposable
		{
			private readonly TimeSpan keepTime;

			private Queue<Handle> queue = new Queue<Handle>();

			public TmpPool(TimeSpan keepTime)
			{
				this.keepTime = keepTime;
			}

			public void Dispose()
			{
				foreach(var handle in queue)
				{
					handle.Dispose(true);
				}
				queue = null!;
			}

			IDisposableHandle ITemporaryMetadataPool<T>.AllocateHandle(T disposable)
			{
				var handle = new Handle(disposable);
				queue.Enqueue(handle);
				return handle;
			}

			void ITemporaryMetadataPool.Update()
			{
				if(queue.Count > 0)
				{
					DateTime now = DateTime.Now;
					DateTime time = now.Subtract(keepTime);
					Handle handle;
					while(queue.Count > 0 && (handle = queue.Peek()).enqueueTime <= time)
					{
						queue.Dequeue();
						if(handle.updateTime > time)
						{
							handle.enqueueTime = now;
							queue.Enqueue(handle);
						}
						else
						{
							handle.Dispose(true);
						}
					}
				}
			}

			private class Handle : IDisposableHandle
			{
				internal DateTime enqueueTime;
				internal DateTime updateTime;

				private T disposable;

				private bool isDisposed = false;

				public Handle(T disposable)
				{
					this.disposable = disposable;
					enqueueTime = updateTime = DateTime.Now;
				}

				void IDisposableHandle.Postpone()
				{
					updateTime = DateTime.Now;
				}

				void IDisposableHandle.Dispose()
				{
					Dispose(false);
				}

				public void Dispose(bool dispose)
				{
					dispose &= !isDisposed;
					isDisposed = true;

					if(dispose) disposable.Dispose();
				}
			}
		}

		private interface ITemporaryMetadataPool : IDisposable
		{
			void Update();
		}
	}
}