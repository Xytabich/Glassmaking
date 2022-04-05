using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	public class BlockRendererGlassSmeltery : IRenderer
	{
		public double RenderOrder => 0.5;

		public int RenderRange => 24;

		private TextureAtlasPosition mixTexture, meltTexture;
		private Matrixf ModelMat = new Matrixf();

		private BlockPos pos;
		private ICoreClientAPI api;

		private MeshRef meshRef = null;

		private float percent = 0;
		private float height = 0;
		private float frustum = 0;

		private bool isMix = false;
		private int glowLevel = 0;

		private MeshRef bathMesh;
		private int bathTextureId;

		private EnumRenderStage renderStage;
		private float zOffset;

		private float maxHeight, offset, frustumMin, frustumMax;

		public BlockRendererGlassSmeltery(ICoreClientAPI api, BlockPos pos, EnumRenderStage renderStage,
			MeshRef bathMesh, ITexPositionSource tex, int bathTextureId,
			float maxHeight, float offset, float frustumMin, float frustumMax, float zOffset = 0)
		{
			this.api = api;
			this.pos = pos;
			this.renderStage = renderStage;
			this.bathMesh = bathMesh;
			this.bathTextureId = bathTextureId;
			this.zOffset = zOffset;
			this.maxHeight = maxHeight;
			this.offset = offset;
			this.frustumMin = frustumMin;
			this.frustumMax = frustumMax;
			mixTexture = tex["mix"];
			meltTexture = tex["melt"];
			api.Event.RegisterRenderer(this, renderStage, "glassmaking:smeltery");
		}

		public void SetHeight(float percent)
		{
			percent = GameMath.Clamp(percent, 0, 1);
			if(this.percent != percent)
			{
				this.percent = percent;

				float level = GameMath.Lerp(percent, 1, percent * (1 - frustumMin / frustumMax));
				height = level * maxHeight;
				frustum = frustumMin + (frustumMax - frustumMin) * level;
				meshRef?.Dispose();
				meshRef = null;
				if(height != 0)
				{
					meshRef = api.Render.UploadMesh(GenerateMesh());
				}
			}
		}

		public void SetParameters(bool isMix, int glowLevel)
		{
			this.glowLevel = glowLevel;
			if(this.isMix != isMix)
			{
				this.isMix = isMix;
				if(meshRef != null) api.Render.UpdateMesh(meshRef, GenerateMesh());
			}
		}

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			IStandardShaderProgram standardShaderProgram = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1f + glowLevel / 128f, 1f + glowLevel / 128f, 1f + glowLevel / 512f, 1f));
			standardShaderProgram.ExtraGlow = glowLevel;
			standardShaderProgram.ExtraZOffset = zOffset;
			IRenderAPI render = api.Render;
			Vec3d cameraPos = api.World.Player.Entity.CameraPos;
			standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
			standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
			standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z).Values;
			render.BindTexture2d(bathTextureId);
			render.RenderMesh(bathMesh);
			if(height != 0)
			{
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((0.5f + pos.X) - cameraPos.X, pos.Y - cameraPos.Y + offset + height, (0.5f + pos.Z) - cameraPos.Z).Values;
				render.BindTexture2d(isMix ? mixTexture.atlasTextureId : meltTexture.atlasTextureId);
				render.RenderMesh(meshRef);
			}
			standardShaderProgram.Stop();
		}

		public void Dispose()
		{
			meshRef?.Dispose();
			meshRef = null;
			api.Event.UnregisterRenderer(this, renderStage);
		}

		private MeshData GenerateMesh()
		{
			MeshData mesh = CubeMeshUtil.GetCubeFace(BlockFacing.UP);
			float scale = frustum * 0.5f;
			float uv = Math.Min(2f * scale, 1);
			for(int i = 0; i < mesh.GetVerticesCount(); i++)
			{
				mesh.xyz[3 * i] *= scale;
				mesh.xyz[3 * i + 2] *= scale;
				mesh.xyz[3 * i + 1] = 0;
				mesh.Uv[2 * i] *= uv;
				mesh.Uv[2 * i + 1] *= uv;
			}
			mesh.Flags = new int[24];
			mesh.SetTexPos(isMix ? mixTexture : meltTexture);
			return mesh;
		}
	}
}