using UnityEngine;
using TMPro;
using System.Collections;

public class MathManager : MonoBehaviour
{
    [Header("UI Màn Hình Chính")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI timerText;

    [Header("UI Thông Báo Riêng Biệt")]
    public TextMeshProUGUI p1StatusText;
    public TextMeshProUGUI p2StatusText;

    [Header("UI Đáp Án Dưới Mặt Đất")]
    public TextMeshProUGUI[] answerTexts;

    [Header("Vách Ngăn Khóa Player")]
    [Tooltip("Kéo thả 4 GameObject vách ngăn quanh 4 ô vào đây.")]
    public GameObject[] answerWalls;

    [Header("Hiệu Ứng Trừng Phạt")]
    public GameObject explosionPrefab;
    public float knockbackForce = 15f;
    public float knockbackDuration = 0.4f;

    [Header("Cấu hình Thời gian")]
    public float selectionDuration = 6f;
    public float lockThreshold = 2f;
    public float resultViewDuration = 4f;

    private int correctAnswer;
    private int correctPadIndex;

    private int p1Choice = -1;
    private int p2Choice = -1;
    private bool isAnsweringState = false;
    private bool isLockedState = false;
    private bool hasPunished = false; // Cờ bảo hiểm chống nổ trùng lặp 2 lần

    private GameObject player1Obj;
    private GameObject player2Obj;

    void Start()
    {
        if (questionText == null || timerText == null || p1StatusText == null || p2StatusText == null || answerTexts.Length < 4)
        {
            Debug.LogError("Vui lòng kéo đầy đủ các thành phần UI vào MathManager trong Inspector!");
            return;
        }

        // Tìm kiếm Player dựa trên tên chính xác trong hệ thống
        player1Obj = GameObject.Find("Player Play 1");
        player2Obj = GameObject.Find("Player Play 2");

        UnlockAllWalls();
        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            // ---- BƯỚC 1: KHỞI TẠO LƯỢT CHƠI MỚI ----
            UnlockAllWalls();
            GenerateQuestion();
            isAnsweringState = true;
            isLockedState = false;
            hasPunished = false; // Reset cờ nổ
            p1Choice = -1;
            p2Choice = -1;

            p1StatusText.text = "Hãy chọn đáp án!";
            p2StatusText.text = "Hãy chọn đáp án!";

            // ---- BƯỚC 2: ĐẾM NGƯỢC 6 GIÂY ----
            float timeLeft = selectionDuration;
            while (timeLeft > 0)
            {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString();

                // Kiểm tra điều kiện chạm mốc thời gian khóa (2 giây cuối)
                if (timeLeft <= lockThreshold && !isLockedState)
                {
                    isLockedState = true; // Gán cờ khóa ngay lập tức
                    LockExistingPlayers(); // Kích hoạt vách ngăn cho những người đã đứng sẵn trong ô
                }

                yield return new WaitForSeconds(0.1f);
                timeLeft -= 0.1f;
            }

            // ---- BƯỚC 3: HẾT GIỜ -> TÍNH TOÁN KẾT QUẢ & KÍCH NỔ ----
            isAnsweringState = false;
            UnlockAllWalls(); // Hạ các vách ngăn xuống để thực hiện hiệu ứng đẩy lùi (Knockback)

            // Chỉ chạy tính toán kết quả nếu chưa từng thực hiện kích nổ cho lượt này
            if (!hasPunished)
            {
                hasPunished = true;
                CheckFinalResults();
            }

            // ---- BƯỚC 4: XEM KẾT QUẢ VÀ CHỜ ĐỔI LƯỢT ----
            float waitTimeLeft = resultViewDuration;
            while (waitTimeLeft > 0)
            {
                timerText.text = Mathf.CeilToInt(waitTimeLeft).ToString();
                yield return new WaitForSeconds(1.0f);
                waitTimeLeft -= 1f;
            }
        }
    }

    void GenerateQuestion()
    {
        int num1 = Random.Range(1, 21);
        int num2 = Random.Range(1, 21);
        int operation = Random.Range(0, 4);
        string opSymbol = "";

        switch (operation)
        {
            case 0: opSymbol = "+"; correctAnswer = num1 + num2; break;
            case 1: opSymbol = "-"; correctAnswer = num1 - num2; break;
            case 2:
                num1 = Random.Range(1, 10); num2 = Random.Range(1, 10);
                opSymbol = "x"; correctAnswer = num1 * num2;
                break;
            case 3:
                int k = Random.Range(1, 10); num2 = Random.Range(1, 10);
                num1 = num2 * k; opSymbol = "/"; correctAnswer = num1 / num2;
                break;
        }

        questionText.text = num1 + " " + opSymbol + " " + num2 + " = ?";
        correctPadIndex = Random.Range(0, 4);

        for (int i = 0; i < 4; i++)
        {
            answerTexts[i].color = Color.white;
            if (i == correctPadIndex) answerTexts[i].text = correctAnswer.ToString();
            else
            {
                int wrongAnswer = correctAnswer + Random.Range(-5, 6);
                if (wrongAnswer == correctAnswer) wrongAnswer += 1;
                answerTexts[i].text = wrongAnswer.ToString();
            }
        }
    }

