using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Renderer
{
	public class BlockAnimatableRenderer : IRenderer
	{
		public double RenderOrder => 1;
		public int RenderRange => 99;

		public float[] ModelMat = Mat4f.Create();

		public bool ShouldRender = true;

		private ICoreClientAPI capi;
		private AnimatorBase animator;

		private MeshRef meshref;
		private bool disposeMesh;
		private int textureId;

		private BlockPos blockPos;
		private Vec3f blockRot;
		private ModelTransform transform;
		private Matrixf transformMat = new Matrixf();

		public BlockAnimatableRenderer(ICoreClientAPI capi, BlockPos blockPos, Vec3f blockRot, ModelTransform transform, AnimatorBase animator, MeshRef meshref, bool disposeMesh = true)
		{
			this.capi = capi;
			this.blockPos = blockPos;
			this.blockRot = blockRot;
			this.transform = transform;
			this.animator = animator;
			this.meshref = meshref;
			this.disposeMesh = disposeMesh;

			if(blockRot == null) this.blockRot = new Vec3f();
			if(transform == null) this.transform = ModelTransform.NoTransform;

			transform.CopyTo(transformMat);

			textureId = capi.BlockTextureAtlas.AtlasTextureIds[0];

			capi.Event.EnqueueMainThreadTask(() => {
				capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "glassmaking:blockanimatable");
				capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowFar, "glassmaking:blockanimatable");
				capi.Event.RegisterRenderer(this, EnumRenderStage.ShadowNear, "glassmaking:blockanimatable");
			}, "registerrenderers");
		}

		public void OnRenderFrame(float dt, EnumRenderStage stage)
		{
			if(!ShouldRender || meshref.Disposed || !meshref.Initialized) return;

			bool shadowPass = stage != EnumRenderStage.Opaque;

			LoadModelMatrix();

			IRenderAPI rpi = capi.Render;

			IShaderProgram prevProg = rpi.CurrentActiveShader;
			prevProg?.Stop();

			IShaderProgram prog = rpi.GetEngineShader(shadowPass ? EnumShaderProgram.Shadowmapentityanimated : EnumShaderProgram.Entityanimated);
			prog.Use();
			Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs((int)blockPos.X, (int)blockPos.Y, (int)blockPos.Z);
			rpi.GlToggleBlend(true, EnumBlendMode.Standard);

			if(shadowPass)
			{
				prog.UniformMatrix("modelViewMatrix", Mat4f.Mul(new float[16], capi.Render.CurrentModelviewMatrix, ModelMat));
			}
			else
			{
				prog.Uniform("rgbaAmbientIn", rpi.AmbientColor);
				prog.Uniform("rgbaFogIn", rpi.FogColor);
				prog.Uniform("fogMinIn", rpi.FogMin);
				prog.Uniform("fogDensityIn", rpi.FogDensity);
				prog.Uniform("rgbaLightIn", lightrgbs);
				prog.Uniform("renderColor", ColorUtil.WhiteArgbVec);
				prog.Uniform("alphaTest", 0.1f);
				prog.UniformMatrix("modelMatrix", ModelMat);
				prog.UniformMatrix("viewMatrix", rpi.CameraMatrixOriginf);
				prog.Uniform("windWaveIntensity", (float)0);
				prog.Uniform("skipRenderJointId", -2);
				prog.Uniform("skipRenderJointId2", -2);
				prog.Uniform("glitchEffectStrength", 0f);
			}

			prog.BindTexture2D("entityTex", textureId, 0);
			prog.UniformMatrix("projectionMatrix", rpi.CurrentProjectionMatrix);

			prog.Uniform("addRenderFlags", 0);

			prog.UniformMatrices(
				"elementTransforms",
				GlobalConstants.MaxAnimatedElements,
				animator.Matrices
			);

			capi.Render.RenderMesh(meshref);

			prog.Stop();
			prevProg?.Use();
		}

		private void LoadModelMatrix()
		{
			EntityPlayer entityPlayer = capi.World.Player.Entity;

			Mat4f.Identity(ModelMat);
			Mat4f.Translate(ModelMat, ModelMat,
				(float)(blockPos.X + 0.5 - entityPlayer.CameraPos.X),
				(float)(blockPos.Y + 0.5 - entityPlayer.CameraPos.Y),
				(float)(blockPos.Z + 0.5 - entityPlayer.CameraPos.Z)
			);
			Mat4f.RotateY(ModelMat, ModelMat, GameMath.DEG2RAD * blockRot.Y);
			Mat4f.RotateZ(ModelMat, ModelMat, GameMath.DEG2RAD * blockRot.Z);
			Mat4f.RotateX(ModelMat, ModelMat, GameMath.DEG2RAD * blockRot.X);

			Mat4f.Mul(ModelMat, ModelMat, transformMat.Values);
		}

		public void Dispose()
		{
			capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
			capi.Event.UnregisterRenderer(this, EnumRenderStage.ShadowFar);
			capi.Event.UnregisterRenderer(this, EnumRenderStage.ShadowNear);
			if(disposeMesh) meshref?.Dispose();
		}
	}
}