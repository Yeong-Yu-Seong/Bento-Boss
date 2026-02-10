using UnityEngine;

public class SocketFollowDrawer : MonoBehaviour
{
    public Transform drawerTransform;
    private Vector3 localOffset;
    private Quaternion localRotation;
    
    void Start()
    {
        if (drawerTransform == null)
        {
            Debug.LogError("SocketFollowDrawer: drawerTransform is not assigned!");
            return;
        }
        
        // Store the socket's position and rotation relative to the drawer
        localOffset = drawerTransform.InverseTransformPoint(transform.position);
        localRotation = Quaternion.Inverse(drawerTransform.rotation) * transform.rotation;
    }
    
    void LateUpdate()
    {
        if (drawerTransform == null) return;
        
        // Update socket position and rotation to follow the drawer
        transform.position = drawerTransform.TransformPoint(localOffset);
        transform.rotation = drawerTransform.rotation * localRotation;
    }
}