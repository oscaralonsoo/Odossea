using System.Collections.Generic;
using UnityEngine;

// Network ID <-> GameObject, one per PC (Lab 6 linking context).
//   host:    int id = linking.RegisterNew(go)       a new ID, never reused
//            linking.Register(go, id)               an ID decided by the code (objects of the scene)
//   client:  linking.Register(go, id)               the ID the host sent
//   both:    Find(id) -> null if unknown, never throws
public class NetLinkingContext
{
    readonly Dictionary<int, GameObject> m_objects = new Dictionary<int, GameObject>();
    int m_nextId = 1;                       // 0 means "no ID"

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

    // A new level: every object of the old one is gone. The counter is NOT reset, so an old
    // packet can never point to a new object.
    public void Clear() { m_objects.Clear(); }

    public int Count { get { return m_objects.Count; } }
    public List<int> IdsSnapshot() { return new List<int>(m_objects.Keys); }
}
