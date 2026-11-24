using System.Collections.Generic;
using UnityEngine;

// Network ID <-> GameObject, one per PC.
public class NetLinkingContext
{
    readonly Dictionary<int, GameObject> m_objects = new Dictionary<int, GameObject>();
    int m_nextId = 1;                       // 0 = no ID

    public int RegisterNew(GameObject go)
    {
        int id = m_nextId++;
        m_objects[id] = go;
        return id;
    }

    public void Register(GameObject go, int id) { m_objects[id] = go; }

    public GameObject Find(int id)
    {
        GameObject go;
        return m_objects.TryGetValue(id, out go) ? go : null;
    }

    public void Unregister(int id) { m_objects.Remove(id); }

    // The counter is not reset: an old packet never points to a new object.
    public void Clear() { m_objects.Clear(); }

    public int Count { get { return m_objects.Count; } }
    public List<int> IdsSnapshot() { return new List<int>(m_objects.Keys); }
}
