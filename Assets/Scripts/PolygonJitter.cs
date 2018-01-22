using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonJitter : MonoBehaviour {
	public Mesh m_mesh = null;

	private MeshData m_meshData;
	private bool m_bShow = true;
	public int m_polygon_depth = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 pos = transform.position;
		pos.z = m_polygon_depth * (-0.02f);// + 0.1f * Mathf.Cos(Time.time));
		transform.position = pos;

		if (true) return;
		if (m_mesh != null)
		{
			m_meshData.m_vertices[0].z = Mathf.Cos(Time.time);

			m_mesh.vertices = m_meshData.m_vertices;
			m_mesh.uv = m_meshData.m_uvs;
			m_mesh.uv2 = m_meshData.m_uv2s;
			m_mesh.triangles = m_meshData.m_triangles;

			m_mesh.RecalculateNormals();

			GetComponent<MeshFilter>().mesh = m_mesh;
		}
	}

	public void SetPolygon(Polygon p)
	{
		if (m_mesh == null)
		{
			m_mesh = new Mesh();
		}
		else
		{
			m_mesh.Clear();
		}

		m_meshData = p.m_meshData;
		m_mesh.vertices = m_meshData.m_vertices;
		m_mesh.uv = m_meshData.m_uvs;
		m_mesh.uv2 = m_meshData.m_uv2s;
		m_mesh.triangles = m_meshData.m_triangles;
		m_mesh.RecalculateNormals();
		m_meshData.m_normals = m_mesh.normals;

		GetComponent<MeshFilter>().mesh = m_mesh;
		return;
	}

	public void SetPolygonDepth(int depth)
	{
		m_polygon_depth = depth;
	}

	public void ShowPolygon(bool bShow)
	{
		m_bShow = bShow;
		enabled = bShow;
		GetComponent<MeshRenderer>().enabled = bShow;
	}
}
