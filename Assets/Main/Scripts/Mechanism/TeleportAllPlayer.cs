using System.Collections;
using UnityEngine;

public class TeleportAllPlayer : MonoBehaviour
{
    private static int activeTeleportSequences;

    public static bool IsTeleporting =>
        activeTeleportSequences > 0;

    public IEnumerator TeleportPlayers(bool isPlayer2)
    {
        PlayerManager player1 = null;
        PlayerManager player2 = null;

        activeTeleportSequences++;

        try
        {
            GameObject player1Obj =
                GameObject.FindGameObjectWithTag("Player 1");

            GameObject player2Obj =
                GameObject.FindGameObjectWithTag("Player 2");

            if (player1Obj == null || player2Obj == null)
                yield break;

            player1 = player1Obj.GetComponent<PlayerManager>();
            player2 = player2Obj.GetComponent<PlayerManager>();

            if (player1 == null || player2 == null)
                yield break;

            PlayerMoveAI move1 =
                player1.GetComponent<PlayerMoveAI>();

            PlayerMoveAI move2 =
                player2.GetComponent<PlayerMoveAI>();

            PlayerVFX vfx1 =
                player1.GetComponent<PlayerVFX>();

            PlayerVFX vfx2 =
                player2.GetComponent<PlayerVFX>();

            if (move1 == null || move2 == null ||
                vfx1 == null || vfx2 == null)
            {
                yield break;
            }

            int index1 = move1.currentIndex;
            int index2 = move2.currentIndex;

            if (isPlayer2)
            {
                yield return StartCoroutine(
                    TeleportOnePlayer(
                        player2,
                        move2,
                        vfx2,
                        index1
                    )
                );

                yield return new WaitForSeconds(0.4f);

                yield return StartCoroutine(
                    TeleportOnePlayer(
                        player1,
                        move1,
                        vfx1,
                        index2
                    )
                );
            }
            else
            {
                yield return StartCoroutine(
                    TeleportOnePlayer(
                        player1,
                        move1,
                        vfx1,
                        index2
                    )
                );

                yield return new WaitForSeconds(0.4f);

                yield return StartCoroutine(
                    TeleportOnePlayer(
                        player2,
                        move2,
                        vfx2,
                        index1
                    )
                );
            }
        }
        finally
        {
            if (player1 != null &&
                player1.playerCamera != null)
            {
                player1.playerCamera.isFllow2 = false;
            }

            if (player2 != null &&
                player2.playerCamera != null)
            {
                player2.playerCamera.isFllow2 = false;
            }

            activeTeleportSequences = Mathf.Max(
                0,
                activeTeleportSequences - 1
            );
        }
    }

    private IEnumerator TeleportOnePlayer(
     PlayerManager player,
     PlayerMoveAI move,
     PlayerVFX vfx,
     int targetIndex)
    {
        // Camera theo player này
        player.playerCamera.SetCamera2();
        yield return new WaitForSeconds(0.4f);
        // Hiện -> Ẩn
        yield return StartCoroutine(vfx.DissolveOutRoutine());

        // Teleport
        yield return StartCoroutine(move.TeleportEffect(targetIndex));


        // Ẩn -> Hiện
        yield return StartCoroutine(vfx.DissolveInRoutine());

        // Giữ camera 1 giây
        yield return new WaitForSeconds(0.2f);

        // Trả camera về trạng thái không follow
        player.playerCamera.isFllow2 = false;
    }
}