using System.Collections.Generic;
using UnityEngine;

namespace UnholyTrinity.Players
{
    public class PlanGhosts : MonoBehaviour
    {
        [SerializeField] private GameObject ghostPrefab;

        private readonly List<GameObject> ghosts = new();

        public void Show(List<Pose> poses)
        {
            Hide();

            foreach (Pose pose in poses)
            {
                GameObject ghost = Instantiate(ghostPrefab, pose.position, pose.rotation);
                ghost.name = $"{name} Ghost {ghosts.Count + 1}";
                ghosts.Add(ghost);
            }
        }

        public void Hide()
        {
            foreach (GameObject ghost in ghosts)
            {
                Destroy(ghost);
            }

            ghosts.Clear();
        }
    }
}
