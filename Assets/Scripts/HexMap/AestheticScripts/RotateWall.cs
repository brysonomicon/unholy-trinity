using UnityEngine;

namespace UnholyTrinity.Hex
{
    public class RotateWall : MonoBehaviour
    {
        private static readonly int MinRotationCount = 1;
        private static readonly int MaxRotationCount = 6;
        private static readonly int RotationAngle = 60;
        private static readonly int XRotate, ZRotate = 0;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            int rotateAmount;
            rotateAmount = Random.Range(MinRotationCount, MaxRotationCount);
            rotateAmount *= RotationAngle;
            transform.Rotate(XRotate, rotateAmount, ZRotate);        
        }
    }
}