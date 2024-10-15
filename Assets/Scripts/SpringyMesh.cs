using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class SpringyMesh : MonoBehaviour
{
    Vector3[] vertices;
    Vector3[] prevPos;
    Vector3[] velocity;
    Vector3[] acceleration;
    List<Cell> cells = new List<Cell>();

    public int size;
    public float space;
    public float distance;
    public float centerDistance;

    public class Cell
    {
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

        public void setPosition(ref Vector3[] vertices)
        {
            this.position = (vertices[this.a] + vertices[this.b] + vertices[this.c] + vertices[this.d] + vertices[this.e] + vertices[this.f] + vertices[this.g] + vertices[this.h]) / 8;
        }
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
    }
    void Start()
    {
        Array.Resize(ref vertices, (size * size * size));
        Array.Resize(ref prevPos, (size * size * size));
        Array.Resize(ref velocity, (size * size * size));
        Array.Resize(ref acceleration, (size * size * size));

        assignToEmpty(ref vertices);
        assignToEmpty(ref prevPos);
        assignToEmpty(ref velocity);
        assignToEmpty(ref acceleration);

        createGrid(size, space);
        attachCells();

        int a = cells[0].a;
        Vector3 p = cells[0].position;
        distance = Vector3.Magnitude(vertices[a] - p);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 force = Vector3.zero;
        foreach(Cell cell in cells)
        {
            velocity[cell.a] += force * Time.fixedDeltaTime;
            velocity[cell.b] += force * Time.fixedDeltaTime;
            velocity[cell.c] += force * Time.fixedDeltaTime;
            velocity[cell.d] += force * Time.fixedDeltaTime;
            velocity[cell.e] += force * Time.fixedDeltaTime;
            velocity[cell.f] += force * Time.fixedDeltaTime;
            velocity[cell.g] += force * Time.fixedDeltaTime;
            velocity[cell.h] += force * Time.fixedDeltaTime;
        }

        foreach(Cell cell in cells)
        {
            prevPos[cell.a] = vertices[cell.a];
            prevPos[cell.b] = vertices[cell.b];
            prevPos[cell.c] = vertices[cell.c];
            prevPos[cell.d] = vertices[cell.d];
            prevPos[cell.e] = vertices[cell.e];
            prevPos[cell.f] = vertices[cell.f];
            prevPos[cell.g] = vertices[cell.g];
            prevPos[cell.h] = vertices[cell.h];
        }

        foreach(Cell cell in cells)
        {
            vertices[cell.a] += velocity[cell.a] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.b] += velocity[cell.b] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.c] += velocity[cell.c] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.d] += velocity[cell.d] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.e] += velocity[cell.e] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.f] += velocity[cell.f] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.g] += velocity[cell.g] * 0.1F * Time.fixedDeltaTime;
            vertices[cell.h] += velocity[cell.h] * 0.1F * Time.fixedDeltaTime;
        }

        for(int i = 0; i < 1; i++)
        {

            foreach(Cell cell in cells)
            {
                applyDistanceConstraint(cell.a, cell.b, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.a, cell.c, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.a, cell.e, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.a, cell.position, Time.fixedDeltaTime, distance);

                applyDistanceConstraint(cell.b, cell.d, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.b, cell.f, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.b, cell.position, Time.fixedDeltaTime, distance);

                applyDistanceConstraint(cell.c, cell.d, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.c, cell.g, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.c, cell.position, Time.fixedDeltaTime, space);

                applyDistanceConstraint(cell.d, cell.h, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.d, cell.position, Time.fixedDeltaTime, distance);

                applyDistanceConstraint(cell.e, cell.g, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.e, cell.f, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.e, cell.position, Time.fixedDeltaTime, distance);

                applyDistanceConstraint(cell.f, cell.h, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.f, cell.position, Time.fixedDeltaTime, distance);

                applyDistanceConstraint(cell.g, cell.h, Time.fixedDeltaTime, space);
                applyDistanceConstraint(cell.g, cell.position, Time.fixedDeltaTime, distance);
            }
        }

        foreach(Cell cell in cells)
        {
            velocity[cell.a] = (vertices[cell.a] - prevPos[cell.a]) / Time.fixedDeltaTime;
            velocity[cell.b] = (vertices[cell.b] - prevPos[cell.b]) / Time.fixedDeltaTime;
            velocity[cell.c] = (vertices[cell.c] - prevPos[cell.c]) / Time.fixedDeltaTime;
            velocity[cell.d] = (vertices[cell.d] - prevPos[cell.d]) / Time.fixedDeltaTime;
            velocity[cell.e] = (vertices[cell.e] - prevPos[cell.e]) / Time.fixedDeltaTime;
            velocity[cell.f] = (vertices[cell.f] - prevPos[cell.f]) / Time.fixedDeltaTime;
            velocity[cell.g] = (vertices[cell.g] - prevPos[cell.g]) / Time.fixedDeltaTime;
            velocity[cell.h] = (vertices[cell.h] - prevPos[cell.h]) / Time.fixedDeltaTime;
        }

        foreach(Cell cell in cells)
        {
            cell.setPosition(ref vertices);
        }
    }

    void applyDistanceConstraint(Vector3 target, float deltaTime)
    {
        Vector3 direction = vertices[0] - target;
        float currDis = direction.magnitude;

        float force = currDis - 0f;
        Vector3 forceVector = force * direction.normalized;

        vertices[0] -= forceVector * deltaTime;
    }

    void applyDistanceConstraint(int v1, int v2, float deltaTime, float distance_)
    {
        Vector3 p1 = vertices[v1];
        Vector3 p2 = vertices[v2];

        Vector3 direction = p2 - p1;
        float currDis = direction.magnitude;

        float force = currDis - distance_;
        Vector3 forceVector = force * direction.normalized;

        vertices[v1] += (0.5F * forceVector) * deltaTime;
        vertices[v2] -= (0.5F * forceVector) * deltaTime;
    }

    void applyDistanceConstraint(int v1, Vector3 p, float deltaTime, float distance_)
    {
        Vector3 p1 = vertices[v1];

        Vector3 direction = p - p1;
        float currDis = direction.magnitude;

        float force = currDis - distance_;
        Vector3 forceVector = force * direction.normalized;

        vertices[v1] += forceVector * deltaTime;
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