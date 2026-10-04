using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Shared;
using Shared.Regulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 추천 덱(프리셋 덱) 설정 팝업.
/// 여러 개의 덱 슬롯에 지휘관을 미리 편성해 두고, 전투 준비 화면에서 불러와 쓸 수 있게 한다.
/// - 직업 탭(전체/공격/방어/지원)으로 지휘관 목록을 거르고, 추가/제거 버튼으로 편성
/// - 드래그로 덱 안의 포지션 변경
/// - 덱 이름 변경, 다이아로 덱 슬롯 추가 구매
/// - 팝업을 닫을 때 모든 덱을 한 번에 서버에 저장
/// </summary>
public class UIPreDeckSetting : UIPopup  {

    bool bEnterKeyEnable = false;

    public UIFlipSwitch basicTab;
    public UIFlipSwitch attackTab;
    public UIFlipSwitch defenseTab;
    public UIFlipSwitch supportTab;

    public UISprite dragUnit;
    private int dragStartPosition = -1;
    private List<UIPositionPlate> positionList;
    public UIDefaultListView commanderListView;
    public UIDefaultListView preTroopList;
    private RoTroop[] _currTroop;
    private int selectTroopIdx = 0;
    private EJob commanderJob;

    private int _selectedPositionId;
    private Transform unitFormation;

    // 기획 데이터(define)로 덮어쓰는 기본값
    int maxPredeckCount = 5;    // 최대 덱 슬롯 수
    int basePredeckCount = 5;   // 기본 제공 덱 슬롯 수
    int openCost = 1200;        // 첫 추가 슬롯 가격(다이아)
    int addOpenCost = 200;      // 슬롯을 하나 더 살 때마다 늘어나는 가격
    int predeckCount = 5;       // 현재 보유 덱 슬롯 수

    // 덱 슬롯 목록 배치: 선택한 슬롯만 크게, 살짝 띄워서 표시
    private const float SlotSpacing = 320f;
    private const float SelectedSlotOffset = 13f;
    private const float SelectedSlotScale = 1.1f;
    private const int FormationPlateCount = 9;  // 3x3 포지션

