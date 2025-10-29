using Unity.Netcode;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Distance")]
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 8f;                 // 실제로는 초기화 시 initialPositionY로 세팅

    [Header("Initial View")]
    [SerializeField] private float initialPositionY = 4f;       // 시작 Y=4
    [SerializeField] private float initialRotationY = 0f;       // 시작 yaw(도)

    [Header("Mouse Rotation")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private bool invertY = false;

    [Header("Mouse Button")]
    [SerializeField] private int rotateMouseButton = 1;         // 우클릭=1

    [Header("Zoom")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float minHeight = 4f;
    [SerializeField] private float maxHeight = 15f;

    private Transform target;
    private float currentRotationY = 0f;
    private bool isRotating = false;
    private bool isInitialized = false;

    // 외부 접근용 (카메라 기준 입력 변환 등에 사용)
    public float CurrentRotationY => currentRotationY;

    public void SetTarget(Transform newTarget)
    {
        if (!(NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient))
            return;

        target = newTarget;

        if (!isInitialized)
        {
            // 초기 상태 한 번만 주입
            height = initialPositionY;          // 시작 Y 고정값 적용
            currentRotationY = initialRotationY;
            isInitialized = true;
            Debug.Log($"[Camera] Init -> height:{height}, yaw:{currentRotationY}°");
        }

        if (target != null)
        {
            // 첫 프레임은 바로 스냅
            UpdateCameraPosition(immediate: true);
            Debug.Log($"[Camera] Target set: {target.name}");
        }
    }

    void Update()
    {
        HandleMouseRotation();
        HandleZoom();
    }

    void LateUpdate()
    {
        if (target == null) return;
        // 보간 제거: 항상 즉시 스냅
        UpdateCameraPosition(immediate: true);
    }

    void HandleMouseRotation()
    {
        if (Input.GetMouseButtonDown(rotateMouseButton))
        {
            isRotating = true;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(rotateMouseButton))
        {
            isRotating = false;
            Cursor.visible = true;
        }

        if (!isRotating) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        currentRotationY += mouseX * rotationSpeed;

        // 마우스 Y로 카메라 높이만 조절(피치 대신 높이로 처리)
        height += (invertY ? 1f : -1f) * mouseY * rotationSpeed * 0.5f;
        height = Mathf.Clamp(height, minHeight, maxHeight);
    }

    void HandleZoom()
    {
        if (!enableZoom) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f)) return;

        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    void UpdateCameraPosition(bool immediate)
    {
        // 타겟의 '최종 위치'를 그대로 사용 (보간/보정은 타겟 쪽에서만!)
        Vector3 targetPos = target.position;

        // yaw로 궤도 방향 계산
        float angleRad = currentRotationY * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(angleRad), 0f, -Mathf.Cos(angleRad));

        // 원하는 카메라 위치 = 타겟 + (궤도 방향 * 거리) + (위로 height)
        Vector3 desiredPosition = targetPos + (direction * distance) + (Vector3.up * height);

        // 보간 제거: 즉시 스냅
        transform.position = desiredPosition;

        // 회전도 즉시 타겟을 향하게
        Vector3 lookPoint = targetPos + Vector3.up * 1f;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }
}
