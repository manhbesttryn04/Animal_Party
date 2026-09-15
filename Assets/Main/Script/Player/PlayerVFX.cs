using System.Collections;
using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [Header("Renderer")]
    public Renderer normalRenderer;
    public Renderer respawnRenderer;

    [Header("Particle")]
    public ParticleSystem circleEffect;

    [Header("Time")]
    public float dissolveOutTime = 1f;
    public float dissolveInTime = 1f;

    private Material respawnMat;

   private void Awake()
{
    SetupVFX();
}

private void SetupVFX()
{
    respawnMat = respawnRenderer.material;

    respawnRenderer.enabled = false;
   // normalRenderer.gameObject.SetActive(true);

    respawnMat.SetFloat("_Speed", 0f);
    respawnMat.SetFloat("_Progress", 1f);

    if (circleEffect != null)
        circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
}

    // Hiện -> Ẩn , Hannah em không hiểu cũng không sao
    public IEnumerator DissolveOutRoutine()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.startLeteClip);
        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", 1f);

        if (circleEffect != null)
        {
            circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            circleEffect.Play();
        }

        yield return StartCoroutine(SetProgress(1f, -1f, dissolveOutTime));

        normalRenderer.gameObject.SetActive(false);
    }
    public IEnumerator DissolveOutRoutine1()
    {
       // AudioManager.Instance.PlaySFX(AudioManager.Instance.startLeteClip);
        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", 1f);

        if (circleEffect != null)
        {
            circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            circleEffect.Play();
        }

        yield return StartCoroutine(SetProgress(1f, -1f, dissolveOutTime));

        normalRenderer.gameObject.SetActive(false);
    }


    // Ẩn -> Hiện
    public IEnumerator DissolveInRoutine()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.endLeteClip);
        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", -1f);

        if (circleEffect != null)
        {
            circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            circleEffect.Play();
        }

        yield return StartCoroutine(SetProgress(-1f, 1f, dissolveInTime));

        if (circleEffect != null)
            circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
     
       ;

        respawnRenderer.enabled = false;
        normalRenderer.gameObject.SetActive(true);
    }
    public IEnumerator DissolveInRoutine1()
    {
       // AudioManager.Instance.PlaySFX(AudioManager.Instance.endLeteClip);
        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", -1f);

        if (circleEffect != null)
        {
            circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            circleEffect.Play();
        }

        yield return StartCoroutine(SetProgress(-1f, 1f, dissolveInTime));

        if (circleEffect != null)
            circleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ;

        respawnRenderer.enabled = false;
        normalRenderer.gameObject.SetActive(true);
    }

    private IEnumerator SetProgress(float start, float end, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float value = Mathf.Lerp(start, end, time / duration);
            respawnMat.SetFloat("_Progress", value);

            yield return null;
        }

        respawnMat.SetFloat("_Progress", end);
    }
    // Hiện -> Ẩn, không dùng Particle
    public IEnumerator DissolveOutNoParticleRoutine()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.startLeteClip);

        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", 1f);

        yield return StartCoroutine(SetProgress(1f, -1f, 1f));

        respawnRenderer.enabled = false;
        normalRenderer.gameObject.SetActive(false);
    }
    //==================================================
    // Hiện lại (không dùng Particle)
    //==================================================
    public IEnumerator DissolveInNoParticleRoutine()
    {
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.endLeteClip);

        // Chỉ hiển thị renderer shader
        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        // Bắt đầu từ trạng thái đã biến mất
        respawnMat.SetFloat("_Progress", -1f);

        // Không Play() particle
        yield return StartCoroutine(SetProgress(-1f, 1f, 2f));

        // Trả về renderer bình thường
        respawnRenderer.enabled = false;
        normalRenderer.gameObject.SetActive(true);
    }
  //==================================================
// Hiện -> Ẩn (Không Particle - Có tham số thời gian)
//==================================================
public IEnumerator DissolveOutNoParticleRoutine(float duration)
    {
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.startLeteClip);

        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", 1f);

        yield return StartCoroutine(SetProgress(1f, -1f, duration));

        respawnRenderer.enabled = false;
        normalRenderer.gameObject.SetActive(false);
    }

    //==================================================
    // Ẩn -> Hiện (Không Particle - Có tham số thời gian)
    //==================================================
    public IEnumerator DissolveInNoParticleRoutine(float duration)
    {
        //AudioManager.Instance.PlaySFX(AudioManager.Instance.endLeteClip);

        respawnRenderer.enabled = true;
        normalRenderer.gameObject.SetActive(false);

        respawnMat.SetFloat("_Progress", -1f);

        yield return StartCoroutine(SetProgress(-1f, 1f, duration));

        respawnRenderer.enabled = false;
        normalRenderer.gameObject.SetActive(true);
    }
}