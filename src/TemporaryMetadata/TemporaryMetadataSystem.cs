﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace GlassMaking.TemporaryMetadata
{
    public class TemporaryMetadataSystem : ModSystem
    {
        private List<ITemporaryMetadataPool> pools = new List<ITemporaryMetadataPool>();

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            api.Event.RegisterGameTickListener(OnTick, 1000);
        }

        public ITemporaryMetadataPool<T> CreatePool<T>(TimeSpan keepTime) where T : IDisposable
        {
            var pool = new TmpPool<T>(keepTime);
            pools.Add(pool);
            return pool;
        }

        private void OnTick(float dt)
        {
            for(int i = pools.Count - 1; i >= 0; i--)
            {
                pools[i].Update();
            }
        }

        private class TmpPool<T> : ITemporaryMetadataPool<T>, ITemporaryMetadataPool where T : IDisposable
        {
            private TimeSpan keepTime;

            private Queue<Handle> queue = new Queue<Handle>();

            public TmpPool(TimeSpan keepTime)
            {
                this.keepTime = keepTime;
            }

            IDisposableHandle ITemporaryMetadataPool<T>.AllocateHandle(T disposable)
            {
                var handle = new Handle(disposable);
                queue.Enqueue(handle);
                return handle;
            }

            void ITemporaryMetadataPool.Update()
            {
                DateTime now = DateTime.Now;
                DateTime time = now.Subtract(keepTime);
                Handle handle;
                while((handle = queue.Peek()).enqueueTime <= time)
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

            private class Handle : IDisposableHandle
            {
                public DateTime enqueueTime;
                public DateTime updateTime;

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

        private interface ITemporaryMetadataPool
        {
            void Update();
        }
    }
}