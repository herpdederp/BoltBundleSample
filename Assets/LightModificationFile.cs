using UnityEngine;

[CreateAssetMenu(fileName = "LightModData", menuName = "VRH/LightModificationFile", order = 1)]
public class LightModificationFile : ScriptableObject
{
    public Material skyboxMat;
    [ColorUsage(true, true)]
    public Color skyColor;
    [ColorUsage(true, true)]
    public Color equatorColor;
    [ColorUsage(true, true)]
    public Color groundColor;
}