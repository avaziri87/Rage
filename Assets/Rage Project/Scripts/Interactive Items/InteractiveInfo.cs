using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveInfo : InteractiveItem
{
    [TextArea(3, 10)]
    [SerializeField] string _infoText;

    public override string GetText()
    {
        return _infoText;
    }
}
