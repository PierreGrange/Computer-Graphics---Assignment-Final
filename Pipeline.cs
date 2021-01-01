using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

public class Pipeline : MonoBehaviour
{
    Texture2D ourScreen;
    Renderer ourRenderer;
    Matrix4x4 translation, rotation, scale, viewing, projection, transformations;
    Model P;
    private float angle;
    private bool BackFaceCulling = true;

    
    void Start()                // Start is called before the first frame update
    {
        ourScreen = new Texture2D(256, 256);
        ourRenderer = GetComponent<Renderer>();
        ourRenderer.material.mainTexture = ourScreen;
        
        P = new Model(Model.default_primitives.P);
        //CreateUnityGameObject(P);
    }
    
    void Update()               // Update is called once per frame
    {
        Destroy(ourScreen);
        ourScreen = new Texture2D(256, 256);
        ourRenderer.material.mainTexture = ourScreen;
        List<Vector3> original = P._vertices;

        if (Input.GetKeyDown(KeyCode.Space))
            BackFaceCulling = !BackFaceCulling;

        angle++;

        //              ### New matrices ###
        Matrix4x4 world = Matrix4x4.Rotate(Quaternion.AngleAxis(angle, Vector3.up)) * Matrix4x4.Translate(new Vector3(-2, 1, -2));
        viewing = Matrix4x4.LookAt(new Vector3(0, 0, 5), new Vector3(0, 0, 0), new Vector3(0, 1, 0).normalized);
        projection = Matrix4x4.Perspective(45f, (ourScreen.width / ourScreen.height), 1, 1000);
        Matrix4x4 everythingMatrix = projection * viewing * world;
        List<Vector3> projected_P = find_image_of(original, everythingMatrix);

        for (int index = 0; index < P._index_list.Count; index += 3)                //Drawing the letter's polygons
        {
            int ind1 = P._index_list[index], ind2 = P._index_list[index + 1], ind3 = P._index_list[index + 2];
            Vector2 point1 = Vector2fromVector3(projected_P[ind1]), point2 = Vector2fromVector3(projected_P[ind2]),
                    point3 = Vector2fromVector3(projected_P[ind3]);

            Process_Triangle(point1, point2, point3);

            Vector2 p1 = new Vector2(0, 0), p2 = new Vector2(0, 1), p3 = new Vector2(0.5f, 0.5f);
            //FillUpTriangle(point1, point2, point3);
            //FillUpTriangle(p1, p2, p3);
        }
        ourScreen.Apply();

        /*              ### Previous CA matrices ###
        rotation = Matrix4x4.Rotate(Quaternion.AngleAxis(13f, new Vector3(16, -3, -3).normalized));
        scale = Matrix4x4.Scale(new Vector3(16, 2, 3));
        translation = Matrix4x4.Translate(new Vector3(-2, 1, -2));
        
        Matrix4x4 rotationscale = rotation * scale;
        transformations = rotation * scale * translation;

        List<Vector3> rotated_P = find_image_of(original, rotation);
        List<Vector3> rotatedandscaled_P = find_image_of(original, rotationscale);
        List<Vector3> transformed_P = find_image_of(original, transformations);
        */
    }

