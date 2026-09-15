using UnityEngine;
using TMPro;

public class AutoResizeBubble : MonoBehaviour
{
    [Header("Kéo Background và ChatText vào đây")]
    public RectTransform background;
    public TextMeshProUGUI chatText;

    [Header("Kích thước tối thiểu / tối đa của khung")]
    public float minWidth = 150f;
    public float maxWidth = 400f;
    public float minHeight = 80f;
    public float padding = 30f;

    public void ResizeToFitText(string text)
    {
        if (chatText == null || background == null) return;

        chatText.text = text;
        chatText.ForceMeshUpdate();

        Vector2 preferredSize = chatText.GetPreferredValues(text, maxWidth - padding, 0);

        float finalWidth = Mathf.Clamp(preferredSize.x + padding, minWidth, maxWidth);
        float finalHeight = Mathf.Max(preferredSize.y + padding, minHeight);

        background.sizeDelta = new Vector2(finalWidth, finalHeight);
    }
}