using UnityEngine;

public class AutoRotate : ManagedBehaviour
{
    public Vector3 rotationSpeed = default;
    public bool local = default;

    public override void ManagedUpdate()
    {
        Vector3 rotateAmount = rotationSpeed * Time.deltaTime;

        transform.Rotate(
            rotateAmount,
            local ? Space.Self : Space.World
        );
    }
}
