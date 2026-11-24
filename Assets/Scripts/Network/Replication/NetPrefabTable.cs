using System.Collections.Generic;
using UnityEngine;

// Prefabs the client can create. The index is the 'type' byte. Saved as Assets/Resources/NetPrefabs.asset.
// Index 0 = mass cube.
[CreateAssetMenu(menuName = "Odossea/Net Prefab Table", fileName = "NetPrefabs")]
public class NetPrefabTable : ScriptableObject
{
    public List<GameObject> prefabs = new List<GameObject>();

    static NetPrefabTable s_cached;

    public static NetPrefabTable Load()
    {
        if (s_cached == null) s_cached = Resources.Load<NetPrefabTable>("NetPrefabs");
        if (s_cached == null) Debug.LogError("[Replication] Falta Assets/Resources/NetPrefabs.asset");
        return s_cached;
    }

    public GameObject Get(byte type)
    {
        return type < prefabs.Count ? prefabs[type] : null;
    }
}
