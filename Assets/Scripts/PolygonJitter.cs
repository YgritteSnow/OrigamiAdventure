using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonJitter : MonoBehaviour {
	public bool m_bShow = true;

	public MeshData m_meshData;
	public Mesh m_mesh = null;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SetPolygon(Polygon p)
	{
		m_meshData = p.m_meshData;

		//m_meshData.m_vertices = p.m_meshData.m_vertices.Clone() as Vector3[];
		//m_meshData.m_uvs = p.m_meshData.m_uvs.Clone() as Vector2[];
		//m_meshData.m_triangles = p.m_meshData.m_triangles.Clone() as int[];
		
		// 创建mesh
		if (m_mesh == null)
		{
			m_mesh = new Mesh();
		}
		else
		{
			m_mesh.Clear();
		}
		m_mesh.vertices = m_meshData.m_vertices;
		m_mesh.uv = m_meshData.m_uvs;
		m_mesh.triangles = m_meshData.m_triangles;
		m_mesh.RecalculateNormals();
		m_meshData.m_normals = m_mesh.normals;

		GetComponent<MeshFilter>().mesh = m_mesh;
		return;
	}

	public void ShowPolygon(bool bShow)
	{
		m_bShow = bShow;
		GetComponent<MeshRenderer>().enabled = bShow;
	}
}
