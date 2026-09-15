using System.Collections.Generic;
using UnityEngine;

public class PointCheck : MonoBehaviour
{
    public static PointCheck Instance;

    [Header("Board Points")]
    public List<GameObject> point = new List<GameObject>();

    [Header("Trap Objects")]
    public List<GameObject> traps = new List<GameObject>();

    private void Awake()
    {
        SetupSingleton();
        SetUpBoard();
    }
    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
   private void SetUpBoard()
    {

        LoadPoints();
        FindAllTraps();
    }

    //=========================================================
    // POINT
    //=========================================================

    /// <summary>
    /// Tự động lấy Point từ Point 1 -> Point 33.
    /// </summary>
    [ContextMenu("Load Points")]
    public void LoadPoints()
    {
        point.Clear();

        for (int i = 1; i <= 33; i++)
        {
            GameObject obj = GameObject.Find($"Point {i}");

            if (obj != null)
            {
                point.Add(obj);
            }
            else
            {
              //  Debug.LogWarning($"Không tìm thấy Point {i}");
            }
        }

      //  Debug.Log($"Đã load {point.Count} Point.");
    }

    //=========================================================
    // TRAP
    //=========================================================

    /// <summary>
    /// Tìm tất cả GameObject có tag "Trap".
    /// </summary>
    [ContextMenu("Find All Traps")]
    public void FindAllTraps()
    {
        traps.Clear();

        GameObject[] allTrap = GameObject.FindGameObjectsWithTag("Trap");

        traps.AddRange(allTrap);

       // Debug.Log($"Đã tìm thấy {traps.Count} Trap.");
    }

    /// <summary>
    /// Ẩn toàn bộ Trap.
    /// </summary>
    [ContextMenu("Hide All Traps")]
    public void HideAllTraps()
    {
        foreach (GameObject trap in traps)
        {
            if (trap != null)
            {
                trap.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Hiện toàn bộ Trap.
    /// </summary>
    [ContextMenu("Show All Traps")]
    public void ShowAllTraps()
    {
        foreach (GameObject trap in traps)
        {
            if (trap != null)
            {
                trap.SetActive(true);
            }
        }
    }
}