using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public unsafe class FastMeshSerializer
{
    const int INT_SIZE = sizeof(int);
    const int HEADER_SIZE = 4 * INT_SIZE;

    public static Mesh Deserialize(byte[] bytes)
    {
        byte* pBytes;
        fixed (byte* b = &bytes[0]) pBytes = b;

        int vertex_block_size = *((int*)pBytes);
        int vertex_count = vertex_block_size / sizeof(Vector3);
        byte[] vertex_buf = new byte[vertex_block_size];

        int index_block_size = *(((int*)pBytes) + 1);
        int index_count = index_block_size / sizeof(int);
        byte[] index_buf = new byte[index_block_size];

        int normals_block_size = *(((int*)pBytes) + 2);
        int normals_count = normals_block_size / sizeof(Vector3);
        byte[] normals_buf = new byte[normals_block_size];

        int uvs_block_size = *(((int*)pBytes) + 3);
        int uv_count = uvs_block_size / sizeof(Vector2);
        byte[] uvs_buf = new byte[uvs_block_size];

        int vertices_offset = HEADER_SIZE;
        int indices_offset = HEADER_SIZE + vertex_block_size;
        int normals_offset = HEADER_SIZE + vertex_block_size + index_block_size;
        int uvs_offset = HEADER_SIZE + vertex_block_size + index_block_size + normals_block_size;

        Array.Copy(bytes, vertices_offset, vertex_buf, 0, vertex_block_size);
        Array.Copy(bytes, indices_offset, index_buf, 0, index_block_size);
        Array.Copy(bytes, normals_offset, normals_buf, 0, normals_block_size);

        Array.Copy(bytes, uvs_offset, uvs_buf, 0, uvs_block_size);

        Vector3[] vertices = new Vector3[vertex_count];
        int[] indices = new int[index_count];
        Vector3[] normals = new Vector3[normals_count];
        Vector2[] uvs = new Vector2[uv_count];

        Vector3* pVertices;
        fixed (Vector3* p = &vertices[0]) pVertices = p;

        int* pIndices;
        fixed (int* p = &indices[0]) pIndices = p;

        Vector3* pNormals;
        fixed (Vector3* p = &normals[0]) pNormals = p;

        Vector2* pUvs;
        fixed (Vector2* p = &uvs[0]) pUvs = p;

        Marshal.Copy(vertex_buf, 0, (IntPtr)pVertices, vertex_block_size);
        Marshal.Copy(index_buf, 0, (IntPtr)pIndices, index_block_size);
        Marshal.Copy(normals_buf, 0, (IntPtr)pNormals, normals_block_size);
        Marshal.Copy(uvs_buf, 0, (IntPtr)pUvs, uvs_block_size);

        // Build mesh

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.SetNormals(normals.ToList());
        mesh.SetUVs(0, uvs.ToList());
        return mesh;
    }

    public static byte[] Serialize(Mesh mesh)
    {
        int vertex_block_size = sizeof(Vector3) * mesh.vertices.Length;
        int index_block_size = INT_SIZE * mesh.GetIndices(0).Length;
        int normals_block_size = sizeof(Vector3) * mesh.normals.Length;
        int uv_block_size = sizeof(Vector2) * mesh.uv.Length;

        byte[] data = new byte[vertex_block_size + index_block_size + normals_block_size + uv_block_size + HEADER_SIZE];

        Vector3* pVertices;
        Vector3* pNormals;
        int* pIndices;
        Vector2* pUvs;

        int[] indices = mesh.GetIndices(0);
        fixed (int* p = &indices[0]) pIndices = p;
        fixed (Vector3* p = &mesh.vertices[0]) pVertices = p;
        fixed (Vector3* p = &mesh.normals[0]) pNormals = p;
        fixed (Vector2* p = &mesh.uv[0]) pUvs = p;

        Marshal.Copy((IntPtr)(&vertex_block_size), data, 0, INT_SIZE);
        Marshal.Copy((IntPtr)(&index_block_size), data, INT_SIZE, INT_SIZE);
        Marshal.Copy((IntPtr)(&normals_block_size), data, INT_SIZE * 2, INT_SIZE);
        Marshal.Copy((IntPtr)(&uv_block_size), data, INT_SIZE * 3, INT_SIZE);

        int vertices_offset = HEADER_SIZE;
        int indices_offset = HEADER_SIZE + vertex_block_size;
        int normals_offset = HEADER_SIZE + vertex_block_size + index_block_size;
        int uvs_offset = HEADER_SIZE + vertex_block_size + index_block_size + normals_block_size;

        Marshal.Copy((IntPtr)pVertices, data, vertices_offset, vertex_block_size);
        Marshal.Copy((IntPtr)pIndices, data, indices_offset, index_block_size);
        Marshal.Copy((IntPtr)pNormals, data, normals_offset, normals_block_size);
        Marshal.Copy((IntPtr)pUvs, data, uvs_offset, uv_block_size);

        return data;
    }
}