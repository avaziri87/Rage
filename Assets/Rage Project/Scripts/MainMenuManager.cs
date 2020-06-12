﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public void LoadGame()
    {
        if (ApplicationManager.instance) ApplicationManager.instance.LoadGame();
    }

    public void QuitGame()
    {
        if (ApplicationManager.instance) ApplicationManager.instance.QuitGame();
    }
}

