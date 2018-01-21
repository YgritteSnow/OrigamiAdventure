using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{
	public Vector3[] m_vertices;
	public Vector2[] m_uvs;
	public int[] m_triangles;
	public Vector3[] m_normals; // 缓存法线

	//[Obsolete("")]
	//private MeshData Clone()
	//{
	//	MeshData res = new MeshData();
	//	res.m_vertices = m_vertices.Clone() as Vector3[];
	//	res.m_uvs = m_uvs.Clone() as Vector2[];
	//	res.m_triangles = m_triangles.Clone() as int[];
	//	return res;
	//}
}

public class MeshPaper : MonoBehaviour {
	protected MeshData m_meshData;
	protected Mesh m_mesh;

	// Use this for initialization
	void Start()
	{
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	protected void DebugCreateMesh()
	{
		Mesh mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh;

		m_meshData = new MeshData();
		//
		m_meshData.m_vertices = new Vector3[4];
		m_meshData.m_vertices[0] = new Vector3(1, -1);
		m_meshData.m_vertices[1] = new Vector3(-1, -1);
		m_meshData.m_vertices[2] = new Vector3(-1, 1);
		m_meshData.m_vertices[3] = new Vector3(1, 1);
		//
		m_meshData.m_uvs = new Vector2[4];
		m_meshData.m_uvs[0] = new Vector2(0, 0);
		m_meshData.m_uvs[1] = new Vector2(0, 1);
		m_meshData.m_uvs[2] = new Vector2(1, 0);
		m_meshData.m_uvs[3] = new Vector2(1, 1);
		//
		m_meshData.m_triangles = new int[] { 0, 1, 2, 0, 2, 3 };

		mesh.vertices = m_meshData.m_vertices;
		mesh.uv = m_meshData.m_uvs;
		mesh.triangles = m_meshData.m_triangles;
		mesh.RecalculateNormals();
		m_meshData.m_normals = mesh.normals;

		m_mesh = mesh;
	}
}
