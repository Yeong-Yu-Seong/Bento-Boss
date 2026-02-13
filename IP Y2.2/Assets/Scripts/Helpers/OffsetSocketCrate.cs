/// <summary>
/// File: OffsetSocketCrate.cs
/// Author: Jayden Wong
/// Description: Applies small position offsets to crate lid and socket transforms on start to fix alignment issues.
/// </summary>
using System.Collections;
using UnityEngine;

public class OffsetSocketOnStart : MonoBehaviour
{
  [SerializeField] private Transform crateLidSocket;
  [SerializeField] private Transform crateLid;
  [SerializeField] private float socketZOffset = -0.006f;
  [SerializeField] private float lidZOffset = -0.006f;
  [SerializeField] private float lidYOffset = 0.0015f;
  [SerializeField] private float delay = 0.01f;

  private void Start()
  {
    if (crateLidSocket == null)
    {
      Debug.LogError("Crate lid socket reference is missing.");
      return;
    }

    if (crateLid == null)
    {
      Debug.LogError("Crate lid reference is missing.");
      return;
    }

    Vector3 socketPosition = crateLidSocket.localPosition;
    crateLidSocket.localPosition = new Vector3(
        socketPosition.x,
        socketPosition.y,
        socketPosition.z + socketZOffset
    );

    StartCoroutine(MoveLidAfterDelay());
  }

  // Delay ensures the socket has settled before moving the lid
  private IEnumerator MoveLidAfterDelay()
  {
    yield return new WaitForSeconds(delay);

    Vector3 lidPosition = crateLid.localPosition;
    crateLid.localPosition = new Vector3(
        lidPosition.x,
        lidPosition.y + lidYOffset,
        lidPosition.z + lidZOffset
    );
  }
}
