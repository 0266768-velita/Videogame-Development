using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>
    /// One command row of the combat menu: round key badge, big name, cost and a thin gold underline.
    /// Text always sits on an opaque dark plate, so it stays readable in every state.
    /// Hovering with the mouse slides the row out a little.
    /// </summary>
    public class ActionButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public enum State { Ready, Unaffordable, Locked, Waiting, Active }

        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image keyBadge;
        [SerializeField] Image underline;
        [SerializeField] Text keyLabel;
        [SerializeField] Text nameLabel;
        [SerializeField] Text costLabel;
        [SerializeField, Min(0f)] float hoverSlide = 14f;

        [Header("Colours")]
        [SerializeField] Color plate = MexicaPalette.ObsidianPlate;
        [SerializeField] Color activePlate = MexicaPalette.Tezontle;
        [SerializeField] Color gold = MexicaPalette.AgedGold;
        [SerializeField] Color white = MexicaPalette.Bone;
        [SerializeField] Color muted = MexicaPalette.Ash;
        [SerializeField] Color shortage = MexicaPalette.Shortage;
        [SerializeField] Color tonalli = MexicaPalette.Turquoise;

        Action onClick;
        RectTransform rect;
        Vector2 basePosition;
        float slide;
        bool hovered;

        void Awake()
        {
            rect = (RectTransform)transform;
            basePosition = rect.anchoredPosition;
            if (button != null) button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        public void Setup(string key, string displayName, string cost, Action clicked)
        {
            if (keyLabel != null) keyLabel.text = key;
            if (nameLabel != null) nameLabel.text = displayName.ToUpperInvariant();
            if (costLabel != null) costLabel.text = cost;
            onClick = clicked;
        }

        /// <summary>highlight = pulse in the Tonalli colour (ultimate ready).</summary>
        public void Show(State state, bool highlight)
        {
            bool usable = state == State.Ready || state == State.Active;
            Color accent = highlight ? Color.Lerp(gold, tonalli, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : gold;

            if (background != null) background.color = state == State.Active ? activePlate : plate;
            if (keyBadge != null) keyBadge.color = usable ? accent : new Color(accent.r, accent.g, accent.b, 0.35f);
            if (underline != null) underline.color = usable ? accent : new Color(muted.r, muted.g, muted.b, 0.3f);
            if (nameLabel != null)
            {
                nameLabel.color = usable ? white : muted;
                if (state == State.Locked) nameLabel.text = "LOCKED";
            }
            if (costLabel != null) costLabel.color = state == State.Unaffordable ? shortage : usable ? gold : muted;
            if (button != null) button.interactable = state == State.Ready;

            float target = usable && hovered ? hoverSlide : 0f;
            slide = Mathf.MoveTowards(slide, target, 120f * Time.unscaledDeltaTime);
            if (rect != null) rect.anchoredPosition = basePosition + Vector2.left * slide;
        }
    }
}
