using UnityEngine;

namespace OCDheim
{
    public readonly struct BorderSlicer : ISlicer
    {
        private Vector3 mid { get; }
        private float radius { get; }

        public BorderSlicer(Vector3 mid, float radius)
        {
            this.mid = mid;
            this.radius = radius;
        }

        // Friendly Reminder: (x - h)² + (y - k)² = r²
        private bool IsInsideBorder(float rhs) => !float.IsNaN(rhs);

        public MinMax? SliceZOn(float x)
        {
            var rhs = Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(x - mid.x, 2));
            return IsInsideBorder(rhs) ? new MinMax(mid.z - rhs, mid.z + rhs) : (MinMax?)null;
            
        }

        public MinMax? SliceXOn(float z)
        {
            var rhs = Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(z - mid.z, 2));
            return IsInsideBorder(rhs) ? new MinMax(mid.x - rhs, mid.x + rhs) : (MinMax?)null;
        }
    }
}
