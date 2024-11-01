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
    Cell[] cells;
    public GameObject meshObject;
    Mesh mesh;
    Vector3[] originalMeshVertices;
    Vector3[] meshVertices;
    MeshGrid[] meshGrids;
    Vector3[] meshV;

    public int size;
    public float spaceX;
    public float spaceY;
    public float spaceZ;
    public float diagonalDistance1;
    public float diagonalDistance2;
    public float tipDistanceX;
    public float tipDistanceY;
    public float tipDistanceZ;
    public float tipDiagonalDistance; // for different face
    public float tipFaceDiagonalDistance;
    [Range(6, 100)]
    public int iteration;
    public float timestep;
    public float pullStrength;
    Vector3 gravity = new Vector3(0, -9.8f, 0);

    Vector3 minX, maxX, minY, maxY, minZ, maxZ;
    public float maxDistance, distanceX, distanceY, distanceZ;

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

    public class MeshGrid
    {
        public int meshVertex, cellIndex;
        float u, v, w; // values for triliniear interpolation

        public MeshGrid(int meshVertex, int cellIndex, float u, float v, float w)
        {
            this.meshVertex = meshVertex;
            this.cellIndex = cellIndex;
            this.u = 0F;
            this.v = 0F;
            this.w = 0F;
        }

        public void maintainPosition(ref Cell[] cells, ref Vector3[] vertices, ref Vector3[] meshVertices)
        {
            Vector3 a = vertices[cells[cellIndex].a];
            Vector3 b = vertices[cells[cellIndex].b];
            Vector3 c = vertices[cells[cellIndex].c];
            Vector3 d = vertices[cells[cellIndex].d];
            Vector3 e = vertices[cells[cellIndex].e];
            Vector3 f = vertices[cells[cellIndex].f];
            Vector3 g = vertices[cells[cellIndex].g];
            Vector3 h = vertices[cells[cellIndex].h];

            meshVertices[this.meshVertex] = (1 - u) * (1 - v) * (1 - w) * a + u * (1 - v) * (1 - w) * b + (1 - u) * v * (1 - w) * c + u * v * (1 - w) * d + (1 - u) * (1 - v) * w * e + u * (1 - v) * w * f + (1 - u) * v * w * g + u * v * w * h;
        }

        public void getRelativePosition(int cellIndex, ref Cell[] cells, ref Vector3[] vertices, ref Vector3[] meshVertices)
        {
            Vector3 a = vertices[cells[cellIndex].a];
            Vector3 b = vertices[cells[cellIndex].b];
            Vector3 c = vertices[cells[cellIndex].c];
            Vector3 e = vertices[cells[cellIndex].e];

            this.u = (meshVertices[this.meshVertex] - a).x / (b - a).x;
            this.v = (meshVertices[this.meshVertex] - a).y / (c - a).y;
            this.w = (meshVertices[this.meshVertex] - a).z / (e - a).z;
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
        Array.Resize(ref cells, ((size - 1) * (size - 1) * (size - 1)));

        assignToEmpty(ref vertices);
        assignToEmpty(ref prevPos);
        assignToEmpty(ref velocity);
        assignToEmpty(ref acceleration);
        assignToEmpty(ref offsetVec);

        mesh = meshObject.GetComponent<MeshFilter>().mesh;

        if (mesh != null)
        {
            Vector3[] localVertices = mesh.vertices;
            Array.Resize(ref meshVertices, localVertices.Length);
            Array.Resize(ref meshV, localVertices.Length);

            for (int i = 0; i < localVertices.Length; i++)
            {
                meshVertices[i] = meshObject.transform.TransformPoint(localVertices[i]);
            }
            Array.Resize(ref meshGrids, meshVertices.Length);
            originalMeshVertices = meshVertices;
        }
        else
        {
            Debug.LogError("Mesh or Object not assigned");
        }

        //getting min and max vertices of the mesh for each axes
        getMin(ref originalMeshVertices, ref minX, 0);
        getMin(ref originalMeshVertices, ref minY, 1);
        getMin(ref originalMeshVertices, ref minZ, 2);
        getMax(ref originalMeshVertices, ref maxX, 0);
        getMax(ref originalMeshVertices, ref maxY, 1);
        getMax(ref originalMeshVertices, ref maxZ, 2);

        getMaxDistance(minX, maxX, minY, maxY, minZ, maxZ, ref maxDistance, ref distanceX, ref distanceY, ref distanceZ);

        spaceX = distanceX / size;
        spaceY = distanceY / size;
        spaceZ = distanceZ / size;

        spaceX = Mathf.Round(spaceX * 100f) * 0.01f;
        spaceY = Mathf.Round(spaceY * 100f) * 0.01f;
        spaceZ = Mathf.Round(spaceZ * 100f) * 0.01f;

        createGrid(distanceX, distanceY, distanceZ, size, spaceX, spaceY, spaceZ);
        attachCells();

        //getting additional distance constraints for the points that make up the cells and grid
        int a = cells[0].a;
        int d = cells[0].d;
        int h = cells[0].h;
        diagonalDistance1 = Vector3.Magnitude(vertices[a] - vertices[d]);
        diagonalDistance2 = Vector3.Magnitude(vertices[a] - vertices[h]);
        diagonalDistance1 = Mathf.Round(diagonalDistance1 * 100f) * 0.01f;
        diagonalDistance2 = Mathf.Round(diagonalDistance2 * 100f) * 0.01f;

        int origin = getIndex(0, 0, 0, size);
        int top = getIndex(0, size - 1, 0, size);
        int right = getIndex(size - 1, 0, 0, size);
        int front = getIndex(0, 0, size - 1, size);
        int tipFaceDiagonal = getIndex(size - 1, size - 1, 0, size);
        int tipDiagonal = getIndex(size - 1, size - 1, size - 1, size); // for different faces
        tipDistanceX = Vector3.Magnitude(vertices[right] - vertices[origin]);
        tipDistanceY = Vector3.Magnitude(vertices[top] - vertices[origin]);
        tipDistanceZ = Vector3.Magnitude(vertices[front] - vertices[origin]);
        tipDistanceX = Mathf.Round(tipDistanceX * 100f) * 0.01f;
        tipDistanceY = Mathf.Round(tipDistanceY * 100f) * 0.01f;
        tipDistanceZ = Mathf.Round(tipDistanceZ * 100f) * 0.01f;
        
        tipDiagonalDistance = Vector3.Magnitude(vertices[tipDiagonal] - vertices[origin]);
        tipFaceDiagonalDistance = Vector3.Magnitude(vertices[tipFaceDiagonal] - vertices[origin]);
        tipDiagonalDistance = Mathf.Round(tipDiagonalDistance * 100f) * 0.01f;
        tipFaceDiagonalDistance = Mathf.Round(tipFaceDiagonalDistance * 100f) * 0.01f;


        attachMeshVertexToGridCell();
    }

    // Update is called once per frame
    void Update()
    {
        assignToEmpty(ref offsetVec);
        assignToEmpty(ref acceleration);

        // Vector3 mouse = Input.mousePosition;
        // Ray castPoint = Camera.main.ScreenPointToRay(mouse);
        // RaycastHit hit;
        // if (Physics.Raycast(castPoint, out hit, Mathf.Infinity))
        // {
        //     applyDistanceConstraint(hit.point, timestep);
        // }

        for (int i = 0; i < prevPos.Length; i++)
        {
            prevPos[i] = vertices[i];
        }

        // Apply gravity to each vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            acceleration[i] = gravity;
            // acceleration[i] = new Vector3(0f, 0f, 0f);
            velocity[i] += acceleration[i] * timestep;
            vertices[i] += velocity[i] * timestep;
        }

        for (int i = 0; i < iteration; i++)
        {
            foreach (Cell cell in cells)
            {
                //front face
                applyDistanceConstraint(cell.a, cell.b, timestep, spaceX);
                applyDistanceConstraint(cell.a, cell.c, timestep, spaceY);
                applyDistanceConstraint(cell.b, cell.d, timestep, spaceY);
                applyDistanceConstraint(cell.c, cell.d, timestep, spaceX);

                //back face
                applyDistanceConstraint(cell.e, cell.f, timestep, spaceX);
                applyDistanceConstraint(cell.e, cell.g, timestep, spaceY);
                applyDistanceConstraint(cell.f, cell.h, timestep, spaceY);
                applyDistanceConstraint(cell.g, cell.h, timestep, spaceX);

                //On the face diagonal
                // applyDistanceConstraint(cell.a, cell.d, timestep, diagonalDistance1);
                // applyDistanceConstraint(cell.e, cell.h, timestep, diagonalDistance1);
                // applyDistanceConstraint(cell.a, cell.g, timestep, diagonalDistance1);
                // applyDistanceConstraint(cell.b, cell.h, timestep, diagonalDistance1);
                // applyDistanceConstraint(cell.c, cell.h, timestep, diagonalDistance1);
                // applyDistanceConstraint(cell.a, cell.f, timestep, diagonalDistance1);
                //additional diagonal for a face
                applyDistanceConstraint(cell.c, cell.b, timestep, diagonalDistance1);
                applyDistanceConstraint(cell.c, cell.e, timestep, diagonalDistance1);
                applyDistanceConstraint(cell.d, cell.g, timestep, diagonalDistance1);
                applyDistanceConstraint(cell.d, cell.f, timestep, diagonalDistance1);
                applyDistanceConstraint(cell.f, cell.g, timestep, diagonalDistance1);
                applyDistanceConstraint(cell.b, cell.e, timestep, diagonalDistance1);

                //front face with back face
                applyDistanceConstraint(cell.a, cell.e, timestep, spaceZ);
                applyDistanceConstraint(cell.b, cell.f, timestep, spaceZ);
                applyDistanceConstraint(cell.c, cell.g, timestep, spaceZ);
                applyDistanceConstraint(cell.d, cell.h, timestep, spaceZ);
                //diagonal between front and back face
                applyDistanceConstraint(cell.a, cell.h, timestep, diagonalDistance2);
                applyDistanceConstraint(cell.b, cell.g, timestep, diagonalDistance2);
                applyDistanceConstraint(cell.c, cell.f, timestep, diagonalDistance2);
                applyDistanceConstraint(cell.e, cell.d, timestep, diagonalDistance2);
            }
            //applying constriant for the polar opposites on the x axis;
            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    int a = getIndex(0, y, z, size);
                    int b = getIndex(size - 1, y, z, size);
                    applyDistanceConstraint(a, b, timestep, tipDistanceX);
                }
            }
            //applying constriant for the polar opposites on the y axis;
            for (int z = 0; z < size; z++)
            {
                for (int x = 0; x < size; x++)
                {
                    int a = getIndex(x, 0, z, size);
                    int b = getIndex(x, size - 1, z, size);
                    applyDistanceConstraint(a, b, timestep, tipDistanceY);
                }
            }
            //applying constriant for the polar opposites on the z axis;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int a = getIndex(x, y, 0, size);
                    int b = getIndex(x, y, size - 1, size);
                    applyDistanceConstraint(a, b, timestep, tipDistanceZ);
                }
            }

            int frontBottomLeft = getIndex(0, 0, 0, size);
            int frontTopLeft = getIndex(0, size - 1, 0, size);
            int frontTopRight = getIndex(size - 1, size - 1, 0, size);
            int frontBottomRight = getIndex(size - 1, 0, 0, size);
            int backBottomLeft = getIndex(0, 0, size - 1, size);
            int backTopLeft = getIndex(0, size - 1, size - 1, size);
            int backTopRight = getIndex(size - 1, size - 1, size - 1, size);
            int backBottomRight = getIndex(size - 1, 0, size - 1, size);
            //On different faces diagonal
            applyDistanceConstraint(frontBottomLeft, backTopRight, timestep, tipDiagonalDistance);
            applyDistanceConstraint(frontBottomRight, backTopLeft, timestep, tipDiagonalDistance);
            applyDistanceConstraint(frontTopLeft, backBottomRight, timestep, tipDiagonalDistance);
            applyDistanceConstraint(frontTopRight, backBottomLeft, timestep, tipDiagonalDistance);
            //On the face diagonal
            applyDistanceConstraint(frontBottomLeft, frontTopRight, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(backBottomLeft, backTopRight, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontBottomLeft, backTopLeft, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontBottomRight, backTopRight, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontTopLeft, backTopRight, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontBottomLeft, backBottomRight, timestep, tipFaceDiagonalDistance);
            //additional diagonal for a face
            applyDistanceConstraint(frontTopLeft, frontBottomRight, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontTopLeft, backBottomLeft, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontTopRight, backTopLeft, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontTopRight, backBottomRight, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(backBottomRight, backTopLeft, timestep, tipFaceDiagonalDistance);
            applyDistanceConstraint(frontBottomRight, backBottomLeft, timestep, tipFaceDiagonalDistance);



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

        foreach (MeshGrid meshGrid in meshGrids)
        {
            meshGrid.maintainPosition(ref cells, ref vertices, ref meshVertices);
        }
        for (int i = 0; i < meshVertices.Length; i++)
        {
            meshV[i] = meshObject.transform.InverseTransformPoint(meshVertices[i]);
        }
        meshObject.GetComponent<MeshFilter>().mesh.vertices = meshV;
        meshObject.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        meshObject.GetComponent<MeshFilter>().mesh.RecalculateBounds();
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

    void applyDistanceConstraint(int v1, Vector3 target, float deltaTime, float distance_)
    {
        Vector3 p1 = vertices[v1];

        Vector3 direction = target - p1;
        float currDis = direction.magnitude;

        float force = currDis - distance_;
        Vector3 forceVector = force * direction.normalized;

        vertices[v1] += forceVector * deltaTime;
    }

    void OnDrawGizmos()
    {
        if (vertices == null || vertices.Count() < 1 || cells == null || cells.Count() < 1)
            return;

        for (int i = 0; i < vertices.Count(); i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(vertices[i], 0.05f);
        }

        // for (int i = 0; i < cells.Count(); i++)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(cells[i].position, 0.05f);
        // }

        // foreach(MeshGrid meshGrid in meshGrids)
        // {
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawSphere(meshVertices[meshGrid.meshVertex], 0.05f);
        // }
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

    void createGrid(float distanceX, float distanceY, float distanceZ, int size, float spaceX, float spaceY, float spaceZ)
    {
        int index;
        //components of the point position
        float k = minZ.z;
        float j = minY.y;
        float i = minX.x;
        //........
        float dz = k + distanceZ;
        float dy = j + distanceY;
        float dx = i + distanceX;
        //components of the point grid location
        int ki = 0;
        int ji = 0;
        int ii = 0;

        while (k < dz)
        {
            while (j < dy)
            {
                while (i < dx)
                {
                    //safe guard
                    ii = ii < size ? ii : size - 1;
                    ji = ji < size ? ji : size - 1;
                    ki = ki < size ? ki : size - 1;

                    index = getIndex(ii, ji, ki, size);
                    vertices[index] = new Vector3(i, j, k);

                    i += spaceX;
                    ii += 1;
                }
                i = minX.x;
                ii = 0;

                j += spaceY;
                ji += 1;
            }
            j = minY.y;
            ji = 0;

            k += spaceZ;
            ki += 1;
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
                    int index = getIndex(i, j, k, size - 1);
                    cells[index] = cell;
                }
            }
        }
    }
    void attachMeshVertexToGridCell()
    {
        for (int i = 0; i < meshVertices.Length; i++)
        {
            //get the index of the cell that the meshVertex is in
            Vector3 pos = meshVertices[i];
            pos = pos - new Vector3(minX.x, minY.y, minZ.z);
            Vector3 gridLoc = new Vector3(Mathf.Floor(pos.x / spaceX), Mathf.Floor(pos.y / spaceY), Mathf.Floor(pos.z / spaceZ));

            //safe guard
            gridLoc.x = gridLoc.x > size - 1 ? size - 1 : gridLoc.x < 0 ? 0 : gridLoc.x;
            gridLoc.y = gridLoc.y > size - 1 ? size - 1 : gridLoc.y < 0 ? 0 : gridLoc.y;
            gridLoc.z = gridLoc.z > size - 1 ? size - 1 : gridLoc.z < 0 ? 0 : gridLoc.z;

            int index = getIndex((int)gridLoc.x, (int)gridLoc.y, (int)gridLoc.z, size - 1);
            //get u, v and w
            MeshGrid meshGrid = new MeshGrid(i, index, 0f, 0f, 0f);
            if (index < 0 || index >= cells.Length)
            {
                Debug.LogError($"Invalid index: {index}, cells array size: {cells.Length}");
                return;
            }

            meshGrid.getRelativePosition(index, ref cells, ref vertices, ref meshVertices);
            meshGrids[i] = meshGrid;
        }
    }
    int getIndex(int i, int j, int k, int size)
    {
        return (i * size + j) * size + k;
    }

    void getMin(ref Vector3[] vertices, ref Vector3 min, int mode)
    {
        Vector3 tempMin = new Vector3(1000f, 1000f, 1000f);
        foreach (Vector3 v in vertices)
        {
            if (mode == 0)
            {
                if (v.x < tempMin.x)
                    tempMin = v;
            }
            if (mode == 1)
            {
                if (v.y < tempMin.y)
                    tempMin = v;
            }
            if (mode == 2)
            {
                if (v.z < tempMin.z)
                    tempMin = v;
            }
        }
        min = tempMin;
    }

    void getMax(ref Vector3[] vertices, ref Vector3 max, int mode)
    {
        Vector3 tempMax = new Vector3(-1000f, -1000f, -1000f);
        foreach (Vector3 v in vertices)
        {
            if (mode == 0)
            {
                if (v.x > tempMax.x)
                    tempMax = v;
            }
            if (mode == 1)
            {
                if (v.y > tempMax.y)
                    tempMax = v;
            }
            if (mode == 2)
            {
                if (v.z > tempMax.z)
                    tempMax = v;
            }
        }
        max = tempMax;
    }

    void getMaxDistance(Vector3 minX, Vector3 maxX, Vector3 minY, Vector3 maxY, Vector3 minZ, Vector3 maxZ, ref float maxDistance, ref float distanceX, ref float distanceY, ref float distanceZ)
    {
        distanceX = Vector3.Magnitude(maxX - minX) + 0.85f;
        distanceY = Vector3.Magnitude(maxY - minY) + 0.85f;
        distanceZ = Vector3.Magnitude(maxZ - minZ) + 0.85f;

        distanceX = Mathf.Round(distanceX * 100f) * 0.01f;
        distanceY = Mathf.Round(distanceY * 100f) * 0.01f;
        distanceZ = Mathf.Round(distanceZ * 100f) * 0.01f;

        float disX = Vector3.Magnitude(maxX - minX);
        float disY = Vector3.Magnitude(maxY - minY);
        float disZ = Vector3.Magnitude(maxZ - minZ);


        float tempMaxDis = disX >= disY ? disX : disY;
        maxDistance = tempMaxDis >= disZ ? tempMaxDis : disZ;
        maxDistance = Mathf.Ceil(maxDistance);
    }

}