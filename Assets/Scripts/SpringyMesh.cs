using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class SpringyMesh : MonoBehaviour
{
    Vector3[] vertices;
    int[] triangles;
    Vector3[] velocity;
    float[] mass;
    Vector3[] acceleration;
    [Range(0f, 20f)]
    public float k; // torsional spring coefficient
    [Range(0f, 20f)]
    public float d; // torsional damping coefficient
    [Range(1, 15)]
    public int numberOfIterations = 5;
    public Camera mainCamera;
    public float distanceFromCamera = 20f;
    Mesh mesh;
    Dictionary<Edge, List<int>> edgeToTriangles_original = new Dictionary<Edge, List<int>>();
    Dictionary<Edge, List<int>> edgeToTriangle = new Dictionary<Edge, List<int>>();
    Dictionary<Edge, float> defaultAngleBtnTriangles = new Dictionary<Edge, float>();
    Dictionary<Edge, float> restDistanceOnEdge = new Dictionary<Edge, float>();
    Dictionary<Edge, List<float>> restDistanceNotOnEdge = new Dictionary<Edge, List<float>>();
    Dictionary<int, Vector3> normalDict = new Dictionary<int, Vector3>();
    Vector3 targetPosition;
    Vector3 totalForceV1 = Vector3.zero;
    Vector3 totalForceV2 = Vector3.zero;
    Vector3 totalForceVe1 = Vector3.zero;
    Vector3 totalForceVe2 = Vector3.zero;
    void Awake()
    {
        Application.targetFrameRate = 60;
    }
    void Start()
    {
        // mesh = createMesh();
        // GetComponent<MeshFilter>().mesh = mesh;
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
        Array.Resize(ref velocity, vertices.Length);
        Array.Resize(ref mass, vertices.Length);
        Array.Resize(ref acceleration, vertices.Length);
        manifoldMesh(ref triangles, ref vertices);
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        GetComponent<MeshFilter>().mesh = mesh;
        Array.Resize(ref velocity, vertices.Length);
        Array.Resize(ref mass, vertices.Length);
        Array.Resize(ref acceleration, vertices.Length);

        // for (int i = 0; i < triangles.Length; i += 3)
        // {
        //     Debug.Log($"T{i} -> {triangles[i]}, {triangles[i+1]}, {triangles[i+2]}");
        // }

        // foreach (Vector3 v in vertices)
        // {
        //     Debug.Log(v);
        // }

        assignToValue(ref mass, 1f);
        assignToEmpty(ref velocity);
        assignToValue(ref acceleration, Vector3.zero);

        GetEdgesAndConnectedTriangles(triangles);
        GetEdgesAndAdjecentTriangles();

        vertices[1] += new Vector3(0, 0, -0.05f);
        vertices[3] += new Vector3(0, 0, 0.05f);
    }

    // Update is called once per frame
    void Update()
    {
        assignToValue(ref acceleration, Vector3.down);

        if (Input.GetMouseButton(0))
        {
            assignToValue(ref acceleration, Vector3.zero);

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            targetPosition = ray.origin + ray.direction * distanceFromCamera;

            applyDistanceConstraint(targetPosition, Time.deltaTime);
        }

        foreach (var entry in edgeToTriangle)
        {
            totalForceV1 = Vector3.zero;
            totalForceV2 = Vector3.zero;
            totalForceVe1 = Vector3.zero;
            totalForceVe2 = Vector3.zero;

            //get vertices of the triangle
            Edge edge = entry.Key;
            List<int> triangles = entry.Value;

            int v1 = triangles[0];
            int v2 = triangles[1];

            velocity[v1] += acceleration[v1] * Time.deltaTime;
            velocity[v2] += acceleration[v2] * Time.deltaTime;
            velocity[edge.v1] += acceleration[edge.v1] * Time.deltaTime;
            velocity[edge.v2] += acceleration[edge.v2] * Time.deltaTime;

            Vector3 pv1 = vertices[v1];
            Vector3 pv2 = vertices[v2];
            Vector3 pve1 = vertices[edge.v1];
            Vector3 pve2 = vertices[edge.v2];

            // velocity[v1] = Vector3.ClampMagnitude(velocity[v1], 1f);
            // velocity[v2] = Vector3.ClampMagnitude(velocity[v2], 1f);
            // velocity[edge.v1] = Vector3.ClampMagnitude(velocity[edge.v1], 1f);
            // velocity[edge.v2] = Vector3.ClampMagnitude(velocity[edge.v2], 1f);

            vertices[v1] += velocity[v1] * Time.deltaTime;
            vertices[v2] += velocity[v2] * Time.deltaTime;
            vertices[edge.v1] += velocity[edge.v1] * Time.deltaTime;
            vertices[edge.v2] += velocity[edge.v2] * Time.deltaTime;


            computeTorque(v1, v2, edge.v1, edge.v2, edge, Time.deltaTime);
            for (int i = 0; i < numberOfIterations; i++)
            {
                applyDistanceConstraint(edge, Time.deltaTime);
            }

            blockMesh(edge, ref pv1, ref pv2, ref pve1, ref pve2);

            velocity[v1] = (vertices[v1] - pv1) / Time.deltaTime;
            velocity[v2] = (vertices[v2] - pv2) / Time.deltaTime;
            velocity[edge.v1] = (vertices[edge.v1] - pve1) / Time.deltaTime;
            velocity[edge.v2] = (vertices[edge.v2] - pve2) / Time.deltaTime;
        }

        mesh.vertices = vertices;
    }

    void FixedUpdate()
    {

    }

    void updateVertexPosition(int index, float deltaTime)
    {
        velocity[index] += acceleration[index] * deltaTime;
        vertices[index] += velocity[index] * deltaTime;
    }

    void applyDistanceConstraint(Edge edge, float deltaTime)
    {
        List<int> triangles = edgeToTriangle[edge];
        List<float> constraints = restDistanceNotOnEdge[edge];
        int v1 = triangles[0];
        int v2 = triangles[1];

        //edge.v2 <-- edge.v1
        Vector3 direction = vertices[edge.v2] - vertices[edge.v1];
        float currDis = direction.magnitude;
        float force = 0.5f * (currDis - restDistanceOnEdge[edge]);
        Vector3 forceVector = force * direction.normalized;
        totalForceVe1 += forceVector * deltaTime;
        totalForceVe2 -= forceVector * deltaTime;
        //v2 <-- v1
        direction = vertices[v2] - vertices[v1];
        currDis = direction.magnitude;
        force = 0.5f * (currDis - constraints[0]);
        forceVector = force * direction.normalized;
        totalForceV1 += forceVector;
        totalForceV2 -= forceVector;
        // //v1 <-- edge.v1
        // direction = vertices[v1] - vertices[edge.v1];
        // currDis = direction.magnitude;
        // force = 0.5f * (currDis - constraints[1]);
        // forceVector = force * direction.normalized;
        // totalForceV1 -= forceVector;
        // totalForceVe1 += forceVector;
        // //v1 <-- edge.v2
        // direction = vertices[v1] - vertices[edge.v2];
        // currDis = direction.magnitude;
        // force = 0.5f * (currDis - constraints[2]);
        // forceVector = force * direction.normalized;
        // totalForceV1 -= forceVector;
        // totalForceVe2 += forceVector;
        // //v2 <-- edge.v1
        // direction = vertices[v2] - vertices[edge.v1];
        // currDis = direction.magnitude;
        // force = 0.5f * (currDis - constraints[3]);
        // forceVector = force * direction.normalized;
        // totalForceV2 -= forceVector;
        // totalForceVe1 += forceVector;
        // //v2 <-- edge.v2
        // direction = vertices[v2] - vertices[edge.v2];
        // currDis = direction.magnitude;
        // force = 0.5f * (currDis - constraints[4]);
        // forceVector = force * direction.normalized;
        // totalForceV2 -= forceVector;
        // totalForceVe2 += forceVector;

        vertices[v1] += totalForceV1 * deltaTime;
        vertices[v2] += totalForceV2 * deltaTime;
        vertices[edge.v1] += totalForceVe1 * deltaTime;
        vertices[edge.v2] += totalForceVe2 * deltaTime;

        totalForceV1 = Vector3.zero;
        totalForceV2 = Vector3.zero;
        totalForceVe1 = Vector3.zero;
        totalForceVe2 = Vector3.zero;
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
        if (vertices == null || vertices.Length < 3)
            return;

        foreach (var entry in edgeToTriangle)
        {
            Gizmos.color = Color.red;
            Edge edge = entry.Key;
            Gizmos.DrawLine(vertices[edge.v1], vertices[edge.v2]);
        }

        Gizmos.color = Color.green;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        foreach (var entry in normalDict)
        {
            Gizmos.color = Color.blue;
            int v = entry.Key;
            Vector3 dir = entry.Value;
            Gizmos.DrawLine(vertices[v], vertices[v] + dir * 2f);
        }
    }

    public struct Edge
    {
        public int v1, v2;

        public Edge(int vertexIndex1, int vertexIndex2)
        {
            v1 = Mathf.Min(vertexIndex1, vertexIndex2);
            v2 = Mathf.Max(vertexIndex1, vertexIndex2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge))
                return false;

            Edge other = (Edge)obj;
            return v1 == other.v1 && v2 == other.v2;
        }

        public override int GetHashCode()
        {
            return v1.GetHashCode() ^ v2.GetHashCode();
        }
    }

    void GetEdgesAndConnectedTriangles(int[] t)
    {
        for (int i = 0; i < t.Length; i += 3)
        {
            int v1 = t[i];
            int v2 = t[i + 1];
            int v3 = t[i + 2];

            Edge edge1 = new Edge(v1, v2);
            Edge edge2 = new Edge(v2, v3);
            Edge edge3 = new Edge(v3, v1);

            getDistanceOfEdge(edge1);
            getDistanceOfEdge(edge2);
            getDistanceOfEdge(edge3);

            AddTriangleVertexToEdge(edge1, v3);
            AddTriangleVertexToEdge(edge2, v1);
            AddTriangleVertexToEdge(edge3, v2);
        }
    }
    void GetEdgesAndAdjecentTriangles()
    {
        foreach (var entry in edgeToTriangles_original)
        {
            Edge edge = entry.Key;
            List<int> triangles = entry.Value;

            if (triangles.Count > 1)
            {
                int v1 = triangles[0];
                int v2 = triangles[1];
                float restDis = Vector3.Magnitude(vertices[v2] - vertices[v1]);
                AddConstraintDistanceToVeretex(edge, restDis);
                restDis = Vector3.Magnitude(vertices[v1] - vertices[edge.v1]);
                AddConstraintDistanceToVeretex(edge, restDis);
                restDis = Vector3.Magnitude(vertices[v1] - vertices[edge.v2]);
                AddConstraintDistanceToVeretex(edge, restDis);
                restDis = Vector3.Magnitude(vertices[v2] - vertices[edge.v1]);
                AddConstraintDistanceToVeretex(edge, restDis);
                restDis = Vector3.Magnitude(vertices[v2] - vertices[edge.v2]);
                AddConstraintDistanceToVeretex(edge, restDis);

                edgeToTriangle[edge] = triangles;
                AddAngleBtnTriangles(edge, triangles);
            }
        }
    }

    void AddTriangleVertexToEdge(Edge edge, int index)
    {
        if (!edgeToTriangles_original.ContainsKey(edge))
        {
            edgeToTriangles_original[edge] = new List<int>();
        }

        edgeToTriangles_original[edge].Add(index);
    }

    void AddConstraintDistanceToVeretex(Edge edge, float distance)
    {
        if (!restDistanceNotOnEdge.ContainsKey(edge))
        {
            restDistanceNotOnEdge[edge] = new List<float>();
        }
        restDistanceNotOnEdge[edge].Add(distance);
    }

    void AddAngleBtnTriangles(Edge edge, List<int> triangles)
    {
        //assign the vertices which are not on the edge
        int v1 = triangles[0];
        int v2 = triangles[1];

        //get normal
        Vector3 n1 = Vector3.Normalize(Vector3.Cross(vertices[v1] - vertices[edge.v1], vertices[edge.v2] - vertices[edge.v1]));
        Vector3 n2 = Vector3.Normalize(Vector3.Cross(vertices[v2] - vertices[edge.v2], vertices[edge.v1] - vertices[edge.v2]));
        //rotation axis
        Vector3 h = Vector3.Normalize(vertices[edge.v2] - vertices[edge.v1]);
        //compute angle
        float c = Vector3.Dot(n1, n2);
        float s = Vector3.Dot(Vector3.Cross(n1, n2), h);
        float restAngle = Mathf.Atan2(s, c);
        defaultAngleBtnTriangles[edge] = restAngle;
    }

    void getDistanceOfEdge(Edge edge)
    {
        float distance = Vector3.Magnitude(vertices[edge.v2] - vertices[edge.v1]);
        restDistanceOnEdge[edge] = distance;
    }

    void computeTorque(int v1, int v2, int ve1, int ve2, Edge edge, float deltaTime)
    {
        Vector3 n1 = Vector3.Normalize(Vector3.Cross(vertices[v1] - vertices[ve1], vertices[ve2] - vertices[ve1]));
        Vector3 n2 = Vector3.Normalize(Vector3.Cross(vertices[v2] - vertices[ve2], vertices[ve1] - vertices[ve2]));

        normalDict[v1] = n1;
        normalDict[v2] = n2;

        //t = r x F
        //h = v2-v1 / || v2 - v1 ||
        Vector3 h = Vector3.Normalize(vertices[ve2] - vertices[ve1]);
        //r = v0 - ((v0 - v1) . h) * h
        Vector3 r1 = (vertices[v1] - vertices[ve1]) - (Vector3.Dot((vertices[v1] - vertices[ve1]), h)) * h;
        Vector3 r2 = (vertices[v2] - vertices[ve2]) - (Vector3.Dot((vertices[v2] - vertices[ve2]), h)) * h;
        //costheta = n1 . n2
        float c = Vector3.Dot(n1, n2);
        //sintheta = (n1 x n2) . h
        float s = Vector3.Dot(Vector3.Cross(n1, n2), h);
        // angle = atan2(sintheta, costheta)
        float angle = Mathf.Atan2(s, c);
        //t = k + d
        //thetavel = vel . n / || r ||
        float thetaVel1 = Vector3.Dot(velocity[v1], n1) / Vector3.Magnitude(r1);
        float thetaVel2 = Vector3.Dot(velocity[v2], n2) / Vector3.Magnitude(r2);
        //t = (k(theta - thetarest) - d (thetorsional tavel0 + thetavel3)
        Vector3 torque = (k * (angle - defaultAngleBtnTriangles[edge]) - d * -(thetaVel1 + thetaVel2)) * h;

        //f = (t.h / || r ||) * n
        //force for vertices not on the edge
        Vector3 fv1 = -(Vector3.Dot(torque, h) / Vector3.Magnitude(r1)) * n1;
        Vector3 fv2 = (Vector3.Dot(torque, h) / Vector3.Magnitude(r2)) * n2;
        //distance of vertices v1 and v2 on the hinge
        float dv1 = Vector3.Dot((vertices[v1] - vertices[ve1]), h);
        float dv2 = Vector3.Dot((vertices[v2] - vertices[ve2]), h);
        //force for vertices on the edge
        Vector3 fve2 = -(dv1 * fv1 + dv2 * fv2) / Vector3.Magnitude(vertices[ve2] - vertices[ve1]);
        Vector3 fve1 = (fv1 + fv2 + fve2);
        Vector3 totalForce = fv1 + fv2 + fve2 + fve1;
        Vector3 correction = totalForce / 4;
        fv1 -= correction;
        fv2 -= correction;
        fve2 -= correction;
        fve1 -= correction;

        totalForceV1 += (fv1 / mass[v1]) * deltaTime;
        totalForceV2 += (fv2 / mass[v2]) * deltaTime;
        totalForceVe1 += (fve1 / mass[ve1]) * deltaTime;
        totalForceVe2 += (fve2 / mass[ve2]) * deltaTime;

        // vertices[v1] += (fv1 / mass[v1]) * deltaTime;
        // vertices[v2] += (fv2 / mass[v2]) * deltaTime;
        // vertices[ve1] += (fve1 / mass[ve1]) * deltaTime;
        // vertices[ve2] += (fve2 / mass[ve2]) * deltaTime;

        List<float> constraints = restDistanceNotOnEdge[edge];


        //v1 <-- edge.v1
        Vector3 direction = vertices[v1] - vertices[edge.v1];
        float currDis = direction.magnitude;
        float force = 0.5f * (currDis - constraints[1]);
        Vector3 forceVector = force * direction.normalized;
        totalForceV1 -= forceVector;
        totalForceVe1 += forceVector;
        //v1 <-- edge.v2
        direction = vertices[v1] - vertices[edge.v2];
        currDis = direction.magnitude;
        force = 0.5f * (currDis - constraints[2]);
        forceVector = force * direction.normalized;
        totalForceV1 -= forceVector;
        totalForceVe2 += forceVector;
        //v2 <-- edge.v1
        direction = vertices[v2] - vertices[edge.v1];
        currDis = direction.magnitude;
        force = 0.5f * (currDis - constraints[3]);
        forceVector = force * direction.normalized;
        totalForceV2 -= forceVector;
        totalForceVe1 += forceVector;
        //v2 <-- edge.v2
        direction = vertices[v2] - vertices[edge.v2];
        currDis = direction.magnitude;
        force = 0.5f * (currDis - constraints[4]);
        forceVector = force * direction.normalized;
        totalForceV2 -= forceVector;
        totalForceVe2 += forceVector;

        vertices[v1] += totalForceV1 * deltaTime;
        vertices[v2] += totalForceV2 * deltaTime;
        vertices[ve1] += totalForceVe1 * deltaTime;
        vertices[ve2] += totalForceVe2 * deltaTime;

        totalForceV1 = Vector3.zero;
        totalForceV2 = Vector3.zero;
        totalForceVe1 = Vector3.zero;
        totalForceVe2 = Vector3.zero;
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

    void blockMesh(Edge edge, ref Vector3 pv1, ref Vector3 pv2, ref Vector3 pve1, ref Vector3 pve2)
    {
        List<int> triangles = edgeToTriangle[edge];
        int v1 = triangles[0];
        int v2 = triangles[1];

        if (vertices[edge.v1].y < -10f || vertices[edge.v2].y < -10f)
        {
            if (vertices[edge.v1].y < -10f)
            {
                vertices[edge.v1] = new Vector3(vertices[edge.v1].x, -10f, vertices[edge.v1].z);
                pve1 = new Vector3(vertices[edge.v1].x, -10f, vertices[edge.v1].z);
            }

            if (vertices[edge.v2].y < -10f)
            {
                vertices[edge.v2] = new Vector3(vertices[edge.v2].x, -10f, vertices[edge.v2].z);
                pve2 = new Vector3(vertices[edge.v2].x, -10f, vertices[edge.v2].z);
            }
        }

        if (vertices[v1].y < -10f || vertices[v2].y < -10f)
        {
            if (vertices[v1].y < -10f)
            {
                vertices[v1] = new Vector3(vertices[v1].x, -10f, vertices[v1].z);
                pv1 = new Vector3(vertices[v1].x, -10f, vertices[v1].z);
            }

            if (vertices[v2].y < -10f)
            {
                vertices[v2] = new Vector3(vertices[v2].x, -10f, vertices[v2].z);
                pv2 = new Vector3(vertices[v2].x, -10f, vertices[v2].z);
            }
        }
        // x-axis
        if (vertices[edge.v1].x < -10f || vertices[edge.v2].x < -10f)
        {
            if (vertices[edge.v1].x < -10f)
            {
                vertices[edge.v1] = new Vector3(-10f, vertices[edge.v1].y, vertices[edge.v1].z);
                pve1 = new Vector3(-10f, vertices[edge.v1].y, vertices[edge.v1].z);
            }

            if (vertices[edge.v2].x < -10f)
            {
                vertices[edge.v2] = new Vector3(-10f, vertices[edge.v2].y, vertices[edge.v2].z);
                pve2 = new Vector3(-10f, vertices[edge.v2].y, vertices[edge.v2].z);
            }
        }

        if (vertices[v1].x < -10f || vertices[v2].x < -10f)
        {
            if (vertices[v1].x < -10f)
            {
                vertices[v1] = new Vector3(-10f, vertices[v1].y, vertices[v1].z);
                pv1 = new Vector3(-10f, vertices[v1].y, vertices[v1].z);
            }

            if (vertices[v2].x < -10f)
            {
                vertices[v2] = new Vector3(-10f, vertices[v2].y, vertices[v2].z);
                pv2 = new Vector3(-10f, vertices[v2].y, vertices[v2].z);
            }
        }

        if (vertices[edge.v1].x > 10f || vertices[edge.v2].x > 10f)
        {
            if (vertices[edge.v1].x > 10f)
            {
                vertices[edge.v1] = new Vector3(10f, vertices[edge.v1].y, vertices[edge.v1].z);
                pve1 = new Vector3(10f, vertices[edge.v1].y, vertices[edge.v1].z);
            }

            if (vertices[edge.v2].x > 10f)
            {
                vertices[edge.v2] = new Vector3(10f, vertices[edge.v2].y, vertices[edge.v2].z);
                pve2 = new Vector3(10f, vertices[edge.v2].y, vertices[edge.v2].z);
            }
        }

        if (vertices[v1].x > 10f || vertices[v2].x > 10f)
        {
            if (vertices[v1].x > 10f)
            {
                vertices[v1] = new Vector3(10f, vertices[v1].y, vertices[v1].z);
                pv1 = new Vector3(10f, vertices[v1].y, vertices[v1].z);
            }

            if (vertices[v2].x > 10f)
            {
                vertices[v2] = new Vector3(10f, vertices[v2].y, vertices[v2].z);
                pv2 = new Vector3(10f, vertices[v2].y, vertices[v2].z);
            }
        }
    }

    Mesh createMesh()
    {
        Mesh cubeMesh = new Mesh();

        // Define the 8 vertices of the cube
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-1, -1, -1), // Vertex 0
            new Vector3( 1, -1, -1), // Vertex 1
            new Vector3( 1,  1, -1), // Vertex 2
            new Vector3(-1,  1, -1), // Vertex 3
            new Vector3(-1, -1,  1), // Vertex 4
            new Vector3( 1, -1,  1), // Vertex 5
            new Vector3( 1,  1,  1), // Vertex 6
            new Vector3(-1,  1,  1)  // Vertex 7
        };

        // Define triangles (each face of the cube has two triangles, 12 in total)
        int[] triangles = new int[]
        {
            // Front face
            0, 2, 1,
            0, 3, 2,

            // Back face
            5, 6, 4,
            4, 6, 7,

            // Left face
            4, 7, 0,
            0, 7, 3,

            // Right face
            1, 2, 5,
            5, 2, 6,

            // Top face
            3, 7, 2,
            2, 7, 6,

            // Bottom face
            0, 1, 4,
            4, 1, 5
        };

        // Assign vertices and triangles to the cubeMesh
        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;

        return cubeMesh;
    }

    void manifoldMesh(ref int[] traingles, ref Vector3[] vertices)
    {
        List<int> t1 = new List<int>();
        t1.Add(triangles[0]);
        t1.Add(traingles[1]);
        t1.Add(traingles[2]);
        for (int i = 3; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            foreach (int index in t1)
            {
                if (Vector3.Magnitude(vertices[index] - vertices[v1]) <= Mathf.Epsilon)
                {
                    v1 = index;
                }
                if (Vector3.Magnitude(vertices[index] - vertices[v2]) <= Mathf.Epsilon)
                {
                    v2 = index;
                }
                if (Vector3.Magnitude(vertices[index] - vertices[v3]) <= Mathf.Epsilon)
                {
                    v3 = index;
                }
            }

            t1.Add(v1);
            t1.Add(v2);
            t1.Add(v3);
        }
        triangles = t1.ToArray();
        int[] t2 = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            t2[i] = triangles[i];
        }
        Array.Sort(t2);
        t2 = t2.Distinct().ToArray();
        Vector3[] v = new Vector3[t2.Length];
        for (int i = 0; i < v.Length; i++)
        {
            v[i] = vertices[t2[i]];
        }
        vertices = v;
    }
}