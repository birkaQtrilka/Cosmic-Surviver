using UnityEngine;

public class GridNavigator 
{
    public readonly int Resolution;

    //used to move through the grid using index offsets: current index + offset
    public readonly int downRight;
    public readonly int down;
    public readonly int downLeft;
    public readonly int left;
    public readonly int upLeft;
    public readonly int up;
    public readonly int upRight;
    public readonly int right;
    public readonly int origin;

    public readonly CellPoint[][] LookupTable;
    // this is when moving through the grid using x y coordinates: current position + offset
    public static readonly Vector2[] PosOffsets = new Vector2[]
    {
        new(0,0),
        new(1,0),
        new(1,1),
        new(0,1)
    };
    // the numbers mean between what points of the cell is the additional position.
    // 0-top left corner, 1-top right, 2-bottom right, 3-bottom left
    // corners are organized in an array, and these numbers are the indexes of that array
    public readonly static CornerIndexes north = new(0, 1);
    public readonly static CornerIndexes east = new(1, 2);
    public readonly static CornerIndexes south = new(2, 3);
    public readonly static CornerIndexes west = new(3, 0);
    // [X, o, o, o] ^

    // used to loop through all surrounding vertices (not in a cell)
    public readonly int[] neighborOffsets;
    /* ^^^^^
     * o o o
     *  \|/
     * o-X-o
     *  /|\
     * o o o
     */

    public GridNavigator(int resolution)
    {
        Resolution = resolution;

        downRight = Resolution + 1;
        down = Resolution;
        downLeft = Resolution - 1;
        left = -1;
        upLeft = -Resolution - 1;
        up = -Resolution;
        upRight = -Resolution + 1;
        right = 1;
        origin = 0;

        neighborOffsets = new int[]
        {
            downRight,
            down,
            downLeft,
            left,
            upLeft,
            up,
            upRight,
            right
        };

        LookupTable = InitLookUpTable();
    }

    //the 16 ways that a cell might look like mapped to how to order the verts in the triangles array
    //if it's a pole coordinate, that means it's a vert that should be created in between the existing vertices
    public CellPoint[][] InitLookUpTable()
    {
        //relative to topleft corner of cell (origin)
        return new CellPoint[][]
        {
            new CellPoint[] { /*no triangles*/},//0
            new CellPoint[] { // 1
                //first triangle
                new(west), new(south), new(down),
            },
            new CellPoint[] { //2
                new(east), new(downRight), new(south),
            },
            new CellPoint[] { //3
                //first triangle
                new(west), new(downRight), new(down),
                //second triangle
                new(west), new(east), new(downRight),
            },
            new CellPoint[] { //4
                new(north), new(right), new(east),
            },
            new CellPoint[] { //5
                new(west), new(south), new(down),
                new(west), new(north), new(south),
                new(north), new(east), new(south),
                new(north), new(right), new(east),
            },
            new CellPoint[] { //6
                new(north), new(right), new(downRight),
                new(north), new(downRight), new(south),
            },
            new CellPoint[] { //7
                new(west), new(downRight), new(down),
                new(north), new(downRight), new(west),
                new(north), new(right), new(downRight),
            },
            new CellPoint[] { //8
                new(west), new(origin), new(north),
            },
            new CellPoint[] { //9
                new(origin), new(north), new(south),
                new(origin), new(south), new(down),
            },
            new CellPoint[] { //10
                new(origin), new(north), new(west),
                new(west), new(north), new(east),
                new(west), new(east), new(south),
                new(south), new(east), new(downRight),
            },
            new CellPoint[] { //11
                new(origin), new(north), new(down),
                new(north), new(east), new(down),
                new(east), new(downRight), new(down),
            },
            new CellPoint[] { //12
                new(west), new(origin), new(east),
                new(origin), new(right), new(east),
            },
            new CellPoint[] { //13
                new(origin), new(right), new(east),
                new(origin), new(east), new(south),
                new(origin), new(south), new(down),
            },
             new CellPoint[] { //14
                new(origin), new(right), new(west),
                new(right), new(downRight), new(south),
                new(right), new(south), new(west),
            },
            new CellPoint[] { //15
                new(origin), new(right), new(downRight),
                new(origin), new(downRight), new(down)
            }
        };
    }

    public int GetX(int index)
    {
        return index % Resolution;
    }

    public int GetY(int index)
    {
        return index / Resolution;
    }
}
