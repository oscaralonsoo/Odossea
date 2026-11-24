using System.Collections.Generic;
using UnityEngine;

// The list of prefabs the host can create on the client. The index in the list is the 'type'
// byte of the packet, so the list is the same on both PCs (it is one asset in the build).
//
// Create it with: Project > right click > Create > Odossea > Net Prefab Table
// It MUST be saved as  Assets/Resources/NetPrefabs.asset
//   index 0 = the mass cube
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
