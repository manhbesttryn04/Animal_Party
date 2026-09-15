using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFollowMaterialColor : MonoBehaviour
{
    public Material targetMaterial;

    [Range(0f, 1f)]
    public float colorStrength = 0.7f;

    public Light myLight;

    private void Update()
    {
        if (targetMaterial == null) return;

        Color matColor = Color.white;

        if (targetMaterial.HasProperty("_EmissionColor"))
        {
            matColor = targetMaterial.GetColor("_EmissionColor");
        }
        else if (targetMaterial.HasProperty("_BaseColor"))
        {
            matColor = targetMaterial.GetColor("_BaseColor");
        }
        else if (targetMaterial.HasProperty("_Color"))
        {
            matColor = targetMaterial.GetColor("_Color");
        }

        myLight.color = Color.Lerp(Color.white, matColor, colorStrength);
    }
}