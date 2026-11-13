using System.Collections.Generic;
using TMPro;
using UnholyTrinity.Hex;
using UnityEngine;

namespace UnholyTrinity.Players
{
    // labels the six move directions around the player with the same names the move buttons use,
    // so the third person view shows which way each button goes. the labels aren't parented to the
    // player so its aim rotation doesn't spin them
    public class DirectionCompass : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelPrefab;

        [SerializeField, Min(0.1f), Tooltip("How far out the labels sit, in hexes.")]
        private float radiusInHexes = 1f;

        [SerializeField, Tooltip("Height of the labels above the player's position.")]
        private float labelHeight;

        private readonly List<(TMP_Text label, Vector3 offset)> labels = new();
        
        // fully qualified: inside UnholyTrinity.*, plain "Camera" resolves to the UnholyTrinity.Camera namespace
        private UnityEngine.Camera viewCamera;

        public void Show(HexLayout layout)
        {
            Hide();
            viewCamera = UnityEngine.Camera.main;

            float hexDistance = Vector3.Distance(
                layout.HexToWorld(HexCoord.Zero),
                layout.HexToWorld(HexDirection.D0.ToOffset()));

            for (int i = 0; i < HexDirectionExtensions.Count; i++)
            {
                HexDirection direction = (HexDirection)i;

                TMP_Text label = Instantiate(labelPrefab);
                label.name = $"{name} Direction {direction}";
                label.text = direction.DisplayName(layout.Orientation);

                Vector3 offset = layout.DirectionToWorld(direction) * (hexDistance * radiusInHexes) +
                                 Vector3.up * labelHeight;
                labels.Add((label, offset));
            }

            FollowPlayer();
        }

        public void Hide()
        {
            foreach ((TMP_Text label, Vector3 _) in labels)
            {
                if (label != null)
                {
                    Destroy(label.gameObject);
                }
            }

            labels.Clear();
        }

        // after the cameras and the plan have moved this frame
        private void LateUpdate() => FollowPlayer();

        private void OnDestroy() => Hide();

        private void FollowPlayer()
        {
            if (labels.Count == 0 || viewCamera == null)
            {
                return;
            }

            // facing the same way as the camera keeps the text readable from any view
            Quaternion facing = viewCamera.transform.rotation;

            foreach ((TMP_Text label, Vector3 offset) in labels)
            {
                label.transform.SetPositionAndRotation(transform.position + offset, facing);
            }
        }
    }
}
