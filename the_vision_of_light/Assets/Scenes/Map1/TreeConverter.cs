using UnityEngine;

public class TreeConverter : MonoBehaviour
{
    [Header("Settings")]
    public Terrain terrain;
    public Transform parentObject;

    [ContextMenu("Convert Trees to GameObjects")]
    public void ConvertTrees()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("No Terrain found!");
            return;
        }

        TerrainData data = terrain.terrainData;
        TreeInstance[] treeInstances = data.treeInstances;
        TreePrototype[] treePrototypes = data.treePrototypes;

        if (parentObject == null)
        {
            GameObject newParent = new GameObject("Converted Trees (Solid)");
            parentObject = newParent.transform;
        }

        int count = 0;

        foreach (TreeInstance tree in treeInstances)
        {
            GameObject prefab = treePrototypes[tree.prototypeIndex].prefab;

            float worldX = terrain.transform.position.x + (tree.position.x * data.size.x);
            float worldZ = terrain.transform.position.z + (tree.position.z * data.size.z);

            Vector3 tempPos = new Vector3(worldX, 0, worldZ);
            float groundY = terrain.SampleHeight(tempPos) + terrain.transform.position.y;

            Vector3 finalWorldPos = new Vector3(worldX, groundY, worldZ);

            GameObject newTree = Instantiate(prefab, finalWorldPos, Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0), parentObject);
            
            newTree.transform.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            
            count++;
        }

        Debug.Log("Done! Successfully converted " + count + " trees and snapped them to the ground.");
    }
}