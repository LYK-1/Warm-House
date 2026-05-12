using UnityEngine;
using UnityEngine.UI;

public class Maps : MonoBehaviour
{
    public RawImage MapImage;
    public RenderTexture[] MapTextures;

    void Start()
    {
        for (int i = 0; i < MapTextures.Length; i++)
        {
            if (SaveScript.TrackToPlay - 2 == i)
            {
                MapImage.texture = MapTextures[SaveScript.TrackToPlay - 2];
            }
        }
    }
}
