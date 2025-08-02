using System;
using System.Collections.Generic;
using UnityEngine;

namespace McpToolForUnity
{
    /// <summary>
    /// Represents a 3D voxel grid with mesh generation capabilities
    /// </summary>
    public class VoxelData
    {
        private readonly byte[,,] voxels;
        private readonly int sizeX, sizeY, sizeZ;
        
        public int SizeX => sizeX;
        public int SizeY => sizeY;
        public int SizeZ => sizeZ;
        
        public VoxelData(int sizeX, int sizeY, int sizeZ)
        {
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.sizeZ = sizeZ;
            voxels = new byte[sizeX, sizeY, sizeZ];
        }
        
        public void SetVoxel(int x, int y, int z, byte materialId)
        {
            if (IsValidPosition(x, y, z))
                voxels[x, y, z] = materialId;
        }
        
        public byte GetVoxel(int x, int y, int z)
        {
            return IsValidPosition(x, y, z) ? voxels[x, y, z] : (byte)0;
        }
        
        public bool IsValidPosition(int x, int y, int z)
        {
            return x >= 0 && x < sizeX && y >= 0 && y < sizeY && z >= 0 && z < sizeZ;
        }
        
        public bool IsSolid(int x, int y, int z)
        {
            return GetVoxel(x, y, z) != 0;
        }
        
