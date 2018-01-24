using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonOuterRenderer : MonoBehaviour
{
	public float m_edgeWidth = 0.1f;
	public float m_angStep = 0.2f;
	public GameObject m_sample = null;

	List<Vector3> m_points = new List<Vector3>();
	List<int> m_triangles = new List<int>();
	List<Vector2> m_uvs = new List<Vector2>();

	private PolygonData m_polygon;
	private GameObject m_outerMesh;
	private Mesh m_mesh = null;

	public void SetPolygon(PolygonData p)
	{
		m_polygon = p;
		CalculateMesh();
		CopyMesh();
		RefreshMeshObject();
	}

	public void ShowPolygon(bool bShow)
	{
		m_outerMesh.SetActive(bShow);
	}

	#region 计算Mesh的部分
	void CalculateMesh()
	{
		m_points.Clear();
		m_triangles.Clear();
		m_uvs.Clear();

		int point_count = m_polygon.m_points.Count;
		for (int i = 0; i != point_count; ++i)
		{
			Vector2 cur_point = m_polygon.m_points[i].position;
			Vector2 next_point = m_polygon.m_points[(i + 1) % point_count].position;
			Vector2 last_point = m_polygon.m_points[(i - 1 + point_count) % point_count].position;

			// 边周围的阴影是长方形
			CalculateMeshEdge(cur_point, next_point);

			// 角周围的阴影是扇形
			CalculateMeshCorner(cur_point, next_point, last_point);
		}
	}

	void CalculateMeshEdge(Vector2 cur_point, Vector2 next_point)
	{
		// 计算边向外的方向
		Vector2 edge_dir = next_point - cur_point;
		Vector2 edge_normal = new Vector2(edge_dir.y, -edge_dir.x); // 边是逆时针绕序的，所以直接这样写
		edge_normal.Normalize();

		m_points.Add(cur_point);
		m_uvs.Add(new Vector2(1, 0));
		m_points.Add(cur_point + m_edgeWidth * edge_normal);
		m_uvs.Add(new Vector2(0, 0));
		m_points.Add(next_point + m_edgeWidth * edge_normal);
		m_uvs.Add(new Vector2(0, 0));
		m_points.Add(next_point);
		m_uvs.Add(new Vector2(1, 0));

		m_triangles.Add(m_points.Count - 4);
		m_triangles.Add(m_points.Count - 3);
		m_triangles.Add(m_points.Count - 1);

		m_triangles.Add(m_points.Count - 3);
		m_triangles.Add(m_points.Count - 2);
		m_triangles.Add(m_points.Count - 1);
	}

	void CalculateMeshCorner(Vector2 cur_point, Vector2 next_point, Vector2 last_point)
	{
		Vector2 next_dir = next_point - cur_point;
		Vector2 end_dir = new Vector2(next_dir.y, -next_dir.x); // 边是逆时针绕序的，所以直接这样写
		end_dir.Normalize();
		Vector2 last_dir = cur_point - last_point;
		Vector2 bgn_dir = new Vector2(last_dir.y, -last_dir.x); // 边是逆时针绕序的，所以直接这样写
		bgn_dir.Normalize();
		
		float ang = Quaternion.FromToRotation(bgn_dir, end_dir).eulerAngles.z;

		Quaternion bgn_quat = Quaternion.FromToRotation(Vector2.right, bgn_dir);
		Quaternion end_quat = Quaternion.FromToRotation(Vector2.right, end_dir);
		Quaternion rotate_quat = Quaternion.Euler(0, 0, m_angStep);
		end_dir *= m_edgeWidth;
		bgn_dir *= m_edgeWidth;

		m_points.Add(cur_point);
		m_uvs.Add(new Vector2(1, 0));

		Vector2 cur_dir = bgn_dir;
		Quaternion cur_quat = bgn_quat;
		int cur_point_idx = m_points.Count - 1;
		while (ang > m_angStep)
		{
			ang -= m_angStep; // 每次减少0.2
			cur_dir = rotate_quat * cur_dir;

			m_points.Add(cur_point + bgn_dir);
			m_points.Add(cur_point + cur_dir);
			m_uvs.Add(new Vector2(0, 0));
			m_uvs.Add(new Vector2(0, 0));

			m_triangles.Add(cur_point_idx);
			m_triangles.Add(m_points.Count - 2);
			m_triangles.Add(m_points.Count - 1);

			bgn_dir = cur_dir;
		}
		
		m_points.Add(cur_point + cur_dir);
		m_points.Add(cur_point + end_dir);
		m_uvs.Add(new Vector2(0, 0));
		m_uvs.Add(new Vector2(0, 0));

		m_triangles.Add(cur_point_idx);
		m_triangles.Add(m_points.Count - 2);
		m_triangles.Add(m_points.Count - 1);
	}

	void CopyMesh()
	{
		if(m_mesh == null)
		{
			m_mesh = new Mesh();
		}
		else
		{
			m_mesh.Clear();
		}

		Vector3[] v = new Vector3[m_points.Count];
		m_points.CopyTo(v);
		m_mesh.vertices = v;

		int[] t = new int[m_triangles.Count];
		m_triangles.CopyTo(t);
		m_mesh.triangles = t;

		Vector2[] u = new Vector2[m_uvs.Count];
		m_uvs.CopyTo(u);
		m_mesh.uv = u;

		m_mesh.RecalculateNormals();
	}

	void RefreshMeshObject()
	{
		if(m_outerMesh == null)
		{
			m_outerMesh = GameObject.Instantiate(m_sample);
			m_outerMesh.transform.parent = transform;
			m_outerMesh.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			m_outerMesh.name = "outer";
		}
		m_outerMesh.GetComponent<MeshFilter>().mesh = m_mesh;
	}
	#endregion
}
