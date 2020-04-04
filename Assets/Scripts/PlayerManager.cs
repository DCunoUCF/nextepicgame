﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Drawing;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; set; }

    public GameManager gm;

    // Info about player, PC, name, and gameobject
    [SerializeField]
    public PlayerClass pc;
    public string characterName;
    public string characterNameClone;
    public GameObject player;
    public bool characterSelected, characterFoundOW, inOptions;

    // Inventory stuff
    public Canvas inventoryCanvas;
    public Inventory inventory;
    public UIInventory inventoryUI;
    public GameObject InventoryPanel;
    public GameObject BioPanel;
    public GameObject uiParent;

    // Battle Overlay stuff
    public PlayerHealthBar phb;
    public GameObject battleCanvas;
    public SkillButtons sb;
    public Button opt, inv;

    // Combat information, clist, bools to guard turn logic
    public Vector3 playerLoc, selectedTile;
    public bool inCombat, isTurn, selectingSkill;
    public List<GameObject> highlights;
    public int x, y, tempCD, skillNumber;
    public int movx = 0, movy = 0;
    public bool moved;
    public bool combatInitialized, hold;
    public CList combatInfo;
    public int[] abilityinfo; // int array with {type, dmg, move}

    // References to highlight manager, to communicate with highlighted tiles in combat
    GameObject HM;
    HighlightManager HMScript;

    // Keep only one instance alive
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        this.gm = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        highlights = new List<GameObject>();
        inCombat = false;
        combatInitialized = false;
        isTurn = false;
        selectingSkill = true;
        moved = false;
        abilityinfo = new int[3];
        characterSelected = false;
    }

    void Update()
    {
        // If we're in the OW and the player gameobject has not yet been assigned
        if (!inCombat && characterSelected && !characterFoundOW)
        {
            print("finding character");
            player = GameObject.Find(characterNameClone);
            if (player != null)
                characterFoundOW = true;
        }
        else if(inCombat)// We fightin now bois
        {
            if (moved)
            {
                this.x = this.gm.bm.combatantList[0].gridX;
                this.y = this.gm.bm.combatantList[0].gridY;
                moved = false;
            }

            // Player can select what ability/move to use
            playerTurnCombat();
        }
        else // Not in combat, just check if player wants to open inventory
        {
            if (battleCanvas != null)
                if (battleCanvas.gameObject.activeSelf)
                    battleCanvas.SetActive(false);
            if (!inOptions)
            {
                if (Input.GetButtonDown("Inventory"))
                    inventoryOpen();
            }
            else
            {
                if (Input.GetButtonDown("Inventory"))
                    inv.onClick.Invoke();
            }
        }
    }

    public void initCombat()
    {
        this.player = BattleManager.Instance.player;
        playerLoc = player.transform.position;
        print(playerLoc);
        combatInfo = new CList(this.player);

        battleCanvas.gameObject.SetActive(true);

        combatInitialized = true;
        inCombat = true;
        pc.enterCombat();
        pc.setHighlights();

        HM = GameObject.Find("Highlights");
        HMScript = (HighlightManager)HM.GetComponent("HighlightManager");
        
    }

    // Fills in a few fields for PM to recognize player, as well as set up player UI
    public void initPM()
    {
        characterName = pc.name;
        characterNameClone = pc.clonename;
        characterSelected = true;
        uiParent = GameObject.Find("UIParent");

        // Inventory setup
        inventory = new Inventory(this);
        inventoryCanvas = GameObject.Find("InventoryCanvas").GetComponent<Canvas>();
        inventoryUI = (UIInventory)GameObject.Find("Inventory").GetComponent("UIInventory");
        InventoryPanel = GameObject.Find("InventoryPanel");
        BioPanel = GameObject.Find("BioPanel");

        pc.inventory = inventory;
        pc.inventory.updateStats(pc);
        pc.inventory.addItem(Item.ItemType.Sword, 5);
        pc.inventory.addItem(Item.ItemType.Ressurection, 3);
        pc.inventory.addItem(Item.ItemType.Provisions, 5);
        pc.inventory.addItem(Item.ItemType.Gold, 100);
        pc.inventory.addItem(Item.ItemType.Sword, 50);
        pc.inventory.removeItem(Item.ItemType.Sword, 50);
        pc.inventory.addItem(Item.ItemType.Provisions, 10);

        inventoryUI.gameObject.SetActive(false);

        // Setup battle overlay
        battleCanvas = GameObject.Find("BattleCanvas");
        phb = GameObject.Find("HealthFill").GetComponent<PlayerHealthBar>();
        phb.initHealthBar(pc.maxHealth);
        sb = battleCanvas.GetComponent<SkillButtons>();
        sb.initSkillButtons();
        battleCanvas.gameObject.SetActive(false);

        inv = GameObject.Find("InventoryButtonOW").GetComponent<Button>();
        opt = GameObject.Find("OptionsButtonOW").GetComponent<Button>();


        uiParent.transform.SetParent(gm.transform);
        inventoryCanvas.sortingOrder = 5;
    }

    public void inventoryOpen()
    {
        print("Opening Inventory");

        if (inventoryUI.gameObject.activeSelf)
        {
            inventoryUI.gameObject.SetActive(false);
            gm.om.dm.setInteractable();
        }
        else
        {
            gm.om.dm.setUninteractable();
            inventory.updateStats(pc);
            InventoryPanel.SetActive(true);
            BioPanel.SetActive(false); // This is set active by default already in Inventory.cs
            inventoryUI.gameObject.SetActive(true);
            inventory.setInitSelection();
        }
    }

    // Player turn -> select ability -> select tile -> turn end
    private void playerTurnCombat()
    {
        if (isTurn)
        {
            this.combatInfo = BattleManager.Instance.combatantList[0];
            // Read input and set combat info based off of what skill
            if (Input.GetButtonDown("Skill1") && pc.cooldowns[0] == 0)
            {
                useSkill(1);
            }
            else if (Input.GetButtonDown("Skill2") && pc.cooldowns[1] == 0)
            {
                useSkill(2);
            }
            else if (Input.GetButtonDown("Skill3") && pc.cooldowns[2] == 0)
            {
                useSkill(3);
            }
            else if (Input.GetButtonDown("Skill4") && pc.cooldowns[3] == 0)
            {
                useSkill(4);
            }
            else if (Input.GetButtonDown("Cancel"))
            {
                clearHighlights();
            }
            // Add more cases for more abilities
        }
    }

    public void newTurn()
    {
        isTurn = true;
        pc.updatePlayerCd();
        sb.updateButtons(pc.cooldowns);
    }

    public void useSkill(int n)
    {
        clearHighlights();
        placeHighlights(pc.getPoints(n), n);
        this.abilityinfo = pc.getInfo(n);
        fillCombatInfo(abilityinfo);
        tempCD = pc.getCD(n);
        skillNumber = n - 1; // index for setting the cooldown
    }


    private void fillCombatInfo(int[] info)
    {
        this.combatInfo.attackDmg = abilityinfo[0];
        this.combatInfo.attack = abilityinfo[1];
        this.combatInfo.move = abilityinfo[2] == 1;
        BattleManager.Instance.combatantList[0] = this.combatInfo;
    }

    // CHANGE TO LIST
    // After selecting a tile, the players turn is ended
    public void setSelectedTile(List<Vector3> pos)
    {
        if (this.isTurn)
        {
            clearHighlights();
            this.combatInfo.movTar = pos[0];
            this.combatInfo.atkTar = pos;
            this.isTurn = false;
            this.selectingSkill = true;
            pc.setCD(skillNumber, tempCD);
            BattleManager.Instance.combatantList[0] = this.combatInfo;
            clearHighlights();
        }
    }

    // Waits .3s, used to wait until child highlights are properly orphaned
    IEnumerator HODL()
    {
        yield return new WaitForSeconds(.3f);
        hold = false;
    }

    // Clears current highlights
    public void clearHighlights()
    {
        HMScript.clearTiles();
        StartCoroutine(HODL());
        if (!hold)
        {
            foreach (GameObject highlight in highlights)
                Destroy(highlight);
            highlights.Clear();
        }
        HM.transform.DetachChildren();
        hold = false;
    }

    public void placeHighlights(List<Point> points, int key)
    {
        clearHighlights();
        StartCoroutine(HODL());

        int direction = 0;

        if (this.player.transform.GetChild(0).gameObject.activeSelf) // SE
            direction = 2;
        else if (this.player.transform.GetChild(1).gameObject.activeSelf) // SW
            direction = 3;
        else if (this.player.transform.GetChild(2).gameObject.activeSelf) // NW
            direction = 0;
        else if (this.player.transform.GetChild(3).gameObject.activeSelf) // NE
            direction = 1;

        if (points.Count == 8)
            direction *= 2;


        if (!hold)
        {
            GameObject[] highlight = pc.getHighlight(key);

            int i = -1;
            foreach (Point tile in points)
            {
                i++;
                int newX = x + tile.X;
                int newY = y + tile.Y;
                if (newX < 0 || newX > BattleManager.Instance.gridCell.GetLength(1))
                    continue;

                if (newY < 0 || newY > BattleManager.Instance.gridCell.GetLength(0))
                    continue;

                if (i == 1 || i == 3)
                {
                    if (BattleManager.Instance.gridCell[newX, newY] != null)
                        if (BattleManager.Instance.gridCell[newX, newY].pass)
                            highlights.Add(Instantiate(highlight[1],
                                  BattleManager.Instance.gridCell[newX, newY].center, Quaternion.identity));
                }
                else
                {
                    if (BattleManager.Instance.gridCell[newX, newY] != null)
                            if (BattleManager.Instance.gridCell[newX, newY].pass)
                                highlights.Add(Instantiate(highlight[0],
                                      BattleManager.Instance.gridCell[newX, newY].center, Quaternion.identity));
                }
            }
            foreach (GameObject hl in highlights)
            {
                hl.transform.SetParent(HM.transform);
            }
            HMScript.setTiles(highlights, direction);
        }
        hold = true;
    }

    public string[] getSkillInfo(int i)
    {
        string[] info = new string[2];
        switch (i)
        {
            case 1:
                info[0] = pc.skill1name;
                info[1] = pc.skill1desc;
                return info;
            case 2:
                info[0] = pc.skill2name;
                info[1] = pc.skill2desc;
                return info;
            case 3:
                info[0] = pc.skill3name;
                info[1] = pc.skill3desc;
                return info;
            case 4:
                info[0] = pc.skill4name;
                info[1] = pc.skill4desc;
                return info;
            default:
                return null;
        }
    }

    // Returns the requested stat, 1 - str, 2 - int, 3 - dex
    public int getStat(string i)
    {
        return pc.getStat(i);
    }

    public int getStatModifier(string i)
    {
        return pc.getStatModifier(i);
    }

    public void takeDmg(int i)
    {
        pc.takeDamage(i);
        phb.updateHealthBar(pc.health);
    }

    public void setHealthEvent(int i)
    {
        pc.setHealthEvent(i);
    }
    public void maxHealthEvent(int i)
    {
        pc.changeMaxHealth(i);
    }
}