    UITroop GetPreTroopItem(int index)
    {
        return preTroopList.itemList[index] as UITroop;
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

    protected override void Awake()
    {
        base.Awake();
        maxPredeckCount = int.Parse(regulation.defineDtbl["PRE_DECK_COUNT"].value);
        basePredeckCount = int.Parse(regulation.defineDtbl["BASE_DECK_COUNT"].value);
        openCost = int.Parse(regulation.defineDtbl["DECK_PLUS_CASH"].value);
        addOpenCost = int.Parse(regulation.defineDtbl["DECK_PLUS_CASH_VALUE"].value);
    }

    /// <summary>버튼 클릭 처리. 버튼 이름(key)으로 분기한다.</summary>
    public override void OnClick(GameObject sender)
    {
        SoundManager.PlaySFX("EFM_SelectButton_001", false);

        string key = sender.name;
        if (key == "Close")
        {
            CloseAnimation();
        }
        else if (key.StartsWith("DeckItem-"))   // 덱 슬롯 선택 (잠긴 슬롯이면 구매 팝업)
        {
            int idx = int.Parse(key.Substring(key.IndexOf("-") + 1));

            if (idx + 1 > localUser.statistics.predeckCount)
            {
                OpenBuySlotPopup();
                return;
            }

            selectTroopIdx = idx;
            OnRefresh();
        }
        else if (key.StartsWith("Plate-"))
        {
            int position = int.Parse(key.Substring(key.IndexOf("-") + 1));
            if (_currTroop[selectTroopIdx].GetSlotByPosition(position) != null)
                _selectedPositionId = position;
        }
        else if (key.StartsWith("AddButton-"))
        {
            AddCommander(key.Substring(key.IndexOf("-") + 1));
        }
        else if (key.StartsWith("RemoveButton-"))
        {
            RemoveCommander(key.Substring(key.IndexOf("-") + 1));
        }
        else if (key == "BasicTab")
        {
            SetCommanderJob(EJob.All);
        }
        else if (key == "AttackTab")
        {
            SetCommanderJob(EJob.Attack);
        }
        else if (key == "DefenseTab")
        {
            SetCommanderJob(EJob.Defense);
        }
        else if (key == "SupportTab")
        {
            SetCommanderJob(EJob.Support);
        }
        else if (key == "EditBtn")
        {
            // 선택된 덱의 이름 수정 버튼만 반응
            if (sender.transform.GetComponentInParent<UITroop>() != GetPreTroopItem(selectTroopIdx))
                return;

            OpenRenamePopup();
        }
    }

    /// <summary>선택한 덱의 이름 변경 팝업 (최대 10자)</summary>
    private void OpenRenamePopup()
    {
        UIReceiveUserString recv = UISimplePopup.Create<UIReceiveUserString>("InputUserString");
        recv.SetDefault(_currTroop[selectTroopIdx].nickname);
        recv.SetLimitLength(10);
        recv.Set(false, Localization.Get("1357"), "", null, Localization.Get("1006"), Localization.Get("1000"), null);
        recv.onClick = popupSender =>
        {
            if (popupSender.name == "OK")
            {
                _currTroop[selectTroopIdx].nickname = recv.inputLabel.text;
                _SelectTroop(selectTroopIdx);
            }
        };
    }

    #region 드래그로 포지션 변경

    // 선택된 덱의 플레이트만 드래그할 수 있다.
    private void OnDragStart(GameObject go)
    {
        if (dragUnit == null)
        {
            Debug.LogError("invalid state");
            return;
        }

        string key = go.name;
        if (!key.StartsWith("Plate-"))
            return;

        if (go.transform.GetComponentInParent<UITroop>() != GetPreTroopItem(selectTroopIdx))
            return;

        int id = int.Parse(key.Substring(key.IndexOf("-") + 1));

        UIPositionPlate plate = positionList[id];

        if (_currTroop[selectTroopIdx].GetSlotByPosition(id) == null)
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
        if (!key.StartsWith("Plate-"))
            return;

        if (go.transform.GetComponentInParent<UITroop>() != GetPreTroopItem(selectTroopIdx))
            return;

        int id = int.Parse(key.Substring(key.IndexOf("-") + 1));

        RoTroop.Slot prevSlot = _currTroop[selectTroopIdx].GetSlotByPosition(dragStartPosition);
        RoTroop.Slot targetSlot = _currTroop[selectTroopIdx].GetSlotByPosition(id);
        if (prevSlot != null && targetSlot != null)
        {
            if (prevSlot.position == _selectedPositionId)
                _selectedPositionId = targetSlot.position;
            else if (targetSlot.position == _selectedPositionId)
                _selectedPositionId = prevSlot.position;

            int temp = prevSlot.position;
            prevSlot.position = targetSlot.position;
            targetSlot.position = temp;
        }
        else if (targetSlot == null)
        {
            if (prevSlot.position == _selectedPositionId)
                _selectedPositionId = id;
            prevSlot.position = id;
        }
        OnRefresh();
    }

    #endregion

    /// <summary>
    /// 팝업 진입점. 서버에 저장된 추천 덱을 편집용 부대로 복사한 뒤 화면을 연다.
    /// </summary>
    public void InitOpenPopup()
    {
        if (bEnterKeyEnable)
            return;
        bEnterKeyEnable = true;

        if (positionList == null) positionList = new List<UIPositionPlate>();
        if (_currTroop == null) _currTroop = new RoTroop[maxPredeckCount];
        for (int i = 0; i < maxPredeckCount; i++)
        {
            _currTroop[i] = RoTroop.Create(localUser.id, null);
        }

        predeckCount = localUser.statistics.predeckCount;
        OnResetPlate();
        SetPreDeck();
        InitTab();
        InitCommanderList();
        Open();
    }

    /// <summary>서버에 저장된 덱(포지션 → 지휘관 ID)을 편집용 부대에 채운다.</summary>
    void SetPreDeck()
    {
        for (int i = 0; i < predeckCount; i++)
        {
            Protocols.UserInformationResponse.PreDeck deck = localUser.preDeckList[i];
            int idx = 0;
            foreach (var data in deck.deckData)
            {
                RoTroop.Slot slot = _currTroop[deck.idx].slots[idx];
                RoCommander commander = localUser.FindCommander(data.Value.ToString());
                FillSlotFromCommander(slot, commander);
                slot.position = int.Parse(data.Key) - 1;
                idx++;
            }
            _currTroop[deck.idx].nickname = deck.name;
        }

        InitDeckSlots();
        _SelectTroop(selectTroopIdx);
    }

    /// <summary>
    /// 덱 슬롯 목록을 만든다. 보유 슬롯 + 구매용 잠긴 슬롯 1개를 표시한다. (최대 슬롯 수까지)
    /// </summary>
    private void InitDeckSlots()
    {
        int slotCount = Mathf.Min(predeckCount + 1, maxPredeckCount);
        preTroopList.InitPredeckSlot(_currTroop, slotCount, "DeckItem-");
    }

    /// <summary>
    /// 지휘관 정보로 슬롯을 채운다. 추천 덱은 편성만 저장하므로 체력은 항상 최대로 둔다.
    /// </summary>
    private void FillSlotFromCommander(RoTroop.Slot slot, RoCommander commander)
    {
        slot.unitId = commander.unitId;
        slot.unitLevel = commander.level;
        slot.exp = commander.aExp;
        slot.health = RoTroop.Rate100;
        slot.unitCls = commander.cls;
        slot.unitCostume = commander.currentCostume;
        slot.favorRewardStep = commander.favorRewardStep;
        slot.marry = commander.marry;
        slot.transcendence = commander.transcendence;
        slot.unitRank = commander.rank;
        slot.commanderId = commander.id;
        slot.equipItem = commander.GetEquipItemList();
        slot.weaponItem = commander.WeaponItem;
    }

    /// <summary>선택한 덱에 지휘관을 추가한다. 비어 있는 첫 번째 포지션에 배치한다.</summary>
    private void AddCommander(string commanderId)
    {
        RoTroop troop = _currTroop[selectTroopIdx];

        if (!troop.IsAddSlotPossible())
        {
            NetworkAnimation.Instance.CreateFloatingText(Localization.Get("1329"));
            return;
        }

        if (troop.GetSlotByCommanderId(commanderId) != null)
            return;

        RoTroop.Slot slot = troop.GetNextEmptySlot();
        FillSlotFromCommander(slot, localUser.FindCommander(commanderId));

        for (int i = 0; i < troop.slots.Length; i++)
        {
            if (troop.GetSlotIndexByPosition(i) < 0)
            {
                slot.position = i;
                break;
            }
        }
        OnRefresh();
    }

    /// <summary>선택한 덱에서 지휘관을 뺀다.</summary>
    private void RemoveCommander(string commanderId)
    {
        RoTroop troop = _currTroop[selectTroopIdx];
        int removeTarget = troop.GetSlotIndexByCommanderId(commanderId);
        if (removeTarget < 0)
            return;

        troop.slots[removeTarget].ResetSlot();
        OnRefresh();
    }

    private void OnResetPlate()
    {
        for (int i = 0; i < positionList.Count; i++)
        {
            positionList[i].Reset();
        }
    }

    /// <summary>선택한 덱 슬롯만 크게 띄워서 강조한다.</summary>
    private void SelectTroopPosition()
    {
        for (int i = 0; i < preTroopList.itemList.Count; i++)
        {
            Transform item = GetPreTroopItem(i).gameObject.transform;
            if (i == selectTroopIdx)
            {
                item.localScale = new Vector3(SelectedSlotScale, SelectedSlotScale);
                item.localPosition = new Vector3((i * SlotSpacing) + SelectedSlotOffset, SelectedSlotOffset);
            }
            else
            {
                item.localScale = Vector3.one;
                item.localPosition = new Vector3(i * SlotSpacing, 0);
            }
        }
        preTroopList.SetSelection(selectTroopIdx.ToString(), true);
    }

    /// <summary>
    /// 덱을 선택한다. 선택한 덱의 3x3 포지션 플레이트("00" ~ "08")를 찾아 드래그 대상으로 등록한다.
    /// </summary>
    private void _SelectTroop(int index)
    {
        GetPreTroopItem(index).Set(_currTroop[index]);

        if (selectTroopIdx < localUser.statistics.predeckCount)
        {
            positionList.Clear();

            unitFormation = GetPreTroopItem(selectTroopIdx).gameObject.transform.Find("UnitFormation");
            for (int p = 0; p < FormationPlateCount; p++)
            {
                positionList.Add(unitFormation.Find(p.ToString("00")).GetComponent<UIPositionPlate>());
            }
        }
        SelectTroopPosition();
    }

    private void InitTab()
    {
        UISetter.SetFlipSwitch(basicTab, true);
        UISetter.SetFlipSwitch(attackTab, false);
        UISetter.SetFlipSwitch(defenseTab, false);
        UISetter.SetFlipSwitch(supportTab, false);

        commanderJob = EJob.All;
    }

    /// <summary>직업 탭을 바꾸고 지휘관 목록을 다시 만든다.</summary>
    private void SetCommanderJob(EJob job)
    {
        commanderJob = job;
        InitCommanderList();
    }

    /// <summary>지휘관 목록을 처음 위치로 되돌리고 다시 만든다.</summary>
    private void InitCommanderList()
    {
        commanderListView.ResetPosition();
        RefreshCommanderList();
    }

    /// <summary>선택한 직업의 지휘관 목록을 표시한다. 선택한 덱에 들어 있는 지휘관은 목록에서 편성 상태로 표시된다.</summary>
    private void RefreshCommanderList()
    {
        commanderListView.Init(localUser.GetCommanderList(commanderJob, true), _currTroop[selectTroopIdx], null, "Commander_");
    }

    /// <summary>
    /// 덱 슬롯 구매 팝업.
    /// 가격 = 첫 추가 가격 + (추가로 산 슬롯 수 × 증가분). 다이아가 부족하면 상점 이동 팝업을 띄운다.
    /// </summary>
    void OpenBuySlotPopup()
    {
        int cost = openCost + (addOpenCost * (predeckCount - basePredeckCount));
        UISimplePopup.CreateBool(false, Localization.Get("1002"), Localization.Format("27863", cost.ToString("N0")), null, Localization.Get("1002"), Localization.Get("1000")).onClick = sender =>
        {
            if (sender.name != "OK")
                return;

            if (localUser.cash < cost)
            {
                UISimplePopup.CreateBool(true, "1031", "1032", null, "5348", "1000").onClick = shopSender =>
                {
                    if (shopSender.name == "OK")
                        UIManager.instance.world.mainCommand.OpenDiamondShop();
                };
            }
            else
            {
                network.RequestBuyPredeckSlot();
            }
        };
    }

    /// <summary>화면 갱신. 슬롯을 구매해 보유 슬롯 수가 바뀌었으면 슬롯 목록을 다시 만든다.</summary>
    public override void OnRefresh()
    {
        base.OnRefresh();

        if (predeckCount != localUser.statistics.predeckCount)
        {
            predeckCount = localUser.statistics.predeckCount;
            InitDeckSlots();
        }

        if (_currTroop != null)
        {
            _SelectTroop(selectTroopIdx);
            RefreshCommanderList();
        }
    }

    /// <summary>
    /// 편집한 모든 덱을 서버에 저장한다.
    /// 저장 형식: 덱마다 { 포지션(1부터): 지휘관 ID } + 덱 이름
    /// </summary>
    private void SavePreDecks()
    {
        List<Protocols.UserInformationResponse.PreDeck> deckList = new List<Protocols.UserInformationResponse.PreDeck>();
        for (int i = 0; i < localUser.statistics.predeckCount; i++)
        {
            var send = new Dictionary<string, int>();
            for (int idx = 0; idx < _currTroop[i].slots.Length; ++idx)
            {
                var slot = _currTroop[i].slots[idx];
                if (!slot.IsValid())
                    continue;

                send.Add((slot.position + 1).ToString(), int.Parse(slot.commanderId));
            }

            Protocols.UserInformationResponse.PreDeck deck = new Protocols.UserInformationResponse.PreDeck();
            deck.idx = i;
            deck.name = _currTroop[i].nickname;
            deck.deckData = send;

            deckList.Add(deck);
        }
        network.RequestPreDeckSetting(deckList);
    }

    public void OpenAnimation()
    {
        iTween.MoveTo(this.gameObject, iTween.Hash("y", 0,
                                                   "islocal", true,
                                                   "time", 0.3,
                                                   "delay", 0,
                                                   "easeType", iTween.EaseType.easeOutBack));
    }

    /// <summary>닫기: 편집한 덱을 한 번에 저장하고 퇴장 연출 후 닫는다.</summary>
    public void CloseAnimation()
    {
        SavePreDecks();
        DefaultCloseAnimation();
    }

    public override void Open()
    {
        base.Open();
        OpenAnimation();
    }

    public override void Close()
    {
        base.Close();
        bEnterKeyEnable = false;
    }
}
