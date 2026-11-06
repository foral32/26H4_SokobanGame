using UnityEngine;
public class DebugCheat : MonoBehaviour
{
#if UNITY_EDITOR
    private void Update()
    {
        // 只在按下控制键时检测其他按键
        if (Input.GetKey(KeyCode.LeftControl))
        {

        }
    }
#endif
}
