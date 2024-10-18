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
    public float tipDistance;
    public float diagonalDistance;
    [Range(1, 100)]
    public int iteration;
    public float timestep;
    public float pullStrength;
    Vector3 gravity = new Vector3(0, -9.8f, 0);


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
        int d = cells[0].d;
        diagonalDistance = Vector3.Magnitude(vertices[a] - vertices[d]);
        int top = getIndex(0, size - 1, 0, size);
        int bottom = getIndex(0, 0, 0, size);
        tipDistance = Vector3.Magnitude(vertices[top] - vertices[bottom]);
    }

    // Update is called once per frame
    void Update()
    {
        assignToEmpty(ref offsetVec);
        assignToEmpty(ref acceleration);

        Vector3 mouse = Input.mousePosition;
        Ray castPoint = Camera.main.ScreenPointToRay(mouse);
        RaycastHit hit;
        if (Physics.Raycast(castPoint, out hit, Mathf.Infinity))
        {
            applyDistanceConstraint(hit.point, timestep);
        }

        for (int i = 0; i < prevPos.Length; i++)
        {
            prevPos[i] = vertices[i];
        }

        // Apply gravity to each vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            acceleration[i] = gravity;
            velocity[i] += acceleration[i] * timestep;
            vertices[i] += velocity[i] * timestep;
        }

        for (int i = 0; i < iteration; i++)
        {
            foreach (Cell cell in cells)
            {
                //front face
                applyDistanceConstraint(cell.a, cell.b, timestep, space);
                applyDistanceConstraint(cell.a, cell.c, timestep, space);
                applyDistanceConstraint(cell.b, cell.d, timestep, space);
                applyDistanceConstraint(cell.c, cell.d, timestep, space);

                //back face
                applyDistanceConstraint(cell.e, cell.f, timestep, space);
                applyDistanceConstraint(cell.e, cell.g, timestep, space);
                applyDistanceConstraint(cell.f, cell.h, timestep, space);
                applyDistanceConstraint(cell.g, cell.h, timestep, space);

                //front face with back face
                applyDistanceConstraint(cell.a, cell.e, timestep, space);
                applyDistanceConstraint(cell.b, cell.f, timestep, space);
                applyDistanceConstraint(cell.c, cell.g, timestep, space);
                applyDistanceConstraint(cell.d, cell.h, timestep, space);

                //On the face diagonal
                applyDistanceConstraint(cell.a, cell.d, timestep, diagonalDistance);
                applyDistanceConstraint(cell.e, cell.h, timestep, diagonalDistance);
                applyDistanceConstraint(cell.a, cell.g, timestep, diagonalDistance);
                applyDistanceConstraint(cell.b, cell.h, timestep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.h, timestep, diagonalDistance);
                applyDistanceConstraint(cell.a, cell.f, timestep, diagonalDistance);

                //diagonal between front and back face
                applyDistanceConstraint(cell.a, cell.h, timestep, diagonalDistance);
                applyDistanceConstraint(cell.b, cell.g, timestep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.f, timestep, diagonalDistance);
                applyDistanceConstraint(cell.e, cell.d, timestep, diagonalDistance);

                //additional diagonal for a face
                applyDistanceConstraint(cell.c, cell.b, timestep, diagonalDistance);
                applyDistanceConstraint(cell.c, cell.e, timestep, diagonalDistance);
                applyDistanceConstraint(cell.d, cell.g, timestep, diagonalDistance);
                applyDistanceConstraint(cell.d, cell.f, timestep, diagonalDistance);
                applyDistanceConstraint(cell.f, cell.g, timestep, diagonalDistance);
                applyDistanceConstraint(cell.b, cell.e, timestep, diagonalDistance);
            }
            //applying constriant for the polar opposites on the y axis;
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int a = getIndex(x, 0, z, size);
                    int b = getIndex(x, size - 1, z, size);
                    applyDistanceConstraint(a, b, timestep, tipDistance);
                }
            }
            //applying constriant for the polar opposites on the x axis;
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    int a = getIndex(0, y, z, size);
                    int b = getIndex(size - 1, y, z, size);
                    applyDistanceConstraint(a, b, timestep, tipDistance);
                }
            }
            //applying constriant for the polar opposites on the z axis;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int a = getIndex(x, y, 0, size);
                    int b = getIndex(x, y, size - 1, size);
                    applyDistanceConstraint(a, b, timestep, tipDistance);
                }
            }

            //under the floor
            for (int j = 0; j < vertices.Length; j++)
            {
                if (vertices[j].y < -10F) // Ground level at y = -10
                {
                    vertices[j] = new Vector3(vertices[j].x, -10F, vertices[j].z);
                    prevPos[j] = new Vector3(vertices[j].x, -10F, vertices[j].z);
                }
            }

        }

        for (int i = 0; i < velocity.Length; i++)
        {
            velocity[i] = (vertices[i] - prevPos[i]);
        }


        foreach (Cell cell in cells)
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

        vertices[(size * size * size) - 1] -= pullStrength * forceVector * deltaTime;
    }

    void applyDistanceConstraint(int v1, int v2, float deltaTime, float distance_)
    {
        Vector3 p1 = vertices[v1];
        Vector3 p2 = vertices[v2];

        Vector3 direction = p2 - p1;
        float currDis = direction.magnitude;

        float force = 0.5F * (currDis - distance_);
        Vector3 forceVector = force * direction.normalized;

        vertices[v1] += forceVector * deltaTime;
        vertices[v2] -= forceVector * deltaTime;
    }

    void OnDrawGizmos()
    {
        if (vertices == null || vertices.Count() < 1)
            return;

        for (int i = 0; i < vertices.Count(); i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(vertices[i], 1);
        }
        foreach (Cell cell in cells)
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
        for (int k = 0; k < size; k++)
        {
            for (int j = 0; j < size; j++)
            {
                for (int i = 0; i < size; i++)
                {
                    index = getIndex(i, j, k, size);
                    vertices[index] = new Vector3(i, j, k) * space;
                }
            }
        }
    }

    void attachCells()
    {
        for (int k = 0; k < size - 1; k++)
        {
            for (int j = 0; j < size - 1; j++)
            {
                for (int i = 0; i < size - 1; i++)
                {
                    int a = getIndex(i, j, k, size);
                    int b = getIndex(i + 1, j, k, size);
                    int c = getIndex(i, j + 1, k, size);
                    int d = getIndex(i + 1, j + 1, k, size);
                    int e = getIndex(i, j, k + 1, size);
                    int f = getIndex(i + 1, j, k + 1, size);
                    int g = getIndex(i, j + 1, k + 1, size);
                    int h = getIndex(i + 1, j + 1, k + 1, size);

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
}