using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider slider;
    [SerializeField] private float loadingTime = 2f;

    private void Awake()
    {
        SetupSingleton();
        SetupUI();
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // giữ lại khi đổi scene
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupUI()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);

        if (slider != null)
            slider.value = 0f;
    }

    public IEnumerator ShowLoading()
    {
        if (loadingPanel == null || slider == null)
        {
            yield break;
        }

        loadingPanel.SetActive(true);
        slider.value = 0f;

        float t = 0f;

        while (t < loadingTime)
        {
            // Vẫn chạy kể cả khi Time.timeScale = 0
            t += Time.unscaledDeltaTime;

            slider.value = Mathf.Clamp01(t / loadingTime);

            yield return null;
        }

        slider.value = 1f;

        // Vẫn chờ được khi game đang Pause
        yield return new WaitForSecondsRealtime(1f);
    }

    public void HideLoading()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
}