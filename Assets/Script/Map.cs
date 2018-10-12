using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Map {
	public string name;
	public List<Vector3> spawnPoints;

	public Map(string name,params Vector3[] spawnPoints){
		this.name=name;
		this.spawnPoints=new List<Vector3>(spawnPoints);
	}

	public Vector3 getRandomSpawnPoint(){
		return this.spawnPoints.OrderBy(x=>System.Guid.NewGuid()).First();
	}
}
