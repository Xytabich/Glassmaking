using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockRendererFirebox : IRenderer
    {
        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        private TextureAtlasPosition unlitTexture, litTexture;
        private Matrixf ModelMat = new Matrixf();

        private BlockPos pos;
        private ICoreClientAPI api;

        private MeshRef meshRef = null;

        private int contentHeight = 0;

        private bool isBurning = false;
        private int glowLevel = 0;

        public BlockRendererFirebox(BlockPos pos, ITexPositionSource tex, ICoreClientAPI api)
        {
            this.pos = pos;
            this.api = api;
            this.unlitTexture = tex["unlit"];
            this.litTexture = tex["lit"];
        }

        public void SetHeight(int contentHeight)
        {
            if(this.contentHeight != contentHeight)
            {
                this.contentHeight = contentHeight;
                meshRef?.Dispose();
                meshRef = null;
                if(contentHeight != 0)
                {
                    meshRef = api.Render.UploadMesh(GenerateMesh());
                }
            }
        }

        public void SetParameters(bool isBurning, int glowLevel)
        {
            if(this.isBurning != isBurning)
            {
                this.isBurning = isBurning;
                if(meshRef != null) api.Render.UpdateMesh(meshRef, GenerateMesh());
            }
            this.glowLevel = glowLevel;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if(contentHeight != 0)
            {
                IStandardShaderProgram standardShaderProgram = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1f + glowLevel / 128f, 1f + glowLevel / 128f, 1f + glowLevel / 512f, 1f));
                standardShaderProgram.ExtraGlow = glowLevel;
                IRenderAPI render = api.Render;
                Vec3d cameraPos = api.World.Player.Entity.CameraPos;
                render.BindTexture2d(isBurning ? litTexture.atlasTextureId : unlitTexture.atlasTextureId);
                standardShaderProgram.ModelMatrix = ModelMat.Identity().Translate((0.5f + pos.X) - cameraPos.X, pos.Y - cameraPos.Y + contentHeight / 48f, (0.5f + pos.Z) - cameraPos.Z).Values;
                standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
                standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
                render.RenderMesh(meshRef);
                standardShaderProgram.Stop();
            }
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            meshRef?.Dispose();
        }

        private MeshData GenerateMesh()
        {
            MeshData mesh = CubeMeshUtil.GetCube(0.3125f, contentHeight / 48f, new Vec3f(0f, 1f / 32f, 0f));
            mesh.Flags = new int[24];
            mesh.SetTexPos(isBurning ? litTexture : unlitTexture);
            return mesh;
        }
    }
}