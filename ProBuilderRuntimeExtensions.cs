using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Linq;

namespace SubsurfaceStudios.MeshOperations
{
    public static class MeshOperations
    {
        public static ProBuilderMesh CreatePrimitiveMesh(ShapeType type, Vector3 position, Quaternion rotation, string name)
        {
            var pbm = ShapeGenerator.CreateShape(type, PivotLocation.Center);
            var _gameObject = pbm.gameObject;

            pbm.ToMesh(MeshTopology.Triangles);
            pbm.Refresh();

            _gameObject.AddComponent<MeshCollider>();
            _gameObject.name = name;
            _gameObject.transform.SetPositionAndRotation(position, rotation);

            _gameObject.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;

            return pbm;
        }

        public static void ExtrudeFaces(this ProBuilderMesh mesh, ExtrudeMethod method, float distance, params int[] faces)
        {
            var Faces_formatted = faces.Select((int item) => mesh.faces[item]);
            mesh.Extrude(Faces_formatted, method, distance);
            mesh.ToMesh(MeshTopology.Triangles);
            mesh.Refresh();
        }

        public static void MoveVertices(this ProBuilderMesh mesh, Vector3 movement, params int[] vertices)
        {
            vertices = mesh.FilterUnconnectedVertices(vertices);

            mesh.TranslateVerticesInWorldSpace(vertices, movement);
            mesh.ToMesh(MeshTopology.Triangles);
            mesh.Refresh();
        }

        public static void MoveFaces(this ProBuilderMesh mesh, Vector3 movement, params int[] faces)
        {
            var _faces = new List<Face>();

            foreach (var item in faces)
                _faces.Add(mesh.faces[item]);

            List<int> vertices = new List<int>();

            foreach (Face item in _faces)
                vertices.AddRange(item.indexes);

            mesh.MoveVertices(movement, vertices.Distinct().ToArray());
        }

        public static void ScaleVertices(this ProBuilderMesh mesh, Vector3 origin, float scaleFactor, params int[] vertices)
        {
            vertices = mesh.FilterUnconnectedVertices(vertices);

            origin = mesh.transform.InverseTransformPoint(origin);
            
            var MeshVertices = mesh.GetVertices();

            for(int i = 0; i < MeshVertices.Length; i++)
            {
                if (!vertices.Contains(i)) continue;
                var item = MeshVertices[i];

                var newPosition = Vector3.LerpUnclamped(origin, item.position, scaleFactor);

                var offset = newPosition - item.position;

                mesh.MoveVertices(offset, i);
            }
        }

        public static void ScaleFaces(this ProBuilderMesh mesh, Vector3 origin, float scaleFactor, params int[] faces)
        {
            var _faces = new List<Face>();

            foreach (var item in faces)
                _faces.Add(mesh.faces[item]);

            List<int> vertices = new List<int>();

            foreach (Face item in _faces)
                vertices.AddRange(item.indexes);

            mesh.ScaleVertices(origin, scaleFactor, vertices.Distinct().ToArray());
        }

        public static void RotateVertices(this ProBuilderMesh mesh, Vector3 origin, Quaternion rotation, params int[] vertices)
        {
            vertices = mesh.FilterUnconnectedVertices(vertices);

            origin = mesh.transform.InverseTransformPoint(origin);

            var MeshVertices = mesh.GetVertices();
            for(int i = 0; i < MeshVertices.Length; i++)
            {
                if (!vertices.Contains(i)) continue;

                var item = MeshVertices[i];

                Vector3 newPosition = rotation * (item.position - origin) + origin;

                var offset = newPosition - item.position;

                mesh.MoveVertices(offset, i);
            }
        }

        public static void RotateFaces(this ProBuilderMesh mesh, Vector3 origin, Quaternion rotation, params int[] faces)
        {
            var _faces = new List<Face>();

            foreach (var item in faces)
                _faces.Add(mesh.faces[item]);

            List<int> vertices = new List<int>();

            foreach (Face item in _faces)
                vertices.AddRange(item.indexes);

            mesh.RotateVertices(origin, rotation, vertices.Distinct().ToArray());
        }

        public static Vector3 AveragePositionOfVertices(this ProBuilderMesh mesh, params int[] vertices)
        {
            var MeshVertices = mesh.GetVertices();

            var Average = Vector3.zero;

            for(int i = 0; i < MeshVertices.Length; i++)
            {
                if (!vertices.Contains(i)) continue;
                Average += MeshVertices[i].position;
            }

            Average /= vertices.Length;

            return Average;
        }

        public static Vector3 AveragePositionOfFace(this ProBuilderMesh mesh, Face face)
        {
            var MeshVertices = mesh.GetVertices();

            var Average = Vector3.zero;

            for(int i = 0; i < MeshVertices.Length; i++)
            {
                if (!face.indexes.Contains(i)) continue;

                Average += MeshVertices[i].position;
            }

            Average /= face.indexes.Count;

            return Average;
        }

        public static void MergeVerticesCustom(this ProBuilderMesh mesh, MergeVertexMode mode = MergeVertexMode.AtAveragePosition, params int[] vertices)
        {
            if (vertices.Length < 2) return;

            vertices = mesh.FilterUnconnectedVertices(vertices);

            Vector3 position = mode switch
            {
                MergeVertexMode.AtAveragePosition => mesh.AveragePositionOfVertices(vertices),
                MergeVertexMode.AtFirstVertex => mesh.GetVertices()[vertices[0]].position,
                MergeVertexMode.AtLastVertex => mesh.GetVertices()[vertices[vertices.Length]].position,
                _ => mesh.AveragePositionOfVertices(vertices)
            };


            var Vertices = mesh.GetVertices();
            for (int i = 0; i < Vertices.Length; i++)
            {
                if (!vertices.Contains(i)) continue;
                mesh.MoveVertices(position - Vertices[i].position, i);
            }

            mesh.SetVerticesCoincident(vertices);
            mesh.ToMesh(MeshTopology.Triangles);
            mesh.Refresh();
        }

        public static int[] FilterUnconnectedVertices(this ProBuilderMesh mesh, params int[] vertices)
        {
            List<int> ReturnedVertices = vertices.ToList();

            foreach(var item in vertices)
            {
                foreach (var v in mesh.GetCoincidentVertices(new int[] { item }))
                {
                    ReturnedVertices.Remove(v);
                }
            }

            return ReturnedVertices.ToArray();
        }
        
        public static int FindClosestVertexOnFace(this ProBuilderMesh mesh, int face, Vector3 position)
        {
            var faces = mesh.faces;
            var vertices = mesh.VerticesInWorldSpace();

            KeyValuePair<int, float> Closest;
            var first = faces[face].indexes[0];
            Closest = new KeyValuePair<int, float>(first, Vector3.Distance(position, vertices[first]));
            for(int i = 1; i < faces[face].indexes.Count; i++)
            {
                var index = faces[face].indexes[i];
                var vertexPosition = vertices[index];

                var distance = Vector3.Distance(vertexPosition, position);

                if (distance < Closest.Value) Closest = new KeyValuePair<int, float>(index, distance);
            }

            return Closest.Key;
        }
    }
}
