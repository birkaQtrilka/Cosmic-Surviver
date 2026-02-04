//the algorithm goes cell by cell, this data structure is to represent 2 corners of a cell
//0-top left corner, 1-top right, 2-bottom left, 3-bottom right
public readonly struct CornerIndexes
{
    public readonly int Corner1Offset;
    public readonly int Corner2Offset;

    public CornerIndexes(int corner1, int corner2)
    {
        Corner1Offset = corner1;
        Corner2Offset = corner2;
    }
}
