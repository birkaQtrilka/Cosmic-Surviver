public readonly struct CellPoint
{
    //additional position means another point between the already existing ocean vertices
    public readonly bool IsAdditional;
    // between which corners of the cell the additional position is located
    public readonly CornerIndexes AdditionalPos;

    public readonly int IndexOffset;

    public CellPoint(int offset)
    {
        IsAdditional = false;
        IndexOffset = offset;
        AdditionalPos = new(0, 0);
    }

    public CellPoint(CornerIndexes additionalPos)
    {
        IsAdditional = true;
        IndexOffset = int.MinValue;
        AdditionalPos = additionalPos;
    }
}