using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Outcode
{
    public bool up, down,left, right;

    public Outcode()
    {
        up = false;
        down = false;
        left = false;
        right = false;
    }

        public Outcode(bool u, bool d, bool l, bool r)
    {
        up = u;
        down = d;
        left = l;
        right = r;
    }

    public Outcode(Vector2 point)
    {
        up = (point.y > 1);
        down = (point.y < -1);
        left = (point.x < -1);
        right = (point.x > 1);
    }

    public String CoordsToString()
    {
        string output = "";
        if (up) output += "1"; else output += "0";
        if (down) output += "1"; else output += "0";
        if (left) output += "1"; else output += "0";
        if (right) output += "1"; else output += "0";
        return output;
    }

    public static Outcode operator +(Outcode a, Outcode b) //Logical Or
    {
        return new Outcode((a.up || b.up), (a.down || b.down), (a.left || b.left), (a.right || b.right));
    }

    public static Outcode operator *(Outcode a, Outcode b) //Logical And
    {
        return new Outcode((a.up && b.up), (a.down && b.down), (a.left && b.left), (a.right && b.right));
    }

    public static bool operator ==(Outcode a, Outcode b) //Tests if outcodes are equal
    {
        return (a.up == b.up) && (a.down == b.down) && (a.left == b.left) && (a.right == b.right);
    }

    public static bool operator !=(Outcode a, Outcode b) //Tests if outcodes are equal
    {
        return !(a == b);
    }

}
