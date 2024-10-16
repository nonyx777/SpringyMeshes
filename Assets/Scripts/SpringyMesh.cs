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
    Vector3[] offsetVec;
    List<Cell> cells = new List<Cell>();

    public int size;
    public float space;
    public float centerDistance;
    public float diagonalDistance;
    public float stiffness;
    public float damping;
    public int iteration;

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
        Array.Resize(ref offsetVec, (size * size * size));

        assignToEmpty(ref vertices);
        assignToEmpty(ref prevPos);
        assignToEmpty(ref velocity);
        assignToEmpty(ref acceleration);
        assignToEmpty(ref offsetVec);

        createGrid(size, space);
        attachCells();

        int a = cells[0].a;
        Vector3 p = cells[0].position;
        centerDistance = Vector3.Magnitude(vertices[a] - p);

        int d = cells[0].d;
        diagonalDistance = Vector3.Magnitude(vertices[a] - vertices[d]);
    }

    // Update is called once per frame
    void Update()
    {
        assignToEmpty(ref offsetVec);
        assignToEmpty(ref acceleration);

        Vector3 mouse = Input.mousePosition;
        Ray castPoint = Camera.main.ScreenPointToRay(mouse);
        RaycastHit hit;
        if(Physics.Raycast(castPoint, out hit, Mathf.Infinity))
        {
            applyDistanceConstraint(hit.point, Time.deltaTime);
        }

        Vector3 gravity = new Vector3(0, -9.8f, 0); // Gravity constant (-9.81 m/s²)

        for(int i = 0; i < prevPos.Length; i++)
        {
            prevPos[i] = vertices[i];
        }

        // Apply gravity to each vertex
        // for (int i = 0; i < vertices.Length; i++)
        // {
        //     // Apply gravity to the acceleration of each vertex
        //     acceleration[i] = gravity;

        //     velocity[i] += acceleration[i] * Time.deltaTime;
        //     vertices[i] += velocity[i] * Time.deltaTime;
        // }

        float substep = Time.deltaTime;

        for(int i = 0; i < iteration; i++)
        {
            foreach(Cell cell in cells)
            {
                applyDistanceConstraint(cell.a, cell.b, substep, space);
                applyDistanceConstraint(cell.a, cell.c, substep, space);
                applyDistanceConstraint(cell.a, cell.e, substep, space);
                // applyDistanceConstraint(cell.a, cell.position, substep, centerDistance);

                applyDistanceConstraint(cell.b, cell.d, substep, space);
                applyDistanceConstraint(cell.b, cell.f, substep, space);
                // applyDistanceConstraint(cell.b, cell.position, substep, centerDistance);

                applyDistanceConstraint(cell.c, cell.d, substep, space);
                applyDistanceConstraint(cell.c, cell.g, substep, space);
                // applyDistanceConstraint(cell.c, cell.position, substep, space);

                applyDistanceConstraint(cell.d, cell.h, substep, space);
                // applyDistanceConstraint(cell.d, cell.position, substep, centerDistance);

                applyDistanceConstraint(cell.e, cell.g, substep, space);
                applyDistanceConstraint(cell.e, cell.f, substep, space);
                // applyDistanceConstraint(cell.e, cell.position, substep, centerDistance);

                applyDistanceConstraint(cell.f, cell.h, substep, space);
                // applyDistanceConstraint(cell.f, cell.position, substep, centerDistance);

                applyDistanceConstraint(cell.g, cell.h, substep, space);
                // applyDistanceConstraint(cell.g, cell.position, substep, centerDistance);

                // applyDistanceConstraint(cell.h, cell.position, substep, centerDistance);

                applyDistanceConstraint(cell.a, cell.h, substep, diagonalDistance);
                applyDistanceConstraint(cell.b, cell.g, substep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.f, substep, diagonalDistance);
                applyDistanceConstraint(cell.e, cell.d, substep, diagonalDistance);

                applyDistanceConstraint(cell.a, cell.d, substep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.b, substep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.e, substep, diagonalDistance);
                applyDistanceConstraint(cell.a, cell.g, substep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.h, substep, diagonalDistance);
                applyDistanceConstraint(cell.d, cell.g, substep, diagonalDistance);
                applyDistanceConstraint(cell.d, cell.f, substep, diagonalDistance);
                applyDistanceConstraint(cell.b, cell.h, substep, diagonalDistance);
                applyDistanceConstraint(cell.e, cell.h, substep, diagonalDistance);
                applyDistanceConstraint(cell.f, cell.g, substep, diagonalDistance);
                applyDistanceConstraint(cell.a, cell.f, substep, diagonalDistance);
                applyDistanceConstraint(cell.b, cell.e, substep, diagonalDistance);                
            }
            for(int j = 0; j < vertices.Length; j++)
            {
                vertices[j] += offsetVec[j] * Time.deltaTime;

                // if (vertices[j].y < -10F) // Ground level at y = -10
                // {
                //     vertices[j] = new Vector3(vertices[j].x, -10F, vertices[j].z);
                //     // prevPos[j] = new Vector3(vertices[j].x, -20F, vertices[j].z);
                //     // velocity[j] = new Vector3(velocity[j].x, -velocity[j].y, velocity[j].z); // Reflect velocity on ground hit
                // }
            }
        }

        // for(int i = 0; i < velocity.Length; i++)
        // {
        //     velocity[i] += (vertices[i] - prevPos[i]);
        // }


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

        offsetVec[0] -= 5 * forceVector * deltaTime;
    }

    void applyDistanceConstraint(int v1, int v2, float deltaTime, float distance_)
    {
        Vector3 p1 = vertices[v1];
        Vector3 p2 = vertices[v2];

        Vector3 direction = p2 - p1;
        float currDis = direction.magnitude;

        float force = 0.5F * (currDis - distance_);
        Vector3 forceVector = force * direction.normalized;

        // Vector3 relativeVelocity = velocity[v1] - velocity[v2];
        // Vector3 dampingForce = -damping * relativeVelocity;

        // Vector3 totalForce = forceVector + dampingForce;

        offsetVec[v1] += forceVector * deltaTime;
        offsetVec[v2] -= forceVector * deltaTime;
    }

    void applyDistanceConstraint(int v1, Vector3 p, float deltaTime, float distance_)
    {
        Vector3 p1 = vertices[v1];

        Vector3 direction = p - p1;
        float currDis = direction.magnitude;

        float force = currDis - distance_;
        Vector3 forceVector = force * direction.normalized;

        offsetVec[v1] += forceVector;
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
    void assignToEmpty(ref Boolean[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = false;
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
            array[i] += value;
        }
    }

    void assignToValue(ref Boolean[] array, Boolean value)
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