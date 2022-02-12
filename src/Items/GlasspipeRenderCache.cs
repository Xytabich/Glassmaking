using GlassMaking.TemporaryMetadata;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
	using IMeshContainer = ItemGlassworkPipe.IMeshContainer;

	internal class GlasspipeRenderCache : IMeshContainer, IDisposable
	{
		public object _data;
		public MeshData _mesh;
		public MeshRef meshRef = null;

		public bool isDirty = false;
		public TemperatureState temperature;

		public bool hasMesh => meshRef != null || updateMesh.HasValue;

		internal int id;
		internal IDisposableHandle tmpHandle;

		object IMeshContainer.data { get { return _data; } set { _data = value; } }
		MeshData IMeshContainer.mesh { get { return _mesh; } }

		private bool? updateMesh = null;
		private int prevVertices, prevIndices;

		private GlasspipeCacheManager manager;

		internal GlasspipeRenderCache(GlasspipeCacheManager manager, int id)
		{
			this.manager = manager;
			this.id = id;
		}

		public void BeginMeshChange()
		{
			_mesh.Clear();
		}

		public void EndMeshChange()
		{
			isDirty = false;
			if(meshRef == null || _mesh.VerticesCount != prevVertices || _mesh.IndicesCount != prevIndices)
			{
				prevVertices = _mesh.VerticesCount;
				prevIndices = _mesh.IndicesCount;
				updateMesh = true;
			}
			else
			{
				updateMesh = false;
			}
		}

		public void UpdateMeshRef(ICoreClientAPI capi, CompositeShape shape, ITexPositionSource tex, ModelTransform meshTransform)
		{
			if(updateMesh.HasValue)
			{
				_mesh.SetTexPos(tex["glass"]);
				var baseMesh = manager.GetMesh(capi, shape, tex);
				var toUpload = new MeshData(baseMesh.VerticesCount + _mesh.VerticesCount, baseMesh.IndicesCount + _mesh.IndicesCount, false, true, true, true).WithColorMaps();
				toUpload.AddMeshData(baseMesh);
				_mesh.ModelTransform(meshTransform);
				toUpload.AddMeshData(_mesh);
				if(updateMesh.Value)
				{
					if(meshRef != null) meshRef.Dispose();
					meshRef = capi.Render.UploadMesh(toUpload);
				}
				else
				{
					capi.Render.UpdateMesh(meshRef, toUpload);
				}
				updateMesh = null;
			}
		}

		internal void DisposeMesh()
		{
			if(meshRef != null)
			{
				meshRef.Dispose();
				meshRef = null;
			}
		}

		void IDisposable.Dispose()
		{
			manager.Remove(this);
		}

		public static TemperatureState TemperatureToState(float temperature, float workingTemperature)
		{
			if(temperature < workingTemperature * 0.45f) return TemperatureState.Cold;
			if(temperature < workingTemperature) return TemperatureState.Heated;
			return TemperatureState.Working;
		}

		public static int StateToGlow(TemperatureState state)
		{
			switch(state)
			{
				case TemperatureState.Heated: return 127;
				case TemperatureState.Working: return 255;
				default: return 0;
			}
		}
	}

	internal class GlasspipeCacheManager
	{
		public const string KEY = "glassmaking:glasspipemesh";

		private static int counter = 0;

		private Dictionary<int, GlasspipeRenderCache> containers = new Dictionary<int, GlasspipeRenderCache>();

		private MeshData mesh = null;
		private ITemporaryMetadataPool<GlasspipeRenderCache> pool;

		public GlasspipeCacheManager(ITemporaryMetadataPool<GlasspipeRenderCache> pool)
		{
			this.pool = pool;
		}

		public MeshData GetMesh(ICoreClientAPI capi, CompositeShape shape, ITexPositionSource tex)
		{
			if(mesh == null)
			{
				Shape shapeBase = capi.Assets.TryGet(new AssetLocation(shape.Base.Domain, "shapes/" + shape.Base.Path + ".json")).ToObject<Shape>();
				capi.Tesselator.TesselateShape("pipemesh", shapeBase, out mesh, tex, new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ), 0, 0, 0);
			}
			return mesh;
		}

		public bool TryGet(ItemStack item, out GlasspipeRenderCache container)
		{
			var id = item.TempAttributes.TryGetInt("glassmaking:tmpMeshId");
			if(id.HasValue && containers.TryGetValue(id.Value, out container))
			{
				container.tmpHandle.Postpone();
				return true;
			}
			container = default;
			return false;
		}

		public GlasspipeRenderCache GetOrCreate(ItemStack item)
		{
			var id = item.TempAttributes.TryGetInt("glassmaking:tmpMeshId");
			if(id.HasValue && containers.TryGetValue(id.Value, out var container))
			{
				container.tmpHandle.Postpone();
				return container;
			}
			else
			{
				id = counter++;
				container = new GlasspipeRenderCache(this, id.Value);
				container.tmpHandle = pool.AllocateHandle(container);

				container._mesh = new MeshData(16, 16, false, true, true, true).WithColorMaps();
				item.TempAttributes.SetInt("glassmaking:tmpMeshId", id.Value);
				containers[id.Value] = container;
				return container;
			}
		}

		public void Remove(ItemStack item)
		{
			var id = item.TempAttributes.TryGetInt("glassmaking:tmpMeshId");
			if(id.HasValue)
			{
				item.TempAttributes.RemoveAttribute("glassmaking:tmpMeshId");
				if(containers.TryGetValue(id.Value, out var container))
				{
					Remove(container);
				}
			}
		}

		public void Remove(GlasspipeRenderCache container)
		{
			containers.Remove(container.id);
			container.tmpHandle.Dispose();
			container.DisposeMesh();
		}
	}

	internal enum TemperatureState
	{
		Cold,
		Heated,
		Working
	}
}