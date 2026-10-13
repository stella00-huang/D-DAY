using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DDay
{
    // The authored cover and its invisible hit areas share one transform.
    public sealed class DDayMainMenu : MonoBehaviour
    {
        public DDayGame game;
        public Image background, shade;
        public RectTransform cover;
        public CanvasGroup buttons;
        public Button start, resume, settings, quit;
        Canvas canvas;
        DDayUI ui;
        Coroutine transition;
        bool visible, busy;
        float breathingStart;
        public bool IsTransitioning => busy;
        public bool Visible => canvas != null && canvas.enabled;
        public Texture BackgroundTexture => background.sprite.texture;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            if (game == null) game = FindFirstObjectByType<DDayGame>();
            if (game == null) { Debug.LogError("D-DAY menu has no campaign binding."); enabled = false; return; }
            ui = game.GetComponent<DDayUI>();
            start.onClick.AddListener(() => Begin(false));
            resume.onClick.AddListener(() => Begin(true));
            settings.onClick.AddListener(() => { if (busy) return; DDayAudioManager.Play(SfxCue.Click); ui.OpenTitleSettings(); });
            quit.onClick.AddListener(() => { if (busy) return; DDayAudioManager.Play(SfxCue.Click); Application.Quit(); });
        }
        void Update()
        {
            bool show = game.CurrentPhase == Phase.Title && !ui.TitleSettingsOpen && !ui.TitleCreditsOpen;
            var ending = game.GetComponent<HEEndingPlayer>();
            if (ending != null && ending.OwnsUI) show = false;
            if (show && !visible)
            {
                breathingStart = Time.unscaledTime; busy = false;
                background.color = Color.white; cover.localScale = Vector3.one;
                shade.color = Color.black; buttons.alpha = 1;
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }
            visible = show; canvas.enabled = show;
            GetComponent<GraphicRaycaster>().enabled = show;
            buttons.interactable = buttons.blocksRaycasts = show && !busy;
            resume.interactable = game.CanContinue && !busy;
            if (!show)
            {
                if (transition != null) { StopCoroutine(transition); transition = null; busy = false; }
                return;
            }
            // Fit the whole cover for any aspect ratio; a black underlay fills letterboxing.
            var viewport = ((RectTransform)canvas.transform).rect.size;
            float aspect = background.sprite.rect.width / background.sprite.rect.height;
            float width = Mathf.Min(viewport.x, viewport.y * aspect);
            cover.sizeDelta = new Vector2(width, width / aspect);
            if (!busy)
            {
                float age = Time.unscaledTime - breathingStart;
                float breathing = (1 - Mathf.Cos(age * .18f)) * .5f;
                cover.localScale = Vector3.one * Mathf.Lerp(1, 1.012f, breathing);
                background.color = Color.Lerp(Color.white, new Color(1, .994f, .985f), breathing);
                shade.color = new Color(0, 0, 0, 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(age)));
            }
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null
                && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow)
                    || Input.GetKeyDown(KeyCode.Tab))) start.Select();
        }
        void Begin(bool continuing)
        {
            if (busy || game.CurrentPhase != Phase.Title || continuing && !game.CanContinue) return;
            busy = true; buttons.interactable = buttons.blocksRaycasts = false;
            DDayAudioManager.Play(SfxCue.Click);
            transition = StartCoroutine(EnterOffice(continuing));
        }
        IEnumerator EnterOffice(bool continuing)
        {
            float began = Time.unscaledTime, initial = cover.localScale.x, darkness = shade.color.a;
            while (Time.unscaledTime - began < .8f)
            {
                float ease = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - began) / .8f));
                cover.localScale = Vector3.one * Mathf.Lerp(initial, 1.02f, ease);
                shade.color = new Color(0, 0, 0, Mathf.Lerp(darkness, 1, ease));
                yield return null;
            }
            if (continuing) game.ContinueGame(); else game.StartGame();
            transition = null; busy = false; canvas.enabled = false;
        }
        void OnDisable() { if (transition != null) StopCoroutine(transition); transition = null; busy = false; }
    }
}
