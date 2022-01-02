//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace GlassMaking.Items
//{
//    internal class ItemGlassworkPipeRenderer
//    {
//        private const int RADIAL_SECTIONS_VERTICES = 8;
//        private const int RADIAL_SECTION_INDICES = (RADIAL_SECTIONS_VERTICES + 1) * 2;
//        private const int CAP_SECTION_INDICES = RADIAL_SECTIONS_VERTICES + 2;

//        private const int MAX_RADIUS = 16;

//        private static int nextMeshRefId = 0;

//        private static readonly int[] radialSectionTriangles = {
//            0, 9,  1, 1, 9,  10,
//            1, 10, 2, 2, 10, 11,
//            2, 11, 3, 3, 11, 12,
//            3, 12, 4, 4, 12, 13,
//            4, 13, 5, 5, 13, 14,
//            5, 14, 6, 6, 14, 15,
//            6, 15, 7, 7, 15, 16,
//            7, 16, 8, 8, 16, 17,
//        };

//        private static readonly int[] radialSectionTrianglesInverted = {
//            0, 1, 9,  9,  1, 10,
//            1, 2, 10, 10, 2, 11,
//            2, 3, 11, 11, 3, 12,
//            3, 4, 12, 12, 4, 13,
//            4, 5, 13, 13, 5, 14,
//            5, 6, 14, 14, 6, 15,
//            6, 7, 15, 15, 7, 16,
//            7, 8, 16, 16, 8, 17,
//        };

//        private static readonly int[] capTrianglesUp = {
//            0, 1, 2,
//            0, 2, 3,
//            0, 3, 4,
//            0, 4, 5,
//            0, 5, 6,
//            0, 6, 7,
//            0, 7, 8,
//            0, 8, 9
//        };

//        private static readonly int[] capTrianglesDown = {
//            9, 0, 1,
//            9, 1, 2,
//            9, 2, 3,
//            9, 3, 4,
//            9, 4, 5,
//            9, 5, 6,
//            9, 6, 7,
//            9, 7, 8
//        };

//        private static readonly int[] capTrianglesUpInverted = {
//            1, 0, 2,
//            2, 0, 3,
//            3, 0, 4,
//            4, 0, 5,
//            5, 0, 6,
//            6, 0, 7,
//            7, 0, 8,
//            8, 0, 9
//        };

//        private static readonly int[] capTrianglesDownInverted = {
//            0, 9, 1,
//            1, 9, 2,
//            2, 9, 3,
//            3, 9, 4,
//            4, 9, 5,
//            5, 9, 6,
//            6, 9, 7,
//            7, 9, 8
//        };

//        public override void OnUnloaded(ICoreAPI api)
//        {
//            var meshes = ObjectCacheUtil.TryGet<Dictionary<int, CachedWorkItem>>(api, "glassmaking:pipemesh");
//            if(meshes != null)
//            {
//                ObjectCacheUtil.Delete(api, "glassmaking:pipemesh");
//                foreach(var mesh in meshes)
//                {
//                    mesh.Value.meshref.Dispose();
//                }
//            }
//        }

//        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
//        {
//            if(itemstack.Attributes.HasAttribute("radii"))
//            {
//                CachedWorkItem mesh = CreateOrUpdateMesh(capi, itemstack);

//                renderinfo.ModelRef = mesh.meshref;
//                renderinfo.CullFaces = true;
//            }
//        }

//        private CachedWorkItem CreateOrUpdateMesh(ICoreClientAPI capi, ItemStack itemstack)
//        {
//            int value = itemstack.Attributes.GetInt("meshRefId");
//            if(!itemstack.Attributes.HasAttribute("meshRefId"))
//            {
//                value = ++nextMeshRefId;
//                itemstack.Attributes.SetInt("meshRefId", value);
//            }
//            var meshes = ObjectCacheUtil.GetOrCreate(capi, "glassmaking:pipemesh", () => new Dictionary<int, CachedWorkItem>());
//            var bytes = itemstack.Attributes.GetBytes("radii");
//            if(!meshes.TryGetValue(value, out var mesh) || mesh.isDirty && !Enumerable.SequenceEqual(bytes, mesh.radii))
//            {
//                if(mesh == null)
//                {
//                    mesh = new CachedWorkItem();
//                    meshes.Add(value, mesh);
//                }
//                else
//                {
//                    mesh.meshref.Dispose();
//                }
//                Shape shapeBase = capi.Assets.TryGet(new AssetLocation(Shape.Base.Domain, "shapes/" + Shape.Base.Path + ".json")).ToObject<Shape>();
//                capi.Tesselator.TesselateShape("pipemesh", shapeBase, out var modeldata, capi.Tesselator.GetTextureSource(this), new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), 0, 0, 0);
//                modeldata.AddMeshData(GenMesh(capi, bytes));
//                mesh.meshref = capi.Render.UploadMesh(modeldata);
//                mesh.radii = bytes;
//            }

