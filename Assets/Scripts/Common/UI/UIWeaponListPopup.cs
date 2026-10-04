using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Shared.Regulation;

/// <summary>
/// 무기 장착 팝업 (지휘관 상세의 무기 슬롯에서 연다).
/// 해당 슬롯에 장착할 수 있는 무기 목록(일반 / 전용 탭)을 능력치 기준으로 정렬해 보여주고,
/// 선택한 무기를 장착했을 때의 능력치 변화와 스킬 효과를 현재 장착 무기와 비교한다.
/// </summary>
public class UIWeaponListPopup : UIPopup {

    /// <summary>무기 목록 탭 (서버의 privateType 값과 같다)</summary>
    private enum TabType
    {
        Basic = 0,  // 일반 무기
        Private     // 전용 무기
    }

	public GEAnimNGUI AnimBG;
	public GEAnimNGUI AnimBlock;

    public UIFlipSwitch basicTab;
    public UIFlipSwitch privateTab;

    public UIDefaultListView weaponListView;

    public UILabel useCommander;

    // 현재 능력치
    public UILabel attack;
    public UILabel defence;
    public UILabel accuracy;
    public UILabel luck;
    public UILabel leadership;
    public UILabel criticalDamage;

    // 선택한 무기 장착 시 능력치 변화
    public UILabel attackIncrease;
    public UILabel defenceIncrease;
    public UILabel accuracyIncrease;
    public UILabel luckIncrease;
    public UILabel speedIncrease;
    public UILabel criticalDamageIncrease;

	public UISprite sortBtn;
	public GameObject sortRoot;
	public GUIAnimNGUI sortBtnAnim;
    EWeaponListSortType sortType;

    public UILabel emptyLabel;
    public GameObject bottomRoot;
    public GameObject weaponListRoot;
    public GameObject detailRoot;

    public GameObject equipBtn;
    public GameObject compareBtn;

    public UIWeaponSkillInfo baseSkillInfo;     // 현재 장착 무기 스킬
    public UIWeaponSkillInfo weaponSkillInfo;   // 선택한 무기 스킬

    public UILabel compareBtnLabel;

    private static readonly Color StatDecreaseColor = new Color(248f / 255f, 32f / 255f, 40f / 255f);
    private static readonly Color StatIncreaseColor = new Color(106f / 255f, 166f / 255f, 53f / 255f);

    TabType tabType;
    int selectWeaponId;

    string commanderId;
    int slotType;   // 1: 머리, 2: 오른손, 3: 왼손, 4: 몸통, 5: 특수

    void Start()
	{
        SetAutoDestroy(true);
        OpenPopup();
	}

    /// <summary>팝업 진입점. 장착할 지휘관과 무기 슬롯, 처음 보여줄 탭을 정한다.</summary>
    public void Set(int slotType, int privateType, string commanderId)
	{
        sortType = EWeaponListSortType.Undefined;
        tabType = (TabType)privateType;
        this.slotType = slotType;
        this.commanderId = commanderId;
        UISetter.SetLabel(compareBtnLabel, Localization.Get("70022"));
        InitList();
        SetEmptyLabel();
	}

    /// <summary>
    /// 정렬 기준에 해당하는 무기의 추가 능력치. 정렬하지 않으면 0.
    /// </summary>
    private int GetSortValue(RoWeapon weapon)
    {
        switch (sortType)
        {
            case EWeaponListSortType.TotalStatus:    return weapon.GetTotalAddStatPoint();
            case EWeaponListSortType.Attack:         return weapon.GetAddStatPoint(EItemStatType.ATK);
            case EWeaponListSortType.Defence:        return weapon.GetAddStatPoint(EItemStatType.DEF);
            case EWeaponListSortType.Accuracy:       return weapon.GetAddStatPoint(EItemStatType.ACCUR);
            case EWeaponListSortType.Luck:           return weapon.GetAddStatPoint(EItemStatType.LUCK);
            case EWeaponListSortType.LeaderShip:     return weapon.GetAddStatPoint(EItemStatType.MOB);
            case EWeaponListSortType.CriticalDamage: return weapon.GetAddStatPoint(EItemStatType.CRITDMG);
            default:                                 return 0;
        }
    }

