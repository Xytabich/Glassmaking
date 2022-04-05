using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Renderer
{
	public class WorkbenchWorkpieceRenderer : IRenderer
	{
		public double RenderOrder => 0.5;

		public int RenderRange => 16;

		public Matrixf itemTransform = new Matrixf();

		private ICoreClientAPI api;

		private BlockPos pos;
		private float rotation;

		private MeshRef meshRef = null;
		private Matrixf modelMat = new Matrixf();

		public WorkbenchWorkpieceRenderer(ICoreClientAPI api, BlockPos pos, float rotation)
		{
			this.api = api;
			this.pos = pos;
			this.rotation = rotation;
			api.Event.RegisterRenderer(this, EnumRenderStage.OIT, "glassmaking:workpiece");
		}

		public void SetItemMesh(MeshData mesh)
		{
			meshRef?.Dispose();
			meshRef = null;
			if(mesh != null)
			{
				meshRef = api.Render.UploadMesh(mesh);
			}
		}

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if(meshRef != null)
			{
				var rapi = api.Render;
				rapi.GlDisableCullFace();
				rapi.GlToggleBlend(blend: true);
				var shader = rapi.StandardShader;
				shader.Use();
				shader.Tex2D = api.ItemTextureAtlas.AtlasTextureIds[0];//TODO: probably need to use item atlases
				shader.DontWarpVertices = 0;
				shader.AddRenderFlags = 0;
				shader.RgbaAmbientIn = rapi.AmbientColor;
				shader.RgbaFogIn = rapi.FogColor;
				shader.FogMinIn = rapi.FogMin;
				shader.FogDensityIn = rapi.FogDensity;
				shader.RgbaTint = ColorUtil.WhiteArgbVec;
				shader.NormalShaded = 1;
				shader.ExtraGodray = 0f;
				shader.SsaoAttn = 0f;
				shader.AlphaTest = 0.05f;
				shader.OverlayOpacity = 0f;
				shader.RgbaLightIn = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
				shader.ExtraGlow = 0;
				var cameraPos = api.World.Player.Entity.CameraPos;
				modelMat.Identity();
				modelMat.Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z);
				modelMat.RotateYDeg(rotation);
				modelMat.ReverseMul(itemTransform.Values);
				shader.ModelMatrix = modelMat.Values;
				shader.ViewMatrix = rapi.CameraMatrixOriginf;
				shader.ProjectionMatrix = rapi.CurrentProjectionMatrix;
				rapi.RenderMesh(meshRef);
				shader.Stop();
			}
		}

		public void Dispose()
		{
			meshRef?.Dispose();
			meshRef = null;
			api.Event.UnregisterRenderer(this, EnumRenderStage.OIT);
		}
	}
}