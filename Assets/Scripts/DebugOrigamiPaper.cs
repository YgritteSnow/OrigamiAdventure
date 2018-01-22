using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete("Use PolygonJitter instead.", true)]
public class DebugOrigamiPaper : MonoBehaviour {

	// Use this for initialization
	void Start ()
	{
		GetComponent<OrigamiPaper>().InitOriginalPolygons(DebugPolygons());
	}

	List<Polygon> DebugPolygons()
	{
		List<Polygon> res = new List<Polygon>();
		res.Add(JUtility.GetRectPolygon(1, 1));
		res.Add(JUtility.GetRectPolygon(6,0, 1,1));

		return res;
	}
}
