﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameCtrl
{
    AsyncOperation ao;
    public void StartLoadingGameScene()
    {
        ao = SceneManager.LoadSceneAsync("Game");
        EventMgr.UpdateEvent.AddListener(_checkIsDone);
    }

    public void StartCreatePlayer(int playerID)
    {
        Debug.Log("Player ID = " + playerID);
        if (GameSceneInfo.Instance == null)
        {
            Debug.Log("Null instance");
        }
        else if (GameSceneInfo.Instance.spawnPoints[playerID] == null)
        {
            Debug.Log("Null spawnPoint");
        }
        Transform t = GameSceneInfo.Instance.spawnPoints[playerID].transform;
        if (GameCtrl.IsOnlineGame)
            DataSync.CreateObject(UnitName.Player, t.position, t.rotation);
        else
        {
            Gamef.CreateLocalUnit(UnitName.Player, t.position, t.rotation);
        }
    }

    private void _checkIsDone()
    {
        if (ao.isDone)
        {
            if (IsOnlineGame)
                DataSync.CanControll();
            else
            {
                StartCreatePlayer(0);
            }
            EventMgr.UpdateEvent.RemoveListener(_checkIsDone);
        }
    }
}