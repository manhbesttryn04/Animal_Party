using TMPro;
using UnityEngine;

public class PriceItem : MonoBehaviour
{
    public int price; // Giá của item
    public TextMeshProUGUI priceText; // TextMeshProUGUI để hiển thị giá của item

    private void Start()
    {
        priceText = gameObject.transform.GetChild(3).transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        if (priceText != null)
        {
            priceText.text = price.ToString();
        }
    }
}
