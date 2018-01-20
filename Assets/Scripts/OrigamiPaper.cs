///
/// 1. 加载和预处理原始的纸张及其折痕，创建 PolygonLayer 来展示自己
/// 2. 当进行折叠时，修改发生变化的 PolygonLayer，创建需要增加的 GameObject
///
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrigamiPaper : MonoBehaviour {
	public GameObject m_samplePolygonLayer; // 新增层时赋值此层作为模板
	public List<PolygonLayer> m_polygonLayers = new List<PolygonLayer>();
	private PolygonLayer m_pressingPolygon = null;

	private int m_minLayerDepth = 0;
	private int m_maxLayerDepth = 0;

	#region only the uppest layer get pressing message
	void Update ()
	{
		if (Input.GetMouseButton(0) && m_pressingPolygon == null)
		{
			OnPressDown();
		}
		else if (!Input.GetMouseButton(0) && m_pressingPolygon != null)
		{
			OnPressUp();
		}
	}

	private Vector2 m_local_lastTouchPoint = Vector2.zero;
	void OnPressDown()
	{
		bool cur_is_upside = FromUpside();
		int total_count = m_polygonLayers.Count;
		for (int i = 0; i != m_polygonLayers.Count; ++i)
		{
			PolygonLayer pl = m_polygonLayers[cur_is_upside ? i : total_count - i - 1];
			if(pl.GetTouchPoint(true, ref m_local_lastTouchPoint))
			{
				m_pressingPolygon = pl;
				return;
			}
		}
	}
	void OnPressUp()
	{
		if(m_pressingPolygon == null)
		{
			return;
		}
		
		Vector2 local_newTouchPoint = Vector2.zero;
		if (m_pressingPolygon.GetTouchPoint(false, ref local_newTouchPoint))
		{
			Vector2 local_touch_dir = local_newTouchPoint - m_local_lastTouchPoint;
			FoldByEdgeInLocalByTouchPoint(m_pressingPolygon, local_touch_dir, m_local_lastTouchPoint);
		}

		m_pressingPolygon = null;
	}
	#endregion

	public void InitOriginalPolygons(List<Polygon> polygons)
	{
		CreateLayerByList(polygons, 0);
	}

	public void ClearAllPolygons()
	{
		m_polygonLayers = new List<PolygonLayer>();
		m_minLayerDepth = 0;
		m_maxLayerDepth = 0;

		List<GameObject> children = new List<GameObject>();
		for(int i = 0; i != transform.childCount; ++i)
		{
			GameObject child = transform.GetChild(i).gameObject;
			if(child.name.StartsWith("layer_"))
			{
				children.Add(child);
			}
		}
		foreach(GameObject child in children)
		{
			if (Application.isEditor && !Application.isPlaying)
			{
				GameObject.DestroyImmediate(child);
			}
			else
			{
				GameObject.Destroy(child);
			}
		}
	}

	PolygonLayer CreateLayerByList(List<Polygon> polygons, int layer_depth)
	{
		GameObject new_layer = GameObject.Instantiate(m_samplePolygonLayer);
		new_layer.name = Random.Range(1, 100).ToString();
		new_layer.transform.parent = this.transform;
		new_layer.SetActive(true);
		PolygonLayer pl = new_layer.AddComponent<PolygonLayer>();
		pl.SetLayerDepth(layer_depth);
		pl.SetPolygons(polygons);
		m_polygonLayers.Add(pl);

		m_minLayerDepth = Mathf.Min(m_minLayerDepth, layer_depth);
		m_maxLayerDepth = Mathf.Max(m_maxLayerDepth, layer_depth);
		SortLayer();
		OnResetMaxMinDepth();
		return pl;
	}

	void OnResetMaxMinDepth()
	{
		foreach (PolygonLayer pl in m_polygonLayers)
		{
			pl.ResetColorByLayerDepth(m_minLayerDepth, m_maxLayerDepth);
		}
	}

	void SortLayer()
	{
		// todo 当开始着手做纸张折叠表现时，应当从纸张的层级开始做起
	}

	public bool CheckNeedFoldLayersWorld(PolygonLayer pl, Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_fold_dir, out List<PolygonLayer> to_check_layers, out List<FoldInfo> to_fold_infos)
	{
		// mark all layers which needs to change
		List<PolygonLayer> change_layer = new List<PolygonLayer>();
		to_check_layers = new List<PolygonLayer>();
		to_fold_infos = new List<FoldInfo>();

		to_check_layers.Add(pl);
		
		int to_check_idx = 0;
		while(to_check_idx != to_check_layers.Count)
		{
			PolygonLayer cur_layer = to_check_layers[to_check_idx];

			FoldInfo cur_fold_info;
			if (cur_layer.GetFoldInfoWorld(world_head_pos, world_toe_pos, world_fold_dir, out cur_fold_info))
			{
				to_fold_infos.Add(cur_fold_info);
				foreach (PolygonLayer new_layer in cur_fold_info.linked_layers)
				{
					bool is_marked = false;
					foreach(PolygonLayer marked_layer in to_check_layers)
					{
						if(marked_layer == new_layer)
						{
							is_marked = true;
							break;
						}
					}
					if(!is_marked)
					{
						to_check_layers.Add(new_layer);
					}
				}
			}
			else
			{
				return false;
			}
			++ to_check_idx;
		}

		return true;
	}

	void FormatNewLayersWorld(List<PolygonLayer> fold_layers, List<FoldInfo> fold_infos, Vector2 world_head_pos, Vector2 world_toe_pos, Vector2 world_touch_dir, bool from_upside)
	{
		List<int> new_layer_depth = CalNewLayerDepth(fold_layers, from_upside);
		for (int i = 0; i != fold_layers.Count; ++i)
		{
			PolygonLayer pl = fold_layers[i];
			FoldInfo fi = fold_infos[i];
			if(pl.polygonCount == fi.fold_polygons.Count)
			{
				pl.TransformAlongEdge(world_head_pos, world_toe_pos, world_touch_dir);
			}
			else
			{
				pl.DelPolygons(fi.fold_polygons);
				PolygonLayer new_pl = CreateLayerByList(fi.fold_polygons, new_layer_depth[i]);
				new_pl.transform.SetPositionAndRotation(pl.transform.position, pl.transform.rotation);
				new_pl.TransformAlongEdge(world_head_pos, world_toe_pos, world_touch_dir);
			}
		}
	}

	List<int> CalNewLayerDepth(List<PolygonLayer> fold_layers, bool from_upside)
	{
		List<int> new_depthes = new List<int>();
		if(fold_layers.Count == 0)
		{
			return new_depthes;
		}
		int start_step_param = from_upside ? m_maxLayerDepth + 1 : m_minLayerDepth - 1;

		// 找到fold_layers中的最大最小者
		int min_depth = fold_layers[0].m_layerDepth;
		int max_depth = fold_layers[0].m_layerDepth;
		foreach(PolygonLayer pl in fold_layers)
		{
			min_depth = Mathf.Min(min_depth, pl.m_layerDepth);
			max_depth = Mathf.Max(max_depth, pl.m_layerDepth);
		}
		int end_step_param = from_upside ? max_depth : min_depth;
		for(int i = 0; i != fold_layers.Count; ++i)
		{
			int cur_depth = fold_layers[i].m_layerDepth;
			new_depthes.Add(start_step_param + (end_step_param - cur_depth));
		}

		return new_depthes;
	}

	bool FromUpside()
	{
		// todo 等有了摄像机之后，按照摄像机角度来计算这个数值
		return true;
	}

	public void FoldByEdgeInWorldByEdge(PolygonLayer pl, Vector2 world_fold_dir, Vector2 world_head_pos, Vector2 world_toe_pos)
	{
		List<PolygonLayer> fold_layers;
		List<FoldInfo> fold_infos;
		bool can_fold = CheckNeedFoldLayersWorld(pl, world_head_pos, world_toe_pos, world_fold_dir, out fold_layers, out fold_infos);
		if (can_fold)
		{
			FormatNewLayersWorld(fold_layers, fold_infos, world_head_pos, world_toe_pos, world_fold_dir, FromUpside());
		}
	}

	public void FoldByEdgeInLocalByTouchPoint(PolygonLayer pl, Vector2 local_touch_dir, Vector2 local_last_touch_point)
	{
		Vector2 local_head_pos, local_toe_pos;
		if (pl.FindFoldEdge(local_last_touch_point, ref local_touch_dir, out local_head_pos, out local_toe_pos))
		{
			List<PolygonLayer> fold_layers;
			List<FoldInfo> fold_infos;
			Vector2 world_head_pos = pl.transform.TransformPoint(local_head_pos);
			Vector2 world_toe_pos = pl.transform.TransformPoint(local_toe_pos);
			Vector2 world_touch_dir = pl.transform.TransformVector(local_touch_dir);
			bool can_fold = CheckNeedFoldLayersWorld(pl, world_head_pos, world_toe_pos, world_touch_dir, out fold_layers, out fold_infos);
			if (can_fold)
			{
				FormatNewLayersWorld(fold_layers, fold_infos, world_head_pos, world_toe_pos, world_touch_dir, FromUpside());
			}
		}
	}
}
