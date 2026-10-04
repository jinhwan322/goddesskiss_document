using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Shared;
using Shared.Regulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Shared.Battle;

/// <summary>
/// 전투 준비(부대 편성) 화면.
/// 스테이지, 결투, 레이드, 웨이브 전투 등 10여 가지 전투 타입을 하나의 화면에서 처리한다.
/// - 공통: 부대 편성(드래그로 포지션 변경), 스킬 정보와 예상 타겟 표시, 전투력/기동력 비교
/// - 타입별 UI와 버튼은 하위 파츠(Duel, Plunder, Raid, Defender, WaveBattle, CooperateBattle)가 담당
/// - 전투 타입별로 마지막 편성 덱을 로컬에 저장해 두었다가 다시 불러온다
/// </summary>
public class UIReadyBattle : UIPopup {
    public UIUser leftUser;
    public UITroop left;
    public UIDefaultListView rewardListView;
	public UITroop right;

	public UIDefaultListView skillListView;
	public UILabel skillDescription;
	public UILabel skillName;
	public UILabel unitName;
    public GameObject addSkillRoot;

	public UILabel myPower;
	public UILabel mySpeed;
	public UILabel enemyPower;
	public UILabel enemySpeed;
	public UILabel wave;

	public GameObject previousTroop;
	public GameObject nextTroop;

    public GameObject forBattleRoot;
	public GameObject leftInformation;
	public GameObject leftUnitInformation;
	public GameObject forBattleRightInfoRoot;

	public GameObject easyRoot;
	public GameObject normalRoot;
	public GameObject hardRoot;
    public GameObject attackArrow;
    public GameObject forWaveBattleRightRoot;

	public UISprite speedArrow;
	public UISprite commanderSelectBg;

    public Duel duel;
    public Plunder plunder;
	public Raid raid;
	public Defender defender;
    public WaveBattle waveBattle;
    public CooperateBattle cooperateBattle;

	public GameObject EditTroopBtn;

	public UIScrollView skillDescriptionScroll;

    [HideInInspector]
    public string listItemPrefix = "Troop-";
    public BattleData battleData { get; private set; }

	public List<UIPositionPlate> positionList;
	public List<UIPositionPlate> enemyPositionList;

	private int _selectedPositionId;
	private string _selectedCommanderId;
	public RoTroop selectedTroop { get { return _curLhsTroop; } }
	private int troopPosition;
	private string selectSkill;
	private Dictionary<int, RoCommander> helperCommander = new Dictionary<int, RoCommander>();

    public UILabel lhsWave;

    public GameObject lhsMovePrevTroop;
    public GameObject lhsMoveNextTroop;
    public GameObject disableMarker;

    private int _curLhsTroopIndex;
	private RoTroop _curLhsTroop { get { return lhsTroops[_curLhsTroopIndex]; }}

    [HideInInspector]
    public List<RoTroop> lhsTroops;           

    public UIEngageInfoPopup engageInfo;
	public List<UICommanderSelectItem> commanderList;

    protected override void Awake()
    {
        base.Awake();

        lhsTroops = new List<RoTroop>();
    }

    /// <summary>아군 웨이브 부대 이전/다음 이동 (웨이브 결투, 이벤트 전투처럼 부대가 여러 개일 때)</summary>
    public void LhsMovePrevTroop()
    {
        if (_curLhsTroopIndex <= 0)
            return;

        --_curLhsTroopIndex;

        LhsNextTroop();
    }

    public void LhsMoveNextTroop()
    {
        if (_curLhsTroopIndex >= lhsTroops.Count - 1)
            return;

        ++_curLhsTroopIndex;

        LhsNextTroop();
    }

	/// <summary>적 진영 플레이트의 상성 표시를 초기화한다.</summary>
	private void OnRelease()
	{
		if(battleData.type == EBattleType.Raid || battleData.type == EBattleType.EventRaid)
			return;

		for (int i = 0; i < right.positionPlateList.Count; i++)
		{
			right.positionPlateList[i].ResetIncompatible();
			right.positionPlateList[i].IsActiveHpRoot(battleData.type == EBattleType.Annihilation);
		}
	}

	/// <summary>아군 진영 플레이트를 초기화한다.</summary>
	private void OnResetPlate()
	{
		for(int i = 0; i < positionList.Count; i++)
		{
			positionList[i].Reset();
			positionList[i].IsActiveHpRoot(battleData.type == EBattleType.Annihilation);
		}
	}

    /// <summary>화면 공통 버튼 처리. 처리하지 않은 버튼은 전투 타입별 하위 파츠로 전달한다.</summary>
    public override void OnClick(GameObject sender)
    {
        base.OnClick(sender);
        string key = sender.name;

		if (key == "Close")
        {
            CloseAnimation();
		}
        else if (key == "ReadyTroop")
        {
			UIPopup.Create<UISelectDeckPopup>("SelectDeckPopup").Init(battleData);
        }
        else if (skillListView.Contains(key))
        {
			string id = skillListView.GetPureId(key);
			if(string.Equals(id, "0"))
				return;

			selectSkill = id;
			skillListView.SetSelection(id, true);
			SetSkillInfo();

            SoundManager.PlaySFX("BTN_Norma_001");

		}
        else if (key.StartsWith("Plate-"))
        {
			int position = int.Parse(key.Substring(key.IndexOf("-") + 1));
			RoTroop.Slot slot = _curLhsTroop.GetSlotByPosition(position);
			if (slot != null)
			{
				if(_selectedPositionId != position)
				{
					_selectedPositionId = position;
					InitSkillData();
					SetSkillInfo();
				}
			}
            SoundManager.PlaySFX("BTN_Norma_001");
		}
		else if (key.StartsWith("CommanderSelect-"))
		{
			string id = key.Substring(key.IndexOf("-") + 1);
			RoTroop.Slot slot = _curLhsTroop.GetSlotByCommanderId(id);
			if(slot != null)
			{
				if(_selectedPositionId != slot.position)
				{
					_selectedPositionId = slot.position;
					InitSkillData();
					SetSkillInfo();
				}
			}
		}
        else if (key == "PreviousTroop")
        {
			troopPosition--;
			NextTroop();
		}
        else if (key == "NextTroop")
        {
			troopPosition++;
			NextTroop();
		}
        else if (key == "LhsPrevTroop")
        {
            LhsMovePrevTroop();            
        }
        else if (key == "LhsNextTroop")
        {
            LhsMoveNextTroop();            
        }
		else if(key == "UseConquest")
		{
			NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110322"));
		}
		else if(key == "CommanderSelect")
		{
			OpenCommanderList();
		}
        else
        {
            SendOnClickToInnerParts(sender);
        }
    }
    
    private void _Set(BattleData _battleData)
    {
        if (_battleData != null) {
            battleData = _battleData;
            RoUser defender = battleData.defender;
            if(defender != null)
            {
                if (defender.battleTroopList.Count < 1)
                {
                    Debug.LogError("invalid state");
                }
            }
        }
        UISetter.SetUser(leftUser, localUser);
    }   

	private void _SelectTroop()
	{
		left.Set(_curLhsTroop);
	}

	/// <summary>적 웨이브 부대를 바꿨을 때 화면을 갱신한다.</summary>
	private void NextTroop()
	{
		if(battleData.type == EBattleType.Raid || battleData.type == EBattleType.WaveBattle || battleData.type == EBattleType.EventRaid)
			return;
		
		UISetter.SetLabel(wave, Localization.Format("5628", troopPosition + 1));
        ResetDragObject();
		ResetTargetPlate();
		UpdateArrow();
		OnRefresh();
	}

    /// <summary>아군 웨이브 부대를 바꿨을 때 스킬 정보와 화면을 갱신한다.</summary>
    private void LhsNextTroop()
    {
		if (battleData.type == EBattleType.Raid || battleData.type == EBattleType.WaveBattle || battleData.type == EBattleType.EventRaid)
            return;

        UISetter.SetLabel(lhsWave, Localization.Format("5628", _curLhsTroopIndex + 1));


        if (_curLhsTroop.IsEmpty())
        {
            OnRelease();
            ResetTargetPlate();
            ResetSkillData();
        }
        else
        {
            _selectedPositionId = _curLhsTroop.slots[0].position;
            if (_selectedPositionId <= 0)
            {
                _selectedPositionId = _curLhsTroop.GetNotEmptySlotPosition();                
            }

            if (_selectedPositionId >= 0)
            {
                InitSkillData();
            }
        }

        ResetDragObject();
        LhsUpdateArrow();
        OnRefresh();
    }

    private void LhsUpdateArrow()
    {
        UISetter.SetActive(lhsMovePrevTroop, _curLhsTroopIndex > 0);
        UISetter.SetActive(lhsMoveNextTroop, _curLhsTroopIndex < lhsTroops.Count - 1);

        left.Set(lhsTroops[_curLhsTroopIndex]);
    }

    /// <summary>
    /// 출전할 수 없는 부대 구성인지 검사한다.
    /// 첫 부대가 비어 있거나, 빈 부대 뒤에 다시 편성된 부대가 있으면(중간이 빈 웨이브) true.
    /// </summary>
    public bool IsEmptyTroop()
    {        
        bool prevEmpty = false;
        bool isEmpty = true;
        for (int i = 0; i < lhsTroops.Count; ++i)
        {
            bool emptyTroop = lhsTroops[i].IsEmpty();
            if (i == 0 && emptyTroop)
            {
                isEmpty = true;
                break;
            }

            if (prevEmpty && !emptyTroop)
            {
                isEmpty = true;
                break;
            }

            isEmpty = false;
            prevEmpty = emptyTroop;
        }

        return isEmpty;
    }

	/// <summary>적 부대 이동 화살표와 적 부대 표시를 갱신한다. (웨이브 결투 마지막 부대, 최강 결투장 부대는 비공개)</summary>
	private void UpdateArrow()
	{        
		UISetter.SetActive(previousTroop, troopPosition > 0);
		UISetter.SetActive(nextTroop, troopPosition < battleData.defender.battleTroopList.Count - 1);

        /**
         * 웨이브듀얼 마지막 부대는 비공개
         */
        if (battleData.type == EBattleType.WaveDuel)
        {
            if (troopPosition < (ConstValue.waveDuelTroopCount - 1))
            {
                UISetter.SetActive(disableMarker, false);
                right.Set(battleData.defender.battleTroopList[troopPosition]);
            }
            else
            {
                UISetter.SetActive(disableMarker, true);
                right.Set(RoTroop.Create(localUser.id, null));                
            }
        }
        else if (battleData.type == EBattleType.WorldDuel && !battleData.worldDuelReMatch)
        {
            //최강 결투장 부대는 비공개
            UISetter.SetActive(disableMarker, true);
            right.Set(RoTroop.Create(localUser.id, null));  
        }
        else
        {
            right.Set(battleData.defender.battleTroopList[troopPosition], battleData.type);
        }
	}

