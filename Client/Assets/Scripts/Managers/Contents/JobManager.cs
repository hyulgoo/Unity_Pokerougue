using Google.Protobuf.Protocol;
using Server.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JobManager : JobSerializer
{

    public void Update()
    {
        Flush();
    }

    public void Excute()
    {
        _excute = true;
        if (_jobEnd)
            Managers.Object.MyPlayer.StartCoroutine(TurnEnd());
    }

    IEnumerator TurnEnd()
    {
        yield return new WaitForSeconds(2f);
        C_TurnEnd packet = new C_TurnEnd();
        packet.PlayerId = Managers.Object.MyPlayer.Id;
        Managers.Network.Send(packet);
    }
}
