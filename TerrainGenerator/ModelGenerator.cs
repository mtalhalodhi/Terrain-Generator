using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace TerrainGenerator
{
    public static class ModelGenerator
    {
        public static MeshData GenerateTerrainMesh(double[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            double topLeftX = (width - 1) / -2;
            double topLeftZ = (height - 1) / 2;

            MeshData data = new MeshData(width, height);
            int vertexIndex = 0;

            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {

                    data.Vertices[vertexIndex] = new Point3D(topLeftX + x, topLeftZ - y, heightMap[x, y]);
                    data.UVs[vertexIndex] = new Point((double)x / (double)width, (double)y / (double)height);

                    if (x < width - 1 && y < height - 1)
                    {
                        data.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                        data.AddTriangle(vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
                    }

                    vertexIndex++;
                }
            }

            return data;
        }
    }

    public class MeshData
    {
        public Point3D[] Vertices;
        public int[] Triangles;
        public Point[] UVs;

        int triangleIndex = 0;

        public MeshData(int width, int height)
        {
            Vertices = new Point3D[width * height];
            UVs = new Point[width * height];
            Triangles = new int[(width - 1) * (height - 1) * 6];
        }

        public void AddTriangle(int a, int b, int c)
        {
            Triangles[triangleIndex] = a;
            Triangles[triangleIndex + 1] = b;
            Triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        }

        public MeshGeometry3D CreateMesh()
        {
            var mesh = new Mesh3D(Vertices, UVs, Triangles).ToMeshGeometry3D(true, 0, Triangles.ToList());
            mesh.CalculateNormals();
            return mesh;
        }
    }
}
