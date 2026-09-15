using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCutSceneEndGame : MonoBehaviour
{
    public bool startCutscene = false;

    public Animator anim;
    public float moveSpeed = 3f;

    public bool isMoving = false;


    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (startCutscene && !isMoving)
        {
            StartCoroutine(AutoMove());
        }

        if (isMoving)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }

    IEnumerator AutoMove()
    {
        isMoving = true;
        startCutscene = false;


        // Chạy animation Run
        anim.SetFloat("Run", 1f);

        // Di chuyển 4 giây
        yield return new WaitForSeconds(4f);

        // Dừng lại
        isMoving = false;
        anim.SetFloat("Run", 0f);
    }
}