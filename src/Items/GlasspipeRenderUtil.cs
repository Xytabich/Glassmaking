using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GlassMaking.Items
{
    internal class GlasspipeRenderUtil
    {
        private const int RADIAL_SECTIONS_COUNT = 8;
        private const int RADIAL_SECTION_INDICES = (RADIAL_SECTIONS_COUNT + 1) * 2;
        private const int CAP_SECTION_INDICES = RADIAL_SECTIONS_COUNT + 2;

        private static readonly int[] radialSectionTriangles = {
            0, 9,  1, 1, 9,  10,
            1, 10, 2, 2, 10, 11,
            2, 11, 3, 3, 11, 12,
            3, 12, 4, 4, 12, 13,
            4, 13, 5, 5, 13, 14,
            5, 14, 6, 6, 14, 15,
            6, 15, 7, 7, 15, 16,
            7, 16, 8, 8, 16, 17,
        };

        private static readonly int[] radialSectionTrianglesInverted = {
            0, 1, 9,  9,  1, 10,
            1, 2, 10, 10, 2, 11,
            2, 3, 11, 11, 3, 12,
            3, 4, 12, 12, 4, 13,
            4, 5, 13, 13, 5, 14,
            5, 6, 14, 14, 6, 15,
            6, 7, 15, 15, 7, 16,
            7, 8, 16, 16, 8, 17,
        };

        private static readonly int[] capTrianglesUp = {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5,
            0, 5, 6,
            0, 6, 7,
            0, 7, 8,
            0, 8, 9
        };

        private static readonly int[] capTrianglesDownInverted = {
            9, 0, 1,
            9, 1, 2,
            9, 2, 3,
            9, 3, 4,
            9, 4, 5,
            9, 5, 6,
            9, 6, 7,
            9, 7, 8
        };

        private static readonly int[] capTrianglesUpInverted = {
            1, 0, 2,
            2, 0, 3,
            3, 0, 4,
            4, 0, 5,
            5, 0, 6,
            6, 0, 7,
            7, 0, 8,
            8, 0, 9
        };

        private static readonly int[] capTrianglesDown = {
            0, 9, 1,
            1, 9, 2,
            2, 9, 3,
            3, 9, 4,
            4, 9, 5,
            5, 9, 6,
            6, 9, 7,
            7, 9, 8
        };

        public static int GenerateRadialVertices(MeshData mesh, FastVec2f info, bool isOuter, int glow)
        {
            if(info.Y == 0f)
            {
                AddVertice(mesh, 0, 0, info.X, 8f, glow);
                return 1;
            }
            else
            {
                const float step = GameMath.TWOPI / RADIAL_SECTIONS_COUNT;
                for(int i = 0; i <= RADIAL_SECTIONS_COUNT; i++)
                {
                    float angle = step * i;
                    AddVertice(mesh, GameMath.FastSin(angle) * info.Y, GameMath.FastCos(angle) * info.Y, info.X, Math.Abs(angle - GameMath.PI) * info.Y, glow);
                }
                return RADIAL_SECTIONS_COUNT + 1;
            }
        }

        private static void AddVertice(MeshData mesh, float x, float y, float z, float a, int glow)
        {
            const float scale = 1f / 16f;
            mesh.AddVertexWithFlags(x * scale, y * scale, z * scale, 1f - GetUvCoord(z * scale), GetUvCoord(a * scale), ColorUtil.WhiteArgb, glow | (2 << 8));
            var vec = new Vec3f(x, y, 0).Normalize();
            mesh.AddNormal(vec.X, vec.Y, vec.Z);
        }

        private static float GetUvCoord(float v)
        {
            v = v % 1f;
            if(v < 0) v += 1f;
            return v;
        }

        public static void GenerateRadialFaces(MeshData mesh, int prevVertices, int nextVertices, bool isOuter)//TODO: split mesh if uv is out of bounds (ie if prev uv is greater than next)
        {
            if(prevVertices == 1 || nextVertices == 1)
            {
                if(prevVertices == nextVertices) return;
                GenearateCapFaces(mesh, nextVertices == 1, isOuter);
                return;
            }

            int index = mesh.VerticesCount - RADIAL_SECTION_INDICES;
            var indices = isOuter ? radialSectionTriangles : radialSectionTrianglesInverted;
            for(int i = 0; i < indices.Length; i++)
            {
                mesh.AddIndex(indices[i] + index);
            }
        }

        private static void GenearateCapFaces(MeshData mesh, bool isDown, bool isOuter)
        {
            int index = mesh.VerticesCount - CAP_SECTION_INDICES;
            var indices = isDown ? (isOuter ? capTrianglesDown : capTrianglesDownInverted) : (isOuter ? capTrianglesUp : capTrianglesUpInverted);
            for(int i = 0; i < indices.Length; i++)
            {
                mesh.AddIndex(indices[i] + index);
            }
        }
    }
}