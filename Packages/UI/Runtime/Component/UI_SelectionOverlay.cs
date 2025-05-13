using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yu5h1Lib;
using Yu5h1Lib.UI;

public class UI_SelectionOverlay : UIControl
{
    public SelectableHandler[] handlers;
    public RectTransform overlay;

    protected override void OnInitializing()
    {
        base.OnInitializing();
        handlers = GetComponentsInChildren<SelectableHandler>();
    }
    [SerializeField]
    private bool _allowKeyboardNavigation = true;
    public bool allowKeyboardNavigation 
    { 
        get => _allowKeyboardNavigation;
        set => _allowKeyboardNavigation = value;
    }
    private void Start() {
        foreach (var handler in handlers)
        {
            handler.selected += OnSelected;
            handler.deselected += OnDeselected;
        }
    }
    private void Update()
    {
        if (!allowKeyboardNavigation || handlers.IsEmpty() )
            return;
        if (Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (EventSystem.current.currentSelectedGameObject == null)
                    handlers[0].Select();
        }

    }

    private void OnSelected(BaseEventData data)
    {
        if (!data.selectedObject.activeInHierarchy)
            return;
        if (!overlay.gameObject.activeSelf)
            overlay.gameObject.SetActive(true);
        //overlay.position = data.selectedObject.transform.position;
        MatchFrameToTarget(overlay, (RectTransform)data.selectedObject.transform);
    }
    private void OnDeselected(BaseEventData data)
    {
        overlay.gameObject.SetActive(false);
    }
    public bool Contain(GameObject obj) => TryFindHandler(obj, out _);
    public bool TryFindHandler(GameObject gameObject,out SelectableHandler handler)
    {
        handler = null;
        if (gameObject == null)
            return false;
        foreach (var item in handlers)
            if (item.gameObject == gameObject)
            {
                handler = item;
                return true;
            }
        return false;
    }


    public void MatchFrameToTarget(RectTransform frame, RectTransform target)
    {
        frame.SetParent(target, worldPositionStays: false);
        frame.SetAnchorPreset(AnchorPreset.StretchFull);
    }
}