        /// <summary>
        /// Generate a basic mesh without optimization (simple approach)
        /// </summary>
        public Mesh GenerateBasicMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (IsSolid(x, y, z))
                        {
                            AddVoxelToMesh(x, y, z, vertices, triangles, uvs);
                        }
                    }
                }
            }
            
            var mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Generate an optimized mesh with face culling and greedy meshing
        /// </summary>
        public Mesh GenerateOptimizedMesh()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            // Use greedy meshing for each axis direction (both positive and negative)
            GreedyMeshAxis(vertices, triangles, uvs, Vector3Int.right, Vector3Int.up, Vector3Int.forward, true);   // +X faces
            GreedyMeshAxis(vertices, triangles, uvs, Vector3Int.left, Vector3Int.up, Vector3Int.forward, false);   // -X faces
            GreedyMeshAxis(vertices, triangles, uvs, Vector3Int.forward, Vector3Int.up, Vector3Int.right, true);   // +Z faces  
            GreedyMeshAxis(vertices, triangles, uvs, Vector3Int.back, Vector3Int.up, Vector3Int.right, false);     // -Z faces
            GreedyMeshAxis(vertices, triangles, uvs, Vector3Int.up, Vector3Int.forward, Vector3Int.right, true);   // +Y faces
            GreedyMeshAxis(vertices, triangles, uvs, Vector3Int.down, Vector3Int.forward, Vector3Int.right, false); // -Y faces
            
            var mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private void AddVoxelToMesh(int x, int y, int z, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            // Add all 6 faces of the voxel (no culling)
            AddFaceIfNeeded(x, y, z, Vector3Int.right, vertices, triangles, uvs);   // Right
            AddFaceIfNeeded(x, y, z, Vector3Int.left, vertices, triangles, uvs);    // Left
            AddFaceIfNeeded(x, y, z, Vector3Int.up, vertices, triangles, uvs);      // Up
            AddFaceIfNeeded(x, y, z, Vector3Int.down, vertices, triangles, uvs);    // Down
            AddFaceIfNeeded(x, y, z, Vector3Int.forward, vertices, triangles, uvs); // Forward
            AddFaceIfNeeded(x, y, z, Vector3Int.back, vertices, triangles, uvs);    // Back
        }
        
        private void AddFaceIfNeeded(int x, int y, int z, Vector3Int direction, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            int neighborX = x + direction.x;
            int neighborY = y + direction.y;
            int neighborZ = z + direction.z;
            
            // Only add face if neighbor is empty (face culling optimization)
            if (!IsSolid(neighborX, neighborY, neighborZ))
            {
                AddQuad(new Vector3(x, y, z), direction, vertices, triangles, uvs);
            }
        }
        
        private void AddQuad(Vector3 position, Vector3Int normal, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            int vertexIndex = vertices.Count;
            
            // Generate quad vertices based on normal direction
            Vector3[] quadVertices = GetQuadVertices(position, normal);
            
            vertices.AddRange(quadVertices);
            
            // Add triangle indices (two triangles per quad)
            triangles.AddRange(new int[] {
                vertexIndex, vertexIndex + 1, vertexIndex + 2,
                vertexIndex, vertexIndex + 2, vertexIndex + 3
            });
            
            // Add UVs
            uvs.AddRange(new Vector2[] {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            });
        }
        
        private Vector3[] GetQuadVertices(Vector3 position, Vector3Int normal)
        {
            Vector3[] vertices = new Vector3[4];
            
            if (normal == Vector3Int.right) // +X face
            {
                vertices[0] = position + new Vector3(1, 0, 0);
                vertices[1] = position + new Vector3(1, 1, 0);
                vertices[2] = position + new Vector3(1, 1, 1);
                vertices[3] = position + new Vector3(1, 0, 1);
            }
            else if (normal == Vector3Int.left) // -X face
            {
                vertices[0] = position + new Vector3(0, 0, 1);
                vertices[1] = position + new Vector3(0, 1, 1);
                vertices[2] = position + new Vector3(0, 1, 0);
                vertices[3] = position + new Vector3(0, 0, 0);
            }
            else if (normal == Vector3Int.up) // +Y face
            {
                vertices[0] = position + new Vector3(0, 1, 0);
                vertices[1] = position + new Vector3(0, 1, 1);
                vertices[2] = position + new Vector3(1, 1, 1);
                vertices[3] = position + new Vector3(1, 1, 0);
            }
            else if (normal == Vector3Int.down) // -Y face
            {
                vertices[0] = position + new Vector3(0, 0, 1);
                vertices[1] = position + new Vector3(0, 0, 0);
                vertices[2] = position + new Vector3(1, 0, 0);
                vertices[3] = position + new Vector3(1, 0, 1);
            }
            else if (normal == Vector3Int.forward) // +Z face
            {
                vertices[0] = position + new Vector3(1, 0, 1);
                vertices[1] = position + new Vector3(1, 1, 1);
                vertices[2] = position + new Vector3(0, 1, 1);
                vertices[3] = position + new Vector3(0, 0, 1);
            }
            else if (normal == Vector3Int.back) // -Z face
            {
                vertices[0] = position + new Vector3(0, 0, 0);
                vertices[1] = position + new Vector3(0, 1, 0);
                vertices[2] = position + new Vector3(1, 1, 0);
                vertices[3] = position + new Vector3(1, 0, 0);
            }
            
            return vertices;
        }
        
        private void GreedyMeshAxis(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, 
            Vector3Int normal, Vector3Int axisU, Vector3Int axisV, bool positive)
        {
            int sizeU = GetAxisSize(axisU);
            int sizeV = GetAxisSize(axisV);
            int sizeW = GetAxisSize(normal);
            
            // Mask to track which voxels have been processed
            bool[,] mask = new bool[sizeU, sizeV];
            
            for (int w = 0; w < sizeW; w++)
            {
                // Generate mask for this slice
                GenerateMaskForAxis(mask, w, normal, axisU, axisV, positive);
                
                // Generate quads from mask
                for (int v = 0; v < sizeV; v++)
                {
                    for (int u = 0; u < sizeU; u++)
                    {
                        if (mask[u, v])
                        {
                            // Find the largest rectangle starting from (u, v)
                            int width = 1;
                            int height = 1;
                            
                            // Expand width
                            while (u + width < sizeU && mask[u + width, v])
                                width++;
                            
                            // Expand height
                            bool canExpandHeight = true;
                            while (v + height < sizeV && canExpandHeight)
                            {
                                for (int i = 0; i < width; i++)
                                {
                                    if (!mask[u + i, v + height])
                                    {
                                        canExpandHeight = false;
                                        break;
                                    }
                                }
                                if (canExpandHeight)
                                    height++;
                            }
                            
                            // Add the quad
                            Vector3 pos = GetWorldPositionFromUVW(u, v, w, normal, axisU, axisV);
                            Vector3 sizeVec = GetWorldSizeFromUV(width, height, axisU, axisV);
                            AddGreedyQuad(pos, sizeVec, normal, vertices, triangles, uvs);
                            
                            // Mark used area in mask
                            for (int j = 0; j < height; j++)
                            {
                                for (int i = 0; i < width; i++)
                                {
                                    mask[u + i, v + j] = false;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void GenerateMaskForAxis(bool[,] mask, int w, Vector3Int normal, Vector3Int axisU, Vector3Int axisV, bool positive)
        {
            int sizeU = GetAxisSize(axisU);
            int sizeV = GetAxisSize(axisV);
            
            for (int v = 0; v < sizeV; v++)
            {
                for (int u = 0; u < sizeU; u++)
                {
                    Vector3Int pos = GetVoxelPositionFromUVW(u, v, w, normal, axisU, axisV);
                    Vector3Int neighborPos = pos + (positive ? normal : -normal);
                    
                    bool current = IsSolid(pos.x, pos.y, pos.z);
                    bool neighbor = IsSolid(neighborPos.x, neighborPos.y, neighborPos.z);
                    
                    // Add face if there's a transition from solid to empty
                    mask[u, v] = current && !neighbor;
                }
            }
        }
        
        private Vector3Int GetVoxelPositionFromUVW(int u, int v, int w, Vector3Int normal, Vector3Int axisU, Vector3Int axisV)
        {
            return u * axisU + v * axisV + w * GetAxisVector(normal);
        }
        
        private Vector3Int GetAxisVector(Vector3Int axis)
        {
            return new Vector3Int(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
        }
        
        private Vector3 GetWorldPositionFromUVW(int u, int v, int w, Vector3Int normal, Vector3Int axisU, Vector3Int axisV)
        {
            return u * (Vector3)axisU + v * (Vector3)axisV + w * (Vector3)GetAxisVector(normal);
        }
        
        private Vector3 GetWorldSizeFromUV(int width, int height, Vector3Int axisU, Vector3Int axisV)
        {
            return width * (Vector3)axisU + height * (Vector3)axisV;
        }
        
        private int GetAxisSize(Vector3Int axis)
        {
            if (axis.x != 0) return sizeX;
            if (axis.y != 0) return sizeY;
            return sizeZ;
        }
        
        private void AddGreedyQuad(Vector3 position, Vector3 size, Vector3Int normal, 
            List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            int vertexIndex = vertices.Count;
            
            Vector3[] quadVertices = GetGreedyQuadVertices(position, size, normal);
            vertices.AddRange(quadVertices);
            
            // Add triangle indices
            triangles.AddRange(new int[] {
                vertexIndex, vertexIndex + 1, vertexIndex + 2,
                vertexIndex, vertexIndex + 2, vertexIndex + 3
            });
            
            // Add UVs
            uvs.AddRange(new Vector2[] {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            });
        }
        
        private Vector3[] GetGreedyQuadVertices(Vector3 position, Vector3 size, Vector3Int normal)
        {
            Vector3[] vertices = new Vector3[4];
            
            if (normal == Vector3Int.right) // +X face
            {
                vertices[0] = position + new Vector3(1, 0, 0);
                vertices[1] = position + new Vector3(1, size.y, 0);
                vertices[2] = position + new Vector3(1, size.y, size.z);
                vertices[3] = position + new Vector3(1, 0, size.z);
            }
            else if (normal == Vector3Int.left) // -X face
            {
                vertices[0] = position + new Vector3(0, 0, size.z);
                vertices[1] = position + new Vector3(0, size.y, size.z);
                vertices[2] = position + new Vector3(0, size.y, 0);
                vertices[3] = position + new Vector3(0, 0, 0);
            }
            else if (normal == Vector3Int.up) // +Y face
            {
                vertices[0] = position + new Vector3(0, 1, 0);
                vertices[1] = position + new Vector3(0, 1, size.z);
                vertices[2] = position + new Vector3(size.x, 1, size.z);
                vertices[3] = position + new Vector3(size.x, 1, 0);
            }
            else if (normal == Vector3Int.down) // -Y face
            {
                vertices[0] = position + new Vector3(0, 0, size.z);
                vertices[1] = position + new Vector3(0, 0, 0);
                vertices[2] = position + new Vector3(size.x, 0, 0);
                vertices[3] = position + new Vector3(size.x, 0, size.z);
            }
            else if (normal == Vector3Int.forward) // +Z face
            {
                vertices[0] = position + new Vector3(size.x, 0, 1);
                vertices[1] = position + new Vector3(size.x, size.y, 1);
                vertices[2] = position + new Vector3(0, size.y, 1);
                vertices[3] = position + new Vector3(0, 0, 1);
            }
            else if (normal == Vector3Int.back) // -Z face
            {
                vertices[0] = position + new Vector3(0, 0, 0);
                vertices[1] = position + new Vector3(0, size.y, 0);
                vertices[2] = position + new Vector3(size.x, size.y, 0);
                vertices[3] = position + new Vector3(size.x, 0, 0);
            }
            
            return vertices;
        }
    }
}