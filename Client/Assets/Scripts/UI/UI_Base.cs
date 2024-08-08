using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UI_Base : MonoBehaviour
{
	protected Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();

	public Button _lastSelected = null;
    enum Pointer
    {
        Pointer,
    }

    public abstract void Init();

	private void Awake()
	{
		Init();
        Bind<Image>(typeof(Pointer));
    }

	protected void Bind<T>(Type type) where T : UnityEngine.Object
	{
		string[] names = Enum.GetNames(type);
		UnityEngine.Object[] objects = new UnityEngine.Object[names.Length];
		_objects.Add(typeof(T), objects);

		for (int i = 0; i < names.Length; i++)
		{
			if (typeof(T) == typeof(GameObject))
				objects[i] = Util.FindChild(gameObject, names[i], true);
			else
				objects[i] = Util.FindChild<T>(gameObject, names[i], true);

			if (objects[i] == null)
				Debug.Log($"Failed to bind({names[i]})");
		}
	}

	protected T Get<T>(int idx) where T : UnityEngine.Object
	{
		UnityEngine.Object[] objects = null;
		if (_objects.TryGetValue(typeof(T), out objects) == false)
			return null;

		return objects[idx] as T;
	}

	protected GameObject GetObject(int idx) { return Get<GameObject>(idx); }
	protected Text GetText(int idx) { return Get<Text>(idx); }
	protected Button GetButton(int idx) { return Get<Button>(idx); }
	protected Image GetImage(int idx) { return Get<Image>(idx); }
	protected TMP_InputField GetInputField(int idx) { return Get<TMP_InputField>(idx); }

	public static void BindEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
	{
		UI_EventHandler evt = Util.GetOrAddComponent<UI_EventHandler>(go);

		switch (type)
		{
			case Define.UIEvent.Click:
				evt.OnClickHandler -= action;
				evt.OnClickHandler += action;
				break;
			case Define.UIEvent.Drag:
				evt.OnDragHandler -= action;
				evt.OnDragHandler += action;
				break;
		}
	}

    // 현재 선택된 버튼을 가리키는 Obj PosSetting
    public void SetPointerPos(Transform parent)
    {
		Image pointer = GetImage((int)Pointer.Pointer);
		if (pointer == null) return;

        pointer.transform.SetParent(parent);
        RectTransform selectrect = GetImage((int)Pointer.Pointer).GetComponent<RectTransform>();
        selectrect.offsetMin = Vector2.zero;
        selectrect.offsetMax = Vector2.zero;
    }

	public void ActiveBtn(bool active)
	{
        UnityEngine.Object[] buttons;
		_objects.TryGetValue(typeof(Button), out buttons);

        foreach (UnityEngine.Object btn in buttons)
		{
			Button button = (Button)btn;
            button.interactable = active;
        }
	}
}
