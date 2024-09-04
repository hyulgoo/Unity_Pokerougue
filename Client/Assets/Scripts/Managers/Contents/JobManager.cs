using Server.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobManager : JobSerializer
{
    public bool Excute { get { return _excute; } set { _excute = value; } }

    public void Update()
    {
        Flush();
    }
}