//            return mesh;
//        }

//        private MeshData GenMesh(ICoreClientAPI capi, byte[] radii)
//        {
//            var mesh = new MeshData(24, 36).WithColorMaps().WithRenderpasses().WithNormals();
//            var texture = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("glass-plain")))["material"];

//            if(radii.Length > 0)
//            {
//                bool addCap = true;
//                int innerRadius, outerRadius;
//                AddVertice(mesh, 0, 0, -2.5f, 0, 0);
//                for(int i = 0; i < radii.Length; i++)
//                {
//                    innerRadius = radii[i] & 15;
//                    if(innerRadius == 0)
//                    {
//                        AddVertice(mesh, 0, 0, i + 0.5f, i / 32f, 0);
//                        if(!addCap) GenearateCapFaces(mesh, true, true);
//                        addCap = true;
//                    }
//                    else
//                    {
//                        GenerateRadialVertices(mesh, i, innerRadius, true);
//                        if(addCap)
//                        {
//                            GenearateCapFaces(mesh, false, true);
//                            addCap = false;
//                        }
//                        else GenerateRadialFaces(mesh, true);
//                    }
//                }
//                AddVertice(mesh, 0, 0, -3.5f, 0, 0);
//                for(int i = 0; i < radii.Length; i++)
//                {
//                    outerRadius = ((radii[i] >> 4) & 15) + 1;
//                    GenerateRadialVertices(mesh, i, outerRadius, false);
//                    if(i == 0) GenearateCapFaces(mesh, false, false);
//                    else GenerateRadialFaces(mesh, false);
//                }
//                AddVertice(mesh, 0, 0, radii.Length + 0.5f, radii.Length / 32f, 0);
//                GenearateCapFaces(mesh, true, false);
//            }
//            mesh.SetTexPos(texture);
//            return mesh;
//        }

//        private void GenerateRadialVertices(MeshData mesh, int offset, int radius, bool invertNormal)
//        {
//            float u = 1f / 32f;
//            float v = radius / (8f * RADIAL_SECTIONS_VERTICES);
//            float step = GameMath.PI * 2f / RADIAL_SECTIONS_VERTICES;
//            for(int i = 0; i <= RADIAL_SECTIONS_VERTICES; i++)
//            {
//                AddVertice(mesh, GameMath.FastSin(step * i) * radius, GameMath.FastCos(step * i) * radius, offset, offset * u, i * v);
//            }
//        }

//        private void AddVertice(MeshData mesh, float x, float y, float z, float u, float v)
//        {
//            float scale = 1f / 16f;
//            mesh.AddVertexWithFlags(x * scale, y * scale, z * scale, u, v, int.MaxValue, 255);
//            var vec = new Vec3f(x, y, 0).Normalize();
//            mesh.AddNormal(vec.X, vec.Y, vec.Z);
//        }

//        private void GenerateRadialFaces(MeshData mesh, bool invert)
//        {
//            int index = mesh.VerticesCount - RADIAL_SECTION_INDICES;
//            var indices = invert ? radialSectionTrianglesInverted : radialSectionTriangles;
//            for(int i = 0; i < indices.Length; i++)
//            {
//                mesh.AddIndex(indices[i] + index);
//            }
//        }

//        private void GenearateCapFaces(MeshData mesh, bool isDown, bool invert)
//        {
//            int index = mesh.VerticesCount - CAP_SECTION_INDICES;
//            var indices = isDown ? (invert ? capTrianglesDownInverted : capTrianglesDown) : (invert ? capTrianglesUpInverted : capTrianglesUp);
//            for(int i = 0; i < indices.Length; i++)
//            {
//                mesh.AddIndex(indices[i] + index);
//            }
//        }

//        public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
//        {
//            base.OnModifiedInInventorySlot(world, slot, extractedStack);
//            if(slot.Itemstack != null && slot.Itemstack.Attributes.HasAttribute("meshRefId"))
//            {
//                int value = slot.Itemstack.Attributes.GetInt("meshRefId");
//                var meshes = ObjectCacheUtil.TryGet<Dictionary<int, CachedWorkItem>>(world.Api, "glassmaking:pipemesh");
//                if(meshes != null && meshes.TryGetValue(value, out var mesh))
//                {
//                    mesh.isDirty = true;
//                }
//            }
//        }

//        private class CachedWorkItem
//        {
//            public MeshRef meshref;
//            public byte[] radii;
//            public bool isDirty = false;
//        }
//    }
//}