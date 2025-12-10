using TMPro;
using UnityEngine;

namespace Tanks.Complete
{

    public class WinsController : MonoBehaviour
    {
        [Header("Wins Text UI")]
        public TextMeshProUGUI player1WinsText;
        public TextMeshProUGUI player2WinsText;

        public void UpdateWins(int p1Wins, int p2Wins, int winsToWin)
        {
            if (player1WinsText != null)
            {
                player1WinsText.text = $"Wins : {p1Wins} / {winsToWin}";
            }

            if (player2WinsText != null)
            {
                player2WinsText.text = $"Wins : {p2Wins} / {winsToWin}";
            }
        }
    }
}
