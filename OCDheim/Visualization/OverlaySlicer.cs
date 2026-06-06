using UnityEngine;

namespace OCDheim
{
    public readonly struct OverlaySlicer : ISlicer
    {
        private Vector3 boxMid { get; }
        private float sideSize { get; }

        private bool IsInsideXBounds(float x) => x >= boxMid.x - sideSize * 0.5f && x <= boxMid.x + sideSize * 0.5f;
        private bool IsInsideZBounds(float z) => z >= boxMid.z - sideSize * 0.5f && z <= boxMid.z + sideSize * 0.5f;
        public MinMax? SliceZOn(float x) => IsInsideXBounds(x) ? new MinMax(boxMid.z - sideSize * 0.5f, boxMid.z + sideSize * 0.5f) : null as MinMax?;
        public MinMax? SliceXOn(float z) => IsInsideZBounds(z) ? new MinMax(boxMid.x - sideSize * 0.5f, boxMid.x + sideSize * 0.5f) : null as MinMax?;

        public OverlaySlicer(Vector3 boxMid, float sideSize)
        {
            this.boxMid = boxMid;
            this.sideSize = sideSize;
        }
    }
}
