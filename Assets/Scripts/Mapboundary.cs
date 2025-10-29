using UnityEngine;

public class MapBoundary : MonoBehaviour
{
    [SerializeField] private float mapSize = 50f;
    [SerializeField] private Color boundaryColor = Color.yellow;

    void OnDrawGizmos()
    {
        Gizmos.color = boundaryColor;

        float halfSize = mapSize * 0.5f;

        // 바닥 경계선 그리기
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfSize, 0, -halfSize),
            new Vector3(halfSize, 0, -halfSize),
            new Vector3(halfSize, 0, halfSize),
            new Vector3(-halfSize, 0, halfSize)
        };

        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // 벽 세로선
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[i] + Vector3.up * 5f);
        }
    }
}