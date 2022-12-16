using System.Collections.Generic;

namespace OCDheim
{
    public interface ISide
    {
        void FillUp(HashSet<SnapNode> snapNodes);
    }
}