    /// <summary>
    /// 무기 목록을 만든다. 정렬 기준 능력치가 높은 순, 같으면 무기 데이터 번호 순으로 정렬하고 첫 무기를 선택한다.
    /// </summary>
    public void InitList()
    {
        RoCommander commander = localUser.FindCommander(commanderId);
        List<RoWeapon> weaponDataList = localUser.GetWeaponList(slotType, (int)tabType, commander.unitId);

        weaponDataList.Sort(delegate (RoWeapon row, RoWeapon row1)
        {
            int compare = GetSortValue(row1).CompareTo(GetSortValue(row));
            if (compare != 0)
                return compare;
            return int.Parse(row.data.idx).CompareTo(int.Parse(row1.data.idx));
        });

        CommanderDataRow commanderData = regulation.commanderDtbl[commanderId];
        weaponListView.InitWeapon(weaponDataList, commanderData.unitId, "Weapon-");
        weaponListView.ResetPosition();

        bool hasWeapon = weaponDataList.Count > 0;
        if (hasWeapon)
        {
            selectWeaponId = int.Parse(weaponDataList[0].idx);
            weaponListView.SetSelection(selectWeaponId.ToString(), true);
            ChangeStatusLabel();
        }

        UISetter.SetActive(bottomRoot, hasWeapon);
        UISetter.SetActive(emptyLabel, !hasWeapon);
        UISetter.SetActive(compareBtn, hasWeapon);
    }

    public override void OnClick(GameObject sender)
    {
        string key = sender.name;

        if (key == "Close")
        {
            this.ClosePopup();
        }
        else if (key == "BasicTab")
        {
            ChangeTab(TabType.Basic);
        }
        else if (key == "PrivateTab")
        {
            ChangeTab(TabType.Private);
        }
        else if (key.StartsWith("Weapon-"))
        {
            selectWeaponId = int.Parse(key.Substring(key.IndexOf("-") + 1));
            weaponListView.SetSelection(selectWeaponId.ToString(), true);
            ChangeStatusLabel();
        }
        else if (key == "EquipWeaponBtn")
        {
            if (!equipBtn.GetComponent<UIButton>().isGray)
                EquipCheck();
        }
        else if (key == "CompareBtn")
        {
            OpenSkillCompare(!detailRoot.activeSelf);
        }
		else if (key == "SortBtn")
		{
			ToggleSortMenu();
		}
		else if (key.StartsWith("SortType-"))
		{
			sortType = (EWeaponListSortType)(int.Parse(key.Substring(key.IndexOf("-") + 1)));
			InitList();
		}
    }

    private void ChangeTab(TabType tab)
    {
        tabType = tab;
        sortType = EWeaponListSortType.Undefined;
        InitList();
    }

	/// <summary>정렬 기준 메뉴 열기/닫기</summary>
	void ToggleSortMenu()
	{
		UISetter.SetActive(sortRoot, !sortRoot.activeSelf);
		UISetter.SetSprite(sortBtn, sortRoot.activeSelf ? "qm-down-button" : "qm-up-button");

		if (sortRoot.activeSelf)
			sortBtnAnim.MoveIn(GUIAnimSystemNGUI.eGUIMove.Self);
		else
			sortBtnAnim.MoveOut(GUIAnimSystemNGUI.eGUIMove.Self);
	}

    /// <summary>선택한 무기를 장착한다. 다른 지휘관이 장착 중인 무기면 확인 후 가져온다.</summary>
    void EquipCheck()
    {
        RoWeapon selectWeapon = localUser.FindWeapon(selectWeaponId.ToString());
        if (selectWeapon.currEquipCommanderId > 0)
        {
            UISimplePopup.CreateBool(true, "1303", "70036", "", "1304", "1305").onClick = obj =>
            {
                if (obj.name == "OK")
                    RequestEquip();
            };
        }
        else
        {
            RequestEquip();
        }
    }

    private void RequestEquip()
    {
        network.RequestEquipWeapon(int.Parse(commanderId), selectWeaponId);
        ClosePopup();
    }

    /// <summary>스킬 비교 화면 전환: 현재 장착 무기와 선택한 무기의 스킬 효과를 나란히 보여준다.</summary>
    void OpenSkillCompare(bool open)
    {
        UISetter.SetActive(weaponListRoot, !open);
        UISetter.SetActive(detailRoot, open);
        UISetter.SetLabel(compareBtnLabel, open ? Localization.Get("70035") : Localization.Get("70022"));

        RoCommander commander = localUser.FindCommander(commanderId);
        RoWeapon weapon = commander.FindWeaponItem(slotType);

        baseSkillInfo.Set(commanderId, slotType, weapon == null ? "" : weapon.idx);
        weaponSkillInfo.Set(commanderId, slotType, selectWeaponId.ToString());
    }

