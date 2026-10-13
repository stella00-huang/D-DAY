using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DDay
{
    [RequireComponent(typeof(Button))]
    public sealed class DDayPaperButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public DDayPaperHighlight highlight;
        Button button;
        bool hover, pressed, selected;
        float fromHover, fromPress, targetHover, targetPress, changed, duration = .2f;
        public float HighlightStrength { get; private set; }
        public float PressStrength { get; private set; }
        void Awake() => button = GetComponent<Button>();
        void State()
        {
            fromHover = HighlightStrength; fromPress = PressStrength;
            targetHover = button.IsInteractable() && (hover || selected) ? 1 : 0;
            targetPress = button.IsInteractable() && pressed ? 1 : 0;
            duration = pressed ? .1f : .2f; changed = Time.unscaledTime;
        }
        void Update()
        {
            if (!button.IsInteractable() && (targetHover != 0 || targetPress != 0))
            { pressed = hover = selected = false; State(); }
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - changed) / duration));
            HighlightStrength = Mathf.Lerp(fromHover, targetHover, t);
            PressStrength = Mathf.Lerp(fromPress, targetPress, t);
            // No transform animation: the paper is already painted into the cover.
            highlight.color = PressStrength > 0
                ? new Color(.13f, .09f, .04f, PressStrength * .12f)
                : new Color(1, .94f, .8f, HighlightStrength * .10f);
        }
        public void OnPointerEnter(PointerEventData e) { hover = true; State(); }
        public void OnPointerExit(PointerEventData e) { hover = false; pressed = false; State(); }
        public void OnPointerDown(PointerEventData e) { if (e.button != PointerEventData.InputButton.Left) return; pressed = true; State(); }
        public void OnPointerUp(PointerEventData e) { pressed = false; State(); }
        public void OnSelect(BaseEventData e) { selected = true; State(); }
        public void OnDeselect(BaseEventData e) { selected = false; pressed = false; State(); }
        void OnDisable()
        {
            hover = pressed = selected = false;
            fromHover = fromPress = targetHover = targetPress = HighlightStrength = PressStrength = 0;
            if (highlight != null) highlight.color = Color.clear;
        }
    }
}