    // NÂNG CẤP TOÀN DIỆN: Hàm nhận diện dựa trực tiếp vào thực thể GameObject dậm lên ô
    public void OnPlayerStepOnPad(GameObject playerObj, int padIndex)
    {
        if (!isAnsweringState || playerObj == null) return;

        // Tự động phân tích danh tính Player dựa trên thực thể GameObject truyền vào
        bool isP1 = (playerObj == player1Obj || playerObj.name == "Player Play 1");
        bool isP2 = (playerObj == player2Obj || playerObj.name == "Player Play 2");

        if (isP1)
        {
            // Nếu đã bị khóa tường và đã có lựa chọn từ trước, không cho phép đổi sang ô khác
            if (isLockedState && p1Choice != -1 && p1Choice != padIndex) return;

            p1Choice = padIndex;

            if (isLockedState)
            {
                p1StatusText.text = "P1: ĐÃ KHÓA TRONG Ô " + (padIndex + 1) + "!";
                ActivateWall(padIndex); // Nếu chọn muộn ở 2s cuối, dựng tường bao quanh ngay lập tức
            }
            else
            {
                p1StatusText.text = "P1 đang chọn ô: " + (padIndex + 1);
            }
        }
        else if (isP2)
        {
            if (isLockedState && p2Choice != -1 && p2Choice != padIndex) return;

            p2Choice = padIndex;

            if (isLockedState)
            {
                p2StatusText.text = "P2: ĐÃ KHÓA TRONG Ô " + (padIndex + 1) + "!";
                ActivateWall(padIndex);
            }
            else
            {
                p2StatusText.text = "P2 đang chọn ô: " + (padIndex + 1);
            }
        }
    }

    // Cơ chế kích hoạt vách ngăn bao quanh ô chọn tại thời điểm chạm mốc khóa
    void LockExistingPlayers()
    {
        if (p1Choice != -1)
        {
            p1StatusText.text = "P1: ĐÃ KHÓA TRONG Ô " + (p1Choice + 1) + "!";
            ActivateWall(p1Choice);
        }
        else
        {
            p1StatusText.text = "P1: Chưa chọn - Hãy mau dậm ô!";
        }

        if (p2Choice != -1)
        {
            p2StatusText.text = "P2: ĐÃ KHÓA TRONG Ô " + (p2Choice + 1) + "!";
            ActivateWall(p2Choice);
        }
        else
        {
            p2StatusText.text = "P2: Chưa chọn - Hãy mau dậm ô!";
        }

        Debug.Log("Hệ thống: Đã chốt vị trí hiện tại và kích hoạt vách ngăn cố định.");
    }

    void ActivateWall(int index)
    {
        if (answerWalls != null && index >= 0 && index < answerWalls.Length && answerWalls[index] != null)
        {
            answerWalls[index].SetActive(true);
        }
    }

    void UnlockAllWalls()
    {
        if (answerWalls == null) return;
        for (int i = 0; i < answerWalls.Length; i++)
        {
            if (answerWalls[i] != null) answerWalls[i].SetActive(false);
        }
    }

    void CheckFinalResults()
    {
        // 1. Kiểm tra kết quả Player 1
        if (p1Choice == -1)
        {
            p1StatusText.text = "P1: Không trả lời!";
            ExecutePunishment(player1Obj, Vector3.back);
        }
        else if (p1Choice == correctPadIndex)
        {
            p1StatusText.text = "P1: CHÍNH XÁC!";
        }
        else
        {
            p1StatusText.text = "P1: SAI RỒI!";
            ExecutePunishment(player1Obj, GetKnockbackDirection(p1Choice, player1Obj));
        }

        // 2. Kiểm tra kết quả Player 2
        if (p2Choice == -1)
        {
            p2StatusText.text = "P2: Không trả lời!";
            ExecutePunishment(player2Obj, Vector3.back);
        }
        else if (p2Choice == correctPadIndex)
        {
            p2StatusText.text = "P2: CHÍNH XÁC!";
        }
        else
        {
            p2StatusText.text = "P2: SAI RỒI!";
            ExecutePunishment(player2Obj, GetKnockbackDirection(p2Choice, player2Obj));
        }

        // Đổi màu UI chữ dưới nền đất
        for (int i = 0; i < 4; i++)
        {
            if (i == correctPadIndex) answerTexts[i].color = Color.yellow;
            else if (i == p1Choice || i == p2Choice) answerTexts[i].color = Color.red;
        }

        questionText.text = "Đáp án đúng là: " + correctAnswer;
    }

    Vector3 GetKnockbackDirection(int padIndex, GameObject player)
    {
        if (player == null || answerTexts == null || padIndex >= answerTexts.Length || answerTexts[padIndex] == null)
            return Vector3.up;

        Vector3 padPosition = answerTexts[padIndex].transform.position;
        Vector3 pushDir = player.transform.position - padPosition;
        pushDir.y = 0;

        if (pushDir == Vector3.zero) pushDir = Vector3.forward;
        return pushDir.normalized;
    }

    void ExecutePunishment(GameObject player, Vector3 direction)
    {
        if (player == null) return;

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        StartCoroutine(KnockbackRoutine(player, direction));
    }

    IEnumerator KnockbackRoutine(GameObject player, Vector3 direction)
    {
        if (player == null) yield break;

        CharacterController cc = player.GetComponent<CharacterController>();
        float elapsed = 0f;
        Vector3 finalDirection = (direction + Vector3.up * 0.5f).normalized;

        while (elapsed < knockbackDuration && player != null)
        {
            Vector3 moveAmount = finalDirection * knockbackForce * Time.deltaTime;

            if (cc != null && cc.enabled)
            {
                cc.Move(moveAmount);
            }
            else
            {
                player.transform.position += moveAmount;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}