using System.Collections.Generic;
using UnityEngine;

public class TrapManager : MonoBehaviour
{
    public static TrapManager Instance;

    [Header("--- CẤU HÌNH ---")]
    public bool useRandomSubset = false;
    public int activeTrapCount = 3;

    private readonly List<TrapBase> allTraps = new List<TrapBase>();
    private bool isGameRunning = false; // Thêm biến check game đang chạy

    private void Awake()
    {
        Instance = this;
    }

    // ====== ĐĂNG KÝ / HỦY ĐĂNG KÝ ======
    public void Register(TrapBase trap)
    {
        if (!allTraps.Contains(trap))
            allTraps.Add(trap);

        // BỎ chức năng ép isActive = false lúc Register.
        // Trước đây bẫy bị tắt ngay khi Register nếu game chưa "isGameRunning",
        // khiến bẫy đứng im vô thời hạn nếu ActivateTraps() không được gọi đúng lúc
        // (test scene trực tiếp, thứ tự gọi sai, v.v.). Giờ bẫy mặc định LUÔN active,
        // trừ khi bị tắt thủ công qua DeactivateTraps() hoặc SetActive(false).
    }

    public void Unregister(TrapBase trap)
    {
        allTraps.Remove(trap);
    }

    public bool IsRegistered(TrapBase trap) => allTraps.Contains(trap);

    // ====== GỌI TỪ MiniGame6.StartMiniGame() ======
    public void ActivateTraps()
    {
        isGameRunning = true;

        if (allTraps.Count == 0)
        {
           
            return;
        }

        if (!useRandomSubset)
        {
          

            foreach (var trap in allTraps)
                trap.SetActive(true);
            return;
        }

        // Random 1 tập con bẫy được bật
        List<TrapBase> pool = new List<TrapBase>(allTraps);
        int count = Mathf.Min(activeTrapCount, pool.Count);

        foreach (var trap in allTraps)
            trap.SetActive(false);

        List<string> chosenNames = new List<string>();

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            pool[idx].SetActive(true);
            chosenNames.Add(pool[idx].name);
            pool.RemoveAt(idx);
        }

        // FIX/DEBUG: log rõ bẫy nào được chọn bật ở round này, để không nhầm việc
        // "random không chọn trúng bẫy này" với việc "bẫy bị bug không kích hoạt".
        
    }

    // ====== GỌI TỪ MiniGame6.StopMiniGame() ======
    public void DeactivateTraps()
    {
        isGameRunning = false;


        foreach (var trap in allTraps)
            trap.SetActive(false);
    }

    public int TrapCount() => allTraps.Count;
}