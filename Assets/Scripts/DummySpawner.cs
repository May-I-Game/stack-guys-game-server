using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DummySpawner : MonoBehaviour
{
    //[Header("UI")]
    //[SerializeField] private Button spawnButton;
    //[SerializeField] private Button deleteButton;
    //[SerializeField] private TextMeshProUGUI countText;

    //private GameManager gameManager;

    //void Start()
    //{
    //    // GameManager 참조
    //    gameManager = FindFirstObjectByType<GameManager>();

    //    if (spawnButton != null)
    //    {
    //        spawnButton.onClick.AddListener(OnSpawnButtonClick);
    //    }

    //    if (deleteButton != null)
    //    {
    //        deleteButton.onClick.AddListener(OnDeleteButtonClick);
    //    }

    //    UpdateCountText();

    //    // 주기적으로 카운트 업데이트
    //    InvokeRepeating(nameof(UpdateCountText), 0.5f, 0.5f);
    //}

    //void OnSpawnButtonClick()
    //{
    //    if (NetworkManager.Instance != null)
    //    {
    //        NetworkManager.Instance.SendSpawnDummies(10);
    //        Debug.Log("Spawn 10 dummies requested");
    //    }
    //}

    //void OnDeleteButtonClick()
    //{
    //    if (NetworkManager.Instance != null)
    //    {
    //        NetworkManager.Instance.SendDeleteAllDummies();
    //    }
    //}

    //void UpdateCountText()
    //{
    //    if (countText != null && gameManager != null)
    //    {
    //        int count = gameManager.GetDummyCount();
    //        countText.text = $"Dummies: {count}";
    //    }
    //}

    //void OnDestroy()
    //{
    //    CancelInvoke();
    //}
}
