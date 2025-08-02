using UnityEngine;

namespace McpToolForUnity
{
    /// <summary>
    /// MCP Tools for voxel mesh generation and optimization
    /// </summary>
    public static class VoxelMcpTools
    {
        [McpTool("Create a simple voxel structure (cube) and generate basic mesh")]
        public static string CreateSimpleVoxelCube([McpTool("Size of the cube")] int size)
        {
            var voxelData = new VoxelData(size, size, size);
            
            // Fill the cube
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        voxelData.SetVoxel(x, y, z, 1);
                    }
                }
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            return $"Basic mesh generated with {basicMesh.vertexCount} vertices and {basicMesh.triangles.Length / 3} triangles";
        }
        
        [McpTool("Create a simple voxel structure (cube) and generate optimized mesh")]
        public static string CreateOptimizedVoxelCube([McpTool("Size of the cube")] int size)
        {
            var voxelData = new VoxelData(size, size, size);
            
            // Fill the cube
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        voxelData.SetVoxel(x, y, z, 1);
                    }
                }
            }
            
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            return $"Optimized mesh generated with {optimizedMesh.vertexCount} vertices and {optimizedMesh.triangles.Length / 3} triangles";
        }
        
        [McpTool("Compare basic vs optimized mesh generation for a hollow cube")]
        public static string CompareHollowCube([McpTool("Outer size of the cube")] int outerSize, [McpTool("Wall thickness")] int thickness)
        {
            var voxelData = new VoxelData(outerSize, outerSize, outerSize);
            
            // Create hollow cube
            for (int x = 0; x < outerSize; x++)
            {
                for (int y = 0; y < outerSize; y++)
                {
                    for (int z = 0; z < outerSize; z++)
                    {
                        // Solid if on the outer shell
                        bool isOuterShell = x < thickness || x >= outerSize - thickness ||
                                          y < thickness || y >= outerSize - thickness ||
                                          z < thickness || z >= outerSize - thickness;
                        
                        if (isOuterShell)
                        {
                            voxelData.SetVoxel(x, y, z, 1);
                        }
                    }
                }
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            var basicVertices = basicMesh.vertexCount;
            var basicTriangles = basicMesh.triangles.Length / 3;
            var optimizedVertices = optimizedMesh.vertexCount;
            var optimizedTriangles = optimizedMesh.triangles.Length / 3;
            
            var vertexReduction = (1.0f - (float)optimizedVertices / basicVertices) * 100f;
            var triangleReduction = (1.0f - (float)optimizedTriangles / basicTriangles) * 100f;
            
            return $"Hollow cube ({outerSize}x{outerSize}x{outerSize}, thickness {thickness}):\n" +
                   $"Basic: {basicVertices} vertices, {basicTriangles} triangles\n" +
                   $"Optimized: {optimizedVertices} vertices, {optimizedTriangles} triangles\n" +
                   $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles";
        }
        
        [McpTool("Create a complex voxel structure and compare mesh generation methods")]
        public static string CreateComplexStructure([McpTool("Width")] int width, [McpTool("Height")] int height, [McpTool("Depth")] int depth)
        {
            var voxelData = new VoxelData(width, height, depth);
            
            // Create a more complex structure - stairs with some holes
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        // Create stair-like structure
                        bool isStair = y <= (x * height / width);
                        
                        // Add some random holes for complexity
                        bool hasHole = (x + y + z) % 7 == 0;
                        
                        if (isStair && !hasHole)
                        {
                            voxelData.SetVoxel(x, y, z, 1);
                        }
                    }
                }
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            var basicVertices = basicMesh.vertexCount;
            var basicTriangles = basicMesh.triangles.Length / 3;
            var optimizedVertices = optimizedMesh.vertexCount;
            var optimizedTriangles = optimizedMesh.triangles.Length / 3;
            
            var vertexReduction = (1.0f - (float)optimizedVertices / basicVertices) * 100f;
            var triangleReduction = (1.0f - (float)optimizedTriangles / basicTriangles) * 100f;
            
            return $"Complex structure ({width}x{height}x{depth}):\n" +
                   $"Basic: {basicVertices} vertices, {basicTriangles} triangles\n" +
                   $"Optimized: {optimizedVertices} vertices, {optimizedTriangles} triangles\n" +
                   $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles";
        }
        
        [McpTool("Create and save a voxel mesh as a Unity asset")]
        public static string CreateVoxelMeshAsset([McpTool("Asset name")] string assetName, [McpTool("Size")] int size, [McpTool("Use optimization")] bool useOptimization)
        {
#if UNITY_EDITOR
            var voxelData = new VoxelData(size, size, size);
            
            // Create a simple pattern
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        // Create a checkerboard pattern
                        if ((x + y + z) % 2 == 0)
                        {
                            voxelData.SetVoxel(x, y, z, 1);
                        }
                    }
                }
            }
            
            var mesh = useOptimization ? voxelData.GenerateOptimizedMesh() : voxelData.GenerateBasicMesh();
            mesh.name = assetName;
            
            var path = $"Assets/{assetName}.asset";
            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
            UnityEditor.AssetDatabase.SaveAssets();
            
            return $"Mesh asset '{assetName}' created at {path} with {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles";
#else
            return "This function is only available in the Unity Editor";
#endif
        }
    }
}