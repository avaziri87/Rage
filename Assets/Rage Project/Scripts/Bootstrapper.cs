using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    private void Start()
    {
        if (ApplicationManager.instance) ApplicationManager.instance.LoadMainMenu();
    }
}
