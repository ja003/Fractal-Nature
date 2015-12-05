using UnityEngine;
using System.Collections;

public class FloodNode
{
    public Vertex vertex;
    public int parentIndex;
    public bool processed;

    public FloodNode(Vertex vertex, int parentIndex)
    {
        this.vertex = vertex;
        this.parentIndex = parentIndex;
        processed = false;
    }

    public override string ToString()
    {
        return vertex + "[" + parentIndex + "]";
    }

    public override bool Equals(object obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // If parameter cannot be cast to Point return false.
        FloodNode fn = obj as FloodNode;
        if ((System.Object)fn == null)
        {
            return false;
        }

        // Return true if the fields match:
        return vertex.Equals(fn.vertex);
    }
}

