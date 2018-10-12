using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager {

	private static List<Map> maps=new List<Map>(new Map[]{
		new Map("gameplay",new Vector3(239.4f,0.53f,281.26f),new Vector3(239.34f,0.48f,275.44f)),
		new Map("map_2",new Vector3(10,10,10),new Vector3(20,20,20)),
		new Map("de_mark1",new Vector3(221.58f,0,243.39f),new Vector3(206.26f,0,258.3f),new Vector3(240.293f,6.069f,289.434f),new Vector3(233.8f,0,276.53f),new Vector3(236.23f,14.29f,238.33f),new Vector3(213.82f,1.43f,277.02f)),
	});

	public static Map getMapByName(string name){
		return maps.Find(x => x.name==name);
	}
}
