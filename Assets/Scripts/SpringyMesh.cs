using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class SpringyMesh : MonoBehaviour
{
    Vector3[] vertices;
    List<Cell> cells = new List<Cell>();

    public int size;
    public float space;

    public struct Cell
    {
        //a -> d = lower
        //e -> h = upper
        public int a, b, c, d, e, f, g, h;
        public Vector3 position;

        public Cell(int a, int b, int c, int d, int e, int f, int g, int h, ref Vector3[] vertices)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
            this.e = e;
            this.f = f;
            this.g = g;
            this.h = h;

            this.position = (vertices[a] + vertices[b] + vertices[c] + vertices[d] + vertices[e] + vertices[f] + vertices[g] + vertices[h]) / 8;
        }

        void setPosition(ref Vector3[] vertices)
        {
            this.position = (vertices[a] + vertices[b] + vertices[c] + vertices[d] + vertices[e] + vertices[f] + vertices[g] + vertices[h]) / 8;
        }
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
    }
    void Start()
    {
        Array.Resize(ref vertices, (size * size * size));
        createGrid(size, space);
        attachCells();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void applyDistanceConstraint(Vector3 target, float deltaTime)
    {
        Vector3 direction = vertices[0] - target;
        float currDis = direction.magnitude;

        float force = currDis - 0f;
        Vector3 forceVector = force * direction.normalized;

        vertices[0] -= forceVector * deltaTime;
    }

    void OnDrawGizmos()
    {
        if(vertices == null || vertices.Count() < 1)
            return;

        for(int i = 0; i < vertices.Count(); i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(vertices[i], 1);
        }
        foreach(Cell cell in cells)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(cell.position, 0.5F);
        }
    }

    void assignToEmpty(ref float[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = 0f;
        }
    }
    void assignToEmpty(ref Vector3[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = new Vector3(0f, 0f, 0f);
        }
    }
    void assignToValue(ref float[] array, float value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
    }
    void assignToValue(ref Vector3[] array, Vector3 value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
    }

    void createGrid(int size, float space)
    {
        int index = 0;
        for(int k = 0; k < size; k++)
        {
            for(int j = 0; j < size; j++)
            {
                for(int i = 0; i < size; i++)
                {
                    index = getIndex(i, j, k, size);
                    vertices[index] = new Vector3(i, j, k) * space;
                }
            }
        }
    }

    void attachCells()
    {
        for(int k = 0; k < size-1; k++)
        {
            for(int j = 0; j < size-1; j++)
            {
                for(int i = 0; i < size-1; i++)
                {
                    int a = getIndex(i, j, k, size);
                    int b = getIndex(i+1, j, k, size);
                    int c = getIndex(i, j+1, k, size);
                    int d = getIndex(i+1, j+1, k, size);
                    int e = getIndex(i, j, k+1, size);
                    int f = getIndex(i+1, j, k+1, size);
                    int g = getIndex(i, j+1, k+1, size);
                    int h = getIndex(i+1, j+1, k+1, size);

                    Cell cell = new Cell(a, b, c, d, e, f, g, h, ref vertices);
                    cells.Add(cell);
                }
            }
        }
    }

    int getIndex(int i, int j, int k, int size)
    {
        return (i * size + j) * size + k;
    }

    // void manifoldMesh(ref int[] traingles, ref Vector3[] vertices)
    // {
    //     List<int> t1 = new List<int>();
    //     t1.Add(triangles[0]);
    //     t1.Add(traingles[1]);
    //     t1.Add(traingles[2]);
    //     for (int i = 3; i < triangles.Length; i += 3)
    //     {
    //         int v1 = triangles[i];
    //         int v2 = triangles[i + 1];
    //         int v3 = triangles[i + 2];

    //         foreach (int index in t1)
    //         {
    //             if (Vector3.Magnitude(vertices[index] - vertices[v1]) <= Mathf.Epsilon)
    //             {
    //                 v1 = index;
    //             }
    //             if (Vector3.Magnitude(vertices[index] - vertices[v2]) <= Mathf.Epsilon)
    //             {
    //                 v2 = index;
    //             }
    //             if (Vector3.Magnitude(vertices[index] - vertices[v3]) <= Mathf.Epsilon)
    //             {
    //                 v3 = index;
    //             }
    //         }

    //         t1.Add(v1);
    //         t1.Add(v2);
    //         t1.Add(v3);
    //     }
    //     triangles = t1.ToArray();
    //     int[] t2 = new int[triangles.Length];
    //     for (int i = 0; i < triangles.Length; i++)
    //     {
    //         t2[i] = triangles[i];
    //     }
    //     Array.Sort(t2);
    //     t2 = t2.Distinct().ToArray();
    //     Vector3[] v = new Vector3[t2.Length];
    //     for (int i = 0; i < v.Length; i++)
    //     {
    //         v[i] = vertices[t2[i]];
    //     }
    //     vertices = v;
    // }
}