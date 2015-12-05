using UnityEngine;
using System.Collections;

public class Vertex
{
    public int x { get; set; }
    public int z { get; set; }
    public float height { get; set; }

    public Vertex(int x, int z, float value)
    {
        this.x = x;
        this.z = z;
        this.height = value;
    }
    public Vertex(int x, int z)
    {
        this.x = x;
        this.z = z;
        this.height = 0;
    }
    public void Rewrite(int x, int z, float value)
    {
        this.x = x;
        this.z = z;
        this.height = value;
    }
    public override bool Equals(object obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // If parameter cannot be cast to Point return false.
        Vertex v = obj as Vertex;
        if ((System.Object)v == null)
        {
            return false;
        }

        // Return true if the fields match:
        return x == v.x && z == v.z && height == v.height;
    }

    public bool CoordinatesEquals(Vertex v2)
    {
        return x == v2.x && z == v2.z;
    }

    public bool CoordinatesEquals(Vertex v2, int e)
    {
        return x - e <= v2.x && v2.x <= x + e && z - e <= v2.z && v2.z <= z + e;
    }

    /* not sure if good/necessary
    public override int GetHashCode()
    {
        return x * 98411 + z * 98507;
    }*/

    public Vertex Clone()
    {
        return new Vertex(x, z, height);
    }

    public override string ToString()
    {
        return "[" + x + "," + z + "]=" + height;
    }
}
