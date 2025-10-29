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

    private void Awake()
    {
        fpsText = GetComponent<Text>();
    }

    void Update()
    {
        if (!showFPS) return;

        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // 업데이트 간격마다 FPS 계산
        if (timeleft <= 0f)
        {
            fps = accum / frames;
            timeleft = updateInterval;
            accum = 0f;
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