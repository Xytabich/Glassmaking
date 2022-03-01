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

		private float height = 0;

		private bool isMix = false;
		private int glowLevel = 0;

		private MeshRef bathMesh;
		private int bathTextureId;

		private EnumRenderStage renderStage;
		private float zOffset;

		public BlockRendererGlassSmeltery(ICoreClientAPI api, BlockPos pos, EnumRenderStage renderStage,
			MeshRef bathMesh, ITexPositionSource tex, int bathTextureId, float zOffset = 0)//TODO: add parameters: melt offset, melt max height, melt min width, melt max width (to create a cone)
		{
			this.api = api;
			this.pos = pos;
			this.renderStage = renderStage;
			this.bathMesh = bathMesh;
			this.bathTextureId = bathTextureId;
			this.zOffset = zOffset;
			mixTexture = tex["mix"];
			meltTexture = tex["melt"];
			api.Event.RegisterRenderer(this, renderStage, "glassmaking:smeltery");
		}

		public void SetHeight(float percent)
		{
			if(height != percent)
			{
				height = percent;
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
				standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((0.5f + pos.X) - cameraPos.X, pos.Y - cameraPos.Y + height / 16f, (0.5f + pos.Z) - cameraPos.Z).Values;
				render.BindTexture2d(isMix ? mixTexture.atlasTextureId : meltTexture.atlasTextureId);
				render.RenderMesh(meshRef);
			}
			standardShaderProgram.Stop();
		}

		public void Dispose()
		{
			api.Event.UnregisterRenderer(this, renderStage);
			meshRef?.Dispose();
		}

		private MeshData GenerateMesh()
		{
			MeshData mesh = CubeMeshUtil.GetCubeFace(BlockFacing.UP, 0.3125f, 0.3125f, new Vec3f(0f, -2f / 32f - 0.3125f, 0f));
			mesh.Flags = new int[24];
			mesh.SetTexPos(isMix ? mixTexture : meltTexture);
			return mesh;
		}
	}
}