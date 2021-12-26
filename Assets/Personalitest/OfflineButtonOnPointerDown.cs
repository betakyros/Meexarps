using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OfflineButtonOnPointerDown : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler
{
    public main m;
#if UNITY_WEBGL && !UNITY_EDITOR
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) {
        m.StartInOfflineMode();
    }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData) {
        m.StartInOfflineMode();
    }
#endif
}
