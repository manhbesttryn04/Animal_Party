using UnityEngine;
using UnityEngine.UI;

public class RandomCard : MonoBehaviour
{
    public int itemIndex = -1;
    public Image image;
    public Sprite spriteStar;

    public void ResetCard()
    {
        itemIndex = -1;
        image.sprite = spriteStar;
    }
}