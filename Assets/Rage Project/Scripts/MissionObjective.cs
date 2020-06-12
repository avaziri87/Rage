using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionObjective : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(GameSceneManager.instance)
        {
            PlayerInfo playerIinfo = GameSceneManager.instance.GetPlayerInfo(other.GetInstanceID());
            if (playerIinfo != null)
                playerIinfo.characterManager.DoLevelComplete();
        }
    }
}
