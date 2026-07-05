using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
	/// <summary>
	/// Renderer for glowing items inside the annealer.
	/// Mirrors the GroundStorageRenderer pattern from vanilla.
	/// </summary>
	internal class AnnealerGlowRenderer : IRenderer
	{
		private readonly ICoreClientAPI capi;
		private readonly BlockEntityAnnealer be;
		private readonly Matrixf modelMat = new Matrixf();
		private readonly MultiTextureMeshRef?[] meshRefs;

		public double RenderOrder => 0.5;
		public int RenderRange => 24;

		public AnnealerGlowRenderer(ICoreClientAPI capi, BlockEntityAnnealer be)
		{
			this.capi = capi;
			this.be = be;
			meshRefs = new MultiTextureMeshRef?[be.GlowItemCount];
			UpdateMeshRefs();
			capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "annealerglow");
		}

		/// <summary>
		/// Re-uploads each occupied slot's MeshData as a MultiTextureMeshRef.
		/// Must be called on the main thread (e.g. from updateMeshes).
		/// </summary>
		public void UpdateMeshRefs()
		{
			for(int i = 0; i < be.GlowItemCount; i++)
			{
				meshRefs[i]?.Dispose();
				meshRefs[i] = null;

				var slot = be.Inventory[i];
				if(slot.Empty) continue;

				MeshData? mesh = be.GetSlotMesh(slot);
				if(mesh == null) continue;

				meshRefs[i] = capi.Render.UploadMultiTextureMesh(mesh);
			}
		}

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if(be.Inventory.Empty) return;

			var tfMatrices = be.GlowTfMatrices;
			if(tfMatrices == null) return;

			var pos = be.Pos;
			var camPos = capi.World.Player.Entity.CameraPos;
			var lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

			var prog = capi.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
			capi.Render.GlDisableCullFace();
			capi.Render.GlToggleBlend(true);

			prog.ViewMatrix = capi.Render.CameraMatrixOriginf;
			prog.ProjectionMatrix = capi.Render.CurrentProjectionMatrix;

			for(int i = 0; i < be.GlowItemCount; i++)
			{
				var slot = be.Inventory[i];
				if(slot.Empty || meshRefs[i] == null || i >= tfMatrices.Length) continue;

				float temp = (slot.Itemstack.Attributes["temperature"] as ITreeAttribute)
					?.GetFloat("temperature", 20f) ?? 20f;

				float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f((int)temp);
				int gi = GameMath.Clamp((int)((temp - 500) / 3), 0, 255);

				// tfMatrices[i] is a pure translation matrix (col 3 = translation xyz).
				modelMat
					.Identity()
					.Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
					.Translate(tfMatrices[i][12], tfMatrices[i][13], tfMatrices[i][14]);

				prog.ModelMatrix = modelMat.Values;
				prog.RgbaLightIn = lightrgbs;
				prog.TempGlowMode = gi > 0 ? 1 : 0;
				prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], gi / 255f);
				prog.ExtraGlow = gi;

				capi.Render.RenderMultiTextureMesh(meshRefs[i], "tex");
			}

			prog.TempGlowMode = 0;
			prog.Stop();
			capi.Render.GlEnableCullFace();
		}

		public void Dispose()
		{
			capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
			foreach(var mr in meshRefs)
				mr?.Dispose();
		}
	}
}
