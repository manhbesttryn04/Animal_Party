using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public abstract class TrapBase : MonoBehaviour
{
    [Header("--- TRAP CHUNG ---")]
    [Tooltip("Thời gian hồi chung của bẫy. Kích hoạt xong thì trong khoảng thời gian này KHÔNG AI bị dẫm nữa.")]
    public float cooldown = 3f;

    protected bool isActive = true;

    // Cooldown chung cho toàn bộ bẫy (không phân biệt P1/P2)
    private float lastTriggerTime = -999f;

    // FIX: đổi từ HashSet sang Dictionary lưu thời điểm vào bẫy.
    // NGUYÊN NHÂN "lúc kích hoạt được lúc không": OnTriggerExit đôi khi KHÔNG fire
    // (player bị launch/pull/teleport ra khỏi vùng bẫy quá nhanh, hoặc CharacterController/
    // collider bị tắt-mở giữa lúc đang ở trong trigger). Khi đó carrier bị "kẹt" mãi trong
    // insideTrap, khiến bẫy đó không bao giờ kích hoạt lại cho player đó nữa.
    // -> Thêm safetyStuckTimeout: nếu quá lâu không thấy Exit, tự coi như đã rời và cho phép
    // trigger lại, kèm log cảnh báo để biết khi nào việc này xảy ra.
    private readonly Dictionary<BombCarrier, float> insideTrap = new Dictionary<BombCarrier, float>();

    [Tooltip("Thời gian tối đa 1 player được coi là 'còn trong bẫy' nếu không thấy OnTriggerExit. Đề phòng trường hợp Exit bị miss do bị launch/pull/teleport ra ngoài quá nhanh.")]
    public float stuckSafetyTimeout = 5f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    protected virtual void Awake()
    {
        if (TrapManager.Instance == null)
        {
            //Debug.LogWarning($"[TRAP DEBUG] {name}: TrapManager.Instance là NULL lúc Awake — sẽ thử Register lại ở Start().");
            return;
        }

        //Debug.Log($"[TRAP DEBUG] {name}: Đã Register với TrapManager (Awake). isActive hiện tại = {isActive}.");
        TrapManager.Instance.Register(this);
    }

    protected virtual void Start()
    {
        // FIX: an toàn khi Script Execution Order khiến TrapBase.Awake() chạy TRƯỚC
        // TrapManager.Awake() (Instance chưa set). Start() luôn chạy SAU Awake() của
        // TẤT CẢ script trong scene, nên tới đây TrapManager.Instance chắc chắn đã có.
        if (TrapManager.Instance != null && !TrapManager.Instance.IsRegistered(this))
        {
           // Debug.Log($"[TRAP DEBUG] {name}: Register lại thành công ở Start() (Awake trước đó bị miss vì Instance null).");
            TrapManager.Instance.Register(this);
        }
    }

    protected virtual void OnDestroy()
    {
        TrapManager.Instance?.Unregister(this);
    }

    private void OnTriggerEnter(Collider other)
    {
      //  Debug.Log($"[TRAP DEBUG] {name}: OnTriggerEnter với '{other.name}' (tag: {other.tag}).");
        CheckAndTrigger(other);
    }

    // FIX: bỏ OnTriggerStay để tránh trigger liên tục mỗi frame khi player đứng yên
    // trong vùng bẫy (ví dụ bị đóng băng ngay trên bẫy -> hết cooldown -> bị trigger lại
    // ngay lập tức mà không cần rời khỏi trigger).

    private void OnTriggerExit(Collider other)
    {
        BombCarrier carrier = GetCarrier(other);
        if (carrier != null && insideTrap.ContainsKey(carrier))
        {
          //  Debug.Log($"[TRAP DEBUG] {name}: OnTriggerExit — '{carrier.name}' đã rời khỏi bẫy, có thể trigger lại (sau cooldown).");
            insideTrap.Remove(carrier);
        }
    }

    private BombCarrier GetCarrier(Collider other)
    {
        BombCarrier carrier = other.GetComponent<BombCarrier>();
        if (carrier == null) carrier = other.GetComponentInParent<BombCarrier>();
        return carrier;
    }

    private void CheckAndTrigger(Collider other)
    {
        if (!isActive)
        {
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG kích hoạt vì isActive = false.");
            return;
        }

        // 1. Kiểm tra Cooldown chung (Nếu bẫy vừa bị dẫm cách đây chưa đủ 'cooldown' giây -> Bỏ qua mọi Player khác)
        if (Time.time - lastTriggerTime < cooldown)
        {
            float remaining = cooldown - (Time.time - lastTriggerTime);
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG kích hoạt vì đang cooldown, còn {remaining:F2}s.");
            return;
        }

        // 2. Tìm BombCarrier
        BombCarrier carrier = GetCarrier(other);

        if (carrier == null)
        {
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG tìm thấy BombCarrier trên '{other.name}' (đã thử GetComponent + GetComponentInParent).");
            return;
        }

        if (!carrier.IsGameActive())
        {
            //Debug.Log($"[TRAP DEBUG] {name}: KHÔNG kích hoạt vì '{carrier.name}' chưa IsGameActive (game chưa bắt đầu / đã StopMiniGame).");
            return;
        }

        if (carrier.IsEliminated())
        {
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG kích hoạt vì '{carrier.name}' đã bị loại (IsEliminated).");
            return;
        }

        // 3. Check Tag
        string targetTag = carrier.gameObject.tag;
        if (targetTag != "Player 1" && targetTag != "Player 2")
        {
           // Debug.Log($"[TRAP DEBUG] {name}: KHÔNG kích hoạt vì tag của '{carrier.name}' là '{targetTag}', không phải 'Player 1' hoặc 'Player 2'.");
            return;
        }

        // 4. Nếu player này đang được ghi nhận là "còn ở trong bẫy" (chưa OnTriggerExit)
        // thì không trigger lại — TRỪ khi đã quá lâu (stuckSafetyTimeout) mà vẫn chưa thấy
        // Exit, lúc đó coi như Exit đã bị miss và tự động cho phép trigger lại.
        if (insideTrap.TryGetValue(carrier, out float enterTime))
        {
            float elapsed = Time.time - enterTime;

            if (elapsed < stuckSafetyTimeout)
            {
              //  Debug.Log($"[TRAP DEBUG] {name}: KHÔNG kích hoạt vì '{carrier.name}' vẫn đang được ghi nhận ở trong bẫy (đã {elapsed:F2}s, chưa quá {stuckSafetyTimeout}s).");
                return;
            }

            //Debug.LogWarning($"[TRAP DEBUG] {name}: '{carrier.name}' bị 'kẹt' trong insideTrap quá {stuckSafetyTimeout}s mà không thấy OnTriggerExit — có thể do bị launch/pull/teleport ra ngoài. Tự động cho phép trigger lại.");
        }

        // 5. Đánh dấu thời gian dẫm bẫy MỚI NHẤT + đánh dấu player đang ở trong bẫy
        lastTriggerTime = Time.time;
        insideTrap[carrier] = Time.time;

        //Debug.Log($"[TRAP DEBUG] {name}: KÍCH HOẠT bẫy lên '{carrier.name}'.");

        // 6. Thực thi logic bẫy
        OnPlayerHit(carrier);
    }

    public void SetActive(bool active)
    {
       // Debug.Log($"[TRAP DEBUG] {name}: SetActive({active}) được gọi.");

        isActive = active;

        // Khi tắt bẫy, xóa luôn trạng thái "đang ở trong bẫy" để tránh giữ state cũ
        // khi bẫy được bật lại ở round sau.
        if (!active)
        {
            insideTrap.Clear();
        }
    }

    public bool IsActive() => isActive;

    protected abstract void OnPlayerHit(BombCarrier carrier);
}