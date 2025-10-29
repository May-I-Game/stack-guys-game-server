using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private float updateInterval = 0.5f; // 0.5초마다 업데이트

    private Text fpsText;
    private float accum = 0.0f;
    private int frames = 0;
    private float timeleft;
    private float fps;

    void Start()
    {
        // Canvas 생성
        GameObject canvasGO = new GameObject("FPS Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // FPS Text 생성
        GameObject textGO = new GameObject("FPS Text");
        textGO.transform.SetParent(canvasGO.transform);

        fpsText = textGO.AddComponent<Text>();
        fpsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fpsText.fontSize = 24;
        fpsText.color = Color.yellow;
        fpsText.alignment = TextAnchor.UpperLeft;

        // 위치 설정 (좌측 상단)
        RectTransform rectTransform = textGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10);
        rectTransform.sizeDelta = new Vector2(200, 50);

        timeleft = updateInterval;
    }

    void Update()
    {
        if (!showFPS) return;

        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // 업데이트 간격마다 FPS 계산
        if (timeleft <= 0.0f)
        {
            fps = accum / frames;
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;

            UpdateDisplay();
        }
    }

    void UpdateDisplay()
    {
        if (fpsText != null)
        {
            // FPS에 따라 색상 변경
            if (fps >= 50)
                fpsText.color = Color.green;
            else if (fps >= 30)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.red;

            fpsText.text = $"FPS: {fps:F1}\n" +
                          $"Frame Time: {(1000.0f / fps):F1}ms";
        }
    }
}