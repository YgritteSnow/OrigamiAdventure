using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonRenderer : MonoBehaviour
{
	private Mesh m_mesh = null;
	private PolygonData m_polygon;
	private Vector2 center_position;

	private MeshData m_meshData;

	public void SetPolygon(PolygonData p)
	{
		m_polygon = p;
		CalculateMesh(m_polygon);
		CalUVByProjectToBound(p.m_originBounds);
		CalUV2();
		CopyMesh();
	}

	public void ShowPolygon(bool bShow)
	{
		GetComponent<MeshRenderer>().enabled = bShow;
	}

	#region 计算mesh的部分
	void CalculateMesh(PolygonData m_polygon)
	{
		// 创建mesh数据
		m_meshData = new MeshData();

		// 计算中心点，并将其加入点列表，作为所有三角形都经过的一个公共点
		center_position = CalCenterPoint();

		// 顶点直接拷贝多边形的点
		m_meshData.m_vertices = new Vector3[m_polygon.m_points.Count + 1];
		for (int idx = 0; idx != m_polygon.m_points.Count; ++idx)
		{
			m_meshData.m_vertices[idx] = m_polygon.m_points[idx].position;
		}
		m_meshData.m_vertices[m_polygon.m_points.Count] = center_position;

		// 从一个点出发，构建所有的三角形
		SetTriangleList();
	}

	Vector2 CalCenterPoint()
	{
		Vector2 res = Vector2.zero;
		foreach (PolygonPoint v in m_polygon.m_points)
		{
			res += v.position;
		}
		res /= m_polygon.m_points.Count;
		return res;
	}

	/// <summary>
	/// 根据点的围绕顺序，构建所有的三角形
	/// </summary>
	/// <param name="triangles"></param>
	void SetTriangleList()
	{
		int triangle_count = m_polygon.m_points.Count * 3;
		if (triangle_count <= 0)
		{
			m_meshData.m_triangles = new int[0];
			return;
		}
		m_meshData.m_triangles = new int[triangle_count];
		for (int i = 0; i < m_polygon.m_points.Count; ++i)
		{
			int triangle_idx = i * 3;
			m_meshData.m_triangles[triangle_idx] = i;
			m_meshData.m_triangles[triangle_idx + 1] = (i + 1) % m_polygon.m_points.Count;
			m_meshData.m_triangles[triangle_idx + 2] = m_polygon.m_points.Count;
		}
	}

	/// <summary>
	/// 使用包围盒作为纹理映射的边界，自动计算所有uv坐标。
	/// </summary>
	void CalUVByProjectToBound(Vector4 bound)
	{
		if (m_polygon.m_points.Count == 0)
		{
			m_meshData.m_uvs = new Vector2[0];
			return;
		}
		m_meshData.m_uvs = new Vector2[m_polygon.m_points.Count + 1];

		float width_inv = 1 / (bound.y - bound.x);
		float height_inv = 1 / (bound.z - bound.w);

		for (int idx = 0; idx < m_polygon.m_points.Count; ++idx)
		{
			m_meshData.m_uvs[idx] = new Vector2(
				(m_polygon.m_points[idx].position.x - bound.x) * width_inv,
				(m_polygon.m_points[idx].position.y - bound.w) * height_inv
			);
		}
		m_meshData.m_uvs[m_polygon.m_points.Count] = new Vector2(
			(center_position.x - bound.x) * width_inv,
			(center_position.y - bound.w) * height_inv
		);
	}

	void CalUV2()
	{
		m_meshData.m_uv2s = new Vector2[m_polygon.m_points.Count + 1];
		for (int idx = 0; idx < m_polygon.m_points.Count; ++idx)
		{
			m_meshData.m_uv2s[idx] = new Vector2(1, 0);
		}
		m_meshData.m_uv2s[m_polygon.m_points.Count] = new Vector2(0, 0);
	}

	void CopyMesh()
	{
		if (m_mesh == null)
		{
			m_mesh = new Mesh();
		}
		else
		{
			m_mesh.Clear();
		}
		m_mesh.vertices = m_meshData.m_vertices;
		m_mesh.triangles = m_meshData.m_triangles;
		m_mesh.uv = m_meshData.m_uvs;
		m_mesh.uv2 = m_meshData.m_uv2s;
		m_mesh.RecalculateNormals();

		GetComponent<MeshFilter>().mesh = m_mesh;
	}
	#endregion
}