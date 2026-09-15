using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimalParty.Obstacles
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class DamegeTrap : MonoBehaviour
    {
        public enum TrapType { Laser, Fire }

        [Header("--- Phan Loai Bay ---")]
        [SerializeField] private TrapType trapType = TrapType.Laser;

        [Header("--- Target Detection ---")]
        [SerializeField] private LayerMask targetLayer;

        [Header("--- Wall Raycast ---")]
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private LineRenderer laserLine;
        [SerializeField] private ParticleSystem leftWallImpact;
        [SerializeField] private ParticleSystem rightWallImpact;
        [SerializeField] private float impactOffset = 0.05f;

        [Header("--- Trap Settings ---")]
        [SerializeField] private float hitCooldown = 0.5f;
        [SerializeField] private int coinPenalty = 5;
        [SerializeField] private float stunTime = 0.2f;

        [Header("--- Player Hit Effect ---")]
        [SerializeField] private float playerShakeAmount = 0.08f;
        [SerializeField] private float flashDuration = 0.4f;
        [SerializeField] private float flashInterval = 0.05f;

        [Header("--- VFX Lua Chay (Chi dung cho Fire) ---")]
        [SerializeField] private GameObject fireEffectPrefab;
        [SerializeField] private float burnDuration = 1.5f;
        [Tooltip("Chỉnh tọa độ Y để nhấc cục lửa cao lên ngang ngực hoặc đầu nhân vật")]
        [SerializeField] private Vector3 fireSpawnOffset = new Vector3(0f, 1f, 0f); 

        [Header("--- Debug ---")]
        [SerializeField] private bool showDebug = true;

        private readonly Dictionary<Collider, float> _lastHitTimes = new Dictionary<Collider, float>();
        private Collider _trapCollider;
        private readonly HashSet<PlayerMove> effectPlayers = new HashSet<PlayerMove>();

        private void Awake()
        {
            _trapCollider = GetComponent<Collider>();
            _trapCollider.isTrigger = true;
            if (trapType == TrapType.Laser) HideWallImpact();
        }

        private void Update()
        {
            if (trapType == TrapType.Laser) UpdateWallImpact();
        }

        private void UpdateWallImpact()
        {
            if (laserLine == null || laserLine.positionCount < 2)
            {
                HideWallImpact();
                return;
            }

            Vector3 point0 = laserLine.transform.TransformPoint(laserLine.GetPosition(0));
            Vector3 point1 = laserLine.transform.TransformPoint(laserLine.GetPosition(1));
            Vector3 midPoint = Vector3.Lerp(point0, point1, 0.5f);

            float leftDistance = Vector3.Distance(midPoint, point0);
            float rightDistance = Vector3.Distance(midPoint, point1);

            Vector3 dirMidToLeft = (point0 - midPoint).normalized;
            Vector3 dirMidToRight = (point1 - midPoint).normalized;

            if (Physics.Raycast(midPoint, dirMidToLeft, out RaycastHit leftHit, leftDistance, wallLayer, QueryTriggerInteraction.Ignore))
                ShowOneImpact(leftWallImpact, leftHit);
            else
                StopOneImpact(leftWallImpact);

            if (Physics.Raycast(midPoint, dirMidToRight, out RaycastHit rightHit, rightDistance, wallLayer, QueryTriggerInteraction.Ignore))
                ShowOneImpact(rightWallImpact, rightHit);
            else
                StopOneImpact(rightWallImpact);
        }

        private void ShowOneImpact(ParticleSystem impact, RaycastHit hit)
        {
            if (impact == null) return;
            impact.gameObject.SetActive(true);
            impact.transform.position = hit.point + hit.normal * impactOffset;
            impact.transform.rotation = Quaternion.LookRotation(hit.normal);
            if (!impact.isPlaying) impact.Play();
        }

        private void StopOneImpact(ParticleSystem impact)
        {
            if (impact != null && impact.isPlaying) impact.Stop();
        }

        private void HideWallImpact()
        {
            StopOneImpact(leftWallImpact);
            StopOneImpact(rightWallImpact);
        }

        private void OnTriggerEnter(Collider other) => ProcessHit(other);
        private void OnTriggerStay(Collider other) => ProcessHit(other);

        private void OnTriggerExit(Collider other)
        {
            if (_lastHitTimes.ContainsKey(other)) _lastHitTimes.Remove(other);
        }

        private void ProcessHit(Collider targetCollider)
        {
            if ((targetLayer.value & (1 << targetCollider.gameObject.layer)) == 0) return;
            if (!CanHitTarget(targetCollider)) return;

            HitPlayer(targetCollider);
            _lastHitTimes[targetCollider] = Time.time;
        }

        private bool CanHitTarget(Collider targetCollider)
        {
            if (_lastHitTimes.TryGetValue(targetCollider, out float lastTime))
                return Time.time - lastTime >= hitCooldown;
            return true;
        }

        private void HitPlayer(Collider targetCollider)
        {
            PlayerMiniGame miniGame = targetCollider.GetComponentInParent<PlayerMiniGame>();
            if (miniGame != null) miniGame.UpCoin(0, coinPenalty);

            PlayerMove move = targetCollider.GetComponentInParent<PlayerMove>();
            if (move != null)
            {
                if (FixBugMiniGame3.Instance != null)
                {
                    FixBugMiniGame3.Instance.SetNeedRecover(move);
                }
                // Phát âm thanh theo loại bẫy
                PlayTrapHitSound();

                StartCoroutine(StunRoutine(move));
                
                if (!effectPlayers.Contains(move)) 
                {
                    StartCoroutine(PlayerHitVisualEffect(move));
                    
                    if (trapType == TrapType.Fire && fireEffectPrefab != null)
                    {
                        StartCoroutine(SpawnAndShrinkFire(move.transform));
                    }
                }
            }
        }

        private IEnumerator StunRoutine(PlayerMove move)
        {
            move.isMove = false;
            move.isJump = false;

            if (move.manager != null && move.manager.playerAnimator != null)
                move.manager.playerAnimator.playerAnimator.SetTrigger("Lie");

            yield return new WaitForSeconds(stunTime);

            move.isMove = true;
            move.isJump = true;
        }

        private IEnumerator PlayerHitVisualEffect(PlayerMove move)
        {
            effectPlayers.Add(move);
            Transform playerTransform = move.transform;
            Vector3 originalLocalPos = playerTransform.localPosition;

            Renderer[] renderers = move.GetComponentsInChildren<Renderer>();
            List<Material> materials = new List<Material>();
            List<Color> originalColors = new List<Color>();

            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    materials.Add(mat);
                    if (mat.HasProperty("_BaseColor")) originalColors.Add(mat.GetColor("_BaseColor"));
                    else if (mat.HasProperty("_Color")) originalColors.Add(mat.GetColor("_Color"));
                    else originalColors.Add(Color.white);
                }
            }

            float timer = 0f;
            bool toggleColor = false;

            while (timer < flashDuration)
            {
                Vector3 shakeOffset = new Vector3(
                    Random.Range(-playerShakeAmount, playerShakeAmount),
                    Random.Range(-playerShakeAmount, playerShakeAmount),
                    Random.Range(-playerShakeAmount, playerShakeAmount)
                );
                playerTransform.localPosition = originalLocalPos + shakeOffset;

                Color primaryFlashColor = trapType == TrapType.Fire ? Color.red : Color.white;
                Color flashColor = toggleColor ? primaryFlashColor : Color.black;

                for (int i = 0; i < materials.Count; i++)
                {
                    if (materials[i].HasProperty("_BaseColor")) materials[i].SetColor("_BaseColor", flashColor);
                    else if (materials[i].HasProperty("_Color")) materials[i].SetColor("_Color", flashColor);
                }

                toggleColor = !toggleColor;
                yield return new WaitForSeconds(flashInterval);
                timer += flashInterval;
            }

            playerTransform.localPosition = originalLocalPos;

            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i].HasProperty("_BaseColor")) materials[i].SetColor("_BaseColor", originalColors[i]);
                else if (materials[i].HasProperty("_Color")) materials[i].SetColor("_Color", originalColors[i]);
            }
            effectPlayers.Remove(move);
         
        }

        private IEnumerator SpawnAndShrinkFire(Transform playerTransform)
        {
            if (fireEffectPrefab == null || playerTransform == null)
                yield break;

            GameObject fireVFX = Instantiate(
                fireEffectPrefab,
                playerTransform.position,
                Quaternion.identity,
                playerTransform
            );

            // Có thể object đã bị script khác Destroy ngay sau khi tạo
            if (fireVFX == null)
                yield break;

            Transform fireTransform = fireVFX.transform;

            if (fireTransform == null)
                yield break;

            fireTransform.localPosition = fireSpawnOffset;

            Vector3 originalScale = fireTransform.localScale;
            float timer = 0f;

            while (timer < burnDuration)
            {
                // FixBugMiniGame3 có thể đã xóa hiệu ứng lửa
                if (fireVFX == null || fireTransform == null)
                    yield break;

                timer += Time.deltaTime;

                float progress = Mathf.Clamp01(
                    timer / burnDuration
                );

                fireTransform.localScale = Vector3.Lerp(
                    originalScale,
                    Vector3.zero,
                    progress
                );

                yield return null;
            }

            // Chỉ Destroy nếu object vẫn còn tồn tại
            if (fireVFX != null)
            {
                Destroy(fireVFX);
            }
        }
        private void PlayTrapHitSound()
        {
            AudioManager audio = AudioManager.Instance;

            if (audio == null)
                return;

            AudioClip clip = null;

            switch (trapType)
            {
                case TrapType.Laser:
                    clip = audio.laserHitClip;
                    break;

                case TrapType.Fire:
                    clip = audio.fireHitClip;
                    break;
            }

            if (clip != null)
            {
                audio.PlaySFX(clip);
            }
        }
    }

}