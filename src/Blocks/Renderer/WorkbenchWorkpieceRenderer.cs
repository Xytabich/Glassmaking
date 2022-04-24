﻿using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks.Renderer
{
	public class WorkbenchWorkpieceRenderer : IRenderer, IWorkpieceRenderer
	{
		public double RenderOrder => 0.38;

		public int RenderRange => 16;

		Matrixf IWorkpieceRenderer.itemTransform => itemMat;

		private ICoreClientAPI api;

		private BlockPos pos;
		private float rotation;

		private ItemRenderInfo renderInfo = null;
		private Matrixf modelMat = new Matrixf();
		private Matrixf itemMat = new Matrixf().Identity();

		public WorkbenchWorkpieceRenderer(ICoreClientAPI api, BlockPos pos, float rotation)
		{
			this.api = api;
			this.pos = pos;
			this.rotation = rotation;
			api.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "glassmaking:workpiece");
		}

		public void SetItemRenderInfo(ItemRenderInfo renderInfo)
		{
			this.renderInfo = renderInfo;
		}

		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if(renderInfo != null && renderInfo.ModelRef != null)
			{
				IRenderAPI rapi = api.Render;
				rapi.GlToggleBlend(true);

				IShaderProgram prevProg = rapi.CurrentActiveShader;
				prevProg?.Stop();

				IStandardShaderProgram prog = rapi.StandardShader;
				prog.Use();
				prog.Tex2D = renderInfo.TextureId;
				prog.RgbaTint = ColorUtil.WhiteArgbVec;
				prog.DontWarpVertices = 0;
				prog.NormalShaded = 1;
				prog.AlphaTest = renderInfo.AlphaTest;
				prog.AddRenderFlags = 0;

				prog.OverlayOpacity = renderInfo.OverlayOpacity;
				if(renderInfo.OverlayTexture != null && renderInfo.OverlayOpacity > 0)
				{
					prog.Tex2dOverlay2D = renderInfo.OverlayTexture.TextureId;
					prog.OverlayTextureSize = new Vec2f(renderInfo.OverlayTexture.Width, renderInfo.OverlayTexture.Height);
					prog.BaseTextureSize = new Vec2f(renderInfo.TextureSize.Width, renderInfo.TextureSize.Height);
					//TextureAtlasPosition texPos = rapi.GetTextureAtlasPosition(entityitem.Itemstack);//TODO: how to get..?
					//prog.BaseUvOrigin = new Vec2f(texPos.x1, texPos.y1);
				}

				prog.ExtraGlow = 0;
				prog.RgbaAmbientIn = rapi.AmbientColor;
				prog.RgbaLightIn = api.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
				prog.RgbaFogIn = rapi.FogColor;
				prog.FogMinIn = rapi.FogMin;
				prog.FogDensityIn = rapi.FogDensity;
				prog.ExtraGodray = 0;
				prog.NormalShaded = renderInfo.NormalShaded ? 1 : 0;

				prog.ProjectionMatrix = rapi.CurrentProjectionMatrix;
				prog.ViewMatrix = rapi.CameraMatrixOriginf;

				var cameraPos = api.World.Player.Entity.CameraPos;
				modelMat.Identity();
				modelMat.Translate(pos.X + 0.5 - cameraPos.X, pos.Y + 0.5 - cameraPos.Y, pos.Z + 0.5 - cameraPos.Z);
				modelMat.RotateYDeg(rotation);
				modelMat.Mul(itemMat.Values);
				prog.ModelMatrix = modelMat.Values;

				if(!renderInfo.CullFaces)
				{
					rapi.GlDisableCullFace();
				}

				rapi.RenderMesh(renderInfo.ModelRef);

				if(!renderInfo.CullFaces)
				{
					rapi.GlEnableCullFace();
				}

				prog.Stop();
				prevProg?.Use();
			}
		}

		public void Dispose()
		{
			api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
		}
	}
}