    /// <summary>
    /// 전투 준비 화면 진입점. 전투 타입(BattleData.type)에 따라
    /// 아군 부대 수 결정 → 덱 구성(방어 덱 / 도우미 / 점령전) → 타입별 UI 활성화 → 화면 열기 순서로 초기화한다.
    /// </summary>
    public void InitAndOpenReadyBattle(BattleData _battleData)
    {
        if (_battleData == null) {
            Debug.LogError("invalid state");
            return;
        }

        if (_battleData.type == EBattleType.Undefined) {
            Debug.LogError("unknown type");
            return;
        }

        GameSetting.instance.repeatBattle = false;

        _curLhsTroopIndex = 0;

        lhsTroops.Clear();
        lhsTroops.Add(RoTroop.Create(localUser.id, null));        

        if (_battleData.type == EBattleType.WaveDuel
            || _battleData.type == EBattleType.WaveDuelDefender)
        {
            for (int i = lhsTroops.Count; i < ConstValue.waveDuelTroopCount; ++i)
            {
                lhsTroops.Add(RoTroop.Create(localUser.id, null));
            }                        
		}
		else if(_battleData.type == EBattleType.EventBattle)
		{
			EventBattleFieldDataRow row = regulation.FindEventBattle(_battleData.eventId, _battleData.eventLevel);
			for (int i = lhsTroops.Count; i < row.weWave; ++i)
			{
				lhsTroops.Add(RoTroop.Create(localUser.id, null));
			}
		}

        if (lhsTroops.Count > 1)
        {
            UISetter.SetActive(lhsMovePrevTroop, _curLhsTroopIndex > 0);
            UISetter.SetActive(lhsMoveNextTroop, _curLhsTroopIndex < lhsTroops.Count - 1);            
                
            if (lhsWave != null)
            {
                UISetter.SetActive(lhsWave.gameObject, true);
                UISetter.SetLabel(lhsWave, Localization.Format("5628", _curLhsTroopIndex + 1));
            }            
        }
        else
        {
            UISetter.SetActive(lhsMoveNextTroop, false);
            UISetter.SetActive(lhsMovePrevTroop, false);

            if(lhsWave != null)            
                UISetter.SetActive(lhsWave.gameObject, false);            
        }

        UISetter.SetActive(engageInfo.gameObject, false);

		InitValue();
        _Set(_battleData);
        InitTroopPosition();
        OnResetPlate();
        
        if (battleData.type == EBattleType.Defender || battleData.type == EBattleType.WaveDuelDefender || battleData.type == EBattleType.WorldDuelDefender)
        {
            SelectDefenderTroop();
        }
		else if(battleData.type == EBattleType.WaveBattle || battleData.type == EBattleType.Guerrilla || battleData.type == EBattleType.EventBattle)
        {
            SetHelperDeck();
        }
		else if(battleData.type == EBattleType.Conquest)
		{
			SelectConquestTroop();
		}    
			
		switch (battleData.type)
		{
		case EBattleType.Plunder:
		case EBattleType.Raid:
		case EBattleType.WaveBattle:
		case EBattleType.EventBattle:
			localUser.EngageCommander.Clear();
			break;                
		}      

		UISetter.SetActive(forBattleRoot, battleData.type != EBattleType.Raid && battleData.type != EBattleType.WaveBattle && battleData.type != EBattleType.EventRaid);
		UISetter.SetActive(forBattleRightInfoRoot, (battleData.type != EBattleType.Defender 
            && battleData.type != EBattleType.WaveDuelDefender
            && battleData.type != EBattleType.WorldDuelDefender
            && battleData.type != EBattleType.WorldDuel
            && battleData.type != EBattleType.Conquest)
            || battleData.type == EBattleType.WorldDuel && battleData.worldDuelReMatch);
		UISetter.SetActive(duel, battleData.type == EBattleType.Duel 
            || battleData.type == EBattleType.WaveDuel
            || (battleData.type == EBattleType.WorldDuel)
            || battleData.type == EBattleType.Annihilation
		|| battleData.type == EBattleType.InfinityBattle);
		UISetter.SetActive(plunder, battleData.type == EBattleType.Plunder || battleData.type == EBattleType.SeaRobber || battleData.type == EBattleType.Guerrilla || battleData.type == EBattleType.EventBattle);
		UISetter.SetActive(defender, battleData.type == EBattleType.Defender 
            || battleData.type == EBattleType.WaveDuelDefender
            || battleData.type == EBattleType.WorldDuelDefender
			|| battleData.type == EBattleType.Conquest);
		UISetter.SetActive(raid, battleData.type == EBattleType.Raid || battleData.type == EBattleType.EventRaid);
		UISetter.SetActive(raid.readyRoot, false);
		UISetter.SetActive(raid.startRoot, false);


		UISetter.SetActive(leftInformation, battleData.type != EBattleType.Defender &&
											battleData.type != EBattleType.Conquest &&
											battleData.type != EBattleType.Duel &&
                                            battleData.type != EBattleType.WorldDuel &&
                                            battleData.type != EBattleType.WaveDuelDefender &&
                                            battleData.type != EBattleType.WorldDuelDefender &&
                                            battleData.type != EBattleType.WaveDuel &&
											battleData.type != EBattleType.Annihilation &&
                                            battleData.type != EBattleType.InfinityBattle &&
											battleData.type != EBattleType.Raid &&
											battleData.type != EBattleType.EventRaid &&
                                            battleData.type != EBattleType.CooperateBattle);

		UISetter.SetActive(leftUnitInformation, true);
		UISetter.SetActive(plunder.readyRoot, false);
		UISetter.SetActive(plunder.startRoot, false);

        UISetter.SetActive(waveBattle, battleData.type == EBattleType.WaveBattle);
        UISetter.SetActive(waveBattle.readyRoot, false);
        UISetter.SetActive(waveBattle.startRoot, false);
        UISetter.SetActive(attackArrow, battleData.type != EBattleType.WaveBattle);
        UISetter.SetActive(forWaveBattleRightRoot, battleData.type == EBattleType.WaveBattle);

		UISetter.SetActive(EditTroopBtn, battleData.type != EBattleType.Annihilation && battleData.type != EBattleType.Guerrilla && battleData.type != EBattleType.Conquest && battleData.type != EBattleType.EventBattle);
        UISetter.SetActive(cooperateBattle, battleData.type == EBattleType.CooperateBattle);

		UISetter.SetActive(disableMarker, false);

		UISetter.SetLabel(title, Localization.Get("1252"));
		commanderSelectBg.height = (battleData.type == EBattleType.Annihilation ? 217 : 193);        

		Open();
		SendOnInitToInnerParts();
		NextTroop();
		SetCommanderSlot();
    }

    #region 슬롯 / 지휘관 헬퍼

    /// <summary>
    /// 보유 지휘관 정보로 부대 슬롯을 채운다. (유닛 정보, 장비, 스킬 레벨/SP)
    /// 저장 덱 불러오기, 점령전 덱, 선택 덱 적용에서 공통으로 사용한다.
    /// </summary>
    private void FillSlotFromCommander(RoTroop.Slot slot, RoCommander commander, int position)
    {
        slot.unitId = commander.unitId;
        slot.unitLevel = commander.level;
        slot.exp = commander.aExp;
        slot.health = commander.hp;
        slot.commanderId = commander.id;
        slot.unitCls = commander.cls;
        slot.unitCostume = commander.currentCostume;
        slot.favorRewardStep = commander.favorRewardStep;
        slot.marry = commander.marry;
        slot.transcendence = commander.transcendence;
        slot.unitRank = commander.rank;
        slot.position = position;
        slot.charType = commander.charType;
        slot.userIdx = commander.userIdx;
        slot.equipItem = commander.GetEquipItemList();
        slot.weaponItem = commander.WeaponItem;

        for (int i = 0; i < commander.unitReg.skillDrks.Count; i++)
        {
            Troop.Slot.Skill skill = new Troop.Slot.Skill();
            SkillDataRow row = regulation.skillDtbl[commander.unitReg.skillDrks[i]];
            if (row.isActiveSkillType)
                skill.sp = commander.sp;
            skill.id = commander.unitReg.skillDrks[i];
            skill.lv = commander.skillList[i];

            slot.skills.Add(skill);
        }
    }

    /// <summary>용병(유저 용병 / NPC 용병) 슬롯인지 확인한다.</summary>
    private static bool IsMercenary(RoTroop.Slot slot)
    {
        return slot.charType == ECharacterType.Mercenary || slot.charType == ECharacterType.NPCMercenary;
    }

    /// <summary>슬롯 타입(용병 / 도우미 / 보유 지휘관)에 맞는 곳에서 지휘관 정보를 찾는다.</summary>
    private RoCommander GetSlotCommander(RoTroop.Slot slot)
    {
        if (IsMercenary(slot))
            return localUser.FindMercenaryCommander(slot.commanderId, slot.userIdx, slot.charType);
        if (slot.charType == ECharacterType.Helper)
            return helperCommander[slot.position];
        return localUser.FindCommander(slot.commanderId);
    }

    /// <summary>방어 덱 편성 계열(방어 / 웨이브 결투 방어 / 최강 결투 방어 / 점령전)인지 확인한다.</summary>
    private static bool IsDefenderType(EBattleType type)
    {
        return type == EBattleType.Defender
            || type == EBattleType.WaveDuelDefender
            || type == EBattleType.WorldDuelDefender
            || type == EBattleType.Conquest;
    }

    #endregion

    /// <summary>게릴라 / 웨이브 / 이벤트 전투에서 기획 데이터의 도우미(NPC) 유닛을 아군 부대에 배치한다.</summary>
	void SetHelperDeck()
	{
        EnemyCommanderDataRow helper;
        List<EnemyUnitDataRow> unitList = new List<EnemyUnitDataRow>();

		helperCommander.Clear();

		if(battleData.type == EBattleType.Guerrilla)
        {
            SweepDataRow sweepData = regulation.FindSweepRow(battleData.sweepType, battleData.sweepLevel);

            helper = regulation.enemyCommanderDtbl.Find(row => row.id == sweepData.helper);
            unitList = regulation.enemyUnitDtbl.FindAll(row => row.id == helper.id);
        }
		else if(battleData.type == EBattleType.WaveBattle)
        {
            WaveBattleDataRow wave_db = regulation.FindWaveBattleData(int.Parse(battleData.stageId));
            unitList = regulation.enemyUnitDtbl.FindAll(row => row.id == wave_db.helper.ToString());
        }
		else if(battleData.type == EBattleType.EventBattle)
		{
			EventBattleFieldDataRow eventBattle = regulation.eventBattleFieldDtbl[string.Format("{0}_{1}", battleData.eventId, battleData.eventLevel)];                
			if(eventBattle.helper != 0)
				unitList = regulation.enemyUnitDtbl.FindAll(row => row.id == eventBattle.helper.ToString());
		}

        for (int idx = 0; idx < unitList.Count; ++idx)
        {
            RoTroop.Slot slot = _curLhsTroop.GetNextEmptySlot();
            var unitRow = unitList[idx];
            CommanderDataRow commanderRow = regulation.GetCommanderByUnitId(unitRow.unitId);
            if (commanderRow == null)
                commanderRow = regulation.commanderDtbl["1"];   // 매칭되는 지휘관 데이터가 없으면 기본 지휘관으로 대체
            RoCommander commander = RoCommander.Create(commanderRow.id, unitRow.unitLevel, unitRow.unitGrade, unitRow.unitClass, 0, 0, 0, new List<int>());

            slot.unitId = unitRow.unitId;
            slot.commanderId = commanderRow.id;
            slot.unitLevel = unitRow.unitLevel;
            slot.health = commander.currLevelUnitReg.maxHealth;
            slot.unitCls = unitRow.unitClass;
            slot.unitRank = unitRow.unitGrade;
            slot.charType = ECharacterType.Helper;
            slot.position = unitRow.unitPosition - 1;
            slot.scale = (float)unitRow.unitScale / 100f;

            UnitDataRow unitReg = regulation.unitDtbl[unitRow.unitId];
            for (int i = 0; i < unitReg.skillDrks.Count; i++)
            {
                if (unitReg.skillDrks[i] == "0")
                    continue;

                Troop.Slot.Skill skill = new Troop.Slot.Skill();
                skill.id = unitReg.skillDrks[i];
                skill.lv = unitRow.skillLevel[i];

                slot.skills.Add(skill);
                commander.skillList[i] = unitRow.skillLevel[i];
            }
            helperCommander.Add(slot.position, commander);
        }
	}

	/// <summary>점령전용으로 저장된 덱을 현재 부대에 적용한다.</summary>
	void SetConquestDeck()
	{
		int id = battleData.conquestDeckId;

		if(localUser.conquestDeck[id] != null)
		{
			foreach(var data in localUser.conquestDeck[id].deck)
			{
				RoCommander commander = localUser.FindCommander(data.Value);
				FillSlotFromCommander(_curLhsTroop.GetNextEmptySlot(), commander, int.Parse(data.Key) - 1);
			}
		}
	}

