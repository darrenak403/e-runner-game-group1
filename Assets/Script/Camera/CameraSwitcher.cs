using UnityEngine;
using Unity.Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera tppCamera; // Góc nhìn thứ 3
    public CinemachineCamera fppCamera; // Góc nhìn thứ 1

    private bool isFirstPerson = false;

    void Update()
    {
        // Khi người chơi nhấn phím V
        if (Input.GetKeyDown(KeyCode.V))
        {
            isFirstPerson = !isFirstPerson; // Đảo ngược trạng thái

            if (isFirstPerson)
            {
                // Bật góc nhìn thứ 1 (cho điểm cao hơn)
                fppCamera.Priority = 15;
                tppCamera.Priority = 5;
            }
            else
            {
                // Trở về góc nhìn thứ 3
                tppCamera.Priority = 15;
                fppCamera.Priority = 5;
            }
        }
    }
}