    //Scan fill with lines equations   x=(y-b)/a,   y=a*x+b   with   a=(y2-y1)/(x2-x1)   and   b=y1-a*x1
    private void FillUpTriangle(Vector2 point1, Vector2 point2, Vector2 point3) //WORK IN PROGRESS !!!!!!!!!!!!!!!!!!!!!!
    {
        Vector2 point1ToScale = point1, point2ToScale = point2, point3ToScale = point3;
        point1ToScale = new Vector2((point1ToScale.x + 1) * 255 / 2, (point1ToScale.y + 1) * 255 / 2);
        point2ToScale = new Vector2((point2ToScale.x + 1) * 255 / 2, (point2ToScale.y + 1) * 255 / 2);
        point3ToScale = new Vector2((point3ToScale.x + 1) * 255 / 2, (point3ToScale.y + 1) * 255 / 2);

        Vector2 middle_Y = GetMiddle_Y(point1ToScale, point2ToScale, point3ToScale), biggest_Y = GetBiggest_Y(point1ToScale, point2ToScale, point3ToScale),
            smallest_Y = GetSmallest_Y(point1ToScale, point2ToScale, point3ToScale);

        Vector2Int smallest_Y_Int = Vector2IntfromVector2(smallest_Y), middle_Y_Int = Vector2IntfromVector2(middle_Y),
            biggest_Y_Int = Vector2IntfromVector2(biggest_Y);

        Vector2Int smallest_X_point = Vector2IntfromVector2(GetSmallest_X(point1ToScale, point2ToScale, point3ToScale)), biggest_X_point = Vector2IntfromVector2(GetBiggest_X(point1ToScale, point2ToScale, point3ToScale));

        float m_MainLine = (biggest_Y.y - smallest_Y.y) / (biggest_Y.x - smallest_Y.x),
            c_MainLine = biggest_Y.y - m_MainLine * biggest_Y.x;
        int dx;

        if (Is_Rightof(middle_Y, m_MainLine, c_MainLine))
            dx = 1;
        else
            dx = -1;

        List<Vector2Int> mainLinePixels = Line_Rasterise(biggest_Y_Int, smallest_Y_Int);
        List<Vector2Int> triangleFillList = new List<Vector2Int>();
        if (m_MainLine == 0)
            triangleFillList = Line_Rasterise(smallest_X_point, biggest_X_point);
        else
        {
            foreach (Vector2Int point in mainLinePixels)
            {
                float m, c;
                int y = point.y;
                if (y > middle_Y.y)
                {
                    m = (biggest_Y.y - middle_Y.y) / (biggest_Y.x - middle_Y.x);
                    c = biggest_Y.y - m * biggest_Y.x;
                }
                else
                {
                    m = (middle_Y.y - smallest_Y.y) / (middle_Y.x - smallest_Y.x);
                    c = smallest_Y.y - m * smallest_Y.x;
                }

                for (int x = point.x; is_InsideTriangle(x, y, m, c, dx); x += dx)
                {
                    if (Input.GetKeyDown(KeyCode.Escape))
                        return;
                    triangleFillList.Add(new Vector2Int(x, y));
                }
            }
        }
        draw(triangleFillList);        
    }    

    private bool is_InsideTriangle(int x, int y, float m, float c, int dx) //is the point we want to draw on the good side of the sub line ?
    {
        if (dx > 0)
            return !Is_Rightof(new Vector2(x, y), m, c);
        else
            return Is_Rightof(new Vector2(x, y), m, c);
    }

    bool Is_Rightof(Vector2 point, float m, float c)
    {
        //return (point.y - m * point.x - c) < 0;
        if (m != 0)
            return ((point.y - c) / m) < point.x;
        else
            return false;
    }

    bool Is_Rightof(Vector2Int point, float m, float c)
    {
        return Is_Rightof(new Vector2(point.x, point.y), m, c);
    }

    private Vector2 GetBiggest_X(Vector2 p1, Vector2 p2, Vector2 p3) //WORK TO DO
    {
        if (p1.x >= p2.x)
            if (p1.x >= p3.x)
                return p1;
            else
                return p3;
        else
        if (p2.x >= p3.x)
            return p2;
        else
            return p3;
    }

    private Vector2 GetSmallest_X(Vector2 p1, Vector2 p2, Vector2 p3) //WORK TO DO
    {
        if (p1.x < p2.x)
            if (p1.x < p3.x)
                return p1;
            else
                return p3;
        else
        if (p2.x < p3.x)
            return p2;
        else
            return p3;
    }

    private Vector2 GetBiggest_Y(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        if (p1.y >= p2.y)
            if (p1.y >= p3.y)
                return p1;
            else
                return p3;
        else
        if (p2.y >= p3.y)
            return p2;
        else
            return p3;
    }

