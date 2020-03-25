﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager// : MonoBehaviour
{
    private BattleManager bm;
	private NPCManager myManager;

	private List<CList> enemyList;
	private List<CList> targetList;

	//==========   Constructors   ==========//

	public EnemyManager(NPCManager manager)
	{
		this.myManager = manager;
        this.bm = manager.bm;
	}

	//==========   Unity Methods   ==========//

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //==========   Combat Methods   ==========//

    public void setEntityLists(List<CList> eList, List<CList> tList)
    {
    	this.enemyList = eList;
    	this.targetList = tList;
    }

    // TODO
    public void makeDecisions()
    {
    	// foreach ENEMY in this.enemyList
    		// if this.targetList.Count == 0
    			// move randomly
    		// else
    			// closestTarget = this.targetList.At(0);
    			// foreach POT_TARGET in this.targetList
    				// if (distance to POT_TARGET) < (distance to closestTarget)
    					// closestTarget = POT_TARGET
    			// choose move towards closestTarget or attack towards closestTarget
        Debug.Log("Here's the enemyList!");
        int i = 1;
        foreach (CList e in this.enemyList)
        {
            Debug.Log("Enemy "+i+": "+e.entity.name);
            i++;
        }

        foreach (CList e in this.enemyList)
        {
            if (e.entity.GetComponent<EnemyDunce>() == null)
            {
                e.entity.AddComponent<EnemyDunce>();
                e.entity.GetComponent<EnemyDunce>().setCombatantEntry(e);
                e.entity.GetComponent<EnemyDunce>().setBattleManager(this.bm);
            }

            // e.entity.GetComponent<EnemyDunce>().moveRandomly();
            e.entity.GetComponent<EnemyDunce>().decide();

            e.movTar = e.entity.transform.position + e.entity.GetComponent<EnemyDunce>().getMoveVector();
            e.move = true;
            e.attack = -999;
            e.attackDmg = 1; // Based on what attack we pick
            Debug.Log(e.movTar);
        }

    	// Export our list of decided peoples to the NPCManager
    	this.myManager.importEnemyList(this.enemyList);
    }
}
