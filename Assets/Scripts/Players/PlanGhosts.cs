using System.Collections.Generic;
using UnholyTrinity.Hex;
using UnholyTrinity.Weapons;
using UnityEngine;

namespace UnholyTrinity.Players
{
    // one ghost of a plan: where the player will stand, and the cone of the shot fired from there.
    // Cone is null when that tick doesn't fire
    public readonly struct PlanGhost
    {
        public readonly Pose Pose;
        public readonly List<AimTile> Cone;

        public PlanGhost(Pose pose, List<AimTile> cone = null)
        {
            Pose = pose;
            Cone = cone;
        }
    }

    public class PlanGhosts : MonoBehaviour
    {
        [SerializeField] private GameObject ghostPrefab;

        [SerializeField, Tooltip("Style for the cones of ghosts that fire. Falls back to the style Show is given.")]
        private AimPreview coneStyle;

        // a ghost and its cone are removed together, cone is null if the ghost doesn't fire
        private readonly List<(GameObject ghost, Transform cone)> ghosts = new();

        public void Show(IReadOnlyList<PlanGhost> plan, HexLayout layout, AimPreview fallbackConeStyle)
        {
            Hide();

            AimPreview style = coneStyle != null ? coneStyle : fallbackConeStyle;

            foreach (PlanGhost planned in plan)
            {
                GameObject ghost = Instantiate(ghostPrefab, planned.Pose.position, planned.Pose.rotation);
                ghost.name = $"{name} Ghost {ghosts.Count + 1}";

                // drawn on its own root rather than under the ghost so the ghost's rotation and scale
                // don't skew the tiles
                Transform cone = planned.Cone != null && style != null
                    ? style.Draw(planned.Cone, layout, $"{ghost.name} Cone")
                    : null;

                ghosts.Add((ghost, cone));
            }
        }

        public void Hide()
        {
            foreach ((GameObject ghost, Transform cone) in ghosts)
            {
                DestroyGhost(ghost, cone);
            }

            ghosts.Clear();
        }

        public void PickUpNext()
        {
            if (ghosts.Count == 0)
            {
                return;
            }

            DestroyGhost(ghosts[0].ghost, ghosts[0].cone);
            ghosts.RemoveAt(0);
        }

        private static void DestroyGhost(GameObject ghost, Transform cone)
        {
            Destroy(ghost);

            if (cone != null)
            {
                Destroy(cone.gameObject);
            }
        }
    }
}
