﻿using GlassMaking.TemporaryMetadata;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace GlassMaking.Common
{
	public class CachedMeshRefs
	{
		private Dictionary<object, RefContainer> containers = new Dictionary<object, RefContainer>();

		private ITemporaryMetadataPool<RefContainer> pool;

		internal CachedMeshRefs(ITemporaryMetadataPool<RefContainer> pool)
		{
			this.pool = pool;
		}

		public bool TryGetMeshRef(object key, out RefHandle handle)
		{
			if(containers.TryGetValue(key, out var container))
			{
				container.Postpone();
				handle = new RefHandle(container.meshRef, container);
				return true;
			}

			handle = default;
			return false;
		}

		public RefHandle SetMeshRef(object key, MeshRef meshRef)
		{
			if(containers.TryGetValue(key, out var container))
			{
				container.OnRemoved();
				container = null;
			}

			container = new RefContainer(this, key, meshRef);
			container.tmpHandle = pool.AllocateHandle(container);
			containers[key] = container;

			return new RefHandle(meshRef, container);
		}

		internal class RefContainer : IDisposable
		{
			internal IDisposableHandle tmpHandle;
			internal MeshRef meshRef;
			internal object key;

			private CachedMeshRefs manager;

			public RefContainer(CachedMeshRefs manager, object key, MeshRef meshRef)
			{
				this.manager = manager;
				this.key = key;
				this.meshRef = meshRef;
			}

			public void Postpone()
			{
				tmpHandle.Postpone();
			}

			public void OnRemoved()
			{
				tmpHandle.Dispose();
				meshRef.Dispose();
				meshRef = null;
			}

			void IDisposable.Dispose()
			{
				meshRef.Dispose();
				meshRef = null;
				manager.containers.Remove(key);
			}
		}

		public struct RefHandle
		{
			public MeshRef meshRef;

			public bool isValid => meshRef != null && !meshRef.Disposed;

			private RefContainer container;

			internal RefHandle(MeshRef meshRef, RefContainer container)
			{
				this.meshRef = meshRef;
				this.container = container;
			}

			public void Postpone()
			{
				container.Postpone();
			}
		}
	}
}