using UnityEngine;

namespace UnholyTrinity.Hex
{
    public class RotateWall : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            int rotateAmount;
            rotateAmount = Random.Range(1, 5);
            rotateAmount *= 60;
            transform.Rotate(0, rotateAmount, 0);
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
