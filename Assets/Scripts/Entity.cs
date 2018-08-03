using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
	Default,
	Destroyer,
	Corvette,
	Flagship,
	Frigate,
	Apollo,
	Bulkhead
}

public class Entity : MonoBehaviour
{
	[Header("Entity Properties")]
	[SerializeField]
	private EntityType type;
	[SerializeField]
	protected Transform swarmTarget;
	[SerializeField]
	private float selCircleSize = 1;

	protected GameObject selCircle;
	protected float selCircleSpeed;
	protected bool isSelected;
	protected bool isHovered;

	protected Controller_Commander controller;

	public EntityType Type
	{
		get
		{
			return type;
		}
	}

	// Use this for initialization
	protected void Start ()
	{
		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>();
		selCircle = Instantiate(uiManager.UnitSelCircle, transform.position, Quaternion.identity);
		//selCircle.transform.SetParent(transform);
		selCircle.SetActive(false);
		selCircle.transform.localScale = new Vector3(selCircleSize, selCircleSize, selCircleSize);
		selCircleSpeed = uiManager.UIRules.SELrotateSpeed;
	}
	
	// Update is called once per frame
	protected void Update ()
	{
		selCircle.transform.position = transform.position;
		selCircle.transform.Rotate(Vector3.up * selCircleSpeed * Time.deltaTime);
		//for (int i = 1; i <= selCircle.transform.childCount; i++)
		//{
		//	Transform tran = selCircle.GetComponentsInChildren<Transform>()[i];
		//	float posOrNeg = (i % selCircle.transform.childCount + 1);
		//	tran.eulerAngles = new Vector3(0, tran.eulerAngles.y + selCircleSpeed * Time.deltaTime * posOrNeg, 0);
		//	tran.position = transform.position;
		//}
	}

	public virtual void OnHover(bool hovered)
	{
		isHovered = hovered;
	}

	public virtual void OnSelect(bool selected)
	{
		selCircle.SetActive(selected);
		isSelected = selected;
	}

	public virtual void LinkStats(bool detailed, Controller_Commander newController)
	{
		if (detailed)
			controller = newController;
		else
			controller = null;
	}

	public float GetSelCircleSize()
	{
		return selCircleSize;
	}

	public void SetSelCircleSize(float size)
	{
		selCircleSize = size;
	}
}

public static class EntityUtils
{
	public static string GetDisplayName(EntityType type)
	{
		switch (type)
		{
			default:
				{
					return "no name";
				}
			case EntityType.Destroyer:
				{
					return "Destroyer";
				}
			case EntityType.Corvette:
				{
					return "StarLark";
				}
			case EntityType.Flagship:
				{
					return "Capital Ship";
				}
			case EntityType.Frigate:
				{
					return "Carrier";
				}
			case EntityType.Apollo:
				{
					return "Apollo-class";
				}
			case EntityType.Bulkhead:
				{
					return "Bulkhead Cruiser";
				}
		}
	}

	public static string GetDisplayDesc(EntityType type)
	{
		switch (type)
		{
			default:
				{
					return "No data found.";
				}
			case EntityType.Destroyer:
				{
					return "A warship with armor-draining technology and a powerful long-range superlaser.";
				}
			case EntityType.Corvette:
				{
					return "A small towing unit which can pull ships and shoot down enemy fighters.";
				}
			case EntityType.Flagship:
				{
					return "The hub of operations for any fleet.";
				}
			case EntityType.Frigate:
				{
					return "A frigate which carries fighters into battle. Good at close- and long-range combat.";
				}
			case EntityType.Apollo:
				{
					return "A bodyguard unit with a damage-absorbing-shield projector and ship-disabling ion missiles.";
				}
			case EntityType.Bulkhead:
				{
					return "A large chemical transport which can melt enemy armor and use resources to repair ships .";
				}
		}
	}

	public static string GetInfoBlurb(EntityType type)
	{
		switch (type)
		{
			default:
				{
					return "No data found.";
				}
			case EntityType.Destroyer:
				{
					return "The [name] Fleet Destroyer is the pinnacle of modern warship design. It is equipped with Armor Well technology, which creates a specialized interference field which destabilizes and attracts particles of common armor polymers. These fragments are then flash-vaporized, conveyed around the spacecraft, cooled, and injected into damaged armor plating. The main weapon of the [name] is the Hellrazor-I Radiation Cannon, an experimental weapon system which is powered by the unstable but powerful ephemeral radiation emitted by reactors seconds before overloading. The [name] collects this energy from hostile starships it destroys.";
				}
			case EntityType.Corvette:
				{
					return "The StarLark is a miniature towing ship which can apply its surprisingly powerful engines to pull objects much more massive than itself. Typically, these spaceships find use in mining corporations moving asteroids and cargo containers, but their cheap production cost, easily installable weapons, and mobility make them a common sight in armed forces.";
				}
			case EntityType.Flagship:
				{
					return "Flagship";
				}
			case EntityType.Frigate:
				{
					return "The [name]-II Carrier is a fast response cruiser with a powerful engine block and an extremely compact design allowing it to carry 3 full fighter squadrons in a relatively small frame. Usually deployed in pairs, these cruisers threaten to unleash massive swarms of nimble starcraft which provide cover fire for larger allied warships.";
				}
			case EntityType.Apollo:
				{
					return "The Apollo-class Escort Ship is a specialty cruiser designed by Pantheon Corporation to protect valuable assets. Using patented Surguard technology, which employs arrays of projectors to shape an electromagnetic field around a designated recipient, the Apollo can effectively shield something from projectiles and most energy attacks. It is also armed with cheap but effective ion torpedoes which can temporarily neutralize a threat.";
				}
			case EntityType.Bulkhead:
				{
					return "The Bulkhead Heavy Foundry Cruiser is a material transport with an integrated refinery to process volatile resources during the delivery period. Additionally, it is equipped with dispersal mechanisms that can release material from either of its two storage compartments in a radius around the transport or towards a target location. These functions are designed for terraforming, but some creative fleet commanders have found use for them in combat situations as well, especially for delivering metasteel vapors to damaged ally spacecraft.";
				}
		}
	}
}