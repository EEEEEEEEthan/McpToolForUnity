using UnityEngine;

namespace McpToolForUnity
{
    /// <summary>
    /// Demonstration tools showing the effectiveness of voxel mesh optimization
    /// </summary>
    public static class VoxelDemoTools
    {
        [McpTool("Demo: Solid cube comparison")]
        public static string DemoSolidCube([McpTool("Cube size")] int size)
        {
            var voxelData = new VoxelData(size, size, size);
            
            // Create a solid cube
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
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            // Calculate theoretical values
            int totalVoxels = size * size * size;
            int exposedFaces = 6 * size * size; // Only exterior faces should remain
            int theoreticalVertices = exposedFaces * 4; // 4 vertices per face
            int theoreticalTriangles = exposedFaces * 2; // 2 triangles per face
            
            var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
            var triangleReduction = (1.0f - (float)optimizedMesh.triangles.Length / basicMesh.triangles.Length) * 100f;
            
            return $"Solid Cube {size}³ ({totalVoxels} voxels):\n" +
                   $"Basic: {basicMesh.vertexCount} vertices, {basicMesh.triangles.Length / 3} triangles\n" +
                   $"Optimized: {optimizedMesh.vertexCount} vertices, {optimizedMesh.triangles.Length / 3} triangles\n" +
                   $"Theoretical minimum: {theoreticalVertices} vertices, {theoreticalTriangles} triangles\n" +
                   $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles\n" +
                   $"Efficiency: {(float)optimizedMesh.vertexCount / theoreticalVertices * 100:F1}% of theoretical minimum";
        }
        
        [McpTool("Demo: Hollow sphere comparison")]
        public static string DemoHollowSphere([McpTool("Sphere radius")] int radius)
        {
            int size = radius * 2 + 1;
            var voxelData = new VoxelData(size, size, size);
            var center = new Vector3(radius, radius, radius);
            
            // Create a hollow sphere
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        var pos = new Vector3(x, y, z);
                        var distance = Vector3.Distance(pos, center);
                        
                        // Create shell with thickness of 1-2 voxels
                        if (distance >= radius - 1 && distance <= radius + 0.5f)
                        {
                            voxelData.SetVoxel(x, y, z, 1);
                        }
                    }
                }
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
            var triangleReduction = (1.0f - (float)optimizedMesh.triangles.Length / basicMesh.triangles.Length) * 100f;
            
            return $"Hollow Sphere (radius {radius}):\n" +
                   $"Basic: {basicMesh.vertexCount} vertices, {basicMesh.triangles.Length / 3} triangles\n" +
                   $"Optimized: {optimizedMesh.vertexCount} vertices, {optimizedMesh.triangles.Length / 3} triangles\n" +
                   $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles";
        }
        
        [McpTool("Demo: Checkerboard pattern comparison")]
        public static string DemoCheckerboard([McpTool("Board size")] int size)
        {
            var voxelData = new VoxelData(size, size, size);
            
            // Create 3D checkerboard pattern
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        if ((x + y + z) % 2 == 0)
                        {
                            voxelData.SetVoxel(x, y, z, 1);
                        }
                    }
                }
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
            var triangleReduction = (1.0f - (float)optimizedMesh.triangles.Length / basicMesh.triangles.Length) * 100f;
            
            // In checkerboard, every voxel is isolated, so optimization should be minimal
            return $"3D Checkerboard {size}³:\n" +
                   $"Basic: {basicMesh.vertexCount} vertices, {basicMesh.triangles.Length / 3} triangles\n" +
                   $"Optimized: {optimizedMesh.vertexCount} vertices, {optimizedMesh.triangles.Length / 3} triangles\n" +
                   $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles\n" +
                   $"Note: Minimal optimization expected due to isolated voxels";
        }
        
        [McpTool("Demo: Progressive size comparison")]
        public static string DemoProgressiveSizes()
        {
            var result = "Progressive Size Comparison (Solid Cubes):\n\n";
            
            for (int size = 2; size <= 8; size += 2)
            {
                var voxelData = new VoxelData(size, size, size);
                
                // Fill solid cube
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
                var optimizedMesh = voxelData.GenerateOptimizedMesh();
                
                var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
                
                result += $"{size}³: {basicMesh.vertexCount} → {optimizedMesh.vertexCount} vertices ({vertexReduction:F1}% reduction)\n";
            }
            
            return result;
        }
        
        [McpTool("Demo: Real-world terrain example")]
        public static string DemoTerrain([McpTool("Terrain size")] int size, [McpTool("Max height")] int maxHeight)
        {
            var voxelData = new VoxelData(size, maxHeight, size);
            
            // Generate simple terrain using sine waves
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    // Create height using sine function
                    float height = (Mathf.Sin(x * 0.3f) + Mathf.Sin(z * 0.2f) + 2) * maxHeight / 4;
                    int h = Mathf.RoundToInt(height);
                    
                    // Fill from bottom to height
                    for (int y = 0; y <= h && y < maxHeight; y++)
                    {
                        voxelData.SetVoxel(x, y, z, 1);
                    }
                }
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
            var triangleReduction = (1.0f - (float)optimizedMesh.triangles.Length / basicMesh.triangles.Length) * 100f;
            
            return $"Terrain ({size}×{maxHeight}×{size}):\n" +
                   $"Basic: {basicMesh.vertexCount} vertices, {basicMesh.triangles.Length / 3} triangles\n" +
                   $"Optimized: {optimizedMesh.vertexCount} vertices, {optimizedMesh.triangles.Length / 3} triangles\n" +
                   $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles\n" +
                   $"This demonstrates optimization on realistic terrain data.";
        }
    }
}