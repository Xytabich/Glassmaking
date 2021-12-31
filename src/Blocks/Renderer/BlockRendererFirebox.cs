using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockRendererFirebox : IRenderer
    {
        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        private int unlitTexture, litTexture;
        private Matrixf ModelMat = new Matrixf();

        private BlockPos pos;
        private ICoreClientAPI api;

        private MeshRef meshRef = null;

        private int contentHeight = 0;

        private bool isBurning = false;
        private int glowLevel = 0;

        public BlockRendererFirebox(BlockPos pos, ICoreClientAPI api)
        {
            this.pos = pos;
            this.api = api;
            this.unlitTexture = api.Render.GetOrLoadTexture(new AssetLocation("game", "block/coal/orecoalmix.png"));
            this.litTexture = api.Render.GetOrLoadTexture(new AssetLocation("game", "block/coal/ember.png"));
        }

        public void SetHeight(int contentHeight)
        {
            if(this.contentHeight != contentHeight)
            {
                this.contentHeight = contentHeight;
                meshRef?.Dispose();
                if(contentHeight != 0)
                {
                    MeshData cube = CubeMeshUtil.GetCube(0.3125f, contentHeight / 48f, new Vec3f(0f, 1f / 32f, 0f));
                    cube.Flags = new int[24];
                    meshRef = api.Render.UploadMesh(cube);
                }
            }
        }

        public void SetParameters(bool isBurning, int glowLevel)
        {
            this.isBurning = isBurning;
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
                render.BindTexture2d(isBurning ? litTexture : unlitTexture);
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
    }
}