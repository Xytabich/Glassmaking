using GlassMaking.ItemRender;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace GlassMaking.Items
{
	using IMeshContainer = ItemGlassworkPipe.IMeshContainer;

	internal class PipeLayersRenderer : IItemRenderer<PipeLayersRenderer.Data>
	{
		private int layersCount;
		private int lastLayerSize;
		private TemperatureState temperature;

		private MultiTextureMeshRef meshRef = null;
		private MeshData mesh;

		public PipeLayersRenderer()
		{
			mesh = new MeshData(16, 16, false, true, true, true).WithColorMaps();
		}

		public void UpdateIfChanged(ICoreClientAPI capi, ItemStack itemStack, Data data)
		{
			var amounts = ((IntArrayAttribute)data.glasslayers["amount"]).value;
			if(meshRef != null && temperature == data.temperature && layersCount == amounts.Length && (amounts.Length == 0 || lastLayerSize == amounts[amounts.Length - 1]))
			{
				return;
			}

			temperature = data.temperature;
			layersCount = amounts.Length;
			if(amounts.Length > 0) lastLayerSize = amounts[amounts.Length - 1];

			int prevVertices = mesh.VerticesCount;
			int prevIndices = mesh.IndicesCount;

			UpdateGlasslayersMesh(amounts, GlassRenderUtil.StateToGlow(temperature));

			bool reupload = prevVertices != mesh.VerticesCount || prevIndices != mesh.IndicesCount;
			UpdateMeshRef(capi, itemStack.Item, capi.Tesselator.GetTextureSource(itemStack.Item), ((ItemGlassworkPipe)itemStack.Item).glassTransform, reupload);
		}

		public void SetRenderInfo(ICoreClientAPI capi, ItemStack itemStack, ref ItemRenderInfo renderInfo)
		{
			renderInfo.ModelRef = meshRef;
			renderInfo.CullFaces = true;
		}

		public void UpdateMeshRef(ICoreClientAPI capi, Item item, ITexPositionSource tex, ModelTransform meshTransform, bool reupload)
		{
			mesh.SetTexPos(tex["glass"]);
			mesh.ModelTransform(meshTransform);

			var baseMesh = GlasspipeRenderUtil.GetPipeMesh(capi, item, tex);

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

		private void UpdateGlasslayersMesh(int[] amounts, int glow)
		{
			int count = 0;
			foreach(var amount in amounts)
			{
				count += amount;
			}

			mesh.Clear();
			if(count == 0) return;

			const double invPI = 1.0 / Math.PI;
			var root = Math.Pow(count * invPI, 1.0 / 3.0);
			var shape = new SmoothRadialShape();
			shape.Segments = GameMath.Max(1, (int)Math.Floor(root)) * 2 + 3;

			float radius = (float)Math.Sqrt(count * invPI / root);
			float length = (float)(root * 1.5);
			shape.Outer = new SmoothRadialShape.ShapePart[] { new SmoothRadialShape.ShapePart() {
				Vertices = new float[][] {
					new float[] { -3, 0 },
					new float[] { length * 0.1f, radius  },
					new float[] { length, radius },
					new float[] { length, 0 }
				}
			} };
			SmoothRadialShape.BuildMesh(mesh, shape, (m, i, o) => GlasspipeRenderUtil.GenerateRadialVertices(m, i, o, glow), GlasspipeRenderUtil.GenerateRadialFaces);
		}

		internal struct Data
		{
			public TemperatureState temperature;
			public ITreeAttribute glasslayers;

			public Data(TemperatureState temperature, ITreeAttribute glasslayers)
			{
				this.temperature = temperature;
				this.glasslayers = glasslayers;
			}
		}
	}

	internal class PipeRecipeRenderer : IItemRenderer<PipeRecipeRenderer.Data>, IMeshContainer
	{
		MeshData IMeshContainer.Mesh => mesh;

		private string recipeCode;
		private int recipeStep;
		private float recipeProgress;
		private TemperatureState temperature;

		private MultiTextureMeshRef meshRef = null;
		private MeshData mesh;

		public PipeRecipeRenderer()
		{
			mesh = new MeshData(16, 16, false, true, true, true).WithColorMaps();
		}

		public void UpdateIfChanged(ICoreClientAPI capi, ItemStack itemStack, Data data)
		{
			string code = data.recipeAttribute.GetString("code");
			data.recipe.GetStepAndProgress(data.recipeAttribute, out int step, out float progress);
			if(meshRef != null && temperature == data.temperature && recipeCode == code && recipeStep == step && recipeProgress == progress)
			{
				return;
			}

			temperature = data.temperature;
			recipeCode = code;
			recipeStep = step;
			recipeProgress = progress;

			int prevVertices = mesh.VerticesCount;
			int prevIndices = mesh.IndicesCount;

			data.recipe.UpdateMesh(data.recipeAttribute, this, GlassRenderUtil.StateToGlow(temperature));

			bool reupload = prevVertices != mesh.VerticesCount || prevIndices != mesh.IndicesCount;
			UpdateMeshRef(capi, itemStack.Item, capi.Tesselator.GetTextureSource(itemStack.Item), ((ItemGlassworkPipe)itemStack.Item).glassTransform, reupload);
		}

		public void SetRenderInfo(ICoreClientAPI capi, ItemStack itemStack, ref ItemRenderInfo renderInfo)
		{
			renderInfo.ModelRef = meshRef;
			renderInfo.CullFaces = true;
		}

		public void UpdateMeshRef(ICoreClientAPI capi, Item item, ITexPositionSource tex, ModelTransform meshTransform, bool reupload)
		{
			mesh.SetTexPos(tex["glass"]);
			mesh.ModelTransform(meshTransform);

			var baseMesh = GlasspipeRenderUtil.GetPipeMesh(capi, item, tex);

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

		void IMeshContainer.BeginMeshChange()
		{
			mesh.Clear();
		}

		void IMeshContainer.EndMeshChange() { }

		internal struct Data
		{
			public TemperatureState temperature;
			public GlassBlowingRecipe recipe;
			public ITreeAttribute recipeAttribute;

			public Data(TemperatureState temperature, GlassBlowingRecipe recipe, ITreeAttribute recipeAttribute)
			{
				this.temperature = temperature;
				this.recipe = recipe;
				this.recipeAttribute = recipeAttribute;
			}
		}
	}

	internal static partial class GlasspipeRenderUtil
	{
		internal static MeshData GetPipeMesh(ICoreClientAPI capi, Item item, ITexPositionSource tex)
		{
			var shape = item.Shape;
			return ObjectCacheUtil.GetOrCreate(capi, "glassmaking:glasspipemesh|" + item.Code.ToString(), () => {
				Shape shapeBase = capi.Assets.TryGet(new AssetLocation(shape.Base.Domain, "shapes/" + shape.Base.Path + ".json")).ToObject<Shape>();
				MeshData mesh;
				capi.Tesselator.TesselateShape("glassmaking:pipemesh", shapeBase, out mesh, tex, new Vec3f(shape.rotateX, shape.rotateY, shape.rotateZ), 0, 0, 0);
				return mesh;
			});
		}
	}
}