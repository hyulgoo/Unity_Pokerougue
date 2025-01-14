using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UICommonBase : MonoBehaviour
{
	protected Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();

	public Button _lastSelected = null;
	public Image _pointer = null;

    public enum InitSounds
    {
		None,
		Popup,
		Select,
		Error,
    }

    public InitSounds InitSound { get; set; } = InitSounds.None;

    public abstract void Init();

	private void Awake()
	{
		Init();

		if(_pointer == null)
			_pointer = Util.FindChild<Image>(gameObject, "Pointer", true);
    }

	protected void Bind<T>(Type type) where T : UnityEngine.Object
	{
		string[] names = Enum.GetNames(type);
		UnityEngine.Object[] objects = new UnityEngine.Object[names.Length];
		_objects.Add(typeof(T), objects);

		for (int index = 0; index < names.Length; index++)
		{
			if (typeof(T) == typeof(GameObject))
				objects[index] = Util.FindChild(gameObject, names[index], true);
			else
				objects[index] = Util.FindChild<T>(gameObject, names[index], true);

			if (objects[index] == null)
				Debug.Assert(false, $"Failed to bind({names[index]})");
		}
	}

	protected void BindObject(Type type) { Bind<GameObject>(type); }
    protected void BindButton(Type type) { Bind<Button>(type); }
    protected void BindImage(Type type) { Bind<Image>(type); }
    protected void BindText(Type type) { Bind<TMP_Text>(type); }
    protected void BindInput(Type type) { Bind<TMP_InputField>(type); }

    protected T Get<T>(int idx) where T : UnityEngine.Object
	{
		UnityEngine.Object[] objects = null;
		if (_objects.TryGetValue(typeof(T), out objects) == false)
		{
			Debug.Assert(false, "Fail to Get UIElement!!");
			return null; 
		}

		return objects[idx] as T;
	}

	protected GameObject GetObject(int idx) { return Get<GameObject>(idx); }
	protected TMP_Text GetText(int idx) { return Get<TMP_Text>(idx); }
	protected Button GetButton(int idx) { return Get<Button>(idx); }
	protected Image GetImage(int idx) { return Get<Image>(idx); }
	protected TMP_InputField GetInput(int idx) { return Get<TMP_InputField>(idx); }

	public static void BindEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
	{
		UIEventHandler evt = Util.GetOrAddComponent<UIEventHandler>(go);

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
		if (_pointer == null) 
			return;

        _pointer.transform.SetParent(parent);
        RectTransform selectrect = _pointer.GetComponent<RectTransform>();
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
