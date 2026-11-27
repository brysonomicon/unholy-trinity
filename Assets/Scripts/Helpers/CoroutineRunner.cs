using System.Collections;
using UnityEngine;

namespace UnholyTrinity.Helpers
{
internal static class CoroutineRunner
{
    //The only goal of this is to hold Coroutines for us :D
    private class RunnerHost : MonoBehaviour
    {
    }

    private static RunnerHost _instance;

    private static RunnerHost Instance
    {
        get
        {
            if (_instance)
            {
                return _instance;
            }

            GameObject go = new GameObject("GlobalCoroutineRunner");
            _instance = go.AddComponent<RunnerHost>();
            return _instance;
        }
    }

    public static Coroutine Start(IEnumerator routine) => Instance.StartCoroutine(routine);
    public static void Stop(Coroutine routine) => Instance.StopCoroutine(routine);
}
}