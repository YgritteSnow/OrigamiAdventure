using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugOrigamiPaper : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
		GetComponent<OrigamiPaper>().InitOriginalPolygons(DebugPolygons());
	}

	List<Polygon> DebugPolygons()
	{
		List<Polygon> res = new List<Polygon>();

		Polygon p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(1, 1));
		p.m_points.Add(new PolygonPoint(1, -1));
		p.m_points.Add(new PolygonPoint(-1, -1));
		p.m_points.Add(new PolygonPoint(-1, 1));

		List<PointIndexPair> edge_indexes = new List<PointIndexPair>();
		edge_indexes.Add(new PointIndexPair(0, 1));
		edge_indexes.Add(new PointIndexPair(1, 2));
		edge_indexes.Add(new PointIndexPair(2, 3));
		edge_indexes.Add(new PointIndexPair(3, 0));
		p.SetEdgeByIndexPair(edge_indexes);

		p.InitAll();
		res.Add(p);

		p = new Polygon();
		p.m_points = new List<PolygonPoint>();
		p.m_points.Add(new PolygonPoint(7, 1));
		p.m_points.Add(new PolygonPoint(7, -1));
		p.m_points.Add(new PolygonPoint(5, -1));
		p.m_points.Add(new PolygonPoint(5, 1));

		List<PointIndexPair> edges = new List<PointIndexPair>();
		edges.Add(new PointIndexPair(0, 1));
		edges.Add(new PointIndexPair(1, 2));
		edges.Add(new PointIndexPair(2, 3));
		edges.Add(new PointIndexPair(3, 0));
		p.SetEdgeByIndexPair(edges);

		p.InitAll();
		res.Add(p);

		return res;
	}
}
