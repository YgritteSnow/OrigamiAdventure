using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrigamiOperator
{
	public OrigamiOperator()
	{
		head_pos = Vector3.zero;
		toe_pos = Vector3.zero;
		touch_dir = Vector3.zero;
		is_valid = false;
	}
	public Vector3 head_pos;
	public Vector3 toe_pos;
	public Vector3 touch_dir;

	public bool is_valid;
}

public class OrigamiCreater : MonoBehaviour {
	public List<OrigamiOperator> m_operators = new List<OrigamiOperator>();
	public List<bool> m_operatorsValid = new List<bool>();

	public GameObject m_pointSample = null;
	public OrigamiPaper m_paper = null;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void ResetOperatorCount(int count)
	{
		if(count == m_operators.Count)
		{
			return;
		}
		while(m_operators.Count < count)
		{
			m_operators.Add(new OrigamiOperator());
		}
		while(m_operators.Count > count)
		{
			m_operators.RemoveAt(m_operators.Count - 1);
		}

		ResetOrigamiPaper();
	}

	public void ResetOrigamiPaper()
	{
		ClearOrigamiPaper();
		CalOrigamiPaper();
	}

	public void ClearOrigamiPaper()
	{
		m_paper.ClearAllPolygons();
		InitNullPolygon();
	}

	void InitNullPolygon()
	{
		List<Polygon> res = new List<Polygon>();

		Polygon p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(1, 1));
		p.m_points.Add(new PolygonPoint(1, -1));
		p.m_points.Add(new PolygonPoint(-1, -1));
		p.m_points.Add(new PolygonPoint(-1, 1));

		List<PointIndexPair> edges = new List<PointIndexPair>();
		edges.Add(new PointIndexPair(0, 1));
		edges.Add(new PointIndexPair(1, 2));
		edges.Add(new PointIndexPair(2, 3));
		edges.Add(new PointIndexPair(3, 0));
		p.SetEdgeByIndexPair(edges);

		p.InitTrans();
		p.CalEdgeDistance();
		p.CalculateMesh();
		res.Add(p);

		m_paper.InitOriginalPolygons(res);
	}

	public void CalOrigamiPaper()
	{ }
}