    /// <summary>무기 구성으로 지휘관 유닛의 최종 능력치를 계산한다.</summary>
    private static UnitDataRow CalcUnitStat(RoCommander commander, Dictionary<int, RoWeapon> weapons)
    {
        return RoCommander.InvokeLevel(commander.unitReg,
            commander.rank,
            commander.level,
            commander.cls,
            commander.currentCostume,
            commander.id,
            commander.favorRewardStep,
            commander.marry,
            commander.transcendence,
            commander.GetEquipItemList(),
            commander.completeSetItemEquip,
            weapons, false);
    }

    /// <summary>
    /// 선택한 무기 정보 표시.
    /// 현재 능력치와, 선택한 무기로 해당 슬롯을 바꿨을 때의 능력치를 각각 계산해 차이를 보여준다.
    /// </summary>
    void ChangeStatusLabel()
    {
        RoWeapon weapon = localUser.FindWeapon(selectWeaponId.ToString());
        UISetter.SetActive(useCommander, weapon.currEquipCommanderId > 0);
        if (weapon.currEquipCommanderId > 0)
        {
            CommanderDataRow useCommanderData = regulation.commanderDtbl[weapon.currEquipCommanderId.ToString()];
            UISetter.SetLabel(useCommander, Localization.Format("70024", Localization.Get(useCommanderData.nickname)));
        }

        // 이미 장착 중인 무기를 선택하면 장착 / 비교 버튼을 비활성화
        RoCommander commander = localUser.FindCommander(commanderId);
        RoWeapon equippedWeapon = commander.FindWeaponItem(slotType);
        bool isOtherWeapon = equippedWeapon == null || weapon.idx != equippedWeapon.idx;
        UISetter.SetButtonGray(equipBtn, isOtherWeapon);
        UISetter.SetButtonGray(compareBtn, isOtherWeapon);

        UnitDataRow unitData = CalcUnitStat(commander, commander.WeaponItem);

        // 선택한 무기로 해당 슬롯만 교체한 무기 구성
        Dictionary<int, RoWeapon> changedWeapons = new Dictionary<int, RoWeapon>();
        changedWeapons.Add(weapon.data.slotType, weapon);
        foreach (var weaponData in commander.WeaponItem)
        {
            if (!changedWeapons.ContainsKey(weaponData.Key))
                changedWeapons.Add(weaponData.Key, weaponData.Value);
        }
        UnitDataRow changeUnitData = CalcUnitStat(commander, changedWeapons);

        UISetter.SetLabel(attack, unitData.attackDamage);
        UISetter.SetLabel(defence, unitData.defense);
        UISetter.SetLabel(accuracy, unitData.accuracy);
        UISetter.SetLabel(luck, unitData.luck);
        UISetter.SetLabel(leadership, unitData.speed);
        UISetter.SetLabel(criticalDamage, unitData.criticalDamageBonus);

        SetStatDiff(attackIncrease, changeUnitData.attackDamage - unitData.attackDamage);
        SetStatDiff(defenceIncrease, changeUnitData.defense - unitData.defense);
        SetStatDiff(accuracyIncrease, changeUnitData.accuracy - unitData.accuracy);
        SetStatDiff(luckIncrease, changeUnitData.luck - unitData.luck);
        SetStatDiff(speedIncrease, changeUnitData.speed - unitData.speed);
        SetStatDiff(criticalDamageIncrease, changeUnitData.criticalDamageBonus - unitData.criticalDamageBonus);
    }

    /// <summary>능력치 변화량을 "(+10)" / "(-5)" 형식으로 표시한다. 감소는 빨간색, 증가·유지는 초록색.</summary>
    private void SetStatDiff(UILabel label, int diff)
    {
        UISetter.SetColor(label, diff < 0 ? StatDecreaseColor : StatIncreaseColor);
        UISetter.SetLabel(label, string.Format("({0}{1})", diff < 0 ? "" : "+", diff));
    }

    /// <summary>장착할 무기가 없을 때 슬롯별 안내 문구</summary>
    void SetEmptyLabel()
    {
        string emptyStr = "";
        switch (slotType)
        {
            case 1: emptyStr = "70906"; break;
            case 2: emptyStr = "70903"; break;
            case 3: emptyStr = "70904"; break;
            case 4: emptyStr = "70905"; break;
            case 5: emptyStr = "70907"; break;
        }
        UISetter.SetLabel(emptyLabel, Localization.Get(emptyStr));
    }