    /// <summary>
    /// 전투 타입별로 로컬(PlayerPrefs)에 저장해 둔 덱을 불러와 부대를 구성한다.
    /// 저장 형식: { 부대 번호: { 포지션: 지휘관 ID } } (JSON)
    /// 지휘관 상태가 바뀌었거나 슬롯이 겹치는 등 저장 덱이 유효하지 않으면 삭제하고 초기화한다.
    /// </summary>
    void LoadStageDeck()
	{
		string key = "";
		if(battleData.type == EBattleType.Guerrilla)
		{
			key = string.Format("SweepDeck_{0}_{1}", battleData.sweepType, battleData.sweepLevel);
		}
		else if(battleData.type == EBattleType.EventBattle)
		{
			int exsitEventDeck = Utility.GetEventBattleDeckIndex(battleData.eventId);
			if(exsitEventDeck != -1)
				key = string.Format("EventBattleDeck_{0}", exsitEventDeck);
		}
		else if(battleData.type == EBattleType.Plunder)
		{
			key = "StageDeck";
		}
		else if(battleData.type == EBattleType.Duel)
		{
			key = "DuelDeck";
		}
		else if(battleData.type == EBattleType.Raid)
		{
			key = "RaidDeck";
		}
		else if(battleData.type == EBattleType.Annihilation)
		{
			key = "AnnihilationDeck";
        }         
        else if (battleData.type == EBattleType.WaveDuel)
        {
            key = "WaveDuelDeck";
        }
        else if (battleData.type == EBattleType.CooperateBattle)
        {
            key = "CooperateBattleDeck";
        }
		else if (battleData.type == EBattleType.EventRaid)
		{
			key = "EventRaidBattleDeck";
		}
        else if (battleData.type == EBattleType.WorldDuel)
        {
            key = "WorldDuelDeck";
        }
        else if (battleData.type == EBattleType.InfinityBattle)
        {
            key = "InfinityBattleDeck";
        }
		
		string saveDeck = PlayerPrefs.GetString(key);

		if(!string.IsNullOrEmpty(saveDeck))
		{
            Dictionary<string, Dictionary<string, string>> savedDecks;
            try
            {
				if(battleData.type == EBattleType.EventBattle)
					saveDeck = saveDeck.Split('_')[1]; //두번째 index.
                savedDecks = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(saveDeck);
            }
            catch (System.Exception)
            {
                // 이전 버전 형식(부대 1개짜리 덱)으로 저장된 경우 1번 부대 덱으로 변환
                savedDecks = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> deck =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(saveDeck);
                savedDecks.Add("1", deck);
            }


            Dictionary<string, Dictionary<string, string>>.Enumerator itr = savedDecks.GetEnumerator();
            while (itr.MoveNext())
            {
                int idx = int.Parse(itr.Current.Key) - 1;

                if (idx >= lhsTroops.Count)
                    break;

                Dictionary<string, string> deck = itr.Current.Value;
				if (battleData.type == EBattleType.Guerrilla || battleData.type == EBattleType.EventBattle)
                {
                    if (lhsTroops[idx].NotEmptySlotCount + deck.Count > RoTroop.CommonUnitSlotCount)
                    {
                        DeleteSaveDeck(key);
                        return;
                    }
                }


                foreach (var data in deck)
                {
                    RoCommander commander = localUser.FindCommander(data.Value);
                    if (commander.state != ECommanderState.Nomal)
                    {
                        DeleteSaveDeck(key);
                        return;
                    }
                    else
                    {
                        if (battleData.type == EBattleType.Annihilation)
                        {
                            if (commander.isDie)
                                continue;
                        }
						else if(battleData.type == EBattleType.EventBattle)
						{
							EventBattleFieldDataRow row = regulation.FindEventBattle(battleData.eventId, battleData.eventLevel);

							// 이벤트 전투 직업 제한: Attack/Defense/Support는 해당 직업만 출전, *_x는 해당 직업 출전 불가
							if(row.job != EJob.Undefined && row.job != EJob.All)
							{
								if((int)row.job <= (int)EJob.Support)
								{
									if(commander.job != row.job)
										continue;
								}
								else 
								{
									EJob banJob = EJob.Undefined;
									if(row.job == EJob.Attack_x)
										banJob = EJob.Attack;
									else if(row.job == EJob.Defense_x)
										banJob = EJob.Defense;
									else if(row.job == EJob.Support_x)
										banJob = EJob.Support;

									if(commander.job == banJob)
										continue;
								}
							}
						}
                    }

                    if (lhsTroops[idx].GetSlotByPosition(int.Parse(data.Key) - 1) != null)
                    {
                        DeleteSaveDeck(key);
                        return;
                    }

                    FillSlotFromCommander(lhsTroops[idx].GetNextEmptySlot(), commander, int.Parse(data.Key) - 1);
                }
            }
		}

        if (!_curLhsTroop.IsEmpty())
        {
            _selectedPositionId = _curLhsTroop.slots[0].position;
            InitSkillData();
            OnRefresh();
        }
        else
        {
            ResetSkillData();
            OnRelease();
        }
    }

	/// <summary>외부(지휘관 목록)에서 편성이 바뀌었을 때 지휘관 슬롯 표시를 갱신한다.</summary>
	public void SetCurrentTroop(RoTroop troop)
	{
		SetCommanderSlot();
	}

	/// <summary>하단 지휘관 슬롯 목록을 현재 부대 기준으로 갱신하고, 선택 슬롯이 유효하지 않으면 다시 선택한다.</summary>
	void SetCommanderSlot()
	{
		for(int i = 0; i < commanderList.Count; i++)
		{
			UICommanderSelectItem item = commanderList[i];
			RoTroop.Slot slot = _curLhsTroop.slots[i];
			RoCommander commander = GetSlotCommander(slot);
			if (IsMercenary(slot))
				item.Set(commander, _curLhsTroop, battleData, slot.charType, i);
			else
				item.Set(commander, _curLhsTroop, battleData, slot.charType);
		}
			
		if(_curLhsTroop.IsEmpty())
			ResetSkillData();
		else
		{
			RoTroop.Slot slot = _curLhsTroop.GetSlotByPosition(_selectedPositionId);
			if(_selectedPositionId == -1 || slot == null || slot.commanderId != _selectedCommanderId)
			{
				_selectedPositionId = _curLhsTroop.GetNotEmptySlotPosition();
				InitSkillData();
			}
		}
	}

    /// <summary>선택한 덱을 적용할 때 다른 웨이브 부대와 지휘관이 겹치지 않는지 확인한다.</summary>
    public bool CanStageDeck(Dictionary<string, int> preDeck)
    {        
        for (int i = 0; i < lhsTroops.Count; ++i)
        {
            if (i == _curLhsTroopIndex)
                continue;

            Dictionary<string, int>.Enumerator itr = preDeck.GetEnumerator();
            while (itr.MoveNext())
            {
                if (lhsTroops[i].GetSlotByCommanderId(itr.Current.Value.ToString()) != null)
                {
                    return false;
                }
            }            
        }

        return true;
    }

    /// <summary>선택한 덱(포지션 → 지휘관 ID)을 현재 부대에 적용한다.</summary>
    public void SetStageDeck(Dictionary<string, int> preDeck)  
    {
        _selectedPositionId = -1;
		_selectedCommanderId = "";
        foreach (var slot in _curLhsTroop.slots)
        {
            slot.ResetSlot();
        }
        Dictionary<string, int> stageDeck = preDeck;

        int idx = 0;
        if (stageDeck.Count > 0)
        {
            foreach (var data in stageDeck)
            {
                RoCommander commander = localUser.FindCommander(data.Value.ToString());

				if(battleData.type == EBattleType.Annihilation)
				{
					if(!commander.isAdvancePossible || commander.isDie)
						continue;
				}

                FillSlotFromCommander(_curLhsTroop.slots[idx], commander, int.Parse(data.Key) - 1);
                idx++;
            }

        }
        if (!_curLhsTroop.IsEmpty())
        {
            _selectedPositionId = _curLhsTroop.slots[0].position;
            InitSkillData();
        }
        else
        {
            ResetSkillData();
            OnRelease();
        }
		OnRefresh();
    }

    /// <summary>유효하지 않은 저장 덱을 삭제하고 부대를 초기화한다. (도우미가 있는 전투는 도우미를 다시 배치)</summary>
    private void DeleteSaveDeck(string key)
	{
		PlayerPrefs.DeleteKey(key);
        for (int i = 0; i < lhsTroops.Count; ++i)
        {
            lhsTroops[i].ResetSlots();
        }
		ResetSkillData();
		OnRelease();

		if(battleData.type == EBattleType.Guerrilla || battleData.type == EBattleType.EventBattle || battleData.type == EBattleType.WaveBattle)
		{
			SetHelperDeck();
		}
	}

	/// <summary>
	/// 부대 편성이 "가득 찼는지" 검사한다. 슬롯이 모두 찼거나 출전 가능한 지휘관을 전부 넣었으면 가득 찬 것으로 본다.
	/// false면 전투 시작 전에 확인 팝업을 띄운다.
	/// </summary>
	bool FullUnitCheck()
	{
        int cnt = 0;
		int sweepCnt = 0; //NPC를 제외한 슬롯 수.
		for (int i = 0; i < lhsTroops.Count; ++i)
        {            
            RoTroop troop = lhsTroops[i];
            for (int j = 0; j < troop.slots.Length; ++j)
            {
                RoTroop.Slot slot = troop.slots[j];
				if (slot.IsValidId())
                {
                    ++cnt;                    
                }
				if (slot.IsValidId() && slot.charType != ECharacterType.Helper)
				{
					++sweepCnt;                    
				}
            }
        }
                       
        if (battleData.type == EBattleType.Annihilation)
            return cnt == (RoTroop.CommonUnitSlotCount * lhsTroops.Count) || cnt == localUser.GetAnnihilationCommanderCount();
		else if (battleData.type == EBattleType.Conquest)
            return cnt == (RoTroop.CommonUnitSlotCount * lhsTroops.Count);
		else if (battleData.type == EBattleType.Guerrilla)
		{
			SweepDataRow sweepData = regulation.FindSweepRow(battleData.sweepType, battleData.sweepLevel);
			return cnt == (RoTroop.CommonUnitSlotCount * lhsTroops.Count) || sweepCnt == localUser.GetGuerrillaCommanderCount((EJob)sweepData.type);
		}
        else
            return cnt == (RoTroop.CommonUnitSlotCount * lhsTroops.Count) || cnt == localUser.GetCommanderCount();
	}

	private void InitValue()
	{
		_selectedPositionId = -1;
		_selectedCommanderId = "";
        _curLhsTroopIndex = 0;
	}

    private void InitTroopPosition()
    {
        troopPosition = GetInitTroopPosition();
    }
    /// <summary>섬멸전은 이미 처치한 웨이브의 다음 부대부터 보여준다.</summary>
    private int GetInitTroopPosition()
    {
        if (battleData.type == EBattleType.Annihilation)
        {
            int enemyListCount = 0;

            AnnihilateBattleDataRow annihilate = RemoteObjectManager.instance.regulation.annihilateBattleDtbl.Find(row => row.idx == battleData.stageId);
            if(annihilate != null)
            {                                
                for(int i = 0; i < battleData.defender.battleTroopList.Count; ++i)
                {
                    int slotCount = 0;
                    for(int slotNum = 0; slotNum < battleData.defender.battleTroopList[i].slots.Length; ++slotNum)
                    {
                        if (string.IsNullOrEmpty(battleData.defender.battleTroopList[i].slots[slotNum].unitId))
                            slotCount += 1;                                                                      
                    }

                    if (slotCount == battleData.defender.battleTroopList[i].slots.Length)
                        enemyListCount += 1;
                }

                if (enemyListCount != 0 && enemyListCount < annihilate.battleWave)
                    return enemyListCount;
                else
                    return 0;              
            }            
        }
        else
            return 0;

        return 0;
    }

    public override void OnRefresh()
    {
        base.OnRefresh();
        _Set(battleData);
        SendOnRefreshToInnerParts();
        if (_curLhsTroop != null)
        {
			SetCommanderSlot();
            _SelectTroop();
            SetInfoLabel();
            SetSkillInfo();
        }
    }

	/// <summary>
	/// 아군/적군 전투력과 기동력(선공 수치)을 표시하고,
	/// 전투력 차이 비율로 난이도(쉬움/보통/어려움)와 선공 방향 화살표를 정한다.
	/// </summary>
	void SetInfoLabel()
	{        
		int mPower = _curLhsTroop.GetTotalPower();
        int mSpeed = _curLhsTroop.GetTotalSpeed();
        int mFirstSpeed = mSpeed;
        if (_curLhsTroopIndex != 0)
        {
            mFirstSpeed = lhsTroops[0].GetTotalSpeed();
        }

		int ePower = 0;
        int eSpeed = 0;
        int eFirstSpeed = 0;
        if (battleData.type != EBattleType.WaveBattle || (battleData.type == EBattleType.WorldDuel && battleData.worldDuelReMatch))
        {
            if (battleData.type == EBattleType.Plunder || battleData.type == EBattleType.Guerrilla)
            {
                string eid = battleData.defender.id.Substring(battleData.defender.id.IndexOf("-") + 1);
                var enemy = regulation.enemyCommanderDtbl.Find(row => row.id == eid && row.wave == troopPosition + 1);
                ePower = enemy.power;
            }
            else if (battleData.type == EBattleType.CooperateBattle)
            {
                ePower = regulation.cooperateBattleDtbl[battleData.stageId].power;
            }
            else
                ePower = battleData.defender.battleTroopList[troopPosition].GetTotalPower(battleData.type);

            eSpeed = battleData.defender.battleTroopList[troopPosition].GetTotalSpeed(battleData.type);
            eFirstSpeed = eSpeed;
            if (troopPosition != 0)
            {
                eFirstSpeed = battleData.defender.battleTroopList[0].GetTotalSpeed(battleData.type);
            }
        }

        UISetter.SetLabel(enemyPower, ePower);
        UISetter.SetLabel(enemySpeed, eSpeed);

		UISetter.SetLabel(myPower, mPower);
		UISetter.SetLabel(mySpeed, mSpeed);

		DefineDataRow define = regulation.defineDtbl["ENEMY_DIFFICULTY"];
		int gap = Mathf.Abs(mPower - ePower);
		int powerGap = 0;
		if(mPower == 0 || mPower < gap)
		{
			powerGap = 100;
		}
		else if(gap == 0)
		{
			powerGap = 0;
		}
		else
		{
			powerGap = 100/(mPower/gap);
		}
		int value = int.Parse(define.value);
		UISetter.SetActive(easyRoot, value < powerGap && mPower - ePower > 0);
		UISetter.SetActive(hardRoot, value < powerGap && mPower - ePower < 0);
		UISetter.SetActive(normalRoot, !easyRoot.activeSelf && !hardRoot.activeSelf);
        UISetter.SetSprite(speedArrow, mFirstSpeed >= eFirstSpeed ? "world_direction_02" : "world_direction_01");
	}

