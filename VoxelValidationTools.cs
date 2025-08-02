using UnityEngine;
using System.Text;

namespace McpToolForUnity
{
    /// <summary>
    /// Utility tools for validating and debugging voxel mesh generation
    /// </summary>
    public static class VoxelValidationTools
    {
        [McpTool("Validate mesh generation for a simple test case")]
        public static string ValidateMeshGeneration()
        {
            var result = new StringBuilder();
            
            try
            {
                // Test 1: Single voxel
                result.AppendLine("=== Test 1: Single Voxel ===");
                var singleVoxel = new VoxelData(1, 1, 1);
                singleVoxel.SetVoxel(0, 0, 0, 1);
                
                var basicMesh = singleVoxel.GenerateBasicMesh();
                var optimizedMesh = singleVoxel.GenerateOptimizedMesh();
                
                result.AppendLine($"Single voxel - Basic: {basicMesh.vertexCount} vertices, {basicMesh.triangles.Length / 3} triangles");
                result.AppendLine($"Single voxel - Optimized: {optimizedMesh.vertexCount} vertices, {optimizedMesh.triangles.Length / 3} triangles");
                
                // Single voxel should have 6 faces = 24 vertices, 12 triangles
                if (basicMesh.triangles.Length / 3 != 12)
                    result.AppendLine($"WARNING: Expected 12 triangles for single voxel, got {basicMesh.triangles.Length / 3}");
                
                // Test 2: 2x1x1 line of voxels
                result.AppendLine("\n=== Test 2: 2x1x1 Line ===");
                var line = new VoxelData(2, 1, 1);
                line.SetVoxel(0, 0, 0, 1);
                line.SetVoxel(1, 0, 0, 1);
                
                var lineBasic = line.GenerateBasicMesh();
                var lineOptimized = line.GenerateOptimizedMesh();
                
                result.AppendLine($"2x1x1 line - Basic: {lineBasic.vertexCount} vertices, {lineBasic.triangles.Length / 3} triangles");
                result.AppendLine($"2x1x1 line - Optimized: {lineOptimized.vertexCount} vertices, {lineOptimized.triangles.Length / 3} triangles");
                
                // Should have less triangles in optimized version due to merged faces
                if (lineOptimized.triangles.Length >= lineBasic.triangles.Length)
                    result.AppendLine("WARNING: Optimized mesh should have fewer triangles than basic for connected voxels");
                
                // Test 3: 2x2x2 cube
                result.AppendLine("\n=== Test 3: 2x2x2 Cube ===");
                var cube = new VoxelData(2, 2, 2);
                for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                        for (int z = 0; z < 2; z++)
                            cube.SetVoxel(x, y, z, 1);
                
                var cubeBasic = cube.GenerateBasicMesh();
                var cubeOptimized = cube.GenerateOptimizedMesh();
                
                result.AppendLine($"2x2x2 cube - Basic: {cubeBasic.vertexCount} vertices, {cubeBasic.triangles.Length / 3} triangles");
                result.AppendLine($"2x2x2 cube - Optimized: {cubeOptimized.vertexCount} vertices, {cubeOptimized.triangles.Length / 3} triangles");
                
                // For solid cube, optimized should be exactly 6 faces = 12 triangles
                if (cubeOptimized.triangles.Length / 3 != 12)
                    result.AppendLine($"WARNING: 2x2x2 solid cube should have exactly 12 triangles, got {cubeOptimized.triangles.Length / 3}");
                
                result.AppendLine("\n=== Validation Complete ===");
                result.AppendLine("✓ All tests completed");
                
            }
            catch (System.Exception e)
            {
                result.AppendLine($"\n❌ Error during validation: {e.Message}");
                result.AppendLine($"Stack trace: {e.StackTrace}");
            }
            
            return result.ToString();
        }
        
        [McpTool("Debug mesh details for a specific voxel configuration")]
        public static string DebugMeshDetails([McpTool("Configuration (1=single, 2=line, 3=cube)")] int config)
        {
            VoxelData voxelData;
            string configName;
            
            switch (config)
            {
                case 1:
                    voxelData = new VoxelData(1, 1, 1);
                    voxelData.SetVoxel(0, 0, 0, 1);
                    configName = "Single Voxel";
                    break;
                case 2:
                    voxelData = new VoxelData(3, 1, 1);
                    voxelData.SetVoxel(0, 0, 0, 1);
                    voxelData.SetVoxel(1, 0, 0, 1);
                    voxelData.SetVoxel(2, 0, 0, 1);
                    configName = "3x1x1 Line";
                    break;
                case 3:
                    voxelData = new VoxelData(2, 2, 2);
                    for (int x = 0; x < 2; x++)
                        for (int y = 0; y < 2; y++)
                            for (int z = 0; z < 2; z++)
                                voxelData.SetVoxel(x, y, z, 1);
                    configName = "2x2x2 Cube";
                    break;
                default:
                    return "Invalid configuration. Use 1=single, 2=line, 3=cube";
            }
            
            var basicMesh = voxelData.GenerateBasicMesh();
            var optimizedMesh = voxelData.GenerateOptimizedMesh();
            
            var result = new StringBuilder();
            result.AppendLine($"=== Debug Details for {configName} ===\n");
            
            // Basic mesh details
            result.AppendLine("Basic Mesh:");
            result.AppendLine($"  Vertices: {basicMesh.vertexCount}");
            result.AppendLine($"  Triangles: {basicMesh.triangles.Length / 3}");
            result.AppendLine($"  Bounds: {basicMesh.bounds}");
            
            // Optimized mesh details
            result.AppendLine("\nOptimized Mesh:");
            result.AppendLine($"  Vertices: {optimizedMesh.vertexCount}");
            result.AppendLine($"  Triangles: {optimizedMesh.triangles.Length / 3}");
            result.AppendLine($"  Bounds: {optimizedMesh.bounds}");
            
            // Comparison
            var vertexReduction = (1.0f - (float)optimizedMesh.vertexCount / basicMesh.vertexCount) * 100f;
            var triangleReduction = (1.0f - (float)optimizedMesh.triangles.Length / basicMesh.triangles.Length) * 100f;
            
            result.AppendLine("\nOptimization Results:");
            result.AppendLine($"  Vertex reduction: {vertexReduction:F1}%");
            result.AppendLine($"  Triangle reduction: {triangleReduction:F1}%");
            
            // First few vertices for debugging
            result.AppendLine("\nFirst 8 vertices (Basic):");
            for (int i = 0; i < Mathf.Min(8, basicMesh.vertices.Length); i++)
            {
                result.AppendLine($"  [{i}]: {basicMesh.vertices[i]}");
            }
            
            result.AppendLine("\nFirst 8 vertices (Optimized):");
            for (int i = 0; i < Mathf.Min(8, optimizedMesh.vertices.Length); i++)
            {
                result.AppendLine($"  [{i}]: {optimizedMesh.vertices[i]}");
            }
            
            return result.ToString();
        }
    }
}