using UnityEngine;
using Sirenix.OdinInspector;

public class FixedPosition : ManagedBehaviour
{
    public bool fixX;
    [ShowIf("fixX")]
    public float xPosition;

    public bool fixY;
    [ShowIf("fixY")]
    public float yPosition;

    public bool fixZ;
    [ShowIf("fixZ")]
    public float zPosition;

    public override void ManagedUpdate()
    {
        float x = fixX ? xPosition : transform.position.x;
        float y = fixY ? yPosition : transform.position.y;
        float z = fixZ ? zPosition : transform.position.z;

        transform.position = new Vector3(x, y, z);
    }
}
