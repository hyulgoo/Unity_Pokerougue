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

        if (Input.GetKeyDown(KeyCode.Q))
            Excute();
    }    

    public void Excute()
    {
        if (_jobEnd && Managers.Player.IsTurnProgressing)
        {
            _jobEnd = false;
            Managers.Player.IsTurnProgressing = false;

            C_TurnEnd packet = new C_TurnEnd();
            packet.PlayerId = Managers.Player.MyPlayer.Id;
            Managers.Network.Send(packet);
        }

        _excute = true;
    }

    IEnumerator TurnEnd()
    {
        yield return new WaitForSeconds(2f);
    }
}
