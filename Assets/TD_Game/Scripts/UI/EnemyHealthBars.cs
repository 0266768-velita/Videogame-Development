using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TD_Game
{
    /// <summary>Health bars that follow each enemy in the battle, with a marker over the current target.</summary>
    public class EnemyHealthBars : MonoBehaviour
    {
        [SerializeField] CombatManager combat;
        [Tooltip("Inactive child used as the template for every bar.")]
        [SerializeField] RectTransform template;
        [SerializeField] Vector3 worldOffset = new Vector3(0f, 1.15f, 0f);

        class View
        {
            public RectTransform rect;
            public UIBar bar;
            public Text name;
            public GameObject marker;
        }

        readonly List<View> views = new List<View>();
        RectTransform area;
        Camera cam;

        void Awake()
        {
            area = (RectTransform)transform;
            if (template != null) template.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            var context = combat != null ? combat.Context : null;
            int count = context != null ? context.Enemies.Count : 0;
            if (cam == null) cam = Camera.main;

            for (int i = 0; i < Mathf.Max(count, views.Count); i++)
            {
                if (i >= count) { views[i].rect.gameObject.SetActive(false); continue; }
                var view = GetView(i);
                var enemy = context.Enemies[i];
                bool alive = enemy != null && !enemy.Health.IsDead;
                view.rect.gameObject.SetActive(alive);
                if (!alive || cam == null) continue;

                Vector2 screen = cam.WorldToScreenPoint(enemy.transform.position + worldOffset);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local);
                view.rect.anchoredPosition = local;
                view.bar.Set(enemy.Health.Current, enemy.Health.Max, $"{enemy.Health.Current:0}");
                view.name.text = enemy.Data.displayName;
                view.marker.SetActive(i == context.TargetIndex && combat.PlayerTurn.AcceptingInput);
            }
        }

        View GetView(int i)
        {
            while (views.Count <= i)
            {
                var rect = Instantiate(template, area);
                rect.gameObject.SetActive(true);
                views.Add(new View
                {
                    rect = rect,
                    bar = rect.GetComponentInChildren<UIBar>(true),
                    name = rect.Find("Name").GetComponent<Text>(),
                    marker = rect.Find("TargetMarker").gameObject
                });
            }
            return views[i];
        }
    }
}
