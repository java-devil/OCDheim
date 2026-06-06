namespace OCDheim
{
    public interface ISlicer
    {
        MinMax? SliceZOn(float x);
        MinMax? SliceXOn(float z);
    }

    public struct MinMax
    {
        public float min { get; private set; }
        public float max { get; private set; }

        public MinMax(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