	/// <summary>선택한 스킬의 이름/설명을 표시하고, 스킬 범위를 아군/적군 플레이트에 표시한다.</summary>
	private void SetSkillInfo()
	{
		if(_selectedPositionId == -1)
			return;
		
		SkillDataRow row = regulation.skillDtbl[selectSkill];
		RoTroop.Slot slot = _curLhsTroop.GetSlotByPosition(_selectedPositionId);

        RoCommander commander = GetSlotCommander(slot);


		int idx = commander.GetSkillIndex(selectSkill);
		int level = commander.GetSkillLevel(idx);
		UISetter.SetLabel(skillName, string.Format("Lv{0} {1}",level,Localization.Get(row.skillName)));
		UISetter.SetLabel(skillDescription, Localization.Format(row.skillDescription, (float)((row.startBonus + row.lvBonus*(level-1))/100.0f)));

        ResetTargetPlate();
		if(row.targetType == ESkillTargetType.Own || row.targetType == ESkillTargetType.Friend)
		{
			if(row.targetType == ESkillTargetType.Friend)
			{
				SetFriendTargetPlate(true);
			}
			else
			{
				SetFriendTargetPlate(false);
			}
		}
		else
		{
			SetFriendTargetPlate(false);

            if (battleData.type == EBattleType.WaveDuel)
            {
                /**
                 * 웨이브듀얼 마지막 부대는 비공개
                 */
                if (troopPosition < (ConstValue.waveDuelTroopCount - 1))
                {
                    SetEnemyTargetPlate(row, _selectedPositionId, row._projectileDrks);
                }
            }
            else
            {
                SetEnemyTargetPlate(row, _selectedPositionId, row._projectileDrks);
            }
		}
		skillDescriptionScroll.ResetPosition();
	}

	/// <summary>선택한 슬롯 지휘관의 스킬 목록을 초기화하고 첫 스킬을 선택한다.</summary>
	public void InitSkillData()
	{
		OnRelease();
		List<SkillDataRow> _currentSkillList = new List<SkillDataRow>();
		RoTroop.Slot slot = _curLhsTroop.GetSlotByPosition(_selectedPositionId);

        RoCommander commander = GetSlotCommander(slot);

		_selectedCommanderId = commander.id;
		RoUnit unit = RoUnit.Create(commander.unitId, commander.level, commander.rank,  commander.cls, commander.currentCostume, commander.id, commander.favorRewardStep, commander.marry, commander.transcendence);
		UISetter.SetLabel(unitName, Localization.Format("1021", unit.level + "  " +Localization.Get(unit.unitReg.nameKey)));
        List<string> skillIdList = commander.GetSkillIdList();
        for(int i = 0; i < 4; i++)
		{
			SkillDataRow row = null;
            string id = skillIdList[i];
            if(string.Equals(id, "0"))
				continue;
			
            row = regulation.skillDtbl[id];
			_currentSkillList.Add(row);
		}
		skillListView.InitSkillList(_currentSkillList, "Skill-");
        skillListView.SetSelection(skillIdList[0], true);
        selectSkill = skillIdList[0];

        UISetter.SetActive(addSkillRoot, skillIdList.Count > 4);
        if (skillIdList.Count > 4)
            UISetter.SetGameObjectName(addSkillRoot, string.Format("Skill-{0}", skillIdList[4]));
	}

	public void ResetSkillData()
	{
		UISetter.SetLabel(skillName, "");
		UISetter.SetLabel(skillDescription, "");
		UISetter.SetLabel(unitName, "");
		List<SkillDataRow> _currentSkillList = new List<SkillDataRow>();
		skillListView.InitSkillList(_currentSkillList, "Skill-");
        UISetter.SetActive(addSkillRoot, false);
		_selectedPositionId = -1;
		_selectedCommanderId = "";
		PlateSelect(_selectedPositionId);
	}

	/// <summary>전투 타입별 편성 단계 진입: 준비/시작 UI를 전환하고 저장 덱을 불러온다.</summary>
	public void SelectPlunderTroop()
	{
		_SelectTroop();
		UISetter.SetActive(forBattleRoot, true);
		UISetter.SetActive(leftInformation, false);
		UISetter.SetActive(leftUnitInformation, true);
		UISetter.SetActive(plunder.readyRoot, false);
		UISetter.SetActive(plunder.startRoot, true);
		UISetter.SetActive(raid.readyRoot, false);
		UISetter.SetActive(raid.startRoot, false);
		LoadStageDeck(); // 저장된 덱 로드
		SetInfoLabel(); // 아군 덱, 적군 덱 전투력 및 선공수치 표시
	}

	public void SelectDuelTroop()
	{
		_SelectTroop();
		UISetter.SetActive(leftInformation, false);
		UISetter.SetActive(leftUnitInformation, true);
		UISetter.SetActive(plunder.readyRoot, false);
		UISetter.SetActive(plunder.startRoot, false);
		UISetter.SetActive(raid.readyRoot, false);
		UISetter.SetActive(raid.startRoot, false);
		LoadStageDeck(); // 저장된 덱 로드
		SetInfoLabel(); // 아군 덱, 적군 덱 전투력 및 선공수치 표시
	}

    public void SelectWaveBattleTroop()
    {
        _SelectTroop();
        UISetter.SetActive(forBattleRoot, false);
        UISetter.SetActive(leftInformation, false);
        UISetter.SetActive(leftUnitInformation, true);
        UISetter.SetActive(plunder.readyRoot, false);
        UISetter.SetActive(plunder.startRoot, true);
        UISetter.SetActive(waveBattle.readyRoot, false);
        UISetter.SetActive(waveBattle.startRoot, false);
        UISetter.SetActive(attackArrow, false);
        LoadStageDeck(); // 저장된 덱 로드
        SetInfoLabel(); // 아군 덱, 적군 덱 전투력 및 선공수치 표시
    }

    /// <summary>하위 파츠 초기화가 끝난 다음 프레임에 편성 단계로 진입하기 위한 지연 호출.</summary>
    public IEnumerator SelectDuelTroopDelay()
	{
		yield return null;
		SelectDuelTroop();
	}

	/// <summary>서버에 저장된 방어 부대를 복제해 편집용 부대로 사용한다.</summary>
	public void SelectDefenderTroop()
	{                
        lhsTroops = new List<RoTroop>(localUser.defenderTroops.Count);
        for (int i = 0; i < localUser.defenderTroops.Count; ++i)
        {
            lhsTroops.Add(localUser.defenderTroops[i].Clone());
        }

		if(!_curLhsTroop.IsEmpty())
		{
			_selectedPositionId = _curLhsTroop.slots[0].position;
			InitSkillData();
		}
		else
		{
			OnRelease();
			ResetSkillData();
			ResetTargetPlate();
		}
		OnRefresh();
	}

	public void SelectConquestTroop()
	{
		SetConquestDeck();
		if(!_curLhsTroop.IsEmpty())
		{
			_selectedPositionId = _curLhsTroop.slots[0].position;
			InitSkillData();
		}
		else
		{
			OnRelease();
			ResetSkillData();
		}
		OnRefresh();
	}

	public void SelectRaidTroop()
	{
		_SelectTroop();
		UISetter.SetActive(leftInformation, false);
		UISetter.SetActive(leftUnitInformation, true);
		UISetter.SetActive(plunder.readyRoot, false);
		UISetter.SetActive(plunder.startRoot, false);
		UISetter.SetActive(raid.readyRoot, false);
		UISetter.SetActive(raid.startRoot, true);
		LoadStageDeck();
		SetInfoLabel();
	}

	public IEnumerator SelectRaidTroopDelay()
	{
		yield return null;
		SelectRaidTroop();
	}

    public void SelectCooperateBattleTroop()
    {
        _SelectTroop();
        UISetter.SetActive(leftInformation, false);
        UISetter.SetActive(leftUnitInformation, true);
        UISetter.SetActive(plunder.readyRoot, false);
        UISetter.SetActive(plunder.startRoot, false);
        UISetter.SetActive(raid.readyRoot, false);
        UISetter.SetActive(raid.startRoot, false);
        LoadStageDeck();
        SetInfoLabel();
    }

    public IEnumerator SelectCooperateBattleTroopDelay()
    {
        yield return null;
        SelectCooperateBattleTroop();
    }


	/// <summary>선택 유닛과 적 유닛의 상성(유리/불리/동일)을 적 플레이트에 표시한다.</summary>
	private void SetUnitIncompatible(RoTroop.Slot slot)
	{
		UnitDataRow myUnit = regulation.unitDtbl[slot.unitId];
		for(int i = 0; i < enemyPositionList.Count; i++)
		{
			RoTroop.Slot enemySlot = battleData.defender.battleTroopList[troopPosition].GetSlotByPosition(i);
			if(enemySlot != null)
			{
				UnitDataRow enemyUnit = regulation.unitDtbl[enemySlot.unitId];
				if(myUnit.typeUpper == enemyUnit.type)
				{
					enemyPositionList[i].SetIncompatible(IncompatibleType.Up);
				}
				else if(myUnit.typeDown == enemyUnit.type)
				{
					enemyPositionList[i].SetIncompatible(IncompatibleType.Down);
				}
				else
				{
					enemyPositionList[i].SetIncompatible(IncompatibleType.Equal);
				}
			}
		}
	}

	/// <summary>전투 타입별 보상 목록을 표시한다. (웨이브 전투는 중복 보상 제거)</summary>
	private void SetStageRewardInformation()
	{
		if(!string.IsNullOrEmpty(battleData.stageId) || battleData.sweepLevel != 0)
		{
			List<RewardDataRow> rewardList = null;
            if (battleData.type == EBattleType.Plunder)
                rewardList = regulation.rewardDtbl.FindAll(row => row.category == ERewardCategory.World && row.type == int.Parse(battleData.stageId));
            else if (battleData.type == EBattleType.SeaRobber || battleData.type == EBattleType.Guerrilla)
                rewardList = regulation.rewardDtbl.FindAll(row => row.category == ERewardCategory.Situation && row.type == (int)battleData.sweepType && row.typeIndex == battleData.sweepLevel);
            else if (battleData.type == EBattleType.Annihilation)
                return;
            else if (battleData.type == EBattleType.WaveBattle)
            {
                rewardList = regulation.rewardDtbl.FindAll(row => row.category == ERewardCategory.WaveBattle && row.type == int.Parse(battleData.stageId));
                Dictionary<int, RewardDataRow> dic_reward = new Dictionary<int, RewardDataRow>();
                List<RewardDataRow> newList = new List<RewardDataRow>();
                for(int i= 0; i < rewardList.Count; ++i)
                {
                    if(!dic_reward.ContainsKey(rewardList[i].rewardIdx))
                    {
                        dic_reward.Add(rewardList[i].rewardIdx, rewardList[i]);
                        newList.Add(rewardList[i]);
                    }
                }
                rewardListView.InitRewardList(newList);
                return;
            }
            rewardListView.InitRewardList(rewardList);
        }
		else
		{
			if (battleData.type == EBattleType.EventBattle)
			{
				List<RewardDataRow> rewardList = regulation.rewardDtbl.FindAll(row => row.category == ERewardCategory.EventBattle && row.type == battleData.eventId && row.typeIndex == battleData.eventLevel);
				rewardListView.InitRewardList(rewardList);
			}
		}
	}

	private void PlateSelect(int position)
	{
		CommanderSelect(position);
		for(int i = 0; i < positionList.Count; i++)
		{
			positionList[i].SelectPlateState(i == position ? true : false);
		}
	}

	private void CommanderSelect(int position)
	{
		RoTroop.Slot slot = _curLhsTroop.GetSlotByPosition(position);
		int slotNum = -1;
		if(slot != null)
			slotNum = slot.slotNum - 1;
		for(int i = 0; i < commanderList.Count; i++)
		{
			commanderList[i].SetSelection(i == slotNum ? true : false);
		}
	}

	// ── 드래그로 부대 포지션 변경 ──
	// UICamera 드래그 이벤트로 플레이트 사이에서 유닛을 이동/교체한다. (도우미 유닛은 이동 불가)
	public UISprite dragUnit;
	private int dragStartPosition = -1;
	private void OnDragStart(GameObject go)
	{
		if (dragUnit == null) {
			Debug.LogError("invalid state");
			return;
		}

		string key = go.name;
		if (!key.StartsWith("Plate-")) {
			return;
		}
		int id = int.Parse(key.Substring(key.IndexOf("-") + 1));
		
		UIPositionPlate plate = positionList[id];	

		if(_curLhsTroop.GetSlotByPosition(id) == null || _curLhsTroop.GetSlotByPosition(id).charType == ECharacterType.Helper)
		{
			dragStartPosition = -1;
			return;
		}
		
		plate.CopyThumbnail(dragUnit);
		UISetter.SetActive(dragUnit, true);
		plate.SetThumbnailAlpha(0.5f);
		dragStartPosition = id;
	}
	private void OnDragEnd(GameObject go)
	{
		UISetter.SetActive(dragUnit, false);
		dragStartPosition = -1;
	}
	private void OnDrag(GameObject go, Vector2 delta)
	{
		if (dragStartPosition < 0)
			return;
		dragUnit.transform.position = UICamera.lastWorldPosition;
	}
	
