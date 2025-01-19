using GlassMaking.TemporaryMetadata;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace GlassMaking.Common
{
	public class CachedMeshRefs
	{
		private readonly Dictionary<object, RefContainer> containers = new Dictionary<object, RefContainer>();

		private readonly ITemporaryMetadataPool<RefContainer> pool;

		internal CachedMeshRefs(ITemporaryMetadataPool<RefContainer> pool)
		{
			this.pool = pool;
		}

		public bool TryGetMeshRef(object key, out RefHandle handle)
		{
			if(containers.TryGetValue(key, out var container))
			{
				container.Postpone();
				handle = new RefHandle(container.MeshRef, container);
				return true;
			}

			handle = default;
			return false;
		}

		public RefHandle SetMeshRef(object key, MultiTextureMeshRef meshRef)
		{
			if(containers.TryGetValue(key, out var container))
			{
				container.OnRemoved();
				container = null;
			}

			container = new RefContainer(this, key, meshRef);
			container.TmpHandle = pool.AllocateHandle(container);
			containers[key] = container;

			return new RefHandle(meshRef, container);
		}

		internal class RefContainer : IDisposable
		{
			internal IDisposableHandle TmpHandle = default!;
			internal MultiTextureMeshRef MeshRef;
			internal object key;

			private readonly CachedMeshRefs manager;

			public RefContainer(CachedMeshRefs manager, object key, MultiTextureMeshRef meshRef)
			{
				this.manager = manager;
				this.key = key;
				this.MeshRef = meshRef;
			}

			public void Postpone()
			{
				TmpHandle.Postpone();
			}

			public void OnRemoved()
			{
				TmpHandle.Dispose();
				MeshRef.Dispose();
				MeshRef = null!;
			}

			void IDisposable.Dispose()
			{
				MeshRef.Dispose();
				MeshRef = null!;
				manager.containers.Remove(key);
			}
		}

		public struct RefHandle
		{
			public MultiTextureMeshRef meshRef;

			public bool isValid => meshRef != null && !meshRef.Disposed;

			private RefContainer container;

			internal RefHandle(MultiTextureMeshRef meshRef, RefContainer container)
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