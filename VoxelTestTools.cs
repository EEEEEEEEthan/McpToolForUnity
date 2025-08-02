using UnityEngine;

namespace McpToolForUnity
{
    /// <summary>
    /// Test class to validate voxel mesh generation
    /// </summary>
    public static class VoxelTestTools
    {
        [McpTool("Test basic voxel functionality")]
        public static string TestBasicVoxelFunctionality()
        {
            try
            {
                // Test basic voxel creation
                var voxelData = new VoxelData(3, 3, 3);
                
                // Set some voxels
                voxelData.SetVoxel(0, 0, 0, 1);
                voxelData.SetVoxel(1, 1, 1, 1);
                voxelData.SetVoxel(2, 2, 2, 1);
                
                // Test getting voxels
                if (voxelData.GetVoxel(0, 0, 0) != 1) return "Failed: Voxel not set correctly";
                if (voxelData.GetVoxel(1, 0, 0) != 0) return "Failed: Empty voxel should be 0";
                
                // Test mesh generation
                var basicMesh = voxelData.GenerateBasicMesh();
                if (basicMesh == null) return "Failed: Basic mesh generation returned null";
                
                var optimizedMesh = voxelData.GenerateOptimizedMesh();
                if (optimizedMesh == null) return "Failed: Optimized mesh generation returned null";
                
                return $"Success: Basic mesh has {basicMesh.vertexCount} vertices, Optimized mesh has {optimizedMesh.vertexCount} vertices";
            }
            catch (System.Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
        
        [McpTool("Performance test for different voxel sizes")]
        public static string PerformanceTest([McpTool("Size to test")] int size)
        {
            try
            {
                var voxelData = new VoxelData(size, size, size);
                
                // Fill half the space with voxels in a pattern
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
                
                var startTime = System.DateTime.Now;
                var basicMesh = voxelData.GenerateBasicMesh();
                var basicTime = (System.DateTime.Now - startTime).TotalMilliseconds;
                
                startTime = System.DateTime.Now;
                var optimizedMesh = voxelData.GenerateOptimizedMesh();
                var optimizedTime = (System.DateTime.Now - startTime).TotalMilliseconds;
                
                var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
                var triangleReduction = (1.0f - (float)optimizedMesh.triangles.Length / basicMesh.triangles.Length) * 100f;
                
                return $"Size {size}x{size}x{size}:\n" +
                       $"Basic: {basicMesh.vertexCount} vertices, {basicMesh.triangles.Length / 3} triangles ({basicTime:F1}ms)\n" +
                       $"Optimized: {optimizedMesh.vertexCount} vertices, {optimizedMesh.triangles.Length / 3} triangles ({optimizedTime:F1}ms)\n" +
                       $"Reduction: {vertexReduction:F1}% vertices, {triangleReduction:F1}% triangles\n" +
                       $"Speed: {(basicTime/optimizedTime):F1}x slower for optimization";
            }
            catch (System.Exception e)
            {
                return $"Error: {e.Message}";
            }
        }
    }
}