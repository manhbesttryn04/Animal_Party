using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem.HID;

[RequireComponent(typeof(CharacterController))]
public class PlayerBlockPush : MonoBehaviour
{
    [Header("Block Check")]
    [SerializeField] private LayerMask blockLayer;
    [SerializeField] private string blockTag = "Block";
    [SerializeField] private float checkRadius = 1f;
   
    [Header("Push Settings")]
    [SerializeField] private float startPushDistance = 0.6f;
    [SerializeField] private float pushForce = 4f;
    [SerializeField] private float maxPushSpeed = 6f;

    public  CharacterController controller;

    private readonly Collider[] detectedBlocks = new Collider[20];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        PushAwayFromBlocks();
    }

    private void PushAwayFromBlocks()
    {
        int blockCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            checkRadius,
            detectedBlocks,
            blockLayer,
            QueryTriggerInteraction.Collide
        );

        Vector3 totalPush = Vector3.zero;

        for (int i = 0; i < blockCount; i++)
        {
            Collider block = detectedBlocks[i];

            if (block == null)
                continue;

            if (!block.CompareTag(blockTag))
                continue;

            Vector3 closestPoint = block.ClosestPoint(transform.position);

            Vector3 pushDirection = transform.position - closestPoint;
            pushDirection.y = 0f;

            float distance = pushDirection.magnitude;

            // Player đang nằm bên trong hoặc quá sát tâm collider
            if (distance <= 0.001f)
            {
                pushDirection = transform.position - block.bounds.center;
                pushDirection.y = 0f;

                if (pushDirection.sqrMagnitude <= 0.001f)
                    pushDirection = -transform.forward;

                distance = 0f;
            }

            if (distance >= startPushDistance)
                continue;

            pushDirection.Normalize();

            // Càng gần Block thì lực càng mạnh
            float pushPercent =
                1f - Mathf.Clamp01(distance / startPushDistance);

            totalPush += pushDirection * pushForce * pushPercent;
        }

        totalPush = Vector3.ClampMagnitude(totalPush, maxPushSpeed);

        if (totalPush.sqrMagnitude > 0.001f)
        {
            controller.Move(totalPush * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, startPushDistance);
    }

    
}