    private Vector2 GetMiddle_Y(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector2 biggestY = GetBiggest_Y(p1, p2, p3), smallestY = GetSmallest_Y(p1, p2, p3);
        if (p1 == biggestY)
        {
            if (p2 == smallestY)
                return p3; //p2 smallest, p3 middle, p1 biggest
            else if (p3 == smallestY)
                return p2; //p3 smallest, p2 middle, p1 biggest
        }
        else if (p2 == biggestY)
        {
            if (p1 == smallestY)
                return p3; //p1 smallest, p3 middle, p2 biggest
            else if (p3 == smallestY)
                return p1; //p3 smallest, p1 middle, p2 biggest
        }
        else if (p1 == smallestY)     
            return p2; //p1 smallest, p2 middle, p3 biggest
        else
            return p1; //p2 smallest, p1 middle, p3 biggest

        return new Vector2(999, 999);
    }

    private Vector2 GetSmallest_Y(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        if (p1.y < p2.y)
            if (p1.y < p3.y)
                return p1;
            else
                return p3;
        else
        if (p2.y < p3.y)
            return p2;
        else
            return p3;
    }

    private void Process_Triangle(Vector2 point1, Vector2 point2, Vector2 point3)
    {
        Vector3 p1 = point1, p2 = point2, p3 = point3;
        if (BackFaceCulling)
        {
            if (Vector3.Cross(p2 - p1, p3 - p2).z < 0)
            {
                process_Line(point1, point2);
                process_Line(point2, point3);
                process_Line(point3, point1);
            }
        }
        else
        {
            process_Line(point1, point2);
            process_Line(point2, point3);
            process_Line(point3, point1);
        }             
    }

    private void process_Line(Vector2 point1, Vector2 point2)
    {
        Vector2 start = point1, end = point2;
        if (Line_clip(ref start, ref end))
        {
            start = new Vector2((start.x + 1) * 255 / 2, (start.y + 1) * 255 / 2);
            end = new Vector2((end.x + 1) * 255 / 2, (end.y + 1) * 255 / 2);
            Vector2Int startInt = Vector2IntfromVector2(start), endInt = Vector2IntfromVector2(end);
            List<Vector2Int> list = Line_Rasterise(startInt, endInt);
            draw(list);
        }
    }

    private Vector2 Vector2fromVector3(Vector3 vector3)
    {
        Vector2 vector2 = new Vector2(vector3.x / vector3.z, vector3.y / vector3.z);
        return vector2;
    }

    private Vector2Int Vector2IntfromVector2(Vector2 vector2)
    {
        Vector2Int vector2Int = new Vector2Int((int) vector2.x, (int) vector2.y);
        return vector2Int;
    }

    private void draw(List<Vector2Int> list)
    {
        foreach (Vector2Int pixelcoords in list)
        {
            ourScreen.SetPixel(pixelcoords.x, pixelcoords.y, Color.red);
        }
        //ourScreen.Apply();
    }

    private List<Vector2Int> Line_Rasterise(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> output = new List<Vector2Int>();

        int dx = end.x - start.x;
        if (dx < 0)
            return Line_Rasterise(end, start);        
        int dy = end.y - start.y;
        if (dy < 0)
            return NegY(Line_Rasterise(NegY(start), NegY(end)));

        if (dy > dx)

            return SwapXY(Line_Rasterise(SwapXY(start), SwapXY(end)));

        int p = 2 * dy - dx;
        int twoDy = dy * 2; //Should always be positive
        int twoDyMinusDx = 2 * (dy - dx); // always < 0
        int y = start.y;

        for (int x = start.x; x <= end.x; x++)
        {
            output.Add(new Vector2Int(x, y));
            if (p >= 0)
            {
                y++;
                p += twoDyMinusDx;
            }
            else
                p += twoDy;
        }
        return output;
    }

