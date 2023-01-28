using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NavMeshUtility
{
    [Serializable]
    public class Edge
    {
        public Vector3 start, end;
        public Vector3 startUp, endUp;

        public float length;
        public Quaternion facingNormal;
        public bool facingNormalCalculated;

        public Edge(Vector3 startPoint, Vector3 endPoint)
        {
            start = startPoint;
            end = endPoint;
        }
    }

    public static class NavMeshUtil
    {
        #region Basic

        public static Mesh GetMesh()
        {
            var tr = NavMesh.CalculateTriangulation();
            return new()
            {
                vertices = tr.vertices,
                triangles = tr.indices
            };
        }

        public static void PlaceToEdges(
            List<Edge> edges,
            Action<Vector3, Edge> placeAction,
            float tileWidth = 1f,
            int maxEdgeTile = 10000,
            float heightShift = 0f)
        {
            foreach (Edge edge in edges)
            {
                var tilesCountWidth = (int)Mathf.Clamp(edge.length / tileWidth, 0, maxEdgeTile);

                for (int columnN = 0; columnN < tilesCountWidth; columnN++)
                {
                    // position on edge and shift for half tile width
                    var t = (float)columnN / tilesCountWidth + 0.5f / tilesCountWidth;
                    var placePos = Vector3.Lerp(edge.start, edge.end, t) + edge.facingNormal * Vector3.up * heightShift;

                    placeAction?.Invoke(placePos, edge);
                }
            }
        }

        public static Edge TriToEdge(Mesh mesh, int n1, int n2)
            => new (mesh.vertices[mesh.triangles[n1]], mesh.vertices[mesh.triangles[n2]]);

        public static bool TryAddEdge(ref List<Edge> edges, Edge newEdge)
        {
            //remove duplicate edges
            foreach (var edge in edges)
            {
                if ((edge.start == newEdge.start && edge.end == newEdge.end) ||
                    (edge.start == newEdge.end && edge.end == newEdge.start))
                {
                    edges.Remove(edge);
                    return false;
                }
            }
            edges.Add(newEdge);
            return true;
        }

        public static List<Edge> CalcEdges(
            Mesh mesh,
            bool dontAllignYAxis = false,
            bool invertFacingNormal = false,
            float normalFixThreshold = 0.999f)
        {
            var edges = new List<Edge>();
            for (int i = 0; i < mesh.triangles.Length - 1; i += 3)
            {
                // Calc from mesh open edges vertices
                TryAddEdge(ref edges, TriToEdge(mesh, i, i + 1));
                TryAddEdge(ref edges, TriToEdge(mesh, i + 1, i + 2));
                TryAddEdge(ref edges, TriToEdge(mesh, i + 2, i));
            }

            foreach (Edge edge in edges)
            {
                edge.length = Vector3.Distance(edge.start, edge.end);

                if (!edge.facingNormalCalculated)
                {
                    edge.facingNormalCalculated = true;
                    edge.facingNormal = Quaternion.LookRotation(Vector3.Cross(edge.end - edge.start, Vector3.up));

                    if (edge.startUp.sqrMagnitude > 0f)
                    {
                        var vect = Vector3.Lerp(edge.endUp, edge.startUp, 0.5f) - Vector3.Lerp(edge.end, edge.start, 0.5f);
                        edge.facingNormal = Quaternion.LookRotation(Vector3.Cross(edge.end - edge.start, vect));

                        var normalAngle = Mathf.Abs(Vector3.Dot(Vector3.up, (edge.facingNormal * Vector3.forward).normalized));
                        if (normalAngle > normalFixThreshold)
                        {
                            edge.startUp += new Vector3(0f, 0.1f, 0f);
                            vect = Vector3.Lerp(edge.endUp, edge.startUp, 0.5f) - Vector3.Lerp(edge.end, edge.start, 0.5f);
                            edge.facingNormal = Quaternion.LookRotation(Vector3.Cross(edge.end - edge.start, vect));
                        }
                    }
                    if (dontAllignYAxis)
                    {
                        var edgeForward = Quaternion.LookRotation(edge.end - edge.start);
                        edge.facingNormal = Quaternion.LookRotation(edge.facingNormal * Vector3.forward, edgeForward * Vector3.up);
                    }
                }
                if (invertFacingNormal) edge.facingNormal = Quaternion.Euler(Vector3.up * 180f) * edge.facingNormal;
            }

            return edges;
        }

        #endregion

        #region Create And Destroy

        public static NavMeshLink CreateNavmeshLink(NavMeshLink linkPrefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 endPoint, float width, bool bridirectional)
        {
            var spawnPos = position - rotation * Vector3.forward * 0.02f;

            var link = GameObject.Instantiate(linkPrefab, spawnPos, rotation, parent);
            link.startPoint = Vector3.zero;
            link.endPoint = link.transform.InverseTransformPoint(endPoint);
            link.width = width;
            link.bidirectional = bridirectional;
            link.UpdateLink();
            return link;
        }

        public static NavMeshLink AddNavmeshLink(
            GameObject gameObject,
            Quaternion rotation,
            Vector3 startPointInWorld,
            Vector3 endPoint,
            float width = 0f,
            int costModifier = -1,
            bool autoUpdatePosition = false,
            bool bridirectional = true,
            int area = 0)
        {
            startPointInWorld = gameObject.transform.InverseTransformPoint(startPointInWorld);
            var startPoint = startPointInWorld - rotation * Vector3.forward * 0.02f;

            var link = gameObject.AddComponent<NavMeshLink>();
            link.startPoint = startPoint;
            link.endPoint = gameObject.transform.InverseTransformPoint(endPoint);
            link.width = width;
            link.costModifier = costModifier;
            link.autoUpdate = autoUpdatePosition;
            link.bidirectional = bridirectional;
            link.area = area;
            link.UpdateLink();
            return link;
        }

        public static void DestroyChildNavMeshLinks(Transform parent)
        {
            var navMeshLinkList = parent.GetComponentsInChildren<NavMeshLink>().ToList();

            for (int i = navMeshLinkList.Count - 1; i >= 0; i--)
            {
                var obj = navMeshLinkList[i].gameObject;
                if (obj != null)
                {
#if UNITY_EDITOR
                    EditorApplication.delayCall += () => GameObject.DestroyImmediate(obj);
#else
                    GameObject.Destroy(obj);
#endif
                }
                navMeshLinkList.RemoveAt(i);
            }
        }

        public static void DestroyComponents(GameObject gameObject)
        {
            var components = gameObject.GetComponents<NavMeshLink>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                var index = i;
#if UNITY_EDITOR
                EditorApplication.delayCall += () => GameObject.DestroyImmediate(components[index]);
#else
                GameObject.Destroy(components[i]);
#endif
            }
        }

        #endregion

        public static void TryPutFallLink(Vector3 pos, Quaternion normal, float agentRadius, float maxFallHeight, float sqrMaxJumpHeight, Action<Vector3, bool> putAction, int layerMask = -1, int areaMask = 1)
        {
            var fallStartPos = pos + normal * Vector3.forward * agentRadius * 2f;
            var landingPos = fallStartPos + Vector3.down * maxFallHeight * 1.1f;
            
            if (Physics.Linecast(fallStartPos, landingPos, out var hit, layerMask, QueryTriggerInteraction.Ignore))
            {
                if (NavMesh.SamplePosition(hit.point, out var navMeshHit, 0.5f, areaMask))
                {
                    var sqrDistance = (pos - navMeshHit.position).sqrMagnitude;
                    if (sqrDistance > 1.21f) // 1.1f * 1.1f
                    {
                        var canClimb = sqrDistance <= sqrMaxJumpHeight;
                        putAction?.Invoke(navMeshHit.position, canClimb);
                    }
                }
            }
        }
    }
}
