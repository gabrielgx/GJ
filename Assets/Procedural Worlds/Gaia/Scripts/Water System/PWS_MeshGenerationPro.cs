namespace ProcedualWorlds.WaterSystem.MeshGeneration
{
    using System.Collections.Generic;
    using UnityEngine;
    /// <summary>
    /// Generates a procedural mesh
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class PWS_MeshGenerationPro : MonoBehaviour
    {
        #region PublicVariables
        /// <summary>
        /// Enables or disables gizmos for debug
        /// </summary>
        [HideInInspector]
        public bool m_gizmo;
        /// <summary>
        /// the height the mesh spawns at.
        /// </summary>
        [HideInInspector]
        public int m_height = 0;
        /// <summary>
        /// the size of the gizmo 
        /// </summary>
        [HideInInspector]
        public float m_gizmoSize = 0.1f;
        /// <summary>
        /// Size of mesh
        /// </summary>
        [HideInInspector]
        public Vector3 m_Size = new Vector3(200, 30, 200);
        /// <summary>
        /// Points [vertices] on the mesh
        /// </summary>
        [HideInInspector]
        public Vector2 m_meshDensity = new Vector2(50, 50);
        /// <summary>
        /// Mesh Type in use
        /// </summary>
        [HideInInspector]
        public MeshType m_MeshType;
        /// <summary>
        /// Mesh Type Enum
        /// </summary>
        #endregion
        #region PrivateVariables
        private Vector2 m_uvScale;
        private Vector3 m_sizeScale;
        private List<int> m_triangles = new List<int>();
        private List<Vector3> m_vertices = new List<Vector3>();
        private List<Vector2> m_uvs = new List<Vector2>();
        private Mesh m_generateMesh;
        private Vector2Int m_numberOfPoints;
        #endregion
        #region WaterGeneration
        /// <summary>
        /// Creates a procedural mesh with a vertex count and size.
        /// </summary>
        public void ProceduralMeshGeneration()
        {
            SizeGeneration();
            if (m_MeshType == MeshType.Plane)
            {
                if (m_uvScale.y <= 0 || m_uvScale.x <= 0 || m_sizeScale.x <= 0 || m_sizeScale.y <= 0)
                {
                    Debug.Log("Size was 0, unable to generate mesh");
                    return;
                }
            }
            if (CalculatePolysRequired() == 0)
            {
                Debug.LogWarning("The selected water size is too small for the selected water mesh quality. Please increase water size and / or water mesh quality.");
                return;
            }

            if (!GetComponent<MeshRenderer>())
            {
                Debug.Log("Warning Mesh Render is missing for the water generator");
            }
            if (GetComponent<MeshFilter>())
            {
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                m_generateMesh = new Mesh
                {
                    name = "Procedural Grid"
                };
                VerticesGeneration();
                TriangleGeneration();
                UVGeneration();
                NormalGeneration();
                BoundsGeneration();
                meshFilter.mesh = m_generateMesh;
                ClearData();
            }
            else
            {
                Debug.Log("unable to find MeshFilter, unable to generate mesh");
            }
        }
        #endregion
        #region MeshGeneration
        /// <summary>
        /// Size Generation for a given object
        /// </summary>
        public void SizeGeneration()
        {
            if (m_MeshType == MeshType.Plane)
            {
                m_numberOfPoints = new Vector2Int(Mathf.RoundToInt(m_meshDensity.x / 40 * m_Size.x) +1, Mathf.RoundToInt(m_meshDensity.y / 40 * m_Size.z)+1);
                m_uvScale = new Vector2(1f / (m_numberOfPoints.x), 1f / (m_numberOfPoints.y));
                m_sizeScale = new Vector3(m_Size.x / (m_numberOfPoints.x -1), m_Size.y, m_Size.z / (m_numberOfPoints.y -1));
            }
            if (m_MeshType == MeshType.Circle)
            {
                
                float half = m_Size.x / 2f;
                m_numberOfPoints = new Vector2Int(Mathf.RoundToInt(m_meshDensity.x / 40 * half), Mathf.RoundToInt(m_meshDensity.y / 40 * half));
                m_uvScale = new Vector2(1f / (m_meshDensity.x), 1f / (m_meshDensity.x));
                m_sizeScale = new Vector3(half / (m_numberOfPoints.x), m_Size.y, half / (m_numberOfPoints.y));
                //m_sizeScale = new Vector3(half / (m_meshDensity.x), m_Size.y, half / (m_meshDensity.x));
                
            }
        }
        /// <summary>
        /// Generates the vertices
        /// </summary>
        public void VerticesGeneration()
        {
            if (m_MeshType == MeshType.Plane)
            {
               

                Vector3 MiddlePoint;
                MiddlePoint = new Vector3((float)(m_numberOfPoints.x -1) / 2, 0, (float)(m_numberOfPoints.y -1) / 2);
                for (int y = 0; y < (int)m_numberOfPoints.y ; y++)
                {
                    for (int x = 0; x < (int)m_numberOfPoints.x; x++)
                    {
                        Vector3 vertex = new Vector3(MiddlePoint.x - x, m_height, MiddlePoint.z - y);
                        m_vertices.Add(Vector3.Scale(m_sizeScale, vertex));
                    }
                }

                //Determine number of vertices for mesh index format
                if (m_vertices.Count < 64000)
                {
                    m_generateMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                }
                else
                {
                    m_generateMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                m_generateMesh.vertices = m_vertices.ToArray();
                
            }
            if (m_MeshType == MeshType.Circle)
            {
                m_vertices.Add(Vector3.Scale(m_sizeScale, Vector3.zero));
                for (int CirclePoint = 0; CirclePoint < m_numberOfPoints.x; CirclePoint++)
                {
                    //angle step is the next position in the circle to iterate over.
                    //Mathf.PI one side, *2 both sides
                    float angleStep = (Mathf.PI * 2f) / ((CirclePoint + 1) * 6);
                    for (int point = 0; point < (CirclePoint + 1) * 6; point++)
                    {
                        Vector3 vertex = new Vector3(Mathf.Cos(angleStep * point), 0, Mathf.Sin(-angleStep * point));
                        vertex = vertex * 1 * (CirclePoint + 1);
                        m_vertices.Add(Vector3.Scale(m_sizeScale, vertex));

                    }
                }

                //Determine number of vertices for mesh index format
                if (m_vertices.Count < 64000)
                {
                    m_generateMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                }
                else
                {
                    m_generateMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                m_generateMesh.vertices = m_vertices.ToArray();
            }
        }
        /// <summary>
        /// Generates the Triangles
        /// </summary>
        public void TriangleGeneration()
        {
            if (m_MeshType == MeshType.Plane)
            {
                int x, y;
                for (y = 0; y < (int)m_numberOfPoints.y - 1; y++)
                {
                    for (x = 0; x < (int)m_numberOfPoints.x - 1; x++)
                    {
                        // For each grid cell output two triangles
                        m_triangles.Add((y * (int)m_numberOfPoints.x) + x);
                        m_triangles.Add(((y + 1) * (int)m_numberOfPoints.x) + x);
                        m_triangles.Add((y * (int)m_numberOfPoints.x) + x + 1);
                        m_triangles.Add(((y + 1) * (int)m_numberOfPoints.x) + x);
                        m_triangles.Add(((y + 1) * (int)m_numberOfPoints.x) + x + 1);
                        m_triangles.Add((y * (int)m_numberOfPoints.x) + x + 1);
                    }
                }
                m_generateMesh.triangles = m_triangles.ToArray();
            }
            if (m_MeshType == MeshType.Circle)
            {
                m_triangles.Clear();
                for (int circ = 0; circ < (int)m_numberOfPoints.x; circ++)
                {
                    for (int point = 0, other = 0; point < (circ + 1) * 6; point++)
                    {
                        if (point % (circ + 1) != 0)
                        {
                            // Creates 2 triangles (square generation)
                            m_triangles.Add(GetPoint(circ - 1, other + 1));
                            m_triangles.Add(GetPoint(circ - 1, other));
                            m_triangles.Add(GetPoint(circ, point));
                            //second triangle
                            m_triangles.Add(GetPoint(circ, point));
                            m_triangles.Add(GetPoint(circ, point + 1));
                            m_triangles.Add(GetPoint(circ - 1, other + 1));
                            ++other;
                        }
                        else
                        {
                            // Creates the triangles for sections in the core and 4 break up points.
                            m_triangles.Add(GetPoint(circ, point));
                            m_triangles.Add(GetPoint(circ, point + 1));
                            m_triangles.Add(GetPoint(circ - 1, other));
                            // Do not move to the next point in the smaller circles
                        }
                    }
                }
                m_generateMesh.triangles = m_triangles.ToArray();
            }
        }
        /// <summary>
        /// Calculates the normal of the mesh
        /// </summary>
        public void NormalGeneration()
        {
            m_generateMesh.RecalculateNormals();
        }
        /// <summary>
        /// Generates the UV map for the mesh
        /// </summary>
        public void UVGeneration()
        {
            if (m_MeshType == MeshType.Plane)
            {
                Vector2 MiddlePoint = new Vector2(m_numberOfPoints.x / 2, m_numberOfPoints.y / 2);
                for (int y = 0; y < (int)m_numberOfPoints.y; y++)
                {
                    for (int x = 0; x < (int)m_numberOfPoints.x; x++)
                    {
                        m_uvs.Add(Vector2.Scale(new Vector2(MiddlePoint.x - x, MiddlePoint.y- y), m_uvScale));
                    }
                }
                m_generateMesh.uv = m_uvs.ToArray();
            }
            if (m_MeshType == MeshType.Circle)
            {
                m_uvs.Add(Vector2.Scale(m_uvScale, Vector2.zero));
                for (int CirclePoint = 0; CirclePoint < m_numberOfPoints.x; CirclePoint++)
                {
                    float angleStep = (Mathf.PI * 2f) / ((CirclePoint + 1) * 6);
                    for (int point = 0; point < (CirclePoint + 1) * 6; point++)
                    {
                        Vector2 vertex = new Vector2(Mathf.Cos(angleStep * point), Mathf.Sin(-angleStep * point));
                        vertex = vertex * 1 * (CirclePoint + 1);
                        m_uvs.Add(Vector2.Scale(vertex, m_uvScale));
                    }
                }
                m_generateMesh.uv = m_uvs.ToArray();
            }
        }
        /// <summary>
        /// Generates the bounds of the mesh
        /// </summary>
        public void BoundsGeneration()
        {
            m_generateMesh.RecalculateBounds();
        }
        #endregion
        #region ClearMemory 
        /// <summary>
        /// Clears memory of lists and arrays
        /// </summary>
        public void ClearData()
        {
            m_vertices.Clear();
            m_uvs.Clear();
            m_triangles.Clear();
        }
        #endregion
        #region PointCalculuation
        /// <summary>
        /// Gets the point on a circle
        /// </summary>
        /// <param name="Center"></param>
        /// <param name="Extent"></param>
        /// <returns></returns>
        static int GetPoint(int Center, int Extent)
        {
            // In case of center point no calculation needed
            if (Center < 0)
            {
                return 0;
            }
            Extent = Extent % ((Center + 1) * 6);
            // Make the point index circular
            // Explanation: index = number of points in previous circles + central point + x
            // hence: (0+1+2+...+c)*6+x+1 = ((c/2)*(c+1))*6+x+1 = 3*c*(c+1)+x+1
            return (3 * Center * (Center + 1) + Extent + 1);
        }
        #endregion
        #region MeshType
        public enum MeshType { Plane, Circle };
        #endregion
        #region DensityCalculation
        /// <summary>
        /// Calculate the real amount of triangles.
        /// dependent on MeshType selected.
        /// </summary>
        /// <returns>total triangle count</returns>
        public int CalculatePolysRequired()
        {
            SizeGeneration();
            if (m_MeshType == MeshType.Plane)
            {
                return (int)(m_numberOfPoints.y-1) * (int)(m_numberOfPoints.x-1) *2;
            }
            if (m_MeshType == MeshType.Circle)
            {
                return (int)m_numberOfPoints.x * ((int)m_numberOfPoints.x) * 6;
            }
            return 0;
        }
        #endregion
        #region Gizmo
        /// <summary>
        /// Draws a cube on each triangle point
        /// </summary>
        private void OnDrawGizmos()
        {
            if (m_gizmo)
            {
                if (m_MeshType == MeshType.Plane)
                {
                    if (m_vertices != null)
                    {
                        for (int i = 0; i < m_vertices.Count; i++)
                        {
                            Gizmos.DrawSphere(m_vertices[i], m_gizmoSize);
                        }
                    }
                }
                if (m_MeshType == MeshType.Circle)
                {
                    if (m_vertices != null)
                    {
                        for (int i = 0; i < m_vertices.Count; i++)
                        {
                            Gizmos.DrawSphere(m_vertices[i], m_gizmoSize);
                        }
                    }
                }
            }
        }
        #endregion
    }
}