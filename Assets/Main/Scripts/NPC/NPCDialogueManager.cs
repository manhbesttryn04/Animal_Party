using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NPCDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class ConversationGroup
    {
        [Header("Tên cụm (chỉ để dễ nhìn trong Inspector)")]
        public string groupName = "Group 1";

        [Header("2 NPC")]
        public Transform npcA;
        public Transform npcB;

        [Header("Lời thoại")]
        public string[] linesA;
        public string[] linesB;

        [Header("Timing riêng cho cụm này")]
        public float delayBetweenTalks = 6f;
        public float bubbleDuration = 2.5f;
        public float gapBetweenLines = 1f;

        [Header("Offset bong bóng chat - NPC A (mét thật)")]
        public Vector3 bubbleOffsetA = new Vector3(0, 0.8f, 0);

        [Header("Offset bong bóng chat - NPC B (mét thật)")]
        public Vector3 bubbleOffsetB = new Vector3(0, 0.8f, 0);

        [Header("Offset dấu ... - NPC A (mét thật)")]
        public Vector3 typingOffsetA = new Vector3(0, 0.8f, 0);

        [Header("Offset dấu ... - NPC B (mét thật)")]
        public Vector3 typingOffsetB = new Vector3(0, 0.8f, 0);

        [Header("Random giờ bắt đầu (tránh mọi cụm nói cùng lúc)")]
        public float startDelayMin = 0f;
        public float startDelayMax = 2f;

        [HideInInspector] public bool isRunning = false;
        [HideInInspector] public int lastIndexA = -1;
        [HideInInspector] public int lastIndexB = -1;

        // Cho phép chạy hay không dựa theo khoảng cách camera
        [HideInInspector] public bool allowedByDistance = true;
    }

    [System.Serializable]
    public class SoloNPC
    {
        [Header("Tên (chỉ để dễ nhìn)")]
        public string npcName = "Solo NPC";

        [Header("NPC nói một mình")]
        public Transform npc;

        [Header("Lời độc thoại")]
        public string[] lines;

        [Header("Timing")]
        public float delayBetweenTalks = 6f;
        public float bubbleDuration = 2.5f;

        [Header("Offset bong bóng chat (mét thật)")]
        public Vector3 bubbleOffset = new Vector3(0, 0.8f, 0);

        [Header("Offset dấu ... (mét thật)")]
        public Vector3 typingOffset = new Vector3(0, 0.8f, 0);

        [Header("Random giờ bắt đầu")]
        public float startDelayMin = 0f;
        public float startDelayMax = 2f;

        [HideInInspector] public bool isRunning = false;
        [HideInInspector] public int lastIndex = -1;

        [HideInInspector] public bool allowedByDistance = true;
    }

    [Header("Bubble Prefab dùng chung cho tất cả cụm")]
    public GameObject bubblePrefab;

    [Header("Scale cố định cho bubble chat")]
    public float bubbleFixedScale = 0.02f;

    [Header("Typing Indicator Prefab (dấu 3 chấm trước khi nói)")]
    public GameObject typingIndicatorPrefab;

    [Header("Scale cố định cho typing indicator (...)")]
    public float typingFixedScale = 0.02f;

    [Header("Fallback nếu prefab typing không có TypingDotsAnimation")]
    public float typingDurationFallback = 0.9f;

    [Header("--- Giới hạn theo khoảng cách Camera ---")]
    [Tooltip("Bật để chỉ chạy dialogue cho NPC gần camera")]
    public bool useDistanceLimit = true;

    [Tooltip("Bán kính (mét) tính từ camera - NPC trong phạm vi này mới chạy dialogue")]
    public float maxDistanceFromCamera = 20f;

    [Tooltip("Tần suất kiểm tra khoảng cách (giây) - không cần check mỗi frame")]
    public float distanceCheckInterval = 0.5f;

    Camera cachedCamera;

    [Header("Danh sách các cụm NPC nói chuyện (2 NPC/cụm)")]
    public List<ConversationGroup> groups = new List<ConversationGroup>();

    [Header("Danh sách NPC nói một mình (không cần cặp)")]
    public List<SoloNPC> soloNpcs = new List<SoloNPC>();

    List<GameObject> activeBubbles = new List<GameObject>();
    static NPCDialogueManager instance;

    void Awake()
    {
        SetupSingleton();
    }

    private void SetupSingleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        SetupCamera();
        SetupGroups();
        SetupSoloNPCs();
        SetupDistanceCheck();
    }

    private void SetupCamera()
    {
        cachedCamera = Camera.main;
    }

    private void SetupGroups()
    {
        foreach (var group in groups)
        {
            if (group.npcA == null || group.npcB == null) continue;
            FaceEachOther(group);
            StartCoroutine(RunGroup(group));
        }
    }

    private void SetupSoloNPCs()
    {
        foreach (var solo in soloNpcs)
        {
            if (solo.npc == null) continue;
            StartCoroutine(RunSolo(solo));
        }
    }

    private void SetupDistanceCheck()
    {
        if (useDistanceLimit)
            StartCoroutine(DistanceCheckLoop());
    }

    void OnEnable()
    {
        foreach (var group in groups)
        {
            group.isRunning = false;
            group.lastIndexA = -1;
            group.lastIndexB = -1;
            group.allowedByDistance = true;
        }
        foreach (var solo in soloNpcs)
        {
            solo.isRunning = false;
            solo.lastIndex = -1;
            solo.allowedByDistance = true;
        }
        activeBubbles.Clear();
    }

   
    void OnDisable()
    {
        StopAllCoroutines();
        CleanupAllBubbles();

        foreach (var group in groups)
            group.isRunning = false;
        foreach (var solo in soloNpcs)
            solo.isRunning = false;
    }

    void OnDestroy()
    {
        CleanupAllBubbles();
        if (instance == this)
            instance = null;
    }

    void CleanupAllBubbles()
    {
        foreach (var b in activeBubbles)
        {
            if (b != null) Destroy(b);
        }
        activeBubbles.Clear();
    }

    // Vòng lặp kiểm tra khoảng cách định kỳ, không cần check mỗi frame
    IEnumerator DistanceCheckLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(distanceCheckInterval);

            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null) continue;
            }

            Vector3 camPos = cachedCamera.transform.position;

            foreach (var group in groups)
            {
                if (group.npcA == null) continue;
                float dist = Vector3.Distance(camPos, group.npcA.position);
                group.allowedByDistance = dist <= maxDistanceFromCamera;
            }

            foreach (var solo in soloNpcs)
            {
                if (solo.npc == null) continue;
                float dist = Vector3.Distance(camPos, solo.npc.position);
                solo.allowedByDistance = dist <= maxDistanceFromCamera;
            }
        }
    }

    void FaceEachOther(ConversationGroup group)
    {
        Vector3 dir = (group.npcB.position - group.npcA.position).normalized;
        dir.y = 0;
        if (dir == Vector3.zero) return;

        group.npcA.rotation = Quaternion.LookRotation(dir);
        group.npcB.rotation = Quaternion.LookRotation(-dir);
    }

    IEnumerator RunGroup(ConversationGroup group)
    {
        float initialDelay = Random.Range(group.startDelayMin, group.startDelayMax);
        yield return new WaitForSeconds(initialDelay);

        group.isRunning = true;

        while (group.isRunning)
        {
            yield return new WaitForSeconds(group.delayBetweenTalks);

            // Nếu đang bật giới hạn khoảng cách và NPC đang ở ngoài phạm vi, chờ tới khi vào lại
            if (useDistanceLimit)
            {
                yield return new WaitUntil(() => group.allowedByDistance);
            }

            if (group.npcA == null || group.npcB == null) yield break;

            if (group.linesA.Length > 0)
            {
                yield return StartCoroutine(ShowTyping(group.npcA, group.typingOffsetA));
                string lineA = GetRandomLine(group.linesA, ref group.lastIndexA);
                SpawnBubble(group.npcA, lineA, group.bubbleOffsetA, group.bubbleDuration);
            }

            yield return new WaitForSeconds(group.gapBetweenLines);

            if (group.npcA == null || group.npcB == null) yield break;

            if (group.linesB.Length > 0)
            {
                yield return StartCoroutine(ShowTyping(group.npcB, group.typingOffsetB));
                string lineB = GetRandomLine(group.linesB, ref group.lastIndexB);
                SpawnBubble(group.npcB, lineB, group.bubbleOffsetB, group.bubbleDuration);
            }
        }
    }

    IEnumerator RunSolo(SoloNPC solo)
    {
        float initialDelay = Random.Range(solo.startDelayMin, solo.startDelayMax);
        yield return new WaitForSeconds(initialDelay);

        solo.isRunning = true;

        while (solo.isRunning)
        {
            yield return new WaitForSeconds(solo.delayBetweenTalks);

            if (useDistanceLimit)
            {
                yield return new WaitUntil(() => solo.allowedByDistance);
            }

            if (solo.npc == null) yield break;

            if (solo.lines.Length > 0)
            {
                yield return StartCoroutine(ShowTyping(solo.npc, solo.typingOffset));
                string line = GetRandomLine(solo.lines, ref solo.lastIndex);
                SpawnBubble(solo.npc, line, solo.bubbleOffset, solo.bubbleDuration);
            }
        }
    }

    string GetRandomLine(string[] lines, ref int lastIndex)
    {
        if (lines.Length == 0) return "";
        if (lines.Length == 1) return lines[0];

        int idx;
        do
        {
            idx = Random.Range(0, lines.Length);
        }
        while (idx == lastIndex);

        lastIndex = idx;
        return lines[idx];
    }

    IEnumerator ShowTyping(Transform target, Vector3 offset)
    {
        if (typingIndicatorPrefab == null || target == null) yield break;

        GameObject typing = Instantiate(typingIndicatorPrefab);
        activeBubbles.Add(typing);

        typing.transform.SetParent(target);
        typing.transform.position = target.position + offset;
        typing.transform.rotation = Quaternion.identity;

        var popAnim = typing.GetComponent<BubblePopAnimation>();
        if (popAnim != null)
        {
            popAnim.Init(Vector3.one * typingFixedScale);
        }
        else
        {
            typing.transform.localScale = Vector3.one * typingFixedScale;
        }

        var dotsAnim = typing.GetComponent<TypingDotsAnimation>();
        float waitTime = dotsAnim != null ? dotsAnim.GetFullCycleDuration(1) : typingDurationFallback;

        yield return new WaitForSeconds(waitTime);

        if (typing != null)
        {
            activeBubbles.Remove(typing);
            Destroy(typing);
        }
    }

    void SpawnBubble(Transform target, string text, Vector3 offset, float duration)
    {
        if (bubblePrefab == null || target == null) return;

        GameObject bubble = Instantiate(bubblePrefab);
        activeBubbles.Add(bubble);

        bubble.transform.SetParent(target);
        bubble.transform.position = target.position + offset;
        bubble.transform.rotation = Quaternion.identity;

        var autoResize = bubble.GetComponent<AutoResizeBubble>();
        if (autoResize != null)
        {
            autoResize.ResizeToFitText(text);
        }
        else
        {
            var tmp = bubble.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }

        var popAnim = bubble.GetComponent<BubblePopAnimation>();
        if (popAnim != null)
        {
            popAnim.displayDuration = duration;
            popAnim.Init(Vector3.one * bubbleFixedScale);
        }
        else
        {
            bubble.transform.localScale = Vector3.one * bubbleFixedScale;
            Destroy(bubble, duration);
        }

        StartCoroutine(RemoveFromActiveList(bubble, duration + 1f));
    }

    IEnumerator RemoveFromActiveList(GameObject bubble, float delay)
    {
        yield return new WaitForSeconds(delay);
        activeBubbles.Remove(bubble);
    }

    public void SetGroupActive(string groupName, bool active)
    {
        foreach (var g in groups)
        {
            if (g.groupName == groupName)
                g.isRunning = active;
        }
    }

    public void SetSoloActive(string npcName, bool active)
    {
        foreach (var s in soloNpcs)
        {
            if (s.npcName == npcName)
                s.isRunning = active;
        }
    }
}