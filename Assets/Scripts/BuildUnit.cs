using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildUnit : MonoBehaviour
{
	public EntityType type;
	public GameObject previewObject;
	public GameObject incomingObject;
	public GameObject spawnObject;
	public int cost;
	public int unitCap = 1;
	public float buildTime;
}