	/// <summary>드롭한 위치의 슬롯과 교체하거나 빈 자리로 이동한다. 선택 중인 슬롯이면 선택 위치도 함께 옮긴다.</summary>
	private void OnDragOver(GameObject go, GameObject obj)
	{
		if (dragStartPosition < 0)
			return;
		string key = go.name;
		
		positionList[dragStartPosition].SetThumbnailAlpha(1.0f);
		if (!key.StartsWith("Plate-")) {
			return;
		}
		int id = int.Parse(key.Substring(key.IndexOf("-") + 1));
		
		RoTroop.Slot prevSlot = _curLhsTroop.GetSlotByPosition(dragStartPosition);
		RoTroop.Slot targetSlot = _curLhsTroop.GetSlotByPosition(id);
		if(targetSlot != null && targetSlot.charType == ECharacterType.Helper)
		{
			targetSlot = prevSlot;
		}
		if (prevSlot != null && targetSlot != null) 
		{
			if(prevSlot.position == _selectedPositionId)
			{
				_selectedPositionId = targetSlot.position;
			}
			else if(targetSlot.position == _selectedPositionId)
			{
				_selectedPositionId = prevSlot.position;
			}
			int temp = prevSlot.position;
			prevSlot.position = targetSlot.position;
			targetSlot.position = temp;
		} else if (targetSlot == null) {
			if(prevSlot.position == _selectedPositionId)
			{
				_selectedPositionId = id;
			}
			prevSlot.position = id;
		}

        SoundManager.PlaySFX("BTN_Formation_001");

		OnRefresh();
	}

	void OpenCommanderList()
	{
		uiWorld.commanderList.Init(lhsTroops, _curLhsTroopIndex, battleData);
	}

	bool ExistEnemy(int position)
	{
		RoTroop enemyTroop = battleData.defender.battleTroopList[troopPosition];
		if(enemyTroop.GetSlotByPosition(position) != null)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// 스킬 사용 위치와 타겟 패턴(일반/전열/후열)에 맞는 우선순위 테이블로 첫 번째 타겟 위치를 찾는다.
	/// 전투 로직(Frame)과 같은 우선순위 테이블을 사용한다.
	/// </summary>
	int GetSkillTargetPosition(SkillDataRow row, int position)
	{
		int[,] priorityTable = null;
		switch (position) 
		{
		case 0: case 3: case 6:
			{
				switch(row.targetPattern)
				{
					case ESkillTargetPattern.Normal:
						priorityTable = Frame._PriorityTable036;
						break;
					case ESkillTargetPattern.sequence:
						priorityTable = Frame._PriorityFrontTable036;
						break;
					case ESkillTargetPattern.Front:
						priorityTable = Frame._PriorityFrontTable036;
						break;
					case ESkillTargetPattern.Back:
						priorityTable = Frame._PriorityBackTable036;
						break;
					default:
						priorityTable = Frame._PriorityTable036;
						break;
				}
			}
			break;
		case 1: case 4: case 7:
			{
				switch (row.targetPattern)
				{
					case ESkillTargetPattern.Normal:
						priorityTable = Frame._PriorityTable147;
						break;
					case ESkillTargetPattern.sequence:
						priorityTable = Frame._PriorityFrontTable036;
						break;
					case ESkillTargetPattern.Front:
						priorityTable = Frame._PriorityFrontTable147;
						break;
					case ESkillTargetPattern.Back:
						priorityTable = Frame._PriorityBackTable147;
						break;
					default:
						priorityTable = Frame._PriorityTable147;
						break;
				}    
			}
			break;
		case 2: case 5: case 8:
			{
				switch (row.targetPattern)
				{
				case ESkillTargetPattern.Normal:
					priorityTable = Frame._PriorityTable258;
					break;
				case ESkillTargetPattern.sequence:
					priorityTable = Frame._PriorityFrontTable036;
					break;
				case ESkillTargetPattern.Front:
					priorityTable = Frame._PriorityFrontTable258;
					break;
				case ESkillTargetPattern.Back:
					priorityTable = Frame._PriorityBackTable258;
					break;
				default:
					priorityTable = Frame._PriorityTable258;
					break;
				}
			}
			break;
		}
		for (var r = 0; r < priorityTable.GetLength(0); ++r) {
			for (var c = 0; c < priorityTable.GetLength(1); ++c) {
				if(ExistEnemy(priorityTable[r, c]))
				{
					return priorityTable[r, c];
				}
			}
		}
		return -1;
	}

	/// <summary>
	/// 5x5 스플래시 패턴 문자열에서 타겟을 중심으로 3x3 영역을 잘라, 적 9칸의 피격 여부(1/0) 목록을 만든다.
	/// ('P'는 타겟 중심 칸)
	/// </summary>
	List<int> GetSkillTargetPositionList(int target, string pattern)
	{
		List<int> targetList = new List<int>();

		if(target == -1)
			return targetList;

		var px = target % Troop.ColCount;
		var pz = target / Troop.ColCount;
		int startZ = 2 - pz;
		for(int z = 0; z < Troop.ColCount; z++)
		{
			for(int x = 0; x < Troop.ColCount; x++)
			{
				string result = pattern.Substring((startZ+z)*5, 5);
				int startX = 2 - px;
				string isTarget = result.Substring((startX+x),1);
				if(isTarget.Equals("P"))
					targetList.Add(1);
				else
					targetList.Add(int.Parse(isTarget.ToString()));
			}
		}

		return targetList;
	}

	void SetFriendTargetPlate(bool state)
	{
		if(state)
		{
			for(int i = 0; i < positionList.Count; i++)
			{
				positionList[i].IsTargetPlate(state);
				UISetter.SetActive(positionList[i].selectedRoot, i == _selectedPositionId);
			}
		}
		else
		{
			PlateSelect(_selectedPositionId);
		}
	}

	void SetEnemyTargetPlate(SkillDataRow row, int position, List<string> patternList)
	{
		if(battleData.type == EBattleType.Raid || battleData.type == EBattleType.WaveBattle || battleData.type == EBattleType.Conquest || battleData.type == EBattleType.EventRaid)
			return;

		ResetTargetPlate();
		for(int i = 0; i < patternList.Count; i++)
		{
			if(!regulation.projectileDtbl.ContainsKey(patternList[i]))
				continue;
			
			var patternData = regulation.projectileDtbl[patternList[i]];
			int target = GetSkillTargetPosition(row, position);
			List<int> targetList = GetSkillTargetPositionList(target, patternData.splashPattern);

			if(targetList.Count == 0)
				return;

			for(int j = 0; j < enemyPositionList.Count; j++)
			{
				enemyPositionList[j].IsEnemyTargetPlate(targetList[j] == 1);
			}
		}
	}

	void ResetTargetPlate()
	{
		if(battleData.type == EBattleType.Raid || battleData.type == EBattleType.WaveBattle || battleData.type == EBattleType.Conquest || battleData.type == EBattleType.EventRaid)
			return;
		for(int i = 0; i < enemyPositionList.Count; i++)
		{
			enemyPositionList[i].IsEnemyTargetPlate(false);
		}
	}

	// 화면이 켜져 있는 동안에만 드래그 이벤트를 구독한다
	protected override void OnDisable()
	{
		base.OnDisable();
		UICamera.onDragStart -= OnDragStart;
		UICamera.onDragEnd -= OnDragEnd;
		UICamera.onDrag -= OnDrag;
		UICamera.onDragOut -= OnDragOver;
	}
	
	protected override void OnEnable()
	{
		base.OnEnable();
		UICamera.onDragStart += OnDragStart;
		UICamera.onDragEnd += OnDragEnd;
		UICamera.onDrag += OnDrag;
		UICamera.onDragOut += OnDragOver;
	}

    public void ResetDragObject()
    {
        UISetter.SetActive(dragUnit, false);

        if (dragStartPosition >= 0)
        {
            positionList[dragStartPosition].SetThumbnailAlpha(1.0f);
            dragStartPosition = -1;
        }                
    }

    #region 전투 시작

    /// <summary>
    /// 전투 시작 공통 흐름.
    /// 편성이 가득 찼으면 바로 시작하고, 빈 슬롯이 있으면 확인 팝업을 띄운다.
    /// 확인을 눌러도 부대가 비어 있으면 시작하지 않는다.
    /// </summary>
    /// <param name="startAction">실제 전투 시작(서버 요청) 동작</param>
    /// <param name="checkAllTroops">true면 모든 웨이브 부대를, false면 현재 부대만 비어 있는지 검사</param>
    /// <param name="warningKey">편성 미완료 경고 문구의 로컬라이즈 키</param>
    private void ConfirmAndStartBattle(System.Action startAction, bool checkAllTroops = false, string warningKey = "1331")
    {
        if (FullUnitCheck())
        {
            startAction();
            return;
        }

        UISimplePopup.CreateBool(true, "1303", warningKey, null, "1001", "1000").onClick = sender =>
        {
            if (sender.name != "OK")
                return;

            bool isEmpty = checkAllTroops ? IsEmptyTroop() : _curLhsTroop.IsEmpty();
            if (isEmpty)
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
            else
                startAction();
        };
    }

    /// <summary>현재 선택한 부대로 공격 측 전투 데이터를 만든다.</summary>
    private void SetAttackerWithCurrentTroop()
    {
        battleData.attacker = localUser.CreateForBattle(new List<RoTroop>() { _curLhsTroop });
    }

    /// <summary>웨이브 결투: 모든 웨이브 부대를 함께 출전시킨다.</summary>
    public void StartWaveDuel()
    {
        if (localUser.waveDuelTicket < 1)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.waveDuelTicket);
            return;
        }

