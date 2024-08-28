using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPlayerController : PlayerController
{
    private void Start()
    {
        Managers.Object.MyPlayer = this;
    }
}