    /// <summary>
    /// 무기 스킬 정보 파츠.
    /// 전용 무기는 무기 고유 스킬을, 일반 무기는 지휘관의 원래 스킬을 보여주고
    /// 무기 부가 효과 2개의 설명과 적용 대상(아군/적군)을 표시한다.
    /// </summary>
    [System.Serializable]
    public class UIWeaponSkillInfo : UIInnerPartBase
    {
        public UISprite icon;
        public UISprite skillAttackRange;

        public List<UILabel> effectDescription;
        public List<UILabel> effectActive;

        /// <param name="weaponId">비어 있으면 무기 미장착 상태의 기본 스킬을 표시</param>
        public void Set(string commanderId, int slotType, string weaponId = "")
        {
            RoCommander commander = localUser.FindCommander(commanderId);

            if (string.IsNullOrEmpty(weaponId))
            {
                string skillId = commander.GetWeaponSkillId(slotType);

				if (regulation.skillDtbl.ContainsKey(skillId)) {
					SetSkillIcon(regulation.skillDtbl[skillId]);
				} else {
					UISetter.SetActive(skillAttackRange, false);
					UISetter.SetActive(icon, false);
				}

                SetEmptySkill();
                return;
            }

            RoWeapon weapon = localUser.FindWeapon(weaponId);
            if (weapon.data.privateWeapon != 0 && weapon.data.skillIdx != "0")
            {
                SkillDataRow skillData = regulation.skillDtbl[weapon.data.skillIdx];
                SetSkillIcon(skillData);
                SetEffects(weapon, skillData);
                return;
            }

            string originalSkillId = commander.GetWeaponOriginalSkillId(slotType);
            if (string.IsNullOrEmpty(originalSkillId))
            {
                UISetter.SetActive(icon, false);
                UISetter.SetActive(skillAttackRange, false);
                SetEmptySkill();
            }
            else
            {
                SkillDataRow skillData = regulation.skillDtbl[originalSkillId];
                SetSkillIcon(skillData);
                SetEffects(weapon, skillData);
            }
        }

        private void SetSkillIcon(SkillDataRow skillData)
        {
            UISetter.SetSprite(skillAttackRange, skillData.rangeicon);
            UISetter.SetSprite(icon, skillData.thumbnail);
        }

        /// <summary>
        /// 무기 부가 효과 2개를 표시한다.
        /// 효과 대상이 스킬 대상(아군 스킬 / 적군 스킬)과 다르면 "아군에게 적용" 같은 대상 표시를 붙인다.
        /// </summary>
        private void SetEffects(RoWeapon weapon, SkillDataRow skillData)
        {
            WeaponDataRow weaponData = weapon.data;

            ESkillTargetType skillTarget =
                (skillData.targetType == ESkillTargetType.Own || skillData.targetType == ESkillTargetType.Friend)
                ? ESkillTargetType.Friend
                : ESkillTargetType.Enemy;

            for (int i = 0; i < 2; i++)
            {
                bool hasEffect = weaponData.explanation[i] != "0";
                UISetter.SetLabel(effectDescription[i], hasEffect ? Localization.Get(weaponData.explanation[i]) : Localization.Get("70012"));

                bool differentTarget = weaponData.targetType != skillTarget;
                UISetter.SetActive(effectActive[i], differentTarget && hasEffect);
                if (differentTarget)
                    UISetter.SetLabel(effectActive[i], weaponData.targetType == ESkillTargetType.Friend ? Localization.Get("70014") : Localization.Get("70013"));
            }
        }

        void SetEmptySkill()
        {
            UISetter.SetLabel(effectDescription[0], Localization.Get("70012"));
            UISetter.SetLabel(effectDescription[1], Localization.Get("70012"));

            UISetter.SetActive(effectActive[0], false);
            UISetter.SetActive(effectActive[1], false);
        }
    }

    public void OpenPopup()
	{
		base.Open();

		AnimBG.Reset();
		AnimBlock.Reset();

		this.OnAnimOpen();
	}

	/// <summary>팝업 닫기: 퇴장 연출 후 닫는다.</summary>
	public void ClosePopup()
	{
		this.HidePopup();
	}

	IEnumerator OnEventHidePopup()
	{
		yield return new WaitForSeconds(0.8f);

		base.Close();
	}

	void HidePopup()
	{
		OnAnimClose();
		StartCoroutine(OnEventHidePopup());
	}

	void OnAnimOpen()
	{
		this.AnimBG.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimBlock.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
	}

	void OnAnimClose()
	{
		this.AnimBG.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimBlock.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
	}
}
