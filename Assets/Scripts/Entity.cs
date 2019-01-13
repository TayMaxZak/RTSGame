using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
	Default,
	Destroyer,
	StarLark,
	Flagship,
	Carrier,
	Apollo,
	Bulkhead,
	OldEmpire,
	HybridFrigate
}

public class Entity : MonoBehaviour
{
	public bool printInfo = false;

	[Header("Entity Properties")]
	[SerializeField]
	private EntityType type;
	[SerializeField]
	protected GameObject model; // Primary visual representation of entity
	private MeshRenderer meshRenderer; // Derived from model
	[SerializeField]
	protected Transform swarmTarget; // Object that swarms will focus on
	[SerializeField]
	private float selCircleSize = 1; // Radius of selection circle (actual size is doubled)
	
	[Header("Vision")]
	[SerializeField]
	protected float visionRange = 40; // Radius in which other entities are revealed
	private byte visibilityFlags; // Stores what teams can see this unit
	private const byte TEAM_0 = 1;
	private const byte TEAM_1 = 2;
	private const byte TEAM_2 = 4;
	private const byte TEAM_3 = 8;

	protected bool localVisible = true; // Is this entity visible in this particular game instance
	protected float localOpacity; // From 0 to 1, how dissolved/solid is this entity
	private float localOpacityT = 1; // Used for lerping opacity

	protected bool isSelected; // Is this entity selected by the game instance's commander controller
	protected GameObject selCircle;
	protected float selCircleSpeed; // Rate at which the selection circle rotates
	protected bool isHovered; // Is the game instance's cursor over this entity

	protected Controller_Commander controller; // Used to display entity stats in the UI



	protected Manager_Game gameManager;
	protected GameRules gameRules;

	public EntityType Type
	{
		get
		{
			return type;
		}
	}

	void Awake()
	{
		
	}

	// Use this for initialization
	protected void Start ()
	{
		// Check for null because a subclass may have already set these fields
		if (gameManager == null)
			gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>(); // Find Game Manager
		if (gameRules == null) // Subclass may have already set this field
			gameRules = gameManager.GameRules; // Grab copy of Game Rules

		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>();

		selCircle = Instantiate(uiManager.UnitSelCircle, transform.position, Quaternion.identity);
		//selCircle.transform.SetParent(transform);
		selCircle.SetActive(false);
		selCircle.transform.localScale = new Vector3(selCircleSize, selCircleSize, selCircleSize);
		selCircleSpeed = uiManager.UIRules.SELrotateSpeed;

		meshRenderer = model.GetComponent<MeshRenderer>();
		
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

		localOpacity = Mathf.Lerp(0, 1, localOpacityT);
		localOpacityT = Mathf.Clamp01(localOpacityT + (localVisible ? 1 : -1) * Time.deltaTime * 4);
		meshRenderer.material.SetFloat("_Opacity", localOpacity);
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



	public void ClearTeamVisibility()
	{
		visibilityFlags = 0;
	}

	public void SetTeamVisibility(int otherTeam, bool visible)
	{
		// Bitwise flag i.e. team 0 = 1, team 1 = 2, team 2 = 4, team 3 = 8, etc.
		byte who = 0;
		if (otherTeam == 0)
			who = TEAM_0;
		else if (otherTeam == 1)
			who = TEAM_1;
		else if (otherTeam == 2)
			who = TEAM_2;
		else if(otherTeam == 3)
			who = TEAM_3;

		visibilityFlags = (byte)(visibilityFlags | who);
	}

	public bool VisibleBy(int otherTeam)
	{
		return (visibilityFlags & (1 << otherTeam)) != 0;
	}

	public void SetLocalVisiblity(bool newVis)
	{
		if (localVisible == newVis)
			return;

		localVisible = newVis;
		UpdateLocalVisibility();
	}



	protected virtual void UpdateLocalVisibility()
	{
		//meshRenderer.enabled = visible;
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
			case EntityType.StarLark:
				{
					return "StarLark";
				}
			case EntityType.Flagship:
				{
					return "Capital Ship";
				}
			case EntityType.Carrier:
				{
					return "Redcoat";
				}
			case EntityType.Apollo:
				{
					return "Apollo-class";
				}
			case EntityType.Bulkhead:
				{
					return "Bulkhead Cruiser";
				}
			case EntityType.OldEmpire:
				{
					return "Old Empire Frigate";
				}
			case EntityType.HybridFrigate:
				{
					return "Boxcar";
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
					return "A combat cruiser with armor-draining technology and a powerful long-range superlaser.";
				}
			case EntityType.StarLark:
				{
					return "A hybrid corvette which can pull ships around and shoot down enemy fighters.";
				}
			case EntityType.Flagship:
				{
					return "The hub of operations for any fleet.";
				}
			case EntityType.Carrier:
				{
					return "A hybrid cruiser which carries fighters into battle. Excels at long-range combat.";
				}
			case EntityType.Apollo:
				{
					return "A support frigate with a damage-absorbing-shield projector and ship-disabling ion cannons.";
				}
			case EntityType.Bulkhead:
				{
					return "A support cruiser which can melt enemy armor and use resources to repair ally ships.";
				}
			case EntityType.OldEmpire:
				{
					return "A combat frigate capable of dangerously overclocking its own systems.";
				}
			case EntityType.HybridFrigate:
				{
					return "A hybrid frigate which fills many roles. Has a low-power defensive shield and variable weapons.";
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
			case EntityType.StarLark:
				{
					return "The StarLark is a miniature towing ship which can apply its surprisingly powerful engines to pull objects much more massive than itself. Typically, these spaceships find use in mining corporations moving asteroids and cargo containers, but their cheap production cost, easily installable weapons, and mobility make them a common sight in armed forces.";
				}
			case EntityType.Flagship:
				{
					return "Flagship";
				}
			case EntityType.Carrier:
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