using UnityEngine;

// The number of a mass cube that was shot, the same on both PCs, so one PC can tell the other
// "this cube is gone". Added by code by NetCube: it is not in the prefab.
public class NetCubeId : MonoBehaviour
{
    public uint id;
}
