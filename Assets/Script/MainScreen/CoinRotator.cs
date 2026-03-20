using UnityEngine;

public class CoinRotator : MonoBehaviour
{
    // Tốc độ xoay
    public float speed = 150f;

    void Update()
    {
        // Thêm chữ Space.World để ép nó xoay quanh trục Y thẳng đứng của bản đồ
        transform.Rotate(0, speed * Time.deltaTime, 0, Space.World);
    }
}
