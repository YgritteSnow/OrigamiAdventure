using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

[System.Obsolete("Use OrigamiPaper instead", true)]
public class FoldInfo
{
	public FoldInfo()
	{
		fold_polygons = new List<PolygonData>();
		linked_layers = new List<PolygonLayer>();
	}
	public List<PolygonData> fold_polygons;
	public List<PolygonLayer> linked_layers;
}

// 一层多边形，可以包含多个多边形
[System.Obsolete("Use OrigamiPaper instead", true)]
public class PolygonLayer : MonoBehaviour {
	public MeshData m_meshData;
	private List<PolygonData> m_polygons; // 包含了的多边形
	public Mesh m_mesh; // 实际mesh

	public int m_layerDepth = 0; // 本层的高度
	public float m_layerAnimAmp = 0.1f; // 动画的幅度

	// Use this for initialization
	void Start ()
	{
		if (m_polygons == null || m_polygons.Count == 0)
		{
			return;
		}
	}

	private void Update()
	{
		Vector3 pos = gameObject.transform.position;
		pos.z = m_layerAnimAmp * m_layerDepth;// * (Mathf.Cos(Time.time) + 1);
		gameObject.transform.position = pos;
	}

	#region set polygon and mesh stuff up
	[System.Obsolete("Use PolygonJitter instead.", true)]
	public void SetLayerDepth(int layer_depth)
	{
		m_layerDepth = layer_depth;
		gameObject.name = "layer_" + layer_depth.ToString();
		Vector3 pos = gameObject.transform.position;
		pos.z += layer_depth * 0.2f;
		gameObject.transform.position = pos;
	}
	[System.Obsolete("Use PolygonJitter instead.", true)]
	public void ResetColorByLayerDepth(int min_depth, int max_depth)
	{
		float depth_lerp = max_depth == min_depth ? 1 : (m_layerDepth - min_depth) / (float)(max_depth - min_depth);
		if (Application.isEditor && !Application.isPlaying)
		{
			GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", new Color(depth_lerp, 0, 0));
		}
		else
		{
			GetComponent<MeshRenderer>().material.SetColor("_Color", new Color(depth_lerp, 0, 0));
		}
	}
	public void SetPolygons(List<PolygonData> polygons)
	{
		m_polygons = polygons;
		foreach(PolygonData p in m_polygons)
		{
			p.m_parentLayer = this;
		}
	}
	public int polygonCount { get { return m_polygons.Count; } }

	public void DelPolygons(List<PolygonData> polygons)
	{
		foreach (PolygonData p in polygons)
		{
			m_polygons.Remove(p);
		}
	}
#endregion

#region find touching point and fold edge
	public bool FindFoldEdge(Vector2 touch_point, ref Vector2 local_touch_dir, out Vector2 local_head_pos, out Vector2 local_toe_pos)
	{
		local_head_pos = Vector2.zero;
		local_toe_pos = Vector2.zero;
		if (local_touch_dir.magnitude < JUtility.Epsilon)
		{
			return false;
		}

		PolygonData fold_polygon = null;
		PolygonEdge fold_edge = null;
		float fold_dist = 0;
		for (int i = 0; i != m_polygons.Count; ++i)
		{
			PolygonEdge edge = PolygonEdge.invalid;
			float dist = 0;
			if(m_polygons[i].FindCollideEdge(local_touch_dir, touch_point, ref edge, ref dist) && edge.bInside)
			{
				if (fold_edge == null || dist < fold_dist)
				{
					fold_polygon = m_polygons[i];
					fold_edge = edge;
					fold_dist = dist;
				}
			}
		}
		if (fold_edge != null)
		{
			local_head_pos = fold_polygon.m_points[fold_edge.idx_head].position;
			local_toe_pos = fold_polygon.m_points[fold_edge.idx_toe].position;
			Vector2 edge_dir = local_toe_pos - local_head_pos;
			local_touch_dir -= Vector2.Dot(local_touch_dir, edge_dir) * edge_dir / edge_dir.sqrMagnitude;
			local_touch_dir.Normalize();
			return true;
		}
		return false;
	}

	public bool GetTouchPoint(bool need_inside, ref Vector2 local_touch_point)
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		Vector3 new_pos = transform.InverseTransformPoint(ray.origin);
		Vector3 new_dir = transform.InverseTransformDirection(ray.direction);
		if(Mathf.Abs(new_dir.z) < JUtility.Epsilon)
		{
			return false;
		}
		float param = new_pos.z / new_dir.z;
		Vector3 collide_point = new_pos - param * new_dir;

		if (need_inside)
		{
			bool is_inside = false;
			foreach (PolygonData p in m_polygons)
			{
				if (p.IsPointInside(collide_point))
				{
					is_inside = true;
					break;
				}
			}
			if (!is_inside)
			{
				return false;
			}
		}
		local_touch_point.x = collide_point.x;
		local_touch_point.y = collide_point.y;

#if UNITY_EDITOR
		Debug.DrawRay(collide_point, new_dir, Color.blue);
#endif
		return true;
	}
#endregion

