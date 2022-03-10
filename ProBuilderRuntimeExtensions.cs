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
            origin = mesh.transform.InverseTransformPoint(origin);

            var MeshVertices = mesh.GetVertices();


            var AlreadyOperated = new List<int>();
            for(int i = 0; i < MeshVertices.Length; i++)
            {
                if (AlreadyOperated.Contains(i)) continue;
                if (!vertices.Contains(i)) continue;

                var item = MeshVertices[i];

                Vector3 newPosition = rotation * (item.position - origin) + origin;

                var offset = newPosition - item.position;

                mesh.MoveVertices(offset, i);

                List<int> coincident = new List<int>();
                mesh.GetCoincidentVertices(i, coincident);

                AlreadyOperated.AddRange(coincident);
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
    }
}
