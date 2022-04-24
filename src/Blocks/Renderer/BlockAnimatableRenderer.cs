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

		public bool ShouldRender;

		private ICoreClientAPI capi;
		private AnimatorBase animator;

		private MeshRef meshref;
		private bool disposeMesh;
		private int textureId;

		private BlockPos blockPos;
		private Vec3f blockRot;
		private ModelTransform transform;

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

			if(!shadowPass)
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
			else
			{
				prog.UniformMatrix("modelViewMatrix", Mat4f.Mul(new float[16], capi.Render.CurrentModelviewMatrix, ModelMat));
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
				(float)(blockPos.X - entityPlayer.CameraPos.X),
				(float)(blockPos.Y - entityPlayer.CameraPos.Y),
				(float)(blockPos.Z - entityPlayer.CameraPos.Z)
			);

			Mat4f.Translate(ModelMat, ModelMat, transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
			Mat4f.Scale(ModelMat, ModelMat, transform.ScaleXYZ.X, transform.ScaleXYZ.Y, transform.ScaleXYZ.Z);
			Mat4f.RotateY(ModelMat, ModelMat, GameMath.DEG2RAD * (transform.Rotation.Y + blockRot.Y));
			Mat4f.RotateZ(ModelMat, ModelMat, GameMath.DEG2RAD * (transform.Rotation.Z + blockRot.Z));
			Mat4f.RotateX(ModelMat, ModelMat, GameMath.DEG2RAD * (transform.Rotation.X + blockRot.X));
			Mat4f.Translate(ModelMat, ModelMat, -transform.Origin.X, -transform.Origin.Y, -transform.Origin.Z);
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