#region check whether can fold along the given edge towards the given dir
	/// <summary>
	/// the head and toe position of the edge line.
	/// </summary>
	public bool GetFoldInfoWorld(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, out FoldInfo fold_info)
	{
		Vector2 local_head_pos = transform.InverseTransformPoint(world_head_pos);
		Vector2 local_toe_pos = transform.InverseTransformPoint(world_toe_pos);
		Vector2 local_fold_dir = transform.InverseTransformVector(world_fold_dir);

		fold_info = new FoldInfo();
		fold_info.fold_polygons = new List<PolygonData>();
		foreach (PolygonData p in m_polygons)
		{
			if(p.CheckAllOneSide(local_head_pos, local_toe_pos, local_fold_dir) > 0)
			{
				fold_info.fold_polygons.Add(p);
			}
		}

		foreach (PolygonData p in fold_info.fold_polygons)
		{
			foreach (PolygonEdge edge in p.Edges)
			{
				if (!IsSameEdge(world_head_pos, world_toe_pos, p, edge))
				{
					foreach (PolygonData polygon in PolygonEdgeMapping.GetEdgePolygons(edge.ID))
					{
						if(polygon.m_parentLayer == this)
						{
							continue;
						}

						bool already_has = false;
						foreach(PolygonLayer pl in fold_info.linked_layers)
						{
							if(pl == polygon.m_parentLayer)
							{
								already_has = true;
								break;
							}
						}
						if (!already_has)
						{
							fold_info.linked_layers.Add(polygon.m_parentLayer);
						}
					}
				}
			}
		}

		if (fold_info.fold_polygons.Count == 0)
		{
			return false;
		}
		
		return true;
	}

	bool IsSameEdge(Vector2 world_head_pos, Vector2 world_toe_pos, PolygonData p, PolygonEdge edge)
	{
		Vector2 edge_dir = p.m_points[edge.idx_head].position - p.m_points[edge.idx_toe].position;
		Vector2 world_dir = world_head_pos - world_toe_pos;
		float cross_value = edge_dir.x * world_dir.y - edge_dir.y * world_dir.x;
		return Mathf.Abs(cross_value) < JUtility.Epsilon;
	}

#endregion

#region transform(fold) along edge
	public void TransformAlongEdge(Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_direct)
	{
		transform.RotateAround(world_head_pos, world_toe_pos - world_head_pos, 180);
	}
	#endregion

	#region cut the layer and all crossing polygon apart by a line
	[System.Obsolete("Use binary-tree instead", true)]
	public void CutLayer(Vector2 local_head_pos, Vector2 local_toe_pos)
	{
		List<PolygonData> new_polygons = new List<PolygonData>();
		foreach(PolygonData p in m_polygons)
		{
			PolygonData right_p, left_p;
			int cut_edge_id;
			if(p.CutPolygon(local_head_pos, local_toe_pos, out left_p, out right_p, out cut_edge_id))
			{
				new_polygons.Add(right_p);
				new_polygons.Add(left_p);
			}
			else
			{
				new_polygons.Add(p);
			}
		}
		SetPolygons(new_polygons);
	}
	#endregion

	#region create new fold line in runtime
	[System.Obsolete("Use binary-tree instead", true)]
	public bool AddNewEdgeInWorld(bool bInside, Vector3 world_head_pos, Vector3 world_toe_pos)
	{
		bool bSucceeded = false;
		List<PolygonData> new_polygons = new List<PolygonData>();
		Vector2 local_head_pos = transform.InverseTransformPoint(world_head_pos);
		Vector2 local_toe_pos = transform.InverseTransformPoint(world_toe_pos);
		foreach (PolygonData pl in m_polygons)
		{
			PolygonData right_p, left_p;
			int cut_edge_id;
			if(pl.CutPolygon(local_head_pos, local_toe_pos, out left_p, out right_p, out cut_edge_id))
			{
				new_polygons.Add(right_p);
				new_polygons.Add(left_p);
				bSucceeded = true;
			}
			else
			{
				new_polygons.Add(pl);
			}
		}
		SetPolygons(new_polygons);
		return bSucceeded;
	}
#endregion
}

class PolygonEdgeMapping
{
	private static Dictionary<int, List<PolygonData>> s_edge_mapping = new Dictionary<int, List<PolygonData>>();
	public static void AddEdgePolygon(int edge_id, PolygonData polygon)
	{
		if(!s_edge_mapping.ContainsKey(edge_id))
		{
			s_edge_mapping[edge_id] = new List<PolygonData>();
		}

		foreach(PolygonData p in s_edge_mapping[edge_id])
		{
			if(p == polygon)
			{
				return;
			}
		}
		s_edge_mapping[edge_id].Add(polygon);
	}

	public static void RemoveEdgePolygon(int edge_id, PolygonData polygon)
	{
		if (!s_edge_mapping.ContainsKey(edge_id))
		{
			return;
		}

		s_edge_mapping[edge_id].Remove(polygon);
		if(s_edge_mapping[edge_id].Count == 0)
		{
			s_edge_mapping.Remove(edge_id);
		}
	}

	public static List<PolygonData> GetEdgePolygons(int edge_id)
	{
		if(!s_edge_mapping.ContainsKey(edge_id))
		{
			return new List<PolygonData>();
		}
		else
		{
			return s_edge_mapping[edge_id];
		}
	}
}