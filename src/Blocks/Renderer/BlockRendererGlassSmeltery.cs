using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace GlassMaking.Blocks
{
    public class BlockRendererGlassSmeltery : IRenderer
    {
        public double RenderOrder => 0.5;

        public int RenderRange => 24;

        private int mixTexture, meltTexture;
        private Matrixf ModelMat = new Matrixf();

        private BlockPos pos;
        private ICoreClientAPI api;

        private MeshRef meshRef = null;

        private float height = 0;

        private bool isMix = false;
        private int glowLevel = 0;

        private MeshRef bathMesh;
        private int bathTextureId;

        public BlockRendererGlassSmeltery(BlockPos pos, ICoreClientAPI api, MeshRef bathMesh, int bathTextureId)
        {
            this.pos = pos;
            this.api = api;
            this.bathMesh = bathMesh;
            this.bathTextureId = bathTextureId;
            this.mixTexture = api.Render.GetOrLoadTexture(new AssetLocation("game", "block/coal/orecoalmix.png"));
            this.meltTexture = api.Render.GetOrLoadTexture(new AssetLocation("game", "block/coal/ember.png"));
        }

        public void SetHeight(float percent)
        {
            if(this.height != percent)
            {
                this.height = percent;
                meshRef?.Dispose();
                if(this.height != 0)
                {
                    MeshData cube = CubeMeshUtil.GetCubeFace(BlockFacing.UP, 0.3125f, 0.3125f, new Vec3f(0f, -2f / 32f - 0.3125f, 0f));//TODO: texture atlas
                    cube.Flags = new int[24];
                    meshRef = api.Render.UploadMesh(cube);
                }
            }
        }

        public void SetParameters(bool isMix, int glowLevel)
        {
            this.isMix = isMix;
            this.glowLevel = glowLevel;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            IStandardShaderProgram standardShaderProgram = api.Render.PreparedStandardShader(pos.X, pos.Y, pos.Z, new Vec4f(1f + glowLevel / 128f, 1f + glowLevel / 128f, 1f + glowLevel / 512f, 1f));
            standardShaderProgram.ExtraGlow = glowLevel;
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
                render.BindTexture2d(isMix ? mixTexture : meltTexture);
                render.RenderMesh(meshRef);
            }
            standardShaderProgram.Stop();
        }

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
            meshRef?.Dispose();
        }
    }
}