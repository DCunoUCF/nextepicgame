﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
// using UnityEngine.UI;
using UnityEngine.UIElements;

public class OverworldManager : MonoBehaviour
{
    private GameManager gm;
    private GameObject player;
    public DialogueManager dm;
    public bool playerSpawned;

    public List<GameObject> nodes;
    public int playerNodeId;
    public int nodeTypeCount;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        playerSpawned = false;
        this.gm = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // If we're in the overworld for the first time, plop the player character in
        if (SceneManager.GetActiveScene().name == "Overworld" && !playerSpawned)
        {
        	this.dm = GameObject.Find("DialogueManager").GetComponent<DialogueManager>();
	        this.playerNodeId = this.dm.currentNode;
	        Debug.Log("OverworldManager sees the player at " + this.playerNodeId);

        	nodes = new List<GameObject>();

	        foreach (GameObject n in GameObject.FindGameObjectsWithTag("OWNode"))
	        {
	        	nodes.Add(n);
	        }

            spawnPlayer();
        }

        if (this.dm != null && this.playerNodeId != this.dm.currentNode)
        {
        	foreach (GameObject n in nodes)
        	{
        		this.nodeTypeCount = 0;

        		foreach (int id in n.GetComponent<WorldNode>().NodeIDs)
        		{
        			if (id == this.dm.currentNode)
        			{
        				// Move the player along the map
                        this.TurnPlayer(this.gm.pm.player, new Vector3(n.transform.position.x, n.transform.position.y, this.gm.pm.player.transform.position.z));
        				this.gm.pm.player.transform.position = new Vector3(n.transform.position.x, n.transform.position.y, this.gm.pm.player.transform.position.z);

        				// Rudimentary Camera Movement
        				GameObject cam = GameObject.Find("MainCamera");
        				cam.GetComponent<Camera>().transform.position = new Vector3(this.gm.pm.player.transform.position.x, this.gm.pm.player.transform.position.y, cam.GetComponent<Camera>().transform.position.z);

        				// Update the player node id
        				this.playerNodeId = id;

        				if (n.GetComponent<WorldNode>().NodeTypes[this.nodeTypeCount] == FlagType.Battle)
        				{
                            print("entered combat");
                            StartCoroutine(BattleEvent());
                        }

                        if (n.GetComponent<WorldNode>().NodeTypes[this.nodeTypeCount] == FlagType.Event)
                        {
                            print("entered event");
                            this.SkillSaveEvent();
                        }
                    }

                    this.nodeTypeCount++;
        		}
        	}
        }
    }



    void spawnPlayer()
    {
        player = Instantiate(Resources.Load("Prefabs/PlayerCharacters/TheWhiteKnight1", typeof(GameObject))) as GameObject;

        player.transform.position = nodes[0].transform.position;
        playerSpawned = true;
        gm.pm.initPM();
    }

    void TurnPlayer(GameObject entity, Vector3 movTar)
    {
        // How I WILL do it later entity.dir... maybe?
        float dirX, dirY;
        dirX = movTar.x - entity.transform.localPosition.x;
        dirY = movTar.y - entity.transform.localPosition.y;

        GameObject SE = entity.transform.GetChild(0).gameObject, SW = entity.transform.GetChild(1).gameObject,
                   NW = entity.transform.GetChild(2).gameObject, NE = entity.transform.GetChild(3).gameObject;

        if (dirX > 0)
        {
            if (dirY > 0)
            {
                SE.gameObject.SetActive(false);
                SW.gameObject.SetActive(false);
                NW.gameObject.SetActive(false);
                NE.gameObject.SetActive(true);
            }
            else
            {
                SE.gameObject.SetActive(true);
                SW.gameObject.SetActive(false);
                NW.gameObject.SetActive(false);
                NE.gameObject.SetActive(false);
            }

        }
        else if (dirX < 0)
        {
            if (dirY < 0)
            {
                SE.gameObject.SetActive(false);
                SW.gameObject.SetActive(true);
                NW.gameObject.SetActive(false);
                NE.gameObject.SetActive(false);
            }
            else
            {
                SE.gameObject.SetActive(false);
                SW.gameObject.SetActive(false);
                NW.gameObject.SetActive(true);
                NE.gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator BattleEvent()
    {
        this.dm.Panel.SetActive(false);
        this.gm.sm.setMusicFromDirectory("ForestBattleMusic");
        SceneManager.LoadScene("Battleworld", LoadSceneMode.Additive);
        this.gm.pm.combatInitialized = true;
        this.gm.pm.inCombat = true;

        yield return new WaitUntil(() => this.gm.bm != null);
        yield return new WaitUntil(() => this.gm.bm.isBattleResolved() == true);

        if (this.gm.bm.didWeWinTheBattle())
            this.dm.currentNode += 1;
        else
            this.dm.currentNode += 2;
        
        this.gm.pm.combatInitialized = false;
        this.gm.pm.inCombat = false;

        //SceneManager.LoadScene("Overworld", LoadSceneMode.Single);
        this.dm.Panel.SetActive(true);
        this.dm.EventComplete();
    }

    public void SkillSaveEvent()
    {
        // Check which skill the event is for from WorldNode struct
        // Maybe have difficulties in the WorldNode struct to alter how high the roll needs to be
        // Call getters to the playerClass/Manager to check the player's skill
        // Do random chance roll
        // Setter for dm.currentNode += 1(save) or += 2(fail)

        this.dm.Panel.SetActive(false);
        // Stand-in for first playable... 1 = save, 2 = fail
        int random = (Random.Range(0, 2) + 1);
        print("currentNode before:" + this.dm.currentNode);
        this.dm.currentNode += random;
        print("currentNode after:" + this.dm.currentNode);

        if (random == 1)
        {
            print("SAVE");
        }
        else if (random == 2)
        {
            print("FAIL");
            this.gm.pm.playerScript.setHealth(this.gm.pm.playerScript.getHealth() - 2);
        }

        this.dm.Panel.SetActive(true);
        this.dm.EventComplete();
    }
}
