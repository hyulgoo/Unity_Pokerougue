using Server.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobManager : JobSerializer
{
    public bool Excute { get; set; } = true;

    public void Update()
    {
        if (!Excute) return;

        Flush();
        Excute = false;
    }
}
