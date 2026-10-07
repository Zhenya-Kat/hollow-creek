using UnityEngine;

namespace HollowCreek.World
{
    [RequireComponent(typeof(Light))]
    public sealed class CandleFlicker : MonoBehaviour
    {
        public Transform Flame;
        Light candle;
        float intensity;
        Vector3 flameScale;

        void Awake()
        {
            candle = GetComponent<Light>();
            intensity = candle.intensity;
            if (Flame) flameScale = Flame.localScale;
        }

        void Update()
        {
            var slow = Mathf.PerlinNoise(17.3f, Time.time * 3.5f);
            var fast = Mathf.PerlinNoise(31.7f, Time.time * 11f);
            var factor = 0.86f + slow * 0.20f + fast * 0.08f;
            candle.intensity = intensity * factor;
            if (Flame) Flame.localScale = Vector3.Scale(flameScale, new Vector3(1, 0.90f + slow * 0.20f, 1));
        }
    }
}
