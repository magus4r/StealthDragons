using UnityEngine;
using UnityEngine.UI;
using TMPro;


    public class PlayerGUI : MonoBehaviour
    {
       
        public TMP_Text playerName;

    public void SetPlayerInfo(PlayerInfo info)
        {
            playerName.text = "Player " + info.playerIndex;
            playerName.color = info.ready ? Color.green : Color.white;
        }
    }
