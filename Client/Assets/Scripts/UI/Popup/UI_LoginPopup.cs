using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_LoginPopup : UI_Popup
{
    enum InputFields
    {
        AccountName,
        Password,
    }

    enum Buttons
    {
        Btn_Create,
        Btn_Login,
    }

    public override void Init()
    {
        base.Init();

        BindInput(typeof(InputFields));
        BindButton(typeof(Buttons));

        GetButton((int)Buttons.Btn_Create).onClick.AddListener(OnClickCreateButton);
        GetButton((int)Buttons.Btn_Login).onClick.AddListener(OnClickLoginButton);

        GetInput((int)InputFields.AccountName).gameObject.GetOrAddComponent<UI_InputField>().UI_Popup = this;
        GetInput((int)InputFields.Password).gameObject.GetOrAddComponent<UI_InputField>().UI_Popup = this;
    }

    public void OnClickCreateButton()
    {
        string account = GetInput((int)InputFields.AccountName).text;
        string password = GetInput((int)InputFields.Password).text;

        CreateAccountPacketReq packet = new CreateAccountPacketReq()
        { 
            AccountName = account,
            Password = password,
        };

        Managers.Web.SendPostRequest<CreateAccountPacketRes>("account/create", packet, (res) =>
        {
            Debug.Log(res.CreateOk);
            GetInput((int)InputFields.AccountName).text = "";
            GetInput((int)InputFields.Password).text = "";
        });
    }

    public void OnClickLoginButton()
    {
        Debug.Log("OnClickLoginButton");

        string account = GetInput((int)InputFields.AccountName).text;
        string password = GetInput((int)InputFields.Password).text;

        LoginAccountPacketReq packet = new LoginAccountPacketReq()
        {
            AccountName = account,
            Password = password,
        };

        Managers.Web.SendPostRequest<LoginAccountPacketRes>("account/login", packet, (res) =>
        {
            Debug.Log(res.LoginOk);
            GetInput((int)InputFields.AccountName).text = "";
            GetInput((int)InputFields.Password).text = "";

            if(res.LoginOk)
            {
                Managers.Network.AccountId = res.AccountId;
                Managers.Network.Token = res.Token;
                Managers.Network.Name = account;

                Managers.Network.ConnectToGame(res.ServerList[0]);
                Managers.Scene.LoadScene(Define.Scene.Lobby);
            }
        });
    }
}