    private bool Line_clip(ref Vector2 start, ref Vector2 end)
    {
        Outcode startOutcode = new Outcode(start), endOutcode = new Outcode(end), inViewPortOutcode = new Outcode();

        if ((startOutcode == inViewPortOutcode) && (endOutcode == inViewPortOutcode))
        {
            //print("Trivial Acceptance"); //Draw the whole line
            return true;
        }

        if ((startOutcode * endOutcode) != inViewPortOutcode)
        {
            //print("Trivial Reject"); //Line fully out of the screen (outcode has to have a common 1)
            return false;
        }

        if (startOutcode == inViewPortOutcode)        
            return Line_clip(ref end, ref start);

        if (startOutcode.up)
        {
            start = intercept(start, end, 0);

            return Line_clip(ref start, ref end);
        }

        if (startOutcode.down)
        {
            start = intercept(start, end, 1);
            return Line_clip(ref start, ref end);
        }

        if (startOutcode.left)
        {
            start = intercept(start, end, 2);
            return Line_clip(ref start, ref end);
        }

        if (startOutcode.right)
        {
            start = intercept(start, end, 3);
            return Line_clip(ref start, ref end);
        }

        return Line_clip(ref end, ref start);
    }

    Vector2 intercept(Vector2 start, Vector2 end, int edge)
    {
        float slope = (end.y - start.y) / (end.x - start.x);
        switch (edge)
        {
            case 0: //Up
                return new Vector2(start.x + (1 / slope) * (1 - start.y), 1);

            case 1: //Down
                return new Vector2(start.x + (1 / slope) * (-1 - start.y), -1);

            case 2: //Left
                return new Vector2(-1, start.y + (slope) * (-1 - start.x));

            default: //Right
                return new Vector2(1, start.y + (slope) * (1 - start.x));
        }
    }

    

    private List<Vector3> find_image_of(List<Vector3> vertices, Matrix4x4 transformations)
    {
        List<Vector3> new_image = new List<Vector3>();
        foreach (Vector3 v in vertices)
            new_image.Add(transformations * new Vector4(v.x, v.y, v.z, 1));

        return new_image;
    }
    
private Vector2Int NegY(Vector2Int v)
    {
        return new Vector2Int(v.x, -v.y);
    }

    private List<Vector2Int> NegY(List<Vector2Int> lists)
    {
        List<Vector2Int> output = new List<Vector2Int>();
        foreach (Vector2Int v in lists)
            output.Add(NegY(v));
        return output;
    }

    private Vector2Int SwapXY(Vector2Int end)
    {
        return new Vector2Int(end.y, end.x);
    }

    private List<Vector2Int> SwapXY(List<Vector2Int> lists)
    {
        List<Vector2Int> output = new List<Vector2Int>();
        foreach (Vector2Int v in lists)
            output.Add(SwapXY(v));
        return output;
    }


    public GameObject CreateUnityGameObject(Model model)
    {
        Mesh mesh = new Mesh();
        GameObject newGO = new GameObject();
        MeshFilter mesh_filter = newGO.AddComponent<MeshFilter>();
        MeshRenderer mesh_renderer = newGO.AddComponent<MeshRenderer>();

        List<Vector3> coords = new List<Vector3>();
        List<int> dummy_indices = new List<int>();
        List<Vector2> text_coords = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();

        for (int i = 0; i <= model._index_list.Count - 3; i = i + 3)
        {
            Vector3 normal_for_face = model._face_normals[i / 3];
            normal_for_face = new Vector3(normal_for_face.x, normal_for_face.y, -normal_for_face.z);
            coords.Add(model._vertices[model._index_list[i]]); dummy_indices.Add(i); text_coords.Add(model._texture_coordinates[model._texture_index_list[i]]); normals.Add(normal_for_face);
            coords.Add(model._vertices[model._index_list[i + 1]]); dummy_indices.Add(i + 1); text_coords.Add(model._texture_coordinates[model._texture_index_list[i + 1]]); normals.Add(normal_for_face);
            coords.Add(model._vertices[model._index_list[i + 2]]); dummy_indices.Add(i + 2); text_coords.Add(model._texture_coordinates[model._texture_index_list[i + 2]]); normals.Add(normal_for_face);
        }

        mesh.vertices = coords.ToArray();
        mesh.triangles = dummy_indices.ToArray();
        mesh.uv = text_coords.ToArray();
        mesh.normals = normals.ToArray(); ;

        mesh_filter.mesh = mesh;
        return newGO;
    }
}

    
