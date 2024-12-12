using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager
{
    int _order = 10;
    float _uiSpeed = 0.1f;
    Stack<UICommonPopup> _popupStack = new Stack<UICommonPopup>();
    public UICommonScene SceneUI { get; set; }
    public float UISpeed { get { return _uiSpeed; } }
    public GameObject Root
    {
        get
        {
			GameObject root = GameObject.Find("@UI_Root");
			if (root == null)
				root = new GameObject { name = "@UI_Root" };
            return root;
		}
    }

    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;

        if (sort)
            canvas.sortingOrder = _order++;
        else
            canvas.sortingOrder = 0;
    }

	public T MakeWorldSpaceUI<T>(Transform parent = null, string name = null) where T : UICommonBase
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/WorldSpace/{name}");
		if (parent != null)
			go.transform.SetParent(parent);

        Canvas canvas = go.GetOrAddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

		return Util.GetOrAddComponent<T>(go);
	}

	public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UICommonBase
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");
		if (parent != null)
			go.transform.SetParent(parent);

		return Util.GetOrAddComponent<T>(go);
	}

	public T ShowSceneUI<T>(string name = null) where T : UICommonScene
	{
		if (string.IsNullOrEmpty(name))
			name = typeof(T).Name;

		GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
		T sceneUI = Util.GetOrAddComponent<T>(go);
        SceneUI = sceneUI;

		go.transform.SetParent(Root.transform);

        // 새로 생성하면 선택된 패널로 만들어줌.
        Managers.Select.CurPanel = go;

        return sceneUI;
	}

	public T ShowPopupUI<T>(string name = null) where T : UICommonPopup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");
        T popup = Util.GetOrAddComponent<T>(go);
        _popupStack.Push(popup);

        go.transform.SetParent(Root.transform);

        // 새로 생성하면 선택된 패널로 만들어줌.
        Managers.Select.CurPanel = go;

		return popup;
    }

    public void ClosePopupUI(UICommonPopup popup)
    {
		if (_popupStack.Count == 0)
			return;

        if (_popupStack.Peek() != popup)
        {
            Debug.Log("Close Popup Failed!");
            return;
        }

        ClosePopupUI();
    }

    public void ClosePopupUI()
    {
        if (_popupStack.Count <= 0)
            return;

        UICommonPopup popup = _popupStack.Pop();
        Managers.Resource.Destroy(popup.gameObject);
        popup = null;
        _order--;

        // 현재 선택된 UI를 변경해줌
        if (_popupStack.Count > 0)
        { 
            popup = _popupStack.Peek();
            Managers.Select.CurPanel = popup.gameObject;
        }
        else
        {
            Managers.Select.CurPanel = SceneUI.gameObject;
        }
    }

    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }

    public void Clear()
    {
        CloseAllPopupUI();
        SceneUI = null;
    }

    public void SetLobbyPlayer()
    {
        if (Managers.Scene.CurrentScene.SceneType != Define.Scene.Lobby) 
            return;

        KeyValuePair<int, GameObject>[] nameidlist = Managers.Player.GetObjects();
        int[] ids = new int[nameidlist.Length];
        string[] names = new string[nameidlist.Length];

        for (int i = 0; i < nameidlist.Length; i++)
        {
            ids[i] = nameidlist[i].Key;
            names[i] = nameidlist[i].Value.name;
        }

        if (SceneUI == null) return;
        UILobbyScene lc = SceneUI.gameObject.GetComponent<UILobbyScene>();
        if (lc == null) return;
        lc.SetUserName(ids, names);
    }
}
