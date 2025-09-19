using UnityEngine;
using LDtkUnity;

namespace DeadCellsTestFramework.Tools
{
    /// <summary>
    /// 简单的LDtk集成测试脚本
    /// 用于验证LDtk Unity包是否正确安装和工作
    /// </summary>
    public class LDtkIntegrationTest : MonoBehaviour
    {
        [Header("LDtk Test")]
        [SerializeField] private bool runTestOnStart = false;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                TestLDtkIntegration();
            }
        }
        
        [ContextMenu("Test LDtk Integration")]
        public void TestLDtkIntegration()
        {
            Debug.Log("=== LDtk Integration Test ===");
            
            // Test 1: Check if LDtk types are available
            TestLDtkTypesAvailable();
            
            // Test 2: Look for LDtk components in scene
            TestLDtkComponentsInScene();
            
            Debug.Log("=== LDtk Integration Test Complete ===");
        }
        
        private void TestLDtkTypesAvailable()
        {
            Debug.Log("Testing LDtk types availability...");
            
            try
            {
                // Test if we can reference LDtk types
                System.Type projectType = typeof(LDtkComponentProject);
                System.Type levelType = typeof(LDtkComponentLevel);
                System.Type entityLayerType = typeof(LDtkComponentLayer);
                System.Type entityInstanceType = typeof(LDtkComponentEntity);
                
                Debug.Log($"✓ LDtkComponentProject type available: {projectType.Name}");
                Debug.Log($"✓ LDtkComponentLevel type available: {levelType.Name}");
                Debug.Log($"✓ LDtkComponentLayer type available: {entityLayerType.Name}");
                Debug.Log($"✓ LDtkComponentEntity type available: {entityInstanceType.Name}");
                
                Debug.Log("✓ All LDtk types are available!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ LDtk types not available: {e.Message}");
            }
        }
        
        private void TestLDtkComponentsInScene()
        {
            Debug.Log("Looking for LDtk components in scene...");
            
            // Look for LDtk project component
            LDtkComponentProject[] projects = FindObjectsOfType<LDtkComponentProject>();
            Debug.Log($"Found {projects.Length} LDtkComponentProject(s) in scene");
            
            // Look for LDtk level components
            LDtkComponentLevel[] levels = FindObjectsOfType<LDtkComponentLevel>();
            Debug.Log($"Found {levels.Length} LDtkComponentLevel(s) in scene");
            
            // Look for LDtk entity layers
            LDtkComponentLayer[] entityLayers = FindObjectsOfType<LDtkComponentLayer>();
            Debug.Log($"Found {entityLayers.Length} LDtkComponentLayer(s) in scene");
            
            // Look for LDtk entity instances
            LDtkComponentEntity[] entityInstances = FindObjectsOfType<LDtkComponentEntity>();
            Debug.Log($"Found {entityInstances.Length} LDtkComponentEntity(s) in scene");
            
            if (projects.Length > 0)
            {
                Debug.Log("✓ LDtk integration appears to be working - found LDtk components in scene!");
                
                foreach (var project in projects)
                {
                    Debug.Log($"  - Project: {project.name}");
                }
            }
            else
            {
                Debug.LogWarning("⚠ No LDtk project found in scene. You may need to:");
                Debug.LogWarning("  1. Import an LDtk project file into Unity");
                Debug.LogWarning("  2. Drag the imported project into your scene");
                Debug.LogWarning("  3. Assign the project to LDtkRoomManager.ldtkProject field");
            }
        }
        
        [ContextMenu("Show LDtk Package Info")]
        public void ShowLDtkPackageInfo()
        {
            Debug.Log("=== LDtk Package Information ===");
            Debug.Log("Package: com.cammin.ldtkunity");
            Debug.Log("LDtk Unity integration by Cammin");
            Debug.Log("Documentation: https://cammin.github.io/LDtkToUnity/");
            Debug.Log("GitHub: https://github.com/Cammin/LDtkToUnity");
        }
    }
}