        ConfirmAndStartBattle(() =>
        {
            battleData.attacker = localUser.CreateForBattle(lhsTroops);
            network.RequestPvPStartWaveDuel(battleData);
        }, true, "5040011");
    }

    public void StartDuel()
    {
        if (localUser.challenge < 1)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Challenge);
            return;
        }

        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestPvPStartDuel(battleData); });
    }

    public void StartWorldDuel()
    {
        if (localUser.worldDuelTicket < 1)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.WorldDuelTicket);
            return;
        }

        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestPvPStartWorldDuel(battleData); });
    }

    public void StartSweep()
    {
        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestSituationSweepStart(battleData); });
    }

    public void StartRaid()
    {
        if (localUser.opener < 1)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Key);
            return;
        }

        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestRaid(battleData); });
    }

    public void StartEventRaid()
    {
        if (localUser.eventRaidTicket < 1)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.EventRaidTicket);
            return;
        }

        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestEventRaidBattleStart(battleData); });
    }

    public void StartStageBattle()
    {
        WorldMapStageDataRow row = regulation.worldMapStageDtbl[battleData.stageId];
        if (localUser.bullet < row.bullet)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Bullet);
            return;
        }

        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestPlunder(battleData); });
    }

    public void StartEventBattle()
    {
        if (localUser.oil < 1)
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Oil);
            return;
        }

        ConfirmAndStartBattle(StartEventBattleOrScenario);
    }

    /// <summary>시작 전에 보여줄 이벤트 시나리오가 있으면 먼저 재생하고, 없으면 바로 이벤트 전투를 시작한다.</summary>
    private void StartEventBattleOrScenario()
    {
        if (IsShowEventBattleScenario())
        {
            StartCoroutine(PlayScenarioAndStartEventBattle());
        }
        else
        {
            SetAttackerWithCurrentTroop();
            network.RequestEventBattle(battleData);
        }
    }

    /// <summary>
    /// 용병이 포함된 부대의 전투 시작.
    /// 재화를 확인한 뒤, 계약하지 않은 용병이 있으면 계약 팝업을 먼저 띄운다.
    /// </summary>
    public void SetBattle_withMercenary()
    {
        if (GameSetting.instance.repeatBattle)
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1382"));
            return;
        }

        switch (battleData.type)
        {
            case EBattleType.Plunder:
                {
                    WorldMapStageDataRow row = regulation.worldMapStageDtbl[battleData.stageId];
                    if (localUser.bullet < row.bullet)
                    {
                        uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Bullet);
                        return;
                    }
                }
                break;
            case EBattleType.Raid:
                if (localUser.opener < 1)
                {
                    uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Key);
                    return;
                }
                break;
            case EBattleType.EventBattle:
                if (localUser.oil < 1)
                {
                    uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Oil);
                    return;
                }
                break;
        }

        if (FullUnitCheck())
        {
            engageInfo.SetEngagedPopup();
            return;
        }

        UISimplePopup.CreateBool(true, "1303", "1331", null, "1001", "1000").onClick = sender =>
        {
            if (sender.name != "OK")
                return;

            if (_curLhsTroop.IsEmpty())
            {
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
                return;
            }

            if (CheckEngage())
                engageInfo.SetEngagedPopup();
            else
                StartBattle_withMercenary(-1);
        };
    }

    /// <summary>용병 계약 결과(kind)를 포함해 전투 타입별 시작 요청을 보낸다.</summary>
    public void StartBattle_withMercenary(int kind)
    {
        SetAttackerWithCurrentTroop();
        switch (battleData.type)
        {
            case EBattleType.Raid:
                network.RequestRaid(battleData, kind);
                break;
            case EBattleType.Plunder:
                network.RequestPlunder(battleData, kind);
                break;
            case EBattleType.WaveBattle:
                network.RequestWaveBattleStart(battleData, kind);
                break;
            case EBattleType.EventBattle:
                network.RequestEventBattle(battleData, kind);
                break;
        }
    }

    public void StartAnnihilation()
    {
        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestAnnihilationStageStart(battleData); });
    }

    public void StartInfinityBattle()
    {
        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestInfinityBattleStart(battleData); });
    }

    public void StartWaveBattle()
    {
        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestWaveBattleStart(battleData); });
    }

    public void StartCooperateBattle()
    {
        ConfirmAndStartBattle(() => { SetAttackerWithCurrentTroop(); network.RequestCooperateBattleStart(battleData); });
    }

    #endregion

    /// <summary>현재 부대에 아직 계약하지 않은 용병이 있는지 확인한다.</summary>
    public bool CheckEngage()
    {
        for (int i = 0; i < _curLhsTroop.slots.Length; ++i)
        {
            if (_curLhsTroop.slots[i].charType == ECharacterType.Mercenary || _curLhsTroop.slots[i].charType == ECharacterType.NPCMercenary)
            {
                if(_curLhsTroop.slots[i].existEngage == 0)
                    return true;
            }                
        }

        return false;
    }

    /// <summary>무한 전투: 보상을 받을 수 없는 스테이지면 확인 후 진행한다.</summary>
    public void CheckInfinityBattle()
    {
        if (localUser.InfinityEnableReward(battleData.stageId))
            StartInfinityBattle();
        else
        {
            UISimplePopup.CreateBool(true, "1303",
                "3400004",
                null,
                "1001", "1000"
            ).onClick = sender =>
                {
                    string key = sender.name;
                    if (key == "OK")
                    {
                        StartInfinityBattle();
                    }
                };
        }
    }

    /// <summary>방어 덱을 서버에 저장한다. 웨이브 결투 방어는 모든 웨이브 부대를 저장한다.</summary>
    public void SaveDefenderDeck()
    {
        bool isWaveDuel = battleData.type == EBattleType.WaveDuelDefender;
        ConfirmAndStartBattle(RequestSaveDefenderDeck, true, isWaveDuel ? "5040011" : "1331");
    }

    private void RequestSaveDefenderDeck()
    {
        if (battleData.type == EBattleType.WaveDuelDefender)
            network.RequestWaveDuelDefenderSetting(lhsTroops);
        else if (battleData.type == EBattleType.Defender)
            network.RequestDefenderSetting(_curLhsTroop);
        else if (battleData.type == EBattleType.WorldDuelDefender)
            network.RequestWorldDuelDefenderSetting(_curLhsTroop);
    }

	public void SaveConquestDeck()
	{
		if(FullUnitCheck())
		{
			battleData.attacker = localUser.CreateForBattle(new List<RoTroop>() { _curLhsTroop });
			network.RequestSetConquestTroop(battleData);
		}
		else
			NetworkAnimation.Instance.CreateFloatingText(Localization.Get("18916"));
	}

    /// <summary>전투 시작 전에 보여줄 이벤트 시나리오가 있고, 아직 보지 않았는지 확인한다.</summary>
    private bool IsShowEventBattleScenario()
    {
        EventBattleScenarioDataRow scenarioData = regulation.eventBattleScenarioDtbl.Find(row => row.eventIdx == battleData.eventId.ToString() && row.eventType == battleData.eventLevel.ToString() && row.timing == EventScenarioTimingType.AfterBattleBtn);
        if (scenarioData == null)
            return false;

        int playturn = scenarioData.playTurn;
        int lastPlayturn = localUser.lastShowEventScenarioPlayTurn;
        if (playturn > lastPlayturn)
            return true;
        else
            return false;    
    }

    /// <summary>이벤트 시나리오 대화가 끝날 때까지 기다린 뒤 이벤트 전투를 시작한다.</summary>
    IEnumerator PlayScenarioAndStartEventBattle()
    {
        EventBattleScenarioDataRow scenarioData = regulation.eventBattleScenarioDtbl.Find(
            row => row.eventIdx == battleData.eventId.ToString() && row.timing == EventScenarioTimingType.AfterBattleBtn && row.eventType == battleData.eventLevel.ToString());
        if(scenarioData != null)
        {
            ClassicRpgManager rpgManager = UIManager.instance.world.dialogMgr;
            if (rpgManager != null)
            {
                rpgManager.StartEventScenario();
                rpgManager.InitScenarioDialogue(scenarioData.scenarioIdx, DialoguerCore.DialogueType.Event);
            }

            while (true)
            {
                if (!rpgManager.isPlayDialogue)
                    break;

                yield return null;
            }           
        }

        battleData.attacker = localUser.CreateForBattle(new List<RoTroop>() { _curLhsTroop });
        network.RequestEventBattle(battleData);
    }

    // ── 전투 타입별 하위 파츠 ──
    // 각 파츠는 OnInit/OnRefresh에서 자신이 담당하는 전투 타입일 때만 동작한다.

    /// <summary>결투 / 웨이브 결투 / 최강 결투 / 섬멸전 / 무한 전투 파츠.</summary>
    [System.Serializable]
    public class Duel : UIInnerPartBase
    {
		public UIUser rightUser;
		public UICommander rightCommander;
		public GameObject UseResource;
		public GameObject NotUseResource;
		public GameObject attackBtn;

        public UISprite ticket;

		public UIUser user;
		public GameObject progressing;

		public void Set()
		{
			UIReadyBattle readyBattle = uiWorld.readyBattle;
                      
            UISetter.SetButtonGray(attackBtn, !readyBattle.IsEmptyTroop());
			UISetter.SetActive(progressing, false);
            UISetter.SetActive(UseResource, 
                readyBattle.battleData.type == EBattleType.Duel
                || readyBattle.battleData.type == EBattleType.WaveDuel
                || readyBattle.battleData.type == EBattleType.WorldDuel);

            string ticketRes = "Goods-chlg";

            if (readyBattle.battleData.type == EBattleType.WaveDuel)
                ticketRes = "Goods-wbt";
            else if (readyBattle.battleData.type == EBattleType.Duel)
                ticketRes = "Goods-chlg";
            else if (readyBattle.battleData.type == EBattleType.WorldDuel)
                ticketRes = "Goods-sd";

            UISetter.SetSprite(ticket, ticketRes);

			UISetter.SetActive(NotUseResource, readyBattle.battleData.type == EBattleType.Annihilation || readyBattle.battleData.type == EBattleType.InfinityBattle);
		}

		public override void OnInit(UIPanelBase parent)
		{
			base.OnInit(parent);
			UIReadyBattle readyBattle = parent as UIReadyBattle;
			if(readyBattle.battleData.type != EBattleType.Duel 
                && readyBattle.battleData.type != EBattleType.WaveDuel
                && readyBattle.battleData.type != EBattleType.Annihilation
                && readyBattle.battleData.type != EBattleType.WorldDuel
                && readyBattle.battleData.type != EBattleType.InfinityBattle)
				return;
				
			readyBattle.StartCoroutine(readyBattle.SelectDuelTroopDelay());
			Set();
		}

		public override void OnRefresh()
		{
			base.OnRefresh();
			UIReadyBattle readyBattle = uiWorld.readyBattle;
			if(readyBattle.battleData.type != EBattleType.Duel 
                && readyBattle.battleData.type != EBattleType.WaveDuel
                && readyBattle.battleData.type != EBattleType.Annihilation
                && readyBattle.battleData.type != EBattleType.WorldDuel
                && readyBattle.battleData.type != EBattleType.InfinityBattle)
				return;
			Set();
		}

		public void SetProgressingBattle(Protocols.ScrambleStageInfo.UserInfo userData)
		{
			UISetter.SetButtonEnable(attackBtn, false);
			UISetter.SetActive(progressing, true);
		}

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            UIReadyBattle uiParent = parent as UIReadyBattle;
            string key = sender.name;
            if (key == "Attack") {
                if (attackBtn.GetComponent<UIButton>().isGray)
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
                }
                else
                {
                    localUser.isUpdateAtkRecord = true;

					if (uiParent.battleData.type == EBattleType.Duel)
                    {
                        uiParent.StartDuel();
                    }
					else if (uiParent.battleData.type == EBattleType.Annihilation)
                    {
                            uiParent.StartAnnihilation();
                    }
                    else if (uiParent.battleData.type == EBattleType.WaveDuel)
                    {
                        if (localUser.currentSeasonDuelTime.GetRemain() <= 0)
                        {
                            NetworkAnimation.Instance.CreateFloatingText(Localization.Get("5050013"));
                            return;
                        }

                        uiParent.StartWaveDuel();
                    }
                    else if (uiParent.battleData.type == EBattleType.WorldDuel)
                    {
                        if (localUser.currentSeasonDuelTime.GetRemain() <= 0)
                        {
                            NetworkAnimation.Instance.CreateFloatingText(Localization.Get("5050013"));
                            return;
                        }

                        uiParent.StartWorldDuel();
                    }
                    else if (uiParent.battleData.type == EBattleType.InfinityBattle)
                    {
                        uiParent.CheckInfinityBattle();
                    }
                    else
                    {
                        uiParent.StartSweep();
                    }
                }
            }
        }
    }

    /// <summary>스테이지(약탈) / 소탕 / 게릴라 / 이벤트 전투 파츠. 스테이지 정보, 반복 전투, 소탕(타임머신) 처리.</summary>
    [System.Serializable]
    public class Plunder : UIInnerPartBase
    {        
		public UITroop stageInfo;
        public UILabel skipBattleCost;        
		public UIDefaultListView commanderListView;
		public GameObject RemainCountRoot;        

		public GameObject readyRoot;
		public GameObject startRoot;
		public GameObject readyPlunderRoot;
		public GameObject readySweepRoot;
		public GameObject AttackBtn;
		public GameObject timeMachineRoot;

		public UILabel timeMachine;

		public UILabel title;
		public UILabel description;

		public UISprite icon1;
		public UISprite icon2;
		public UILabel bulletTopCnt;
		public UILabel bulletStartCnt;

		public GameObject jobRoot;
		public UISprite jobIcon;
		public UILabel jobLabel;
		public UILabel banLabel;
		
		//클리어 횟수
		public UILabel clearCnt;
		public GameObject rechargeBtn;

		public GameObject skipBtn;
		public GameObject skipsBtn;
		public UILabel skipCnt;
		public UILabel skipsCnt;

		public GameObject starGrid;
		public GameObject starGridRoot;

		public UICommander bossCommander;

        public UIDefaultListView clearConditionList;

		public UIFlipSwitch flipRepeat;

		public void Set()
        {
			UIReadyBattle readyBattle = uiWorld.readyBattle;

			UISetter.SetLabel(skipBattleCost, 100);
			BattleData data = readyBattle.battleData;
			UISetter.SetButtonGray(AttackBtn, !readyBattle._curLhsTroop.IsEmpty());
			UISetter.SetActive(readyPlunderRoot, data.type == EBattleType.Plunder);
			UISetter.SetActive(readySweepRoot, data.type == EBattleType.Guerrilla);
			List<EnemyCommanderDataRow> enemyList = new List<EnemyCommanderDataRow>();

			UISetter.SetActive(starGridRoot, data.type == EBattleType.Plunder || data.type == EBattleType.EventBattle);
			UISetter.SetActive(clearConditionList, data.type == EBattleType.EventBattle);
			UISetter.SetActive(timeMachineRoot, data.type == EBattleType.Guerrilla || data.type == EBattleType.Plunder);
			UISetter.SetActive(flipRepeat, data.type == EBattleType.EventBattle);
			description.height = data.type == EBattleType.EventBattle ? 75 : 150;

			if(data.type == EBattleType.Plunder)
			{
				RoWorldMap.Stage stage = localUser.FindWorldMapStage(data.stageId);
				WorldMapStageDataRow row = regulation.worldMapStageDtbl[data.stageId];

				UISetter.SetActive(jobRoot, false);
                stageInfo.Set(data.defender.mainTroop);
				enemyList = regulation.enemyCommanderDtbl.FindAll(enemy => enemy.id == stage.data.enemyId);
				bossCommander.Set(enemyList[enemyList.Count - 1].commanderId);
				UISetter.SetActive(rechargeBtn, stage.clearCount == stage.typeData.battleCount);
				UISetter.SetActive(timeMachineRoot, true);
	
				UISetter.SetLabel(title, Localization.Get(row.title));
				UISetter.SetLabel(description, Localization.Get(row.description));
				UISetter.SetLabel(clearCnt, string.Format("{0}/{1}",stage.typeData.battleCount - stage.clearCount,stage.typeData.battleCount));
				UISetter.SetLabel(bulletTopCnt, string.Format("x{0}", row.bullet));
				UISetter.SetLabel(bulletStartCnt, row.bullet);
				UISetter.SetSprite(icon1, "Goods-bult");
				UISetter.SetSprite(icon2, "Goods-bult");
				UISetter.SetRank(starGrid, stage.star);

				int maxCount = Mathf.Min(stage.clearEnableCount, int.Parse(regulation.defineDtbl["BATTLE_CLEAR_MAX_COUNT"].value));
				if(stage.data.bullet * maxCount > localUser.bullet)
					maxCount = localUser.bullet / stage.data.bullet;
				bool skipEnable = stage.canBattle;
				UISetter.SetActive(RemainCountRoot, row.type != EStageTypeIdRange.GuardPost && stage.typeData.battleCount <= int.Parse(regulation.defineDtbl["BATTLE_CLEAR_MAX_COUNT"].value));
				UISetter.SetButtonGray(skipBtn, skipEnable && int.Parse(data.stageId) <= localUser.lastClearStage);
				UISetter.SetButtonGray(skipsBtn, skipEnable && int.Parse(data.stageId) <= localUser.lastClearStage);
				UISetter.SetLabel(skipCnt, Localization.Format("1251", 1));
				UISetter.SetLabel(skipsCnt, Localization.Format("1251", Mathf.Max(maxCount, 1)));
				UISetter.SetGameObjectName(skipsBtn, string.Format("SkipsBtn-{0}", maxCount));
			}
			else if(data.type == EBattleType.SeaRobber || data.type == EBattleType.Guerrilla)
			{
				SweepDataRow sweepData = regulation.FindSweepRow(readyBattle.battleData.sweepType, readyBattle.battleData.sweepLevel);
				enemyList = regulation.enemyCommanderDtbl.FindAll(enemy => enemy.id == sweepData.uid);
				bossCommander.Set(enemyList[enemyList.Count - 1].commanderId);
				UISetter.SetLabel(title, Localization.Get(sweepData.name));
				UISetter.SetActive(RemainCountRoot, false);
				UISetter.SetLabel(description, Localization.Get(sweepData.description));
				UISetter.SetLabel(bulletTopCnt, string.Format("x{0}", 1));
				UISetter.SetLabel(bulletStartCnt, 1);
				UISetter.SetSprite(icon1, "Goods-swp");
				UISetter.SetSprite(icon2, "Goods-swp");

				int maxCount = int.Parse(regulation.defineDtbl["WARROOM_CLEAR_MAX_COUNT"].value);
				if(maxCount > localUser.sweepTicket)
					maxCount = localUser.sweepTicket;
				bool skipEnable = localUser.GetSweepClearState(readyBattle.battleData.sweepType, readyBattle.battleData.sweepLevel);
				UISetter.SetActive(timeMachineRoot, true);
				UISetter.SetButtonGray(skipBtn, skipEnable && localUser.sweepTicket > 0);
				UISetter.SetButtonGray(skipsBtn, skipEnable && localUser.sweepTicket > 0);
				UISetter.SetLabel(skipCnt, Localization.Format("1251", 1));
				UISetter.SetLabel(skipsCnt, Localization.Format("1251", localUser.sweepTicket > maxCount ? maxCount : Mathf.Max(localUser.sweepTicket, 1)));
				UISetter.SetGameObjectName(skipsBtn, string.Format("SkipsBtn-{0}", maxCount));

                UISetter.SetActive(jobRoot, (EJob)sweepData.type != EJob.Undefined);
                UISetter.SetSprite(jobIcon, "com_icon_" + ((EJob)sweepData.type).ToString().ToLower());
                if ((EJob)sweepData.type == EJob.Attack) UISetter.SetLabel(jobLabel, Localization.Get("1323"));
                else if ((EJob)sweepData.type == EJob.Defense) UISetter.SetLabel(jobLabel, Localization.Get("1324"));
                else if ((EJob)sweepData.type == EJob.Support) UISetter.SetLabel(jobLabel, Localization.Get("1325"));
                

            }
			else if(data.type == EBattleType.EventBattle)
			{
				EventBattleFieldDataRow row = regulation.FindEventBattle(data.eventId, data.eventLevel);

				stageInfo.Set(data.defender.mainTroop);
				enemyList = regulation.enemyCommanderDtbl.FindAll(enemy => enemy.id == row.enemy);
				bossCommander.Set(enemyList[enemyList.Count - 1].commanderId);
				UISetter.SetActive(rechargeBtn, false);

				UISetter.SetLabel(title, Localization.Get(row.name));
				UISetter.SetLabel(description, Localization.Get(row.explanation));
				UISetter.SetLabel(bulletTopCnt, string.Format("x{0}", 1));
				UISetter.SetLabel(bulletStartCnt, 1);
				UISetter.SetSprite(icon1, "Goods-eaac");
				UISetter.SetSprite(icon2, "Goods-eaac");
				UISetter.SetRank(starGrid, data.eventClear);
				UISetter.SetActive(RemainCountRoot, false);

				UISetter.SetActive(jobRoot, row.job != EJob.Undefined);
				UISetter.SetLabel(banLabel, row.job == EJob.Attack || row.job == EJob.Defense || row.job == EJob.Support ? Localization.Get("18905") : Localization.Get("10000012"));
				if(row.job != EJob.Undefined)
				{
					UISetter.SetSprite(jobIcon, "com_icon_" + row.job.ToString().ToLower());
					if (row.job == EJob.Attack) UISetter.SetLabel(jobLabel, Localization.Get("1323"));
					else if (row.job == EJob.Defense) UISetter.SetLabel(jobLabel, Localization.Get("1324"));
					else if (row.job == EJob.Support) UISetter.SetLabel(jobLabel, Localization.Get("1325"));
					else if (row.job == EJob.Attack_x) UISetter.SetLabel(jobLabel, Localization.Get("1323"));
					else if (row.job == EJob.Defense_x) UISetter.SetLabel(jobLabel, Localization.Get("1324"));
					else if (row.job == EJob.Support_x) UISetter.SetLabel(jobLabel, Localization.Get("1325"));
				}

                       
				flipRepeat.Lock(data.eventClear <= 0);
                UISetter.SetFlipSwitch(flipRepeat, GameSetting.instance.repeatBattle);

				clearConditionList.InitClearCondition(row);
			}
		}

        public override void OnInit(UIPanelBase parent)
        {
            base.OnInit(parent);
			UIReadyBattle readyBattle = parent as UIReadyBattle;

			if(readyBattle.battleData.type != EBattleType.Plunder && readyBattle.battleData.type != EBattleType.SeaRobber && readyBattle.battleData.type != EBattleType.Guerrilla && readyBattle.battleData.type != EBattleType.EventBattle)
				return;

			UISetter.SetActive(readyRoot, true);
			UISetter.SetActive(startRoot, false);
			readyBattle.SetStageRewardInformation();
			Set();
        }

		public override void OnRefresh()
		{
			base.OnRefresh();

			UIReadyBattle readyBattle = uiWorld.readyBattle;

			if(readyBattle.battleData.type != EBattleType.Plunder && readyBattle.battleData.type != EBattleType.SeaRobber && readyBattle.battleData.type != EBattleType.Guerrilla && readyBattle.battleData.type != EBattleType.EventBattle)
				return;

			Set();
		}                      
        
        public void Ready()
        {
            UIReadyBattle uiParent = parentPanelBase as UIReadyBattle;            

            if (uiParent.battleData.type == EBattleType.Plunder)
            {
				RoWorldMap.Stage stage = localUser.FindWorldMapStage(uiParent.battleData.stageId);
                if (!stage.canBattle)
                {
                    EVipRechargeType type = stage.typeData.battleCount == 0 ? EVipRechargeType.StageType1 : EVipRechargeType.StageType2;
                    uiWorld.mainCommand.OpenVipRechargePopUp(type);
                    return;
                }
            }
            else if (uiParent.battleData.type == EBattleType.Guerrilla)
            {
                if (localUser.sweepTicket < 1)
                {
                    UISimplePopup.CreateOK(true, "5635", "12014", null, "1001");
                    return;
                }
            }
            
            uiParent.SelectPlunderTroop();         
        }

        public void Attack()
        {
            UIReadyBattle uiParent = parentPanelBase as UIReadyBattle;            

            if (AttackBtn.GetComponent<UIButton>().isGray)
            {
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
            }
            else
            {
                if (uiParent.battleData.type == EBattleType.Plunder)
                {                                        
                    if (uiParent.CheckEngage())
                        uiParent.SetBattle_withMercenary();
                    else
                        uiParent.StartStageBattle();                    
                }
                else if (uiParent.battleData.type == EBattleType.EventBattle)
                {                    
                    if (uiParent.CheckEngage())
                        uiParent.SetBattle_withMercenary();
                    else
                        uiParent.StartEventBattle();
                }
                else
                {
                    uiParent.StartSweep();                    
                }
            }
        }

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {            
            UIReadyBattle uiParent = parent as UIReadyBattle;
            base.OnClick(sender, parent);
            string key = sender.name;
            if (key == "Ready") {
                Ready();
            } else if (key == "Attack") {
                Attack(); 
			} else if (key == "SkipBtn" || key.StartsWith("SkipsBtn")) {
				// SkipBtn: 1회 소탕, SkipsBtn-N: N회 연속 소탕
				bool skips = key != "SkipBtn";
				GameObject btn = skips ? skipsBtn : skipBtn;
				if (btn.GetComponent<UIButton>().isGray)
				{
					ShowSkipUnavailableMessage(uiParent);
				}
				else
				{
					SoundManager.PlaySFX("BTN_Sweep_001");

					if (uiParent.battleData.type == EBattleType.Plunder)
						UseTimeMachine(skips);
					else
						UseTimeMachineSweep(skips);
				}
			} else if (key == "RechargeBtn") {
				RoWorldMap.Stage stage = localUser.FindWorldMapStage(uiParent.battleData.stageId);
				EVipRechargeType type = stage.typeData.battleCount == 3 ? EVipRechargeType.StageType1 : EVipRechargeType.StageType2;
				uiWorld.mainCommand.OpenVipRechargePopUp(type);
            }
            else if (key == "RepeatMode")
            {
                if (!GameSetting.instance.repeatBattle)
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), string.Format(Localization.Get("10000002"), uiParent.battleData.type == EBattleType.Plunder ? Localization.Get("4006") : Localization.Get("4008")));

                    GameSetting.instance.repeatBattle = true;
                    UISetter.SetFlipSwitch(flipRepeat, GameSetting.instance.repeatBattle);
                }
                else
                {
                    GameSetting.instance.repeatBattle = false;
                    UISetter.SetFlipSwitch(flipRepeat, GameSetting.instance.repeatBattle);
                }                
            }
            else if (key == "RepeatModeLock")
            {
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("10000007"));	
            }
        }

		/// <summary>소탕 버튼이 비활성일 때 원인에 맞는 안내(횟수 충전 팝업 / 소탕권 부족 / 클리어 필요)를 보여준다.</summary>
		private void ShowSkipUnavailableMessage(UIReadyBattle uiParent)
		{
			if (uiParent.battleData.type == EBattleType.Plunder)
			{
				RoWorldMap.Stage stage = localUser.FindWorldMapStage(uiParent.battleData.stageId);
				if (!stage.canBattle)
				{
					EVipRechargeType type = stage.typeData.battleCount == 3 ? EVipRechargeType.StageType1 : EVipRechargeType.StageType2;
					uiWorld.mainCommand.OpenVipRechargePopUp(type);
				}
				else
				{
					NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("6138"));
				}
			}
			else
			{
				if (localUser.GetSweepClearState(uiParent.battleData.sweepType, uiParent.battleData.sweepLevel))
					UISimplePopup.CreateOK(true, "5635", "12014", null, "1001");
				else
					NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("6138"));
			}
		}

		private void UseTimeMachine(bool skips)
		{
			UIReadyBattle readyBattle = uiWorld.readyBattle;
			if(skips)
			{
				int count = int.Parse(skipsBtn.name.Substring(skipsBtn.name.IndexOf("-") + 1));
				if(count == 0)
				{
					uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Bullet);
					return;
				}
				network.RequestUseTimeMachine(readyBattle.battleData.stageId, count, 0);
			}
			else
			{
				RoWorldMap.Stage stage = localUser.FindWorldMapStage(readyBattle.battleData.stageId);
				if(localUser.bullet < stage.data.bullet)
				{
					uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Bullet);
					return;
				}

				network.RequestUseTimeMachine(readyBattle.battleData.stageId, 1, 0);
			}
		}

		private void UseTimeMachineSweep(bool skips)
		{
			int maxCount = int.Parse(regulation.defineDtbl["WARROOM_CLEAR_MAX_COUNT"].value);
			int count = 1;
			UIReadyBattle readyBattle = uiWorld.readyBattle;

			if(skips)
			{
				if(localUser.sweepTicket > maxCount)
				{
					count = maxCount;
				}
				else
				{
					count = localUser.sweepTicket;
				}
			}
			network.RequestUseTimeMachineSweep(readyBattle.battleData.sweepType, readyBattle.battleData.sweepLevel, count);
		}		         
    }

	/// <summary>레이드 / 이벤트 레이드 파츠. 보스 위치와 체력을 표시한다.</summary>
	[System.Serializable]
	public class Raid : UIInnerPartBase
	{
		public GameObject readyRoot;
		public GameObject startRoot;
		public GameObject raidRoot;
		public GameObject eventRaidRoot;
		public GameObject useRoot;
		public GameObject AttackBtn;

		public UISprite useIcon;
		public UILabel useCount;
		public UIGrid useGrid;

		public List<UISprite> raidPositionList;

		public UIUnit raidUnit;

		//이벤트 레이드
		public UISprite bossThumb;
		public UIProgressBar hpProgress;
		public UILabel hpLabel;
		public UILabel maxHpLabel;

		public void Set()
		{
			UIReadyBattle readyBattle = uiWorld.readyBattle;

			UISetter.SetButtonGray(AttackBtn, !readyBattle._curLhsTroop.IsEmpty());
		}
		
		public override void OnInit(UIPanelBase parent)
		{
			base.OnInit(parent);
			UIReadyBattle readyBattle = parent as UIReadyBattle;

			if(readyBattle.battleData.type != EBattleType.Raid && readyBattle.battleData.type != EBattleType.EventRaid)
				return;

			ResetRaidPosition();
			readyBattle.StartCoroutine(readyBattle.SelectRaidTroopDelay());

			UISetter.SetActive(raidRoot, readyBattle.battleData.type == EBattleType.Raid);
			UISetter.SetActive(eventRaidRoot, readyBattle.battleData.type == EBattleType.EventRaid);
			if(readyBattle.battleData.type == EBattleType.Raid)
			{
				raidUnit.Set(readyBattle.battleData.raidData.commanderId);
				RaidChallengeDataRow rcdr = RemoteObjectManager.instance.regulation.raidChallengeDtbl[readyBattle.battleData.raidData.raidId.ToString()];
				UISetter.SetActive(raidPositionList[rcdr.commanderPos], true);
				UISetter.SetSprite(raidPositionList[rcdr.commanderPos], "raid-bg-boss-fm02");
				for(int i = 0; i < rcdr.parts.Count; i++)
				{
					if(!string.IsNullOrEmpty(rcdr.parts[i]) && rcdr.parts[i] != "0")
					{
						UISetter.SetActive(raidPositionList[rcdr.partsPosition[i]], true);
						UISetter.SetSprite(raidPositionList[rcdr.partsPosition[i]], "raid-bg-boss-fm03");
					}
				}

				string id = ((int)EGoods.Key).ToString();
				GoodsDataRow goodsData = regulation.FindGoodsServerFieldName(id);
				UISetter.SetActive(useRoot, true);
				useGrid.repositionNow = true;
				UISetter.SetSprite(useIcon, goodsData.iconId);
				UISetter.SetLabel(useCount, 1);
			}
			else if(readyBattle.battleData.type == EBattleType.EventRaid)
			{
				RoTroop.Slot slot = readyBattle.battleData.defenderTroop.slots[0];

				var unit = RoUnit.Create(slot.unitId, slot.unitLevel, slot.unitRank, slot.unitCls, slot.unitCostume, slot.commanderId, slot.favorRewardStep, slot.marry, slot.transcendence, EBattleType.EventRaid);

				UISetter.SetSprite(bossThumb, string.Format("{0}_Front", unit.currLevelReg.resourceName));
				UISetter.SetLabel(hpLabel, slot.health.ToString("N0"));
				UISetter.SetLabel(maxHpLabel, string.Format("/{0}", unit.currLevelReg.originMaxHealth.ToString("N0")));
				UISetter.SetProgress(
					hpProgress, 
					(float)slot.health / (float)unit.currLevelReg.originMaxHealth
				);

				UISetter.SetActive(raidPositionList[slot.position], true);
				UISetter.SetSprite(raidPositionList[slot.position], "raid-bg-boss-fm02");

				string id = ((int)EGoods.EventRaidChallenge).ToString();
				GoodsDataRow goodsData = regulation.FindGoodsServerFieldName(id);
				useGrid.repositionNow = true;
				UISetter.SetSprite(useIcon, goodsData.iconId);
				UISetter.SetLabel(useCount, 1);
			}
			Set();
		}

		void ResetRaidPosition()
		{
			for(int i = 0; i < raidPositionList.Count; i++)
			{
				UISetter.SetActive(raidPositionList[i], false);
			}
		}

		public override void OnRefresh()
		{
			base.OnRefresh();
			UIReadyBattle readyBattle = uiWorld.readyBattle;
			if(readyBattle.battleData.type != EBattleType.Raid && readyBattle.battleData.type != EBattleType.EventRaid)
				return;
			
			Set();
		}
		
		public override void OnClick(GameObject sender, UIPanelBase parent)
		{
			UIReadyBattle uiParent = parent as UIReadyBattle;
			string key = sender.name;
			if (key == "Attack") {
                if (AttackBtn.GetComponent<UIButton>().isGray)
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
                }
                else
                {
                    if (uiParent.battleData.type == EBattleType.Raid)
                    {
                        if (uiParent.CheckEngage())
                            uiParent.SetBattle_withMercenary();
                        else
                            uiParent.StartRaid();
                    }
					else if(uiParent.battleData.type == EBattleType.EventRaid)
					{
						uiParent.StartEventRaid();
					}
                }
			}
		}
	}

	/// <summary>방어 덱 / 점령전 덱 저장 파츠.</summary>
	[System.Serializable]
	public class Defender : UIInnerPartBase
	{
		public GameObject saveDeckBtn;

		public void Set()
		{
			UIReadyBattle readyBattle = uiWorld.readyBattle;
         
            UISetter.SetButtonGray(saveDeckBtn, !readyBattle.IsEmptyTroop());
		}

		public override void OnInit(UIPanelBase parent)
		{
			base.OnInit(parent);
			UIReadyBattle readyBattle = parent as UIReadyBattle;
			if (!IsDefenderType(readyBattle.battleData.type))
				return;
			Set();
		}

		public override void OnRefresh()
		{
			base.OnRefresh();
			UIReadyBattle readyBattle = uiWorld.readyBattle;
			if (IsDefenderType(readyBattle.battleData.type))
			{
				Set();
			}
		}

		public override void OnClick(GameObject sender, UIPanelBase parent)
		{
			UIReadyBattle uiParent = parent as UIReadyBattle;
			string key = sender.name;
			if (key == "Setting") 
			{       
                if (uiParent.IsEmptyTroop())
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
                }
                else
                {
                    if (uiParent.battleData.type == EBattleType.Defender
                        || uiParent.battleData.type == EBattleType.WaveDuelDefender
                        || uiParent.battleData.type == EBattleType.WorldDuelDefender)
                        uiParent.SaveDefenderDeck();
					else if (uiParent.battleData.type == EBattleType.Conquest)
                        uiParent.SaveConquestDeck();
                }
            }
		}
	}

    /// <summary>웨이브 전투 파츠.</summary>
    [System.Serializable]
    public class WaveBattle : UIInnerPartBase
    {        
        public UITroop stageInfo;        
        public UICommander bossCommander;

        public GameObject readyRoot;
        public GameObject startRoot;
        public GameObject AttackBtn;

        public UILabel title;
        public UILabel description;

        public UILabel bulletTopCnt;
             
        private WaveBattleDataRow row;

        public void Set()
        {
            UIReadyBattle readyBattle = uiWorld.readyBattle;
            
            BattleData data = readyBattle.battleData;
            UISetter.SetButtonGray(AttackBtn, !readyBattle._curLhsTroop.IsEmpty());

            row = regulation.FindWaveBattleData(int.Parse(data.stageId));
            bossCommander.Set("20101");

            UISetter.SetLabel(title, Localization.Get(row.name));
            UISetter.SetLabel(description, Localization.Get(row.explanation));

            UISetter.SetLabel(bulletTopCnt, string.Format("x{0}", 1));
        }

        public override void OnInit(UIPanelBase parent)
        {
            base.OnInit(parent);
            UIReadyBattle readyBattle = parent as UIReadyBattle;

            if (readyBattle.battleData.type != EBattleType.WaveBattle)
                return;

            UISetter.SetActive(readyRoot, true);
            UISetter.SetActive(startRoot, false);
            readyBattle.SetStageRewardInformation();
            Set();           
        }

        public override void OnRefresh()
        {
            base.OnRefresh();

            UIReadyBattle readyBattle = uiWorld.readyBattle;

            if (readyBattle.battleData.type != EBattleType.WaveBattle)
                return;

            Set();
        }        

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            UIReadyBattle uiParent = parent as UIReadyBattle;
            base.OnClick(sender, parent);
            string key = sender.name;
            if (key == "Ready")
            {
                uiParent.SelectWaveBattleTroop();
            }
            else if (key == "Attack")
            {
                if (AttackBtn.GetComponent<UIButton>().isGray)
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
                }
                else
                {
                    if (uiParent.battleData.type == EBattleType.WaveBattle)
                    {
                        if (uiParent.CheckEngage())
                            uiParent.SetBattle_withMercenary();
                        else
                            uiParent.StartWaveBattle();
                    }
                }
            }            
        }               
    }


    /// <summary>협동 전투 파츠. 보스형 적이면 보스 위치를 표시한다.</summary>
    [System.Serializable]
    public class CooperateBattle : UIInnerPartBase
    {        
        public GameObject attackBtn;

        public GameObject bossView;
        public UISprite bossUnit;
        public List<UISprite> bossPositionList;

        private CooperateBattleDataRow _coopDr;

        public void Set()
        {
            UIReadyBattle readyBattle = uiWorld.readyBattle;
            UISetter.SetButtonGray(attackBtn, !readyBattle._curLhsTroop.IsEmpty());

            BattleData data = readyBattle.battleData;
            _coopDr = regulation.cooperateBattleDtbl[data.stageId];

            if (_coopDr.enemyType == ECooperateBattleEnemyType.Boss)
            {
                UISetter.SetActive(readyBattle.forBattleRoot, false);

                UISetter.SetActive(bossView, true);
                UnitDataRow unitDr = regulation.unitDtbl[_coopDr.enemy];
                UISetter.SetSprite(bossUnit, string.Format("{0}_Front", unitDr.resourceName));

                ResetRaidPosition();
                UISetter.SetActive(bossPositionList[_coopDr.enemyrPos - 1], true);
                UISetter.SetSprite(bossPositionList[_coopDr.enemyrPos - 1], "raid-bg-boss-fm02");
                for (int i = 0; i < _coopDr.parts.Count; i++)
                {
                    if (!string.IsNullOrEmpty(_coopDr.parts[i]) && _coopDr.parts[i] != "0")
                    {
                        UISetter.SetActive(bossPositionList[_coopDr.partsPos[i] - 1], true);
                        UISetter.SetSprite(bossPositionList[_coopDr.partsPos[i] - 1], "raid-bg-boss-fm03");
                    }
                }
            }
            else
            {
                UISetter.SetActive(readyBattle.forBattleRoot, true);
                UISetter.SetActive(bossView, false);
            }                  
        }

        void ResetRaidPosition()
        {
            for (int i = 0; i < bossPositionList.Count; i++)
            {
                UISetter.SetActive(bossPositionList[i], false);
            }
        }

        public override void OnInit(UIPanelBase parent)
        {
            base.OnInit(parent);

            UIReadyBattle readyBattle = parent as UIReadyBattle;
            if (readyBattle.battleData.type != EBattleType.CooperateBattle)
                return;

            readyBattle.StartCoroutine(readyBattle.SelectCooperateBattleTroopDelay());
            Set();            
        }

        public override void OnRefresh()
        {
            UIReadyBattle readyBattle = uiWorld.readyBattle;
            if (readyBattle.battleData.type != EBattleType.CooperateBattle)
                return;

            Set();
        }        

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            UIReadyBattle uiParent = parent as UIReadyBattle;
            string key = sender.name;
            if (key == "Attack")
            {
                if (attackBtn.GetComponent<UIButton>().isGray)
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("1332"));
                }
                else
                {                    
                    if (uiParent.battleData.type == EBattleType.CooperateBattle)
                    {
                        if (localUser.currentCooperateRemainTime.GetRemain() <= 0)
                        {
                            NetworkAnimation.Instance.CreateFloatingText(Localization.Get("5090010"));
                            return;
                        }                        

                        uiParent.StartCooperateBattle();                        
                    }
                }                
            }
        }
    }

    public void OpenAnimation()
	{
		iTween.MoveTo(this.gameObject, iTween.Hash("y",0,
		                                           "islocal",true,
		                                           "time",0.3,
		                                           "delay",0,
		                                           "easeType", iTween.EaseType.easeOutBack));
	}
	
	public void CloseAnimation()
	{
		DefaultCloseAnimation();
	}

	public override void Open()
	{
		base.Open();
		OpenAnimation();
	}
}
