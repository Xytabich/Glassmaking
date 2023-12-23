using GlassMaking.ItemRender;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
	internal class GlassLadleRenderer : IItemRenderer<GlassLadleRenderer.Data>
	{
		private int amount;
		private TemperatureState temperature;

		private MultiTextureMeshRef meshRef = null;

		public void UpdateIfChanged(ICoreClientAPI capi, ItemStack itemStack, Data data)
		{
			var amount = data.glassmelt.GetInt("amount");
			if(meshRef != null && temperature == data.temperature && this.amount == amount)
			{
				return;
			}

			temperature = data.temperature;
			this.amount = amount;

			var item = (ItemGlassLadle)itemStack.Item;
			UpdateMeshRef(capi, itemStack.Item, capi.Tesselator.GetTextureSource(itemStack.Item),
				(float)amount / item.maxGlassAmount, GlassRenderUtil.StateToGlow(temperature), item.glassTransform);
		}

		public void SetRenderInfo(ICoreClientAPI capi, ItemStack itemStack, ref ItemRenderInfo renderInfo)
		{
			renderInfo.ModelRef = meshRef;
			renderInfo.CullFaces = true;
		}

		public void UpdateMeshRef(ICoreClientAPI capi, Item item, ITexPositionSource tex, float fill, int glow, ModelTransform meshTransform)
		{
			var baseMesh = GetLadleMesh(capi, item, tex);
			var mesh = GetMeltMesh(fill, glow, tex, meshTransform);

			if(meshRef != null) meshRef.Dispose();

			meshRef = capi.Render.UploadMultiTextureMesh(baseMesh);
			int index = meshRef.meshrefs.Length;
			Array.Resize(ref meshRef.meshrefs, index + 1);
			meshRef.meshrefs[index] = capi.Render.UploadMesh(mesh);
			Array.Resize(ref meshRef.textureids, index + 1);
			meshRef.textureids[index] = mesh.TextureIds[0];
		}

		public void Dispose()
		{
			if(meshRef != null)
			{
				meshRef.Dispose();
				meshRef = null;
			}
		}

		private MeshData GetMeltMesh(float fill, int glow, ITexPositionSource tex, ModelTransform meshTransform)
		{
			var mesh = CubeMeshUtil.GetCubeFace(BlockFacing.UP).WithColorMaps();
			mesh.Flags = new int[mesh.VerticesCount];
			mesh.TextureIndices = new byte[mesh.VerticesCount / mesh.VerticesPerFace];
			mesh.Scale(Vec3f.Zero, 0.5f, 0.5f, 0.5f);
			mesh.Translate(0.5f, 0, 0.5f);
			mesh.SetTexPos(tex["glass"]);

			for(int i = 0; i < mesh.VerticesCount; i++)
			{
				mesh.xyz[i * 3 + 1] = fill;
				mesh.Flags[i] = glow & 255;
			}

			mesh.ModelTransform(meshTransform);
			return mesh;
		}

		private static MeshData GetLadleMesh(ICoreClientAPI capi, Item item, ITexPositionSource tex)
		{
			var shape = item.Shape;
			return ObjectCacheUtil.GetOrCreate(capi, "glassmaking:glassladle|" + item.Code.ToString(), () => {
				Shape shapeBase = capi.Assets.TryGet(new AssetLocation(shape.Base.Domain, "shapes/" + shape.Base.Path + ".json")).ToObject<Shape>();
				MeshData mesh;
				capi.Tesselator.TesselateShape("glassmaking:ladlemesh", shapeBase, out mesh, tex, new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ), 0, 0, 0);
				return mesh;
			});
		}

		internal struct Data
		{
			public TemperatureState temperature;
			public ITreeAttribute glassmelt;

			public Data(TemperatureState temperature, ITreeAttribute glassmelt)
			{
				this.temperature = temperature;
				this.glassmelt = glassmelt;
			}
		}
	}
}