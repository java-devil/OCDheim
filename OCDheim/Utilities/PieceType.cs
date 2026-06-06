using static OCDheim.PieceType;

namespace OCDheim
{
    public enum PieceType
    {
        CONSTRUCTION,
        FURNITURE,
        TABLE
    }
    
    public static class PieceTypeHelpers
    {
        public static bool IsSnappable(this PieceType type)
        {
            return type == CONSTRUCTION || type == TABLE;
        }
    }
}
