using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Cache;
using Shared.Regulation;

/// <summary>상세 화면 진입 모드: Recruit = 미보유 지휘관 미리보기, Training = 보유 지휘관 육성</summary>
public enum CommanderDetailType
{
    Recruit = 0,
    Training
}

/// <summary>훈련(경험치 티켓 사용) 결과</summary>
public enum CommanderTrainingResult
{
    Fail = 0,
    Success,
    LevelUp
}

/// <summary>
/// 지휘관 상세 팝업.
/// 탭(지휘관 / 유닛 / 호감도 / 시나리오 / 코스튬 / 보이스)별 정보와 육성 기능(훈련, 승급, 클래스업, 스킬 레벨업,
/// 장비·무기 장착, 코스튬 변경·구매, 선물, 결혼)을 한 화면에서 제공한다.
/// 미보유 지휘관(Recruit)은 기획 기본값으로 가상의 지휘관을 만들어 미리보기로 보여준다.
/// </summary>
public class UICommanderDetail : UIPopup
{
    public GEAnimNGUI AnimLeft;
    public GEAnimNGUI AnimRight;
    public GEAnimNGUI AnimBlock;

    bool bBackKeyEnable = false;
    bool bEnterKeyEnable = false;

    public GameObject card;
    public GameObject info;


    public UISprite trainingIcon;
    public UILabel ticket;

    public UIFlipSwitch cardTab;


    [HideInInspector]
    public CommanderDetailType type;

    public RoCommander commander;
    public UICommander uiCommanderLeft;
    public UICommander uiCommanderRight;
    public UICommander uiCommanderUpgrade;
    [SerializeField]
    private UICommander uiDontHaveCommander;

    public UIUnit uiUnit;

    public CommanderLevelUp commanderLevelUp;
    public GameObject Tab;
    public UIDragScrollView backDragScrollView;

    public UIDefaultListView skillListView;
    public UIScrollView skillScrollView;

    public UILabel skillPoint;
    public UILabel maxSkillPoint;
    public UITimer skillPointRechargeTimer;

    public GameObject recruit;

    public GameObject training;
    public GameObject commanderMedalInfo;
    public GameObject btnZoomExit;

    private bool isZoom = false;

    public UILabel trainingGradeGold;
    public UILabel trainingGold;
    public UILabel recruitGradeGold;
    public UILabel classUpGold;
    public UILabel transcendenceLevel;

    public GameObject haveSkillInfo;
    public GameObject dontHaveSkillInfo;

    public GameObject skillRechargeBtn;
    public GameObject skillLevelUpBtn;
    public GameObject trainingBtn;
    public GameObject rankUpBtn;
    public GameObject exchangeBtn;
    public GameObject transcendenceBtn;

    public GameObject classUpBtn;
    public GameObject badgeCommander;
    public GameObject badgeUnit;
    public GameObject badgeRankUp;
    public GameObject badgeClassUp;
    public GameObject badgeTranscendenceSkillUp;

    public UIDefaultListView ticketListView;
    //-- card size

    float device_width = 0f;
    float device_height = 0f;
    float device_aspectX = 1.0f;
    float device_aspectY = 2.0f;

    bool bTouchEnable = false;
    bool bScaleCard = false;

    private TimeData skillPointReChargeTime;
    private bool trainingPress;
    private int ticketCnt;
	private int ticketWeight;
    private ETrainingTicketType ticketType;



    public UIFlipSwitch commanderTab;
    public UIFlipSwitch unitTab;
    public UIFlipSwitch favorTab;
    public UIFlipSwitch scenarioTab;
    public UIFlipSwitch costumeTab;
    public UIFlipSwitch voiceTab;

    public GameObject CommanderInfo;
    public GameObject UnitInfo;
    public GameObject FavorInfo;
    public GameObject ScenarioInfo;
    public GameObject CostumeInfo;
    public GameObject VoiceInfo;

    public UIDefaultListView commanderClassMaterialView;
    public UILabel commonMedal;

    public GameObject unitRoot;
    public UILabel useTicket;
    public UILabel classUpLimit;
    public UILabel maxRank;
    public UILabel maxClass;
    public GameObject nextBtn;
    public GameObject preBtn;
    public GameObject trainingEffect;
    public Animation rankUpAnimation;

    public UISlider unitScaleSlider;
    private GameObject unitObj;
    private UINavigationPopUp naviPopUp;
    private List<RoCommander> commanderList;

    //코스튬
    public UIScrollView costumeScrollView;
    public UIDefaultListView costumeHaveListView;
    public UIDefaultListView costumeDontHaveListView;
    public GameObject costumeHaveLine;
    public UIBuyContents buyContents;
    private string preCostume;

    //호감도
    public UIDefaultListView favorStepListView;
    public UIDefaultListView commanderGiftListView;
    public UILabel commanderFavorStep;
    public UIProgressBar commanderFavorProgress;
    public UILabel commanderFavorValue;
    public UILabel commanderGift;
    public GameObject commanderGiftBtn;
    public UILabel addGiftCount;
    public GameObject heartRoot;
    public GameObject giftProgressEffect;
    private string preCommanderId;

    public GameObject cardRoot;


	// 시나리오
    public UIDefaultListView scenarioTitleListView;
    public ScenarioContent scenarioContents;
    private List<CommanderScenarioDataRow> scenarioList = null;
    [SerializeField]
    private GameObject contentsObject;
    [SerializeField]
    private GameObject LeftLockDesc;
    [SerializeField]
    private GameObject RightLockDesc;
    [SerializeField]
    private UISprite LeftScenarioBG;
    [SerializeField]
    private UISprite RightScenarioBG;
    [SerializeField]
    private GameObject toggle_item;
    [SerializeField]
    private GameObject toggle_unit;
    [SerializeField]
    private GameObject itemStatusLeft;
    [SerializeField]
    private GameObject statusRight;
    [SerializeField]
    private GameObject itemStatusRight;

    [SerializeField]
    private GameObject basicCostumeCheck;
    [SerializeField]
    private List<UIItemDetail> itemDetailList;
    [SerializeField]
    private List<UISprite> itemSlotImg;
    [SerializeField]
    private List<GameObject> itemSlotLockImg;
    [SerializeField]
    private UIItemDetail itemSetDesc;

    [HideInInspector]
    public UIHaveItemListPopup haveItemListPopup;

    [SerializeField]
    private List<UISprite> equipButton;
    [SerializeField]
    private GameObject upgradeItemBadge;

    //보이스
    public UIScrollView voiceScrollView;
    public UIDefaultListView voiceInteractionView;
    public UIDefaultListView voiceBattleView;
    public GameObject voiceBattleLine;
    public GameObject nextCommander;


    //보유하지 않은 사령관 정보
    [SerializeField]
    private GameObject dontHaveInfo;
    [SerializeField]
    private UIDefaultListView dontHaveSkillListView;
    [SerializeField]
    private UILabel dontHaveMedal;
    [SerializeField]
    private UIProgressBar dontHaveMedalProgress;

    //장비
    public List<UICommanderWeaponItem> weaponList;
    [SerializeField]
    private GameObject weaponRoot;
    [SerializeField]
    private GameObject weaponGrid;
    [SerializeField]
    private GameObject UnitStat;
    [SerializeField]
    private GameObject UnitIcon;
    [SerializeField]
    private UILabel weaponName;
    [SerializeField]
    private GameObject weaponSkillInfoRoot;
    [SerializeField]
    private UICommanderSkill weaponSkill;
    [SerializeField]
    private List<GameObject> weaponSkillEffect;
    [SerializeField]
    private UILabel weaponTakeLabel;
    [SerializeField]
    private GameObject weaponUpgradeBtn;
    [SerializeField]
    private GameObject weaponTakeOffBtn;
    [SerializeField]
    private WeaponSkillDescription weaponSkillDescription;
    [SerializeField]
    private GameObject weaponSkillEffectEmpty;
    [SerializeField]
    private GameObject weaponSkillEmpty;
    [SerializeField]
    private GameObject weaponEmpty;
    [SerializeField]
    private UILabel weaponSkillTitle;
    [SerializeField]
    private UICommanderWeaponItem selectWeaponItem;
    [SerializeField]
    private UISprite weaponToggleBtn;

    public UILabel weaponSetLabel;
    public UILabel weaponSetEffectLabel;
    public UISprite weaponSetEffectLeftArrow;
    public UISprite weaponSetEffectRightArrow;

    private UIWeaponUpgradePopup weaponUpgradePopup;
    private UIWeaponListPopup weaponListPopup;
    private int selectWeaponSlot;

	public UISkillLevelUpContents skillLevelUpContents;
    [SerializeField]
    private GameObject marryComplete;
    [SerializeField]
    private GameObject ringBoxIcon;

    /// <summary>버튼 클릭 처리. 버튼 이름(key)으로 분기하고, 처리하지 않은 버튼은 하위 파츠로 전달한다.</summary>
    public override void OnClick(GameObject sender)
    {
        if (trainingPress) return;

        string key = sender.name;

        if (key == "Close")
        {
            this.ClosePopup();

            UISetter.SetActive(nextBtn, false);
            UISetter.SetActive(preBtn, false);
        }
        else if (key == "InfoClose")
        {
            UISetter.SetActive(info, false);
        }
        else if (key == "InfoOpen")
        {
            UISetter.SetActive(info, true);
            UISetter.SetActive(commanderLevelUp, false);

            SoundManager.PlaySFX("SE_MenuOpen_001");
        }
        else if (key == "CardBtn")
        {
            if (cardTab.GetState() == SwitchStatus.ON)
            {
                device_width = Screen.width;
                device_height = Screen.height;
                device_aspectX = 1.6f;
                device_aspectY = 1.6f;

                this.InitAndOpenCard();

                this.CardScaleUpSize();


            }
            else
            {
                InitAndOpenCard();
            }
        }
        else if (skillListView.Contains(key))
        {

            string id = skillListView.GetPureId(key);
            if (string.Equals(id, "0"))
                return;

            SetSkillInfo(id);
        }
        else if (dontHaveSkillListView.Contains(key))
        {

            string id = dontHaveSkillListView.GetPureId(key);
            if (string.Equals(id, "0"))
                return;

            SetSkillInfo(id);
        }

        else if (key.StartsWith("SkillLevelUpBtn-"))
        {
            int skillIndex = commander.GetSkillIndex(key.Replace("SkillLevelUpBtn-", ""));
            int skillLevel = commander.GetSkillLevel(skillIndex);

            var skillCostRow = regulation.skillCostDtbl[(skillLevel).ToString()];
            if (skillCostRow == null) return;
            int upgradeCost = skillCostRow.typeCost[skillIndex - 1];
            int upgradeLimitLevel = skillCostRow.typeLimitLevel[skillIndex - 1];

            if (upgradeCost == 0 || upgradeLimitLevel == 0) return; // Max

			if (commander.level < upgradeLimitLevel) { // 필요 레벨 부족
				NetworkAnimation.Instance.CreateFloatingText (new Vector3 (0, -0.5f, 0), String.Format (Localization.Get ("8033"), upgradeLimitLevel));
			} else {
				UISetter.SetActive (skillLevelUpContents.root, true);
				skillLevelUpContents.Set (commander, skillIndex);
			}
        }
        else if (key == "SkillPointRechargeBtn")
        {
            SkillPointRechargePoint();

        }
        else if (key == "RecruitWaitBtn")
        {
            CommanderDelay();
        }
        else if (key == "RecruitWaitCancleBtn")
        {
            CommanderDelayCancel();
        }
        else if (key == "RecruitWaitRenewBtn")
        {
            CommanderDelay();
        }
        else if (key == "TrainingBtn")
        {
            // 훈련은 버튼을 누르고 있는 동안 연속 사용(OnPressTrainingBtn / OnReleaseTrainingBtn)으로 처리
        }
        else if (key == "RankUpBtn")
        {
            OnPromotionBtnClicked();
        }
        else if (key == "TranscendenceBtn")
        {
            UIPopup.Create<UITranscendencePopup>("TranscendencePopup").InitAndOpenTranscendence(commander);
        }
        else if (key == "ClassUpBtn")
        {
            OnClassUpBtnClicked();
        }
        else if (key == "TutorialClassUpBtn")
        {
            OnTutorialClassUpBtnClicked();
        }
        else if (key == "GetMedalBtn")
        {
            SetNavigationOpen(EStorageType.Medal, commander.id);
        }
        else if (key == "BtnZoomExit")
        {
            this.CardScaleDownSize();

        }
        else if (key == "TrainingTicketRoot")
        {
            SetTrainingTicketList();
            UISetter.SetActive(ticketListView, !ticketListView.gameObject.activeSelf);
        }
        else if (key.StartsWith("Ticket-"))
        {
            string ticket = key.Substring("Ticket-".Length);
            ticketType = (ETrainingTicketType)Enum.Parse(typeof(ETrainingTicketType), ticket);
            ticketWeight = 0;
            trainingPress = false;
            StopCoroutine("CommanderTraining");
            if (ticketCnt == 0)
            {
                TrainingResult();
            }

            TrainingResultNetwork();
        }
        else if (key == "CommanderBtn")
        {
            SaveChangeCostume();
            SetTab(true, false, false, false, false, false);
        }
        else if (key == "UnitBtn")
        {
            SaveChangeCostume();
            SetTab(false, true, false, false, false, false);
            SetActiveItemStatus(false);
        }
        else if (key == "CostumeBtn")
        {
            SetTab(false, false, false, false, true, false);

            SetTabPanelDepths(2);
            nextCommander.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 2;
            backDragScrollView.scrollView = costumeScrollView;

        }
        else if (key == "VoiceBtn")
        {
            SaveChangeCostume();
            SetTab(false, false, false, false, false, true);

            SetTabPanelDepths(-2);
            nextCommander.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 2;
            backDragScrollView.scrollView = voiceScrollView;
        }
        else if (key == "FavorBtn")
        {
            SaveChangeCostume();
            SetTab(false, false, true, false, false, false);

            SetTabPanelDepths(-2);
        }
        else if (key == "ScenarioBtn")
        {
            SaveChangeCostume();
            SetTab(false, false, false, true, false, false);

            cardRoot.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 3;

            if (scenarioList != null && scenarioList.Count > 0)
            {
                scenarioTitleListView.ResetPosition();                
                ScenarioListItem scenarioItem = scenarioTitleListView.itemList[0] as ScenarioListItem;
                if (scenarioItem != null)
                    scenarioItem.OnClick();
            }                
        }
        else if (key.StartsWith("Material-"))
        {
            string id = key.Substring("Material-".Length);
            SetNavigationOpen(EStorageType.Part, id);
        }
        else if (key == "ExchangeBtn")
        {
            if (localUser.medal > 0)
            {
                SetExchangeOpen();
            }
            else
            {
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("8029"));
            }
        }
        else if (key.StartsWith("Costume_"))
        {
            string id = key.Substring("Costume_".Length);

            if (commander.currentCostume != int.Parse(id))
            {
                SetCostume(id, false);
                uiCommanderRight.Set(commander);
                UIManager.instance.world.mainCommand.spineTest.Set(commander.resourceId);
            }
        }
        else if (key.StartsWith("DonHaveCostume_"))
        {
            string id = key.Substring("DonHaveCostume_".Length);

            CommanderCostumeDataRow row = commander.getCostumeData(int.Parse(id));

            if (row != null)
            {
                buyContents.Set(row);

                uiCommanderLeft.spine.SetSkin(row.skinName);
            }
        }
        else if (favorStepListView.Contains(key))
        {
            int itemId = int.Parse(favorStepListView.GetPureId(key));

            UIItemBase item = favorStepListView.FindItem(itemId.ToString());
            FavorListItem favorItem = item as FavorListItem;

            if (commander.favorStep < itemId)
            {
                // 아직 달성하지 않은 단계
            }
            else if (commander.favorRewardStep + 1 < itemId)
            {
                // 이전 단계 보상부터 받아야 함
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("20048"));
            }
            else
            {
                if (commander.favorRewardStep >= itemId)
                {
                    // 이미 받은 보상
                }
                else
                {
                    if (favorItem.openState)
                    {
                        FavorDataRow favor = commander.GetFavorData(itemId);
                        if (favor.statType != StatType.NONE)
                        {
                            OpenCommanderStatsUp(favor.statType, favor.stat);
                        }
                        network.RequestGetFavorReward(commander.id, itemId);
                    }
                }
            }
        }
        else if (key == "CommanderGiftButton")
        {
            SoundManager.PlaySFX("SE_Training_001");

            if (bMaxFavorStep) //Max
            {
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("20032"));
            }
            else if (localUser.commanderGift < 1)
            {
                //충전팝업                
                uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.CommandersGift);
            }
            else
            {
                bFavorStepUp = true;
                network.RequestGiftFood(commander.id, "56", 1); //사령관의 선물 gcid = 56
                uiCommanderLeft.spine.skeletonAnimation.GetComponent<UIInteraction>().GiftInteraction(InteractionType.COMMGIFT);
                GiftHeartEffect();
                GiftProgressEffect();
            }

        }
        else if (key == "TempScenario")
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("6037"));
        }
        else if (key == "checkBox")
        {
            if (commander.isBasicCostume)
            {
                PlayerPrefs.DeleteKey(commander.id);
                uiCommanderRight.Set(commander);
            }
            else
            {
                int ctid = RemoteObjectManager.instance.regulation.commanderCostumeDtbl.Find(row => row.cid == int.Parse(commander.id) && row.sell == 0).ctid;
                commander.SetBasicCostumeKey(ctid.ToString());
            }
            SetCostume(commander.currentCostume.ToString(), false);
            UIManager.instance.world.mainCommand.spineTest.Set(commander.resourceId);
        }
        else if (key == "Toggle_item" || key == "Toggle_unit")
        {
            SetActiveItemStatus(toggle_item.activeSelf);
            if (toggle_item.activeSelf)
                SetItemEquipButton();
        }
        else if (key == "Toggle_Weapon")
        {
            UISetter.SetActive(toggle_unit, true);
            UISetter.SetActive(toggle_item, false);
            UISetter.SetActive(weaponToggleBtn, false);
            OpenWeaponUI(!weaponRoot.activeSelf);
        }
        else if (key.StartsWith("Weapon-"))
        {
            int id = int.Parse(key.Substring("Weapon-".Length));
            if (selectWeaponSlot != id)
            {
                selectWeaponSlot = id;
                SelectWeapon(selectWeaponSlot);
            }
        }
        else if (key.StartsWith("WeaponEffect-"))
        {
            int id = int.Parse(key.Substring("WeaponEffect-".Length));
            OpenWeaponSkillEffectInfo(id);
        }
        else if (key == "WeaponTakeBtn")
        {
            OpenWeaponTakePopup();
        }
        else if (key == "WeaponTakeOffBtn")
        {
            RoWeapon weapon = commander.FindWeaponItem(selectWeaponSlot);
            if (weapon != null)
                network.RequestReleaseWeapon(int.Parse(commander.id), int.Parse(weapon.idx));
        }
        else if (key == "WeaponUpgradeBtn")
        {
            OpenWeaponUpgradePopup();
        }
        else if (key.StartsWith("WeaponSkill-"))
        {
            string id = key.Substring("WeaponSkill-".Length);

            SetWeaponSkillInfo(id);
        }
        else if (key.StartsWith("Slot_"))
        {
            string id = key.Substring("Slot_".Length);

            if (itemSlotLockImg[int.Parse(id) - 1].activeSelf)
            {
                int grade = 0;
                int stringIdx = 0;
                switch (id)
                {
                    case "1":
                        grade = int.Parse(regulation.defineDtbl["EQUIPITEM_1SLOT_OPEN_CLASS_LIMIT"].value);
                        break;
                    case "2":
                        grade = int.Parse(regulation.defineDtbl["EQUIPITEM_2SLOT_OPEN_CLASS_LIMIT"].value);
                        break;
                    case "3":
                        grade = int.Parse(regulation.defineDtbl["EQUIPITEM_3SLOT_OPEN_CLASS_LIMIT"].value);
                        break;
                    case "4":
                        grade = int.Parse(regulation.defineDtbl["EQUIPITEM_4SLOT_OPEN_CLASS_LIMIT"].value);
                        break;
                }
                stringIdx = 8900 + grade;
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), string.Format(Localization.Get("5030422"), Localization.Get(stringIdx.ToString())));
                SoundManager.PlaySFX("BTN_Negative_001");
            }
            else
            {
                if (haveItemListPopup == null)
                    haveItemListPopup = UIPopup.Create<UIHaveItemListPopup>("HaveItemPopup");
                haveItemListPopup.currCommander = commander;
                haveItemListPopup.SetHaveItemList(true, int.Parse(id));
                SoundManager.PlaySFX("BTN_Positive_001");
            }
        }
        else if (key.StartsWith("VoiceInteraction_"))
        {
            string id = key.Substring("VoiceInteraction_".Length);

            PlayVoice( int.Parse(id));
        }
        else if (key.StartsWith("VoiceBattle_"))
        {
            string id = key.Substring("VoiceBattle_".Length);

            PlayVoice(int.Parse(id), true);
        }
        else if (key == "VoiceBlock")
        {
            NetworkAnimation.Instance.CreateFloatingText(Localization.Get("10083"));
        }
        else if(key == "RingBoxIcon")
        {
            if(localUser.ring > 0)
            {
                
                UIMultiplePopup multiplePopup = Create<UIMultiplePopup>("MultiplePopup");
                multiplePopup.SetData(2, Localization.Get("1303"), string.Format(Localization.Get("20101"), commander.nickname)+ "\n"+Localization.Format("20102", localUser.ring) , Localization.Get("20103"),
                    "Close", null, "GetMarried", Localization.Get("1000"), null, Localization.Get("20100"),
                            false, true, EPaymentType.Ring, 1);

                multiplePopup.onClick = popupSender =>
                {
                    string popupKey = popupSender.name;
                    if (popupKey == "GetMarried")
                    {
                        network.RequestGetMarried(int.Parse(commander.id));                   
                    }
                };
            }
            else
            {                
                NotEnough(MultiplePopUpType.NOTENOUGH_RING);                
            }
        }
        else {
            SendOnClickToInnerParts(sender);
        }
        base.OnClick(sender);

    }

    /// <summary>
    /// 탭 전환 시 겹쳐 보이는 스크롤뷰 / 카드 / 유닛 패널의 NGUI depth를 팝업 depth 기준으로 다시 맞춘다.
    /// unitRootOffset으로 3D 유닛 모델을 앞(+) 또는 뒤(-)로 보낸다.
    /// </summary>
    private void SetTabPanelDepths(int unitRootOffset)
    {
        int baseDepth = gameObject.GetComponent<UIPanel>().depth;
        unitRoot.GetComponent<UIPanel>().depth = baseDepth + unitRootOffset;
        costumeScrollView.GetComponent<UIPanel>().depth = baseDepth + 1;
        cardRoot.GetComponent<UIPanel>().depth = baseDepth + 3;
        favorStepListView.GetComponent<UIPanel>().depth = baseDepth + 1;
        commanderGiftListView.GetComponent<UIPanel>().depth = baseDepth + 1;
        voiceScrollView.GetComponent<UIPanel>().depth = baseDepth + 1;
        skillScrollView.GetComponent<UIPanel>().depth = baseDepth + 1;
    }

    /// <summary>유닛 탭에서 장비 아이템 정보 / 유닛 능력치 표시를 전환한다.</summary>
    private void SetActiveItemStatus(bool isActive)
    {   
        OpenWeaponUI(false);
        UISetter.SetActive(itemStatusRight, isActive);
        UISetter.SetActive(itemStatusLeft, isActive);
        UISetter.SetActive(toggle_unit, isActive);
        UISetter.SetActive(statusRight, !isActive);
        UISetter.SetActive(toggle_item, !isActive && type == CommanderDetailType.Training);
        UISetter.SetActive(weaponToggleBtn, !isActive && !weaponRoot.activeSelf && (commander.state == ECommanderState.Nomal && type != CommanderDetailType.Recruit));
    }

    /// <summary>장착 중인 장비 아이템 상세와 세트 효과를 표시한다.</summary>
    private void SetItemStatus()
    {                
        for (int i = 0; i < itemDetailList.Count; ++i)
        {
            itemDetailList[i].SetLock(true);
        }

        itemSetDesc.SetEffect(commander);
        Dictionary<int, RoItem> itemList = commander.GetEquipItemList();

        if (itemList == null)
            return;

        foreach (KeyValuePair<int, RoItem> pair in itemList)
        {            
            itemDetailList[pair.Key - 1].SetItemDetail(pair.Value, commander);            
        }                
    }

    /// <summary>장비 슬롯 아이콘과 클래스별 슬롯 잠금 상태를 표시한다.</summary>
    private void SetEquipItemSlot()
    {
        ResetItemSlot();
        CheckOpenSlotGrade();
        Dictionary<int, RoItem> itemList = commander.GetEquipItemList();

        if (itemList == null)        
            return;                   

        foreach(KeyValuePair<int, RoItem> pair in itemList)
        {
            EquipItemDataRow itemDb = regulation.equipItemDtbl.Find(row => row.key == pair.Value.id);
            if(itemDb != null)
            {                
                UISetter.SetActive(itemSlotImg[pair.Key - 1], true);
                UISetter.SetSprite(itemSlotImg[pair.Key - 1], itemDb.equipItemIcon);
            }                
        }

    }

    private void ResetItemSlot()
    {
        for (int i = 0; i < itemSlotImg.Count; ++i)
            UISetter.SetActive(itemSlotImg[i], false);
    }

    private void CheckOpenSlotGrade()
    {
        int grade = commander.cls;

        for(int i = 0; i < itemSlotImg.Count; ++i)
            UISetter.SetActive(itemSlotLockImg[i], true);

        if(int.Parse(regulation.defineDtbl["EQUIPITEM_1SLOT_OPEN_CLASS_LIMIT"].value) <= grade)
            UISetter.SetActive(itemSlotLockImg[0], false);

        if (int.Parse(regulation.defineDtbl["EQUIPITEM_2SLOT_OPEN_CLASS_LIMIT"].value) <= grade)
            UISetter.SetActive(itemSlotLockImg[1], false);

        if (int.Parse(regulation.defineDtbl["EQUIPITEM_3SLOT_OPEN_CLASS_LIMIT"].value) <= grade)
            UISetter.SetActive(itemSlotLockImg[2], false);

        if (int.Parse(regulation.defineDtbl["EQUIPITEM_4SLOT_OPEN_CLASS_LIMIT"].value) <= grade)
            UISetter.SetActive(itemSlotLockImg[3], false);

    }

    /// <summary>장착 장비의 스탯 타입별 버튼 이미지를 표시하고, 강화 재료가 모두 있으면 강화 가능 뱃지를 띄운다.</summary>
    private void SetItemEquipButton()
    {
        Dictionary<int, RoItem> equipItemList = commander.GetEquipItemList();
        string defaultIdx = "L_item_rune_";
        string spriteName = string.Empty;

        UISetter.SetActive(upgradeItemBadge, false);
        for (int i = 0; i < equipButton.Count; ++i)
            UISetter.SetActive(equipButton[i], false);

        if (equipItemList == null)
            return;

        foreach (KeyValuePair<int, RoItem> pair in equipItemList)
        {
            switch (pair.Value.statType)
            {
                case EItemStatType.ATK:
                    spriteName = "red0";
                    break;
                case EItemStatType.DEF:
                    spriteName = "green0";
                    break;
                case EItemStatType.ACCUR:
                    spriteName = "yellow0";
                    break;
                case EItemStatType.LUCK:
                    spriteName = "blue0";
                    break;
            }
            spriteName = spriteName + pair.Key;
            UISetter.SetActive(equipButton[pair.Key - 1], true);
            UISetter.SetSprite(equipButton[pair.Key - 1], defaultIdx + spriteName);
            
            // 아이템이 업그레이드가 가능한 아이템을 장착중이라면 뱃지 표시를 해준다.
            EquipItemUpgradeDataRow upgradeItemDb = regulation.equipItemUpgradeDtbl.Find(
                                                    row => row.upgradeType == pair.Value.upgradeType && row.level == pair.Value.level);
            if (upgradeItemDb != null)
            {
                RoPart mainPart = localUser.FindPart(upgradeItemDb.upgradeMaterial1);
                if (mainPart != null && mainPart.count > upgradeItemDb.upgradeMaterial1Volume)
                {
                    RoPart subPart_1 = localUser.FindPart(upgradeItemDb.upgradeMaterial2);
                    if (subPart_1 != null && subPart_1.count > upgradeItemDb.upgradeMaterial2Volume)
                    {
                        RoPart subPart_2 = localUser.FindPart(upgradeItemDb.upgradeMaterial3);
                        if (subPart_2 != null && subPart_2.count > upgradeItemDb.upgradeMaterial3Volume)
                        {
                            RoPart subPart_3 = localUser.FindPart(upgradeItemDb.upgradeMaterial4);
                            if (subPart_3 != null && subPart_3.count > upgradeItemDb.upgradeMaterial4Volume)
                            {
                                RoPart subPart_4 = localUser.FindPart(upgradeItemDb.upgradeMaterial5);
                                if (subPart_4 != null && subPart_4.count > upgradeItemDb.upgradeMaterial5Volume)
                                {
                                    UISetter.SetActive(upgradeItemBadge, true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 보유 / 미보유 코스튬 목록을 구성하고 현재 코스튬을 적용한다.
    /// 미보유 목록의 줄 수에 맞춰 보유 목록의 위치를 계산해, 한 스크롤뷰 안에 두 목록을 이어 붙인다.
    /// </summary>
    private void SetCostume(string id, bool isResetPosition = true)
    {
        List<CommanderCostumeDataRow> haveList = commander.getCostumeList(true);
        List<CommanderCostumeDataRow> dontHaveList = commander.getCostumeList(false);

        if (type == CommanderDetailType.Recruit)
        {
            List<int> haveCostumeList = localUser.GetDonHaveCommCostumeList(commander.id);

            if (haveCostumeList != null)
            {
                foreach (var data_1 in haveCostumeList)
                {
                    CommanderCostumeDataRow costume = regulation.FindCostumeData(data_1);
                    if (costume != null)
                    {
                        if (!haveList.Contains(costume)) haveList.Add(costume);
                        if (dontHaveList.Contains(costume)) dontHaveList.Remove(costume);
                    }
                }
            }

        }


        int lineCount = (int)Mathf.Ceil((float)dontHaveList.Count / 3);
        int offsetY = (int)costumeDontHaveListView.grid.cellHeight * lineCount;

        costumeHaveLine.transform.localPosition = new Vector3(costumeHaveLine.transform.localPosition.x,
                                                               costumeDontHaveListView.transform.localPosition.y - offsetY + 55,
                                                               costumeHaveListView.transform.localPosition.z);

        costumeHaveListView.transform.localPosition = new Vector3(costumeHaveListView.transform.localPosition.x,
                                                                  costumeHaveLine.transform.localPosition.y - 175,
                                                                   costumeHaveListView.transform.localPosition.z);


        haveList.Sort(delegate (CommanderCostumeDataRow row, CommanderCostumeDataRow row1)
        {
            return row.order.CompareTo(row1.order);
        });

        dontHaveList.Sort(delegate (CommanderCostumeDataRow row, CommanderCostumeDataRow row1)
        {
            return row.order.CompareTo(row1.order);
        });

        if (commander.isBasicCostume)
        {            
            if(commander.id == uiCommanderLeft.commanderId) uiCommanderLeft.spine.SetSkin(commander.currentViewCostume);
            if(commander.currentCostume != int.Parse(id))
                commander.currentCostume = int.Parse(id);
        }
        else
        {
            commander.currentCostume = int.Parse(id);
            if (commander.id == uiCommanderLeft.commanderId) uiCommanderLeft.spine.SetSkin(commander.getCurrentCostumeName());            
        }

        costumeHaveListView.Init(haveList, id, "Costume_", null, commander);

        costumeDontHaveListView.Init(dontHaveList, null, "DonHaveCostume_", commander.eventCostume);

        if (isResetPosition) //리스트 아이템 재정렬 여부 - 처음 진입인지 진입 후 스킨 교체인지 체크도 가능
        {
            costumeDontHaveListView.scrollView.ResetPosition();
            costumeHaveListView.scrollView.ResetPosition();
        }
        else //진입 후 스킨 교체일때
        {
            GameObject changeCostume = Utility.LoadAndInstantiateGameObject("Prefabs/UI/CostumeChange", card.transform);
            Destroy(changeCostume, 1);
        }

        UISetter.SetActive(basicCostumeCheck, commander.isBasicCostume);
    }

    private int preFavorStep;
    private bool bFavorStepUp;
    private bool bMaxFavorStep;
    /// <summary>호감도 탭: 단계별 보상 목록, 선물 목록, 현재 호감도 진행률을 표시한다.</summary>
    private void SetFavor()
    {
        //보상리스트
        if (!pressGiftBtn)
        {
            List<FavorDataRow> list = commander.GetFavorList();
            favorStepListView.Init(list, commander.id, commander.favorStep, commander.favorRewardStep, "FavorStep_");
        }

        //선물
        commanderGiftListView.InitGiftList(regulation.GetCommanderGiftList(), "Gift_");

        //Commander 호감도 정보
        UISetter.SetLabel(commanderFavorStep, commander.favorStep);

        if (bFavorStepUp && preFavorStep != commander.favorStep)
        {
            TweenScale scale = heartRoot.GetComponent<TweenScale>();
            scale.enabled = true;
            scale.ResetToBeginning();
        }
        preFavorStep = commander.favorStep;
        bFavorStepUp = false;

        FavorStepDataRow stepData = RemoteObjectManager.instance.regulation.FindFavorStepData(commander.favorStep + 1);

        bMaxFavorStep = IsMaxFavorStep();
        
        if (bMaxFavorStep) 
        {
            UISetter.SetLabel(commanderFavorValue, Localization.Get("1309"));
            UISetter.SetProgress(commanderFavorProgress, 0);           
        }
        else
        {
            UISetter.SetLabel(commanderFavorValue, commander.favorPoint + "/" + stepData.favor);
            UISetter.SetProgress(commanderFavorProgress, (float)commander.favorPoint / (float)stepData.favor);            
        }

        UISetter.SetLabel(commanderGift, localUser.commanderGift + "/" + 5);   // 사령관의 선물 최대 보유 수

        UISetter.SetActive(addGiftCount, giftCount > 0);
        UISetter.SetLabel(addGiftCount, "+" + giftCount);

        if (preCommanderId != commander.id)
        {
            favorStepListView.scrollView.ResetPosition();
        }
    }

    /// <summary>
    /// 호감도가 최대 단계인지 확인한다.
    /// 결혼 가능한 지휘관은 결혼 전까지 13단계가 최대이고, 그 외에는 다음 단계 데이터가 없으면 최대다.
    /// </summary>
    private bool IsMaxFavorStep()
    {
        if (commander.possibleMarry && commander.marry != 1)
            return commander.favorStep >= 13;
        return commander.GetFavorData(commander.favorStep + 1) == null;
    }

    /// <summary>시나리오 탭: 지휘관 스토리 목록을 정렬해 표시한다. 스토리가 없으면 잠금 화면을 보여준다.</summary>
    private void SetScenario()
    {
        scenarioList = RemoteObjectManager.instance.regulation.FindCommanderScenarioList(commander.id);

        if (scenarioList == null || scenarioList.Count <= 0)
        {
            this.SetScenarioBackGround(true);
            return;
        }

        // 필요 호감도(heart) 오름차순, 같으면 order 순
        scenarioList.Sort(delegate (CommanderScenarioDataRow row, CommanderScenarioDataRow row1)
        {
            if (row.heart != row1.heart)
                return row.heart.CompareTo(row1.heart);
            return row.order.CompareTo(row1.order);
        });

        InitScenarioList();
    }

    public void InitScenarioList()
    {
        this.SetScenarioBackGround(false);        
        scenarioTitleListView.Init(scenarioList, "STORY-");

        if (preCommanderId != commander.id)
        {
            scenarioTitleListView.scrollView.ResetPosition();
        }        
    }

    private void SetScenarioBackGround(bool isLock)
    {
        UISetter.SetActive(scenarioTitleListView, !isLock);
        UISetter.SetActive(contentsObject, !isLock);
        UISetter.SetActive(LeftLockDesc, isLock);
        UISetter.SetActive(RightLockDesc, isLock);

        Color bg_color = new Color();
        if (isLock)
            bg_color = new Color(193.0f / 255.0f, 193.0f / 255.0f, 193.0f / 255.0f);
        else
            bg_color = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);

        LeftScenarioBG.color = bg_color;
        RightScenarioBG.color = bg_color;
    }

    private void GiftHeartEffect()
    {
        GameObject heartEffect = Utility.LoadAndInstantiateGameObject("Prefabs/UI/FavorInfo/FavorCardHeart", card.transform);
        Destroy(heartEffect, 0.5f);
    }

    private void GiftProgressEffect()
    {
        UISetter.SetActive(giftProgressEffect, false);
        UISetter.SetActive(giftProgressEffect, true);

    }

    /// <summary>보이스 탭: 교감 대사와 전투 대사 목록을 구성한다. (전투 대사는 치명타/승리 치명타/패배 대사가 앞)</summary>
    private void SetVoice(bool isResetPosition = true)
    {

        List<InteractionDataRow> interactionList = regulation.FindVoiceInteractionData(commander.resourceId);
       
        List<CommanderVoiceDataRow> battleList = commander.reg.GetVoiceDataList();
        
        int lineCount = (int)Mathf.Ceil((float)interactionList.Count / 5);
        int offsetY = (int)voiceInteractionView.grid.cellHeight * lineCount;

        voiceBattleLine.transform.localPosition = new Vector3(voiceBattleLine.transform.localPosition.x,
                                                               voiceInteractionView.transform.localPosition.y - offsetY - 70,
                                                               voiceBattleLine.transform.localPosition.z);

        voiceBattleView.transform.localPosition = new Vector3(voiceBattleView.transform.localPosition.x,
                                                                  voiceBattleLine.transform.localPosition.y - 95,
                                                                   voiceBattleView.transform.localPosition.z);

        
        interactionList.Sort(delegate (InteractionDataRow row, InteractionDataRow row1)
        {
            return row.favorup.CompareTo(row1.favorup);    
        });

        battleList.Sort(delegate (CommanderVoiceDataRow row, CommanderVoiceDataRow row1)
        {
            int a_state = 0;
            int b_state = 0;

            if (row.type == ECommanderVoiceEventType.Fatal || row.type == ECommanderVoiceEventType.WinFatal || row.type == ECommanderVoiceEventType.Lose) a_state = -1;
            if (row1.type == ECommanderVoiceEventType.Fatal || row1.type == ECommanderVoiceEventType.WinFatal || row1.type == ECommanderVoiceEventType.Lose) b_state = -1;

            if (a_state == b_state) return row.type.CompareTo(row1.type);
            else return a_state.CompareTo(b_state);

        });

        

        voiceInteractionView.InitVoiceInteraction(interactionList, commander.favorRewardStep, commander.marry, "VoiceInteraction_");
        voiceBattleView.InitVoiceBattle(battleList, "VoiceBattle_");
        
        voiceInteractionView.scrollView.ResetPosition();
        voiceBattleView.scrollView.ResetPosition();
        
    }

    private void PlayVoice(int idx, bool bBattle = false)
    {
        if(bBattle)
        {           
            uiCommanderLeft.spine.skeletonAnimation.GetComponent<UIInteraction>().VoiceBattle(commander.reg.GetVoiceDataRow((ECommanderVoiceEventType)idx));
        }
        else
        {
             uiCommanderLeft.spine.skeletonAnimation.GetComponent<UIInteraction>().VoiceInteraction(idx);
        }
       
    }

    public void InfoClose()
    {       
        UISetter.SetActive(info, false);                
    }

    /// <summary>유닛 아이콘 영역을 드래그하면 3D 유닛 모델을 회전시킨다.</summary>
    private void OnDrag(GameObject go, Vector2 delta)
    {
        if (go.name != "UnitIcon")
            return;

        if (unitObj != null)
            unitObj.transform.localRotation = Quaternion.Euler(0f, -delta.x, 0f) * unitObj.transform.localRotation;
    }


    protected override void OnDisable()
    {
        base.OnDisable();
        UICamera.onDrag -= OnDrag;

    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UICamera.onDrag += OnDrag;

    }

    protected override void Awake()
    {
        skillPointReChargeTime = TimeData.Create();
        unitScaleSlider.value = 0.7f;
        base.Awake();
    }

    private void SetTrainingTicketList()
    {
        ticketListView.InitTicket(regulation.GetTrainingTicketList(), commander, "Ticket-");
    }


    string UnitBundleName;
    Vector3 unitOriginScale;
    /// <summary>
    /// 유닛 3D 모델을 캐시에서 생성해 상세 화면에 배치한다. (에셋번들 빌드에서는 번들을 먼저 받고, 이전 유닛 번들은 해제)
    /// 그림자 / 이펙트 / 무한궤도 같은 전투용 오브젝트는 끈다.
    /// </summary>
    IEnumerator CreateUnitFromCache()
    {
#if ASSETBUNDLE_DOWN
        if (!string.IsNullOrEmpty(UnitBundleName) && UnitBundleName != commander.unitReg.prefabId + ".assetbundle") AssetBundleManager.DeleteAssetBundle(UnitBundleName);
        if (!AssetBundleManager.HasAssetBundle(commander.unitReg.prefabId + ".assetbundle")) yield return StartCoroutine(PatchManager.Instance.LoadPrefabAssetBundle(commander.unitReg.prefabId + ".assetbundle"));
#endif
        if (unitObj != null) DestroyImmediate(unitObj);
        unitObj = CacheManager.instance.UnitCache.Create(commander.unitReg.prefabId, unitRoot.transform).gameObject;
        Utility.SetLayer(unitObj, unitRoot.layer);
        unitOriginScale = unitObj.transform.localScale;

        unitObj.transform.localPosition = new Vector3(0, 0, 100);
        unitObj.transform.localRotation = Quaternion.Euler(0, -130, 0);
        unitObj.transform.localScale = new Vector3((40 + (unitScaleSlider.value * 80)) * unitOriginScale.x, (40 + (unitScaleSlider.value * 80)) * unitOriginScale.y, (40 + (unitScaleSlider.value * 80)) * unitOriginScale.z);

        Transform shadow = unitObj.transform.Find("unit_shadow");
        if (shadow != null)
        {
            shadow.gameObject.SetActive(false);
        }

        Transform effects = unitObj.transform.Find("effects");
        if (effects != null)
        {
            effects.gameObject.SetActive(false);
        }

        Transform caterpillar = unitObj.transform.Find("caterpillar");
        if (caterpillar != null)
        {
            caterpillar.gameObject.SetActive(false);
        }

        if (unitObj != null) UnitBundleName = (commander.unitReg.prefabId).ToLower() + ".assetbundle";
        else UnitBundleName = string.Empty;

        yield return null;
    }

    /// <summary>훈련 티켓 수, 지휘관/유닛 정보, 클래스업 비용과 재료를 갱신한다.</summary>
    private void SetTicketInfo()
    {
        UISetter.SetLabel(ticket, localUser.resourceList[ticketType.ToString()]);
        UIManager.instance.world.mainCommand.OnRefresh();
        uiCommanderUpgrade.Set(commander);
        uiCommanderRight.Set(commander);

        if (type == CommanderDetailType.Recruit) uiDontHaveCommander.Set(commander);

        RoUnit unit = RoUnit.Create(commander.unitId, commander.level, 1, commander.cls, commander.currentCostume, commander.id, commander.favorRewardStep, commander.marry, commander.transcendence);
        uiUnit.Set(unit);

        CommanderClassDataRow classdata = RemoteObjectManager.instance.regulation.commanderClassDtbl.Find(list => list.index == int.Parse(commander.id) && list.cls == unit.cls);
        UISetter.SetLabel(classUpGold, classdata != null ? classdata.gold.ToString() : Localization.Get("1309"));

        commanderClassMaterialView.InitMaterial(commander.GetClassUpMaterial(), "Material-");
        UISetter.SetButtonGray(classUpBtn, commander.IsClassUp());
        UISetter.SetActive(classUpBtn, classdata != null);
        UISetter.SetActive(maxClass, classdata == null);

        if (classdata != null) UISetter.SetLabel(classUpLimit, Localization.Format("8038", classdata.level));
        UISetter.SetActive(classUpLimit, classdata != null && classdata.level > commander.level);
    }

    /// <summary>지휘관 ID와 진입 모드로 상세 정보를 설정한다.</summary>
    public void Set(string _id, CommanderDetailType _type)
    {
        ticketType = ETrainingTicketType.ctt1;
        InitTab(_type);

        SetCommanderDetailInfo(_id, _type);

        uiCommanderLeft.SetCommanderData(commander);
    }

    private void InitTab(CommanderDetailType detailType)
    {
        type = detailType;

        UISetter.SetFlipSwitch(cardTab, true);
        
        SetTab(false, true, false, false, false, false);

        UISetter.SetActive(commanderTab, type == CommanderDetailType.Training);
        UISetter.SetActive(favorTab, type == CommanderDetailType.Training);
        UISetter.SetActive(scenarioTab, type == CommanderDetailType.Training);
        UISetter.SetActive(voiceTab, type == CommanderDetailType.Training);
        unitTab.transform.parent.GetComponent<UIGrid>().enabled = true;
    }


    /// <summary>
    /// 상세 화면 데이터 전체를 구성한다.
    /// 미보유(Recruit)면 기획 기본값(NO_GET_COMMANDER_*)으로 가상의 지휘관을 만들어 보여준다.
    /// </summary>
    private void SetCommanderDetailInfo(string _id, CommanderDetailType _type)
    {
        ResetUIActive();
        trainingPress = false;
        ticketCnt = 0;
        type = _type;

        UISetter.SetActive(dontHaveInfo, type == CommanderDetailType.Recruit);


        UISetter.SetActive(toggle_item, type == CommanderDetailType.Training && !toggle_unit.activeSelf);

        if (commander != null) commander.DeleteCurrLevelUnitReg();

        if (type == CommanderDetailType.Training)
        {
            commander = localUser.FindCommander(_id);
        }
        else
        {            
           
            commander = RoCommander.Create(_id, 1, localUser.FindCommander(_id).rank , 1, 0, 0, 0, new List<int>());
            //Define값으로 셋팅

            UISetter.SetLabel(dontHaveMedal, localUser.FindCommander(_id).medal + "/" + commander.maxMedal);
            UISetter.SetProgress(dontHaveMedalProgress, (float)localUser.FindCommander(_id).medal / (float)commander.maxMedal);

            commander.rank =  int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_STAR"].value);
            commander.cls = int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_GRADE"].value);
            commander.level = int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_LEVEL"].value);
            
            commander.SetSkillLevel(1, int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_SKILL01"].value));
            commander.SetSkillLevel(2, int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_SKILL02"].value));
            commander.SetSkillLevel(3, int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_SKILL03"].value));
            commander.SetSkillLevel(4, int.Parse(regulation.defineDtbl["NO_GET_COMMANDER_SKILL04"].value));

            
        }

        ECommanderState state = commander.state;

        UISetter.SetActive(haveSkillInfo, state == ECommanderState.Nomal && type != CommanderDetailType.Recruit);
        UISetter.SetActive(dontHaveSkillInfo, state != ECommanderState.Nomal || type == CommanderDetailType.Recruit);

        CommanderRankDataRow rankRow = regulation.FindCommanderRankData(commander.rank + 1);
        int addMedal = commander.medal;

        UISetter.SetLabel(trainingGradeGold, rankRow != null ? rankRow.gold.ToString() : Localization.Get("1309"));
        UISetter.SetLabel(recruitGradeGold, rankRow != null ? rankRow.gold.ToString() : Localization.Get("1309"));

        UISetter.SetLabel(transcendenceLevel, "Lv " + commander.CurrentTranscendenceStep());

        UISetter.SetLabel(commonMedal, localUser.medal);

        var ticketRow = regulation.commanderTrainingTicketDtbl[ticketType.ToString()];
        UISetter.SetLabel(trainingGold, ticketRow.gold);

        UISetter.SetButtonGray(rankUpBtn, rankRow != null && (commander.maxMedal <= addMedal && rankRow.gold <= localUser.gold));
        UISetter.SetActive(rankUpBtn, rankRow != null);
        UISetter.SetActive(transcendenceBtn, rankRow == null);
        UISetter.SetActive(badgeCommander, (rankRow != null && (commander.maxMedal <= addMedal)) || (rankRow == null && commander.IsTranscendenceSkillUp()));
        UISetter.SetActive(badgeRankUp, rankRow != null && (commander.maxMedal <= addMedal));
        UISetter.SetActive(maxRank, false);
        UISetter.SetActive(badgeTranscendenceSkillUp, rankRow == null && commander.IsTranscendenceSkillUp());

        UISetter.SetActive(badgeUnit, commander.IsClassUp(false));
        UISetter.SetActive(badgeClassUp, commander.IsClassUp(false));        

        UISetter.SetButtonGray(exchangeBtn, localUser.medal > 0);
        UISetter.SetActive(exchangeBtn, true);


        UISetter.SetActive(commanderMedalInfo, type != CommanderDetailType.Recruit);
       

        if (type != CommanderDetailType.Recruit)
        {
            UISetter.SetActive(recruit, state != ECommanderState.Nomal);
            UISetter.SetActive(training, state == ECommanderState.Nomal);
        }
        

        SetTicketInfo();
        SetTrainingTicket();
        SetTrainingTicketList();
        InitSkillData();
        SetArrowBtn();

        //게임중 획득한 사령관 기본코스튬 셋팅
        if (commander.haveCostumeList == null)
        {
            commander.SetBaseCostume();
        }

        SetCostume(commander.currentCostume.ToString());

        if (preCommanderId != commander.id)
        {
            preCostume = commander.currentCostume.ToString();
        }

        SetFavor();
        this.SetScenario();
        this.SetItemStatus();
        this.SetEquipItemSlot();

        if (toggle_item.activeSelf)
            SetItemEquipButton();


        SetVoice();
        SetWeapon();
        
        UISetter.SetActive(marryComplete, commander.marry == 1);
        marryComplete.GetComponentInChildren<UIPanel>().depth = 50;
        UISetter.SetActive(ringBoxIcon, commander.possibleMarry && commander.marry == 0 && commander.favorRewardStep >= 13);

        preCommanderId = commander.id;
    }

    IEnumerator SetCommanderDetailSpineUnit()
    {


        yield return StartCoroutine(uiCommanderLeft.SetCommander(commander));

        UISetter.SetActive(uiCommanderLeft.spine, true);

        uiCommanderLeft.spine.target.GetComponent<UIInteraction>().marry = commander.marry;

        yield return StartCoroutine(CreateUnitFromCache());
    }

    IEnumerator SetCommanderDetail(string _id, CommanderDetailType _type)
    {

        SetCommanderDetailInfo(_id, _type);

        StartCoroutine(SetCommanderDetailSpineUnit());

        yield break;
    }

    public void ResetUIActive()
    {
        isZoom = false;
               
        UISetter.SetActive(recruit, false);
        UISetter.SetActive(training, false);

        UISetter.SetActive(badgeCommander, false);
        UISetter.SetActive(badgeUnit, false);
        UISetter.SetActive(badgeRankUp, false);
        UISetter.SetActive(badgeClassUp, false);

    }

    public void InitAndOpenCard()
    {

        _SetPage(true, false, false, false);

    }


    private void _SetPage(bool cardState, bool infoState, bool scenarioState, bool favorState)
    {
        UISetter.SetActive(card, cardState);
        UISetter.SetActive(info, infoState);

        UISetter.SetActive(commanderLevelUp, false);
    }

    /// <summary>탭 버튼과 탭 내용을 전환한다. (유닛 탭 진입 시 서버에 자원 충전 상태를 요청)</summary>
    private void SetTab(bool comm, bool unit, bool favor, bool scenario, bool costume, bool voice)
    {
        UISetter.SetFlipSwitch(commanderTab, comm);
        UISetter.SetFlipSwitch(unitTab, unit);
        UISetter.SetFlipSwitch(favorTab, favor);
        UISetter.SetFlipSwitch(scenarioTab, scenario);
        UISetter.SetFlipSwitch(costumeTab, costume);
        UISetter.SetFlipSwitch(voiceTab, voice);

        UISetter.SetActive(CommanderInfo, comm);
        UISetter.SetActive(UnitInfo, unit);
        UISetter.SetActive(UnitStat, unit);
        UISetter.SetActive(weaponRoot, false);
        UISetter.SetActive(FavorInfo, favor);
        UISetter.SetActive(ScenarioInfo, scenario);
        UISetter.SetActive(CostumeInfo, costume);
        UISetter.SetActive(VoiceInfo, voice);

        badgeCommander.transform.localPosition = comm ? new Vector2(badgeCommander.transform.localPosition.x, 40) : new Vector2(badgeCommander.transform.localPosition.x, 26);
        badgeUnit.transform.localPosition = unit ? new Vector2(badgeUnit.transform.localPosition.x, 40) : new Vector2(badgeUnit.transform.localPosition.x, 26);

        if (type != CommanderDetailType.Recruit && unit)
            network.RequestBulletCharge();
    }

    /// <summary>코스튬 탭에서 바꾼 코스튬을 다른 탭으로 이동할 때 서버에 저장한다.</summary>
    private void SaveChangeCostume()
    {
        //RefreshOpenedUI 에서 preCostume 이 commander.currentCostume으로 변경되기 때문에 코스튬 탭을 벗어날때도 저장이 필요
        if (type == CommanderDetailType.Training)
        {
            if (commander != null && preCommanderId == commander.id && preCostume != commander.currentCostume.ToString())
            {
                network.RequestChangeCommanderCostume(int.Parse(commander.id), commander.currentCostume);

                preCostume = commander.currentCostume.ToString();
            }
        }
    }

    public void InitSkillData()
    {
        List<SkillDataRow> _currentSkillList = new List<SkillDataRow>();

        List<string> skillIdList = commander.GetSkillIdList();

        for (int i = 0; i < skillIdList.Count; i++)
        {
            string skillId = skillIdList[i];
            if (!string.Equals(skillId, "0"))
            {
                SkillDataRow row = regulation.skillDtbl[skillId];
                _currentSkillList.Add(row);
            }
        }

        if (type == CommanderDetailType.Training)
        {
            skillListView.InitCommanderSkillList(_currentSkillList, commander, "Skill-");
            skillListView.ResetPosition();
        }
        else
        {
            dontHaveSkillListView.InitCommanderSkillList(_currentSkillList, commander, "Skill-");
        }
    }

    void SetTrainingTicket()
    {
        int type = (int)ticketType;
        var goods = regulation.goodsDtbl[type.ToString()];
        UISetter.SetSprite(trainingIcon, goods.iconId);
        UISetter.SetLabel(ticket, localUser.resourceList[ticketType.ToString()]);
        UISetter.SetButtonEnable(trainingBtn, commander.TrainingEnable(ticketType));
    }

    private void SetSkillInfo(string id)
    {
        UISkillInfoPopup skillInfoPopup = UISimplePopup.Create<UISkillInfoPopup>("SkillInfoPopup");
        skillInfoPopup.Set(true, "10056", null, null, null, Localization.Get("10048"), null);
        skillInfoPopup.SetInfo(commander, id);
    }

    public void GetCommanderStage()
    {
        uiWorld.worldMap.InitAndOpenWorldMap("1", "1");
    }

    public void UseTrainingTicket(GameObject sender)
    {
        string key = sender.name;
        string ticket = key.Substring("Ticket-".Length);
        ticketType = (ETrainingTicketType)Enum.Parse(typeof(ETrainingTicketType), ticket);

        if (localUser.resourceList[ticketType.ToString()] < 1)
            return;

        network.RequestCommanderLevelUp(commander.id, 1, ticketType.ToString());
    }

    public void TrainingResultNetwork()
    {
        if (ticketCnt == 0)
            return;

        network.RequestCommanderLevelUp(commander.id, ticketCnt, ticketType.ToString());
        ticketCnt = 0;
        UISetter.SetActive(useTicket, ticketCnt > 0);
    }

    /// <summary>
    /// 훈련 버튼을 누르고 있는 동안 티켓을 연속으로 사용한다.
    /// 경험치는 클라이언트에서 먼저 반영해 바로 보여주고, 손을 떼면 사용한 티켓 수를 한 번에 서버로 요청한다.
    /// </summary>
    public void OnPressTrainingBtn(GameObject sender)
    {
        string key = sender.name;
        string ticket = key.Substring("Ticket-".Length);
        ticketType = (ETrainingTicketType)Enum.Parse(typeof(ETrainingTicketType), ticket);

        if (localUser.resourceList[ticketType.ToString()] < 1)  //티켓 없음
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("8031"));
            return;
        }
        trainingPress = true;
        StartCoroutine("CommanderTraining");
    }

    public void OnReleaseTrainingBtn()
    {
		ticketWeight = 0;
        trainingPress = false;
        StopCoroutine("CommanderTraining");
        if (ticketCnt == 0)
        {
            TrainingResult();

            if (commander.Training(ticketType) == CommanderTrainingResult.Fail) //사령관 레벨보다 높을 수 없음
            {
                if (localUser.resourceList[ticketType.ToString()] > 0)
                {
                    NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("8035"));
                }

            }
        }

        TrainingResultNetwork();

        UISetter.SetActive(useTicket, false);
    }

    public void TrainingResult()
    {
		CommanderTrainingResult state = commander.Training(ticketType, 1 + ticketWeight);
        if (state == CommanderTrainingResult.Fail)
            return;
        else if (state == CommanderTrainingResult.LevelUp)
		{
            OpenCommanderLevelUp();
		}

		ticketCnt += (1 + ticketWeight);

        UISetter.SetActive(useTicket, ticketCnt > 0);
        UISetter.SetLabel(useTicket, "x " + ticketCnt);

        StartTrainingAnimation();
        SetTicketInfo();
        SetTrainingTicketList();
    }

    /// <summary>
    /// 누르고 있는 시간이 길수록 한 번에 사용하는 티켓 수(ticketWeight)를 늘린다.
    /// 보유 티켓 수와 최대 훈련 가능 횟수를 넘지 않도록 보정한다.
    /// </summary>
    private IEnumerator CommanderTraining()
    {
        float speed = 0.05f;
		float time = 0;
		ticketWeight = 0;

		int trainingMaximumCnt = commander.GetTrainingMaximumCount (ticketType);
		        
        while (trainingPress)
        {	
			if (trainingMaximumCnt < 1)
				yield break;

			if (localUser.resourceList [ticketType.ToString ()] < 1) {
				yield break;		
			}

			time += Time.deltaTime;
			ticketWeight += (int)time;

			int tmp = (ticketWeight + 1) - localUser.resourceList [ticketType.ToString ()];
			if (tmp > 0) {
				ticketWeight -= tmp;
				if (ticketWeight < 0)
					ticketWeight = 0;
			}

			tmp = (ticketCnt + ticketWeight + 1) - trainingMaximumCnt;
			if (tmp > 0) {
				ticketWeight -= tmp;
				if (ticketWeight < 0)
					ticketWeight = 0;
			}				

			TrainingResult ();

            yield return new WaitForSeconds(speed);

            if (speed > 0.15f)
                speed -= 0.05f;

        }

        yield return true;
    }

    private void StartTrainingAnimation()
    {
        GameObject icon = GameObject.Instantiate(trainingIcon.gameObject);
        icon.transform.parent = trainingIcon.transform.parent;
        icon.transform.localScale = trainingIcon.transform.localScale;
        icon.transform.localPosition = trainingIcon.transform.localPosition;

        SoundManager.PlaySFX("SE_Training_001");

        iTween.MoveTo(icon, iTween.Hash("y", icon.transform.localPosition.y + 20,
                                        "islocal", true,
                                        "time", 0.7f,
                                        "delay", 0,
                                        "easeType", iTween.EaseType.linear,
                                        "oncomplete", "EndTrainingAnimation",
                                        "oncompletetarget", this.gameObject,
                                        "oncompleteparams", icon));
        icon.GetComponent<TweenAlpha>().PlayForward();

        UISetter.SetActive(trainingEffect, false);
        UISetter.SetActive(trainingEffect, true);
    }

    public void EndTrainingAnimation(GameObject obj)
    {
        DestroyImmediate(obj);
    }

    public void OnPromotionBtnClicked()
    {
        CommanderRankDataRow rankRow = regulation.FindCommanderRankData(commander.rank + 1);
        int addMedal = commander.medal;

        if (commander.maxMedal > addMedal)  //메달부족
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("8030"));
        }
        else if (rankRow.gold > localUser.gold) //골드부족
        {
            UISimplePopup.CreateBool(true, "1029", "1030", null, "1001", "1000").onClick = sender =>
            {
                string keyName = sender.name;
                if (keyName == "OK")
                {
                    uiWorld.camp.GoNavigation("MetroBank");
                }
            };
        }
        else
        {
            RemoteObjectManager.instance.RequestCommanderRankUp(commander.id);
        }
    }

    /// <summary>클래스업 가능 여부 코드(0: 가능, 2: 재료 부족, 100~998: 필요 레벨 + 100, 999: 골드 부족)에 따라 처리한다.</summary>
    public void OnClassUpBtnClicked()
    {
        int type = commander.ImPossibleClassUp();

        //재료부족
        if (type == 2)
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("8032"));
        }
        else if (type >= 100 && type < 999) //지휘관 레벨부족
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), String.Format(Localization.Get("8033"), type - 100));
        }
        else if (type == 999) //돈 부족
        {
            UISimplePopup.CreateBool(true, "1029", "1030", null, "1001", "1000").onClick = sender =>
            {
                string keyName = sender.name;
                if (keyName == "OK")
                {
                    uiWorld.camp.GoNavigation("MetroBank");
                }
            };
        }
        else if (type == 0)
        {
            if (!UIManager.instance.world.unitUpgradeComplete.isActive)
            {
                RemoteObjectManager.instance.RequestCommanderClassUp(commander.id);
            }
        }
    }

    public void OnTutorialClassUpBtnClicked()
    {
        RemoteObjectManager.instance.RequestTutorialCommanderClassUp(commander.id);
    }

    public override void OnRefresh()
    {
        base.OnRefresh();

        StartCoroutine(SetCommanderDetail(commander.id, type));

        if (weaponUpgradePopup != null)
            weaponUpgradePopup.OnRefresh();
    }

    public void OpenCommanderLevelUp()
    {
        if (commanderLevelUp != null && !commanderLevelUp.isActive)
        {
            SoundManager.PlaySFX("SE_PilotLevelUp_001");
        }

        UISetter.SetActive(commanderLevelUp, true);
        commanderLevelUp.Set(commander);
        StartCoroutine(commanderLevelUp.StartRemoveCount());
    }

    public void OpenCommanderStatsUp(StatType type, int stat)
    {

        if (commanderLevelUp != null && !commanderLevelUp.isActive)
        {
            SoundManager.PlaySFX("SE_PilotLevelUp_001");
        }

        UISetter.SetActive(commanderLevelUp, true);
        commanderLevelUp.Set(commander.resourceId, type, stat);
        StartCoroutine(commanderLevelUp.StartRemoveCount());
    }

    public void CommanderDelay()
    {
        RemoteObjectManager.instance.
            RequestRecruitCommanderDelay(commander.id);
    }

    public void CommanderDelayCancel()
    {
        RemoteObjectManager.instance.
            RequestCommanderDelayCancle(commander.id);
    }

    private void SkillPointRechargePoint()
    {
        VipRechargeDataRow row = RemoteObjectManager.instance.regulation.FindVipRechargeData(EVipRechargeType.Skill);

        if (!localUser.resourceRechargeList.ContainsKey(((int)EVipRechargeType.Skill).ToString()))  //resourceRechargeList에 없다면 충전횟수가 0 
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Skill);
        }
        else if (localUser.resourceRechargeList[((int)EVipRechargeType.Skill).ToString()] < row.GetMaxRechargeCount(localUser.vipLevel)) // resourceRechargeList에 있으면 충전가능횟수 비교
        {
            uiWorld.mainCommand.OpenVipRechargePopUp(EVipRechargeType.Skill);
        }
        else
        {
            // VIP 레벨에 따른 충전 가능 횟수를 모두 사용

            UISimplePopup.CreateBool(true, "1303", "12006", null, "5348", "1000").onClick = sender =>
            {
                string key = sender.name;
                if (key == "OK")
                {
                    uiWorld.mainCommand.OpenDiamondShop();
                }
            };
        }
    }

    /// <summary>좌우 이동용 목록에서 현재 지휘관의 인덱스를 찾는다.</summary>
    private int GetCurrentCommanderIndex()
    {
        for (int i = 0; i < commanderList.Count; i++)
        {
            if (commanderList[i].id == commander.id)
                return i;
        }
        return 0;
    }

    /// <summary>육성 모드에서 코스튬이 바뀌었으면 서버에 저장을 요청한다.</summary>
    private void RequestCostumeChangeIfNeeded()
    {
        if (type == CommanderDetailType.Training && preCostume != commander.currentCostume.ToString())
            RemoteObjectManager.instance.RequestChangeCommanderCostume(int.Parse(commander.id), commander.currentCostume);
    }

    /// <summary>다음 지휘관으로 이동한다. 이동 전에 바뀐 코스튬을 저장한다.</summary>
    public void NextCommander()
    {
        if (trainingPress || commanderList == null) return;

        int idx = GetCurrentCommanderIndex();
        if (idx + 1 < commanderList.Count)
        {
            RequestCostumeChangeIfNeeded();
            StartCoroutine(SetCommanderDetail(commanderList[idx + 1].id, type));
        }
    }

    /// <summary>이전 지휘관으로 이동한다. 이동 전에 바뀐 코스튬을 저장한다.</summary>
    public void PreCommander()
    {
        if (trainingPress || commanderList == null) return;

        int idx = GetCurrentCommanderIndex();
        if (idx > 0)
        {
            RequestCostumeChangeIfNeeded();
            StartCoroutine(SetCommanderDetail(commanderList[idx - 1].id, type));
        }
    }

    /// <summary>목록의 처음/끝 여부에 따라 좌우 화살표를 표시한다.</summary>
    private void SetArrowBtn()
    {
        if (commanderList == null)
        {
            UISetter.SetActive(nextBtn, false);
            UISetter.SetActive(preBtn, false);
            return;
        }

        int idx = GetCurrentCommanderIndex();
        UISetter.SetActive(nextBtn, idx + 1 < commanderList.Count);
        UISetter.SetActive(preBtn, idx > 0);
    }

    private int giftCount = 0;
    private bool pressGiftBtn = false;
    /// <summary>
    /// 선물 버튼을 누르고 있는 동안 선물을 연속으로 준다. (훈련과 같은 방식)
    /// 호감도는 클라이언트에서 먼저 올려 보여주고, 손을 떼면 준 개수만큼 한 번에 서버로 요청한다.
    /// </summary>
    public void OnPressGiftBtn(GameObject sender)
    {
        string key = sender.name;
        string giftIdx = key.Substring("Gift_".Length);

        giftCount = 0;

        GoodsDataRow goodsData = RemoteObjectManager.instance.regulation.FindGoodsServerFieldName(giftIdx);

        if (localUser.resourceList[goodsData.serverFieldName] < 1)  // 네비게이션 팝업
        {
            SetNavigationOpen(EStorageType.Food, giftIdx);
        }
        else if (bMaxFavorStep) //Max
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("20032"));
        }
        else
        {
            if (!localUser.sendingGift)
            {
                pressGiftBtn = true;
                StartCoroutine("FoodGift", goodsData);
            }
        }


    }

    public void OnReleaseGiftBtn(GameObject sender)
    {
        string key = sender.name;
        string giftIdx = key.Substring("Gift_".Length);

        pressGiftBtn = false;         
        StopCoroutine("FoodGift");

        List<FavorDataRow> list = commander.GetFavorList();
        favorStepListView.Init(list, commander.id, commander.favorStep, commander.favorRewardStep, "FavorStep_");

        if (giftCount > 0)
        {
            bFavorStepUp = true;
            localUser.sendingGift = true;
            network.RequestGiftFood(commander.id, giftIdx, giftCount);
            uiCommanderLeft.spine.skeletonAnimation.GetComponent<UIInteraction>().GiftInteraction(InteractionType.GIFT);
            GiftHeartEffect();
        }

        giftCount = 0;

        UISetter.SetActive(addGiftCount, false);
    }

    private IEnumerator FoodGift(GoodsDataRow goodsData)
    {
        float speed = 0.05f;
        while (pressGiftBtn)
        {
            if (localUser.resourceList[goodsData.serverFieldName] < 1)
            {
                yield return true;
            }
            else
            {
                FoodGiftResult(goodsData);
            }
            yield return new WaitForSeconds(speed);

            if (speed > 0.15f)
                speed -= 0.05f;

        }

        yield return true;
    }

    /// <summary>선물 1개의 효과를 클라이언트에 먼저 반영한다. 호감도가 넘치면 여러 단계를 한 번에 올린다.</summary>
    private void FoodGiftResult(GoodsDataRow goodsData)
    {
        FavorStepDataRow stepData = RemoteObjectManager.instance.regulation.FindFavorStepData(commander.favorStep + 1);
        CommanderGiftDataRow giftData = regulation.GetCommanderGift(goodsData.type);
        int addPoint = 0;
        
        bool bMax = IsMaxFavorStep();
        
        if (!bMax) //Max가 아닐때
        {
            giftCount++;
            localUser.resourceList[goodsData.serverFieldName]--;
            addPoint = giftData.favorPoint;
            if (commander.favorPoint + addPoint >= stepData.favor)
            {
                while (commander.favorPoint + addPoint >= stepData.favor)
                {
                    commander.favorStep++;
                    commander.favorPoint = (commander.favorPoint + addPoint) - stepData.favor;
                    addPoint = 0;

                    stepData = RemoteObjectManager.instance.regulation.FindFavorStepData(commander.favorStep + 1);

                    bFavorStepUp = true;

                    if (stepData == null) break;
                }
            }
            else
            {
                commander.favorPoint += addPoint;
            }

            SoundManager.PlaySFX("SE_Training_001");

            GiftProgressEffect();

            SetFavor();
        }
    }

    public void OnSlideValueChange()
    {

        float value = unitScaleSlider.value;

        if (unitObj != null) unitObj.transform.localScale = new Vector3((40 + (value * 80)) * unitOriginScale.x, (40 + (value * 80)) * unitOriginScale.y, (40 + (value * 80)) * unitOriginScale.z);
    }

    public void OpenPopupShow()
    {
        base.Open();
        this.OnAnimOpen();
    }

    /// <summary>닫기 버튼: 퇴장 연출 후 닫고, 바뀐 코스튬을 저장한다. (연출 중 중복 입력 방지)</summary>
    public void ClosePopup()
    {
        if (bBackKeyEnable == false)
        {
            bBackKeyEnable = true;
            this.HidePopup();

            RequestCostumeChangeIfNeeded();
            preCommanderId = "";
        }
    }

    public override void Open()
    {
        base.Open();
        this.OnAnimOpen();
    }

    public override void Close()
    {
        base.Close();
        bBackKeyEnable = false;
        bEnterKeyEnable = false;
        if (naviPopUp != null)
            naviPopUp.Close();
        
    }

    void HidePopup()
    {
        OnAnimClose();

        StartCoroutine(OnEventHidePopup());
    }

    void OnAnimOpen()
    {
        AnimLeft.Reset();
        AnimRight.Reset();
        AnimBlock.Reset();

        this.AnimLeft.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
        this.AnimRight.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
        this.AnimBlock.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);

    }

    IEnumerator OnEventHidePopup()
    {
        yield return new WaitForSeconds(0.8f);

        base.Close();

        bBackKeyEnable = false;
        bEnterKeyEnable = false;

        UISetter.SetActive(unitObj, false);
    }

    void OnAnimClose()
    {
        this.AnimLeft.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
        this.AnimRight.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
        this.AnimBlock.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);

    }

    /// <summary>카드 확대 상태에서 뒤로가기(Escape) 키를 누르면 축소한다.</summary>
    void Update()
    {
        if (isZoom == true)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CardScaleDownSize();
            }
        }
     
    }

    #region Weapon

    List<int> lockWeaponSlot = new List<int>();

    void OpenWeaponUI(bool open)
    {
        UISetter.SetActive(weaponRoot, open);
        UISetter.SetActive(UnitStat, !open);
        UnitIcon.transform.localPosition = weaponRoot.activeSelf ? new Vector3(-178, -95, 0) : new Vector3(-189, 50, 0);
        if (open)
        {
            unitScaleSlider.value = 0.7f;
            OnSlideValueChange();
        }

        unitRoot.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
        weaponGrid.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 2;
        cardRoot.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 3;
        skillScrollView.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
        nextCommander.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 2;
    }

    /// <summary>무기 탭: 슬롯별 해금 클래스와 보유 무기에 따라 잠금/장착 상태를 표시한다.</summary>
    void SetWeapon()
    {
		#if WEAPON_LOCK
        UISetter.SetActive(weaponToggleBtn.gameObject, false);
        return;
		#endif

        UnitIcon.transform.localPosition = weaponRoot.activeSelf ? new Vector3(-178, -95, 0) : new Vector3(-189, 50, 0);
        if (lockWeaponSlot.Count == 0)
        {
            lockWeaponSlot.Add(int.Parse(regulation.defineDtbl["WEAPON_OPEN_CLASS_HEAD"].value));
            lockWeaponSlot.Add(int.Parse(regulation.defineDtbl["WEAPON_OPEN_CLASS_RIGHTHAND"].value));
            lockWeaponSlot.Add(int.Parse(regulation.defineDtbl["WEAPON_OPEN_CLASS_LEFTHAND"].value));
            lockWeaponSlot.Add(int.Parse(regulation.defineDtbl["WEAPON_OPEN_CLASS_BODY"].value));
            lockWeaponSlot.Add(int.Parse(regulation.defineDtbl["WEAPON_OPEN_CLASS_SPECIAL"].value));
        }

        weaponSkillDescription.Close();
        for (int i = 0; i < weaponList.Count; i++)
        {
            UICommanderWeaponItem item = weaponList[i];
            RoWeapon weapon = commander.FindWeaponItem(i + 1);
			List<RoWeapon> invenList = localUser.GetWeaponList (i + 1);
			if (lockWeaponSlot[i] > commander.cls || invenList.Count == 0)
				item.Lock(commander.unitId, i + 1);
            else
				item.Set(weapon, commander.unitId, i + 1);
        }
        SelectWeapon(selectWeaponSlot);
    }

    /// <summary>선택한 무기 슬롯의 무기 정보, 연결 스킬, 부가 효과, 세트 효과를 표시한다.</summary>
    void SelectWeapon(int selectSlot)
    {
        RoWeapon weapon = commander.FindWeaponItem(selectSlot);
		selectWeaponItem.Set(weapon, commander.unitId, selectSlot);
        UISetter.SetLabel(weaponTakeLabel, weapon != null ? Localization.Get("5030418") : Localization.Get("5030411"));

        for (int i = 0; i < weaponList.Count; i++)
        {
            UICommanderWeaponItem item = weaponList[i];
            item.SetSelection(i + 1 == selectSlot);
        }

		UISetter.SetActive (weaponUpgradeBtn, weapon != null);
		UISetter.SetActive (weaponTakeOffBtn, weapon != null);
		UISetter.SetActive(weaponSkillTitle, weapon != null);
        if (weapon == null)
        {
            EmptyWeapon();
            return;
        }

        WeaponSetInfo(weapon.data.weaponSetType);
        UISetter.SetActive(weaponEmpty, false);
        WeaponDataRow weaponData = weapon.data;
        UISetter.SetLabel(weaponName, Localization.Get(weapon.data.weaponName));

        UISetter.SetActive(weaponSkillInfoRoot, weaponData.skillPoint != EWeaponSkill.None);
        UISetter.SetActive(weaponSkillEmpty, weaponData.skillPoint == EWeaponSkill.None);

        if (weaponData.skillPoint != EWeaponSkill.None)
        {
            string skillId = commander.GetSkillId((int)weaponData.skillPoint);
                
			if (!string.Equals (skillId, "0")) 
			{
				int skillLevel = commander.skillList [(int)weaponData.skillPoint - 1];
				SkillDataRow row = regulation.skillDtbl [skillId];
				weaponSkill.SetSkill (row, skillLevel, 1, 1, true, 1);
				UISetter.SetGameObjectName (weaponSkill.gameObject, string.Format ("WeaponSkill-{0}", row.key));
			}
			else
			{
				UISetter.SetActive(weaponSkillInfoRoot, false);
				UISetter.SetActive(weaponSkillEmpty, true);
			}


        }
         
        bool effectEmpty = true;
        for(int i = 0; i < weaponData.statusEffectDrks.Count; i++)
        {
            if (weaponData.statusEffectDrks[i] != "0")
                effectEmpty = false;
            UISetter.SetActive(weaponSkillEffect[i], weaponData.statusEffectDrks[i] != "0");
        }
        if (weaponData.privateWeapon == 0)
            UISetter.SetLabel(weaponSkillTitle, Localization.Get("70004"));
        else
        {
            if (weaponData.skillPoint == EWeaponSkill.AddSkill)
                UISetter.SetLabel(weaponSkillTitle, Localization.Format("70005", (int)weaponData.skillPoint));
            else
                UISetter.SetLabel(weaponSkillTitle, Localization.Format("70943", (int)weaponData.skillPoint));
        }

        UISetter.SetActive(weaponSkillEffectEmpty, effectEmpty);
    }

    void EmptyWeapon()
    {
        UISetter.SetActive(weaponEmpty, true);
        UISetter.SetActive(weaponSkillInfoRoot, false);
        UISetter.SetActive(weaponSkillEmpty, true);
        WeaponSetInfo();
    }

    void OpenWeaponSkillEffectInfo(int idx)
    {
        RoWeapon weapon = commander.FindWeaponItem(selectWeaponSlot);

        if (weapon == null)
            return;

        string title = idx == 0 ? Localization.Get("70010") : Localization.Get("70011");
        string type = "";
        ESkillTargetType targetType;

        WeaponDataRow weaponData = weapon.data;
        string skillId = commander.GetSkillId((int)weaponData.skillPoint);
        SkillDataRow skillData = regulation.skillDtbl[skillId];
        if (skillData.targetType == ESkillTargetType.Own || skillData.targetType == ESkillTargetType.Friend)
            targetType = ESkillTargetType.Friend;
        else
            targetType = ESkillTargetType.Enemy;

        if (weaponData.targetType != targetType)
            type = weaponData.targetType == ESkillTargetType.Friend ? Localization.Get("70014") : Localization.Get("70013");

        weaponSkillDescription.SetLabel(title, type, Localization.Get(weaponData.explanation[idx]));
    }

    public void WeaponSkillDescriptionClose()
    {
        weaponSkillDescription.Close();
    }

    void OpenWeaponTakePopup()
    {
        if (weaponListPopup == null)
        {
            weaponListPopup = UIPopup.Create<UIWeaponListPopup>("WeaponListPopup");
            weaponListPopup.Set(selectWeaponSlot, 0, commander.id);
        }
    }
        
    void OpenWeaponUpgradePopup()
    {
        RoWeapon weapon = commander.FindWeaponItem(selectWeaponSlot);
        if (weaponUpgradePopup == null && weapon != null)
        {
            weaponUpgradePopup = UIPopup.Create<UIWeaponUpgradePopup>("WeaponUpgradePopup");
            weaponUpgradePopup.Set(weapon.idx);
        }
    }

    private void SetWeaponSkillInfo(string id)
    {
        UISkillInfoPopup skillInfoPopup = UISimplePopup.Create<UISkillInfoPopup>("SkillInfoPopup");
        skillInfoPopup.Set(true, "10056", null, null, null, Localization.Get("10048"), null);
        int level = commander.GetWeaponSkillLevel(selectWeaponSlot);
        skillInfoPopup.SetInfo(id, level);
    }

    /// <summary>무기 세트 효과를 표시한다. 5부위 세트를 모두 장착하면 활성 색상과 화살표로 강조한다.</summary>
    private void WeaponSetInfo(string type = "0")
    {
        UISetter.SetActive(weaponSetLabel, type != "0");
        UISetter.SetActive(weaponSetEffectLabel, type != "0");
        UISetter.SetActive(weaponSetEffectLeftArrow, type != "0");
        UISetter.SetActive(weaponSetEffectRightArrow, type != "0");

        if (type != "0")
        {
            WeaponSetDataRow setData = regulation.weaponSetDtbl[type];
            int setCnt = commander.EnableWeaponSet(type);

            string arrowName = setCnt == 5 ? "eq-arrow-on" : "eq-arrow-off";
            int arrowScale = setCnt == 5 ? 1 : -1;

            UISetter.SetSprite(weaponSetEffectLeftArrow, arrowName);
            UISetter.SetSprite(weaponSetEffectRightArrow, arrowName);

            UISetter.SetScale(weaponSetEffectLeftArrow, new Vector3(arrowScale, 1, 1));
            UISetter.SetScale(weaponSetEffectRightArrow, new Vector3(arrowScale * -1, 1, 1));

            UISetter.SetColor(weaponSetEffectLabel, setCnt == 5 ? new Color(1, 240f/255f, 0) : new Color(115f/255f, 123f/255f, 131f/255f));
            weaponSetEffectLabel.effectColor = setCnt == 5 ? new Color(204f/255f, 5f/255f, 5f/255f) : Color.white;
            UISetter.SetLabel(weaponSetLabel, Localization.Format("70948", setCnt));

            string str_eff = setData.weaponSetStatAddPoint.ToString();
            if (setData.weaponSetStatType == EItemSetType.CRITDMG || setData.weaponSetStatType == EItemSetType.CRITR)
                str_eff = str_eff + "%";

            UISetter.SetLabel(weaponSetEffectLabel, string.Format("{0} +{1}", ItemSetTypeString(setData.weaponSetStatType), str_eff));
        }
    }

    private string ItemSetTypeString(EItemSetType setType)
    {
        switch (setType)
        {
            case EItemSetType.ATK:
                return Localization.Get("1");
            case EItemSetType.DEF:
                return Localization.Get("2");
            case EItemSetType.ACCUR:
                return Localization.Get("5");
            case EItemSetType.LUCK:
                return Localization.Get("3");
            case EItemSetType.CRITDMG:
                return Localization.Get("8");
            case EItemSetType.CRITR:
                return Localization.Get("6");
        }
        return "";
    }

    [System.Serializable]
    public class WeaponSkillDescription : UIInnerPartBase
    {
        public UILabel title;
        public UILabel type;
        public UILabel description;

        public void SetLabel(string title, string type, string description)
        {
            UISetter.SetActive(root, true);
            UISetter.SetLabel(this.title, title);
            UISetter.SetLabel(this.type, type);
            UISetter.SetLabel(this.description, description);
        }

        public void Close()
        {
            UISetter.SetActive(root, false);
        }
    }

    #endregion Weapon

    /// <summary>레벨업 / 능력치 상승 연출 파츠.</summary>
    [System.Serializable]
    public class CommanderLevelUp : UIInnerPartBase
    {
        public UISprite imageLabel;
        public UILabel attackStat;
        public UILabel healthStat;
        public UILabel defenseStat;
		public UILabel luckStat;
		public UILabel accStat;
        public UISprite commanderIcon;
		public UILabel attack;
        public UILabel health;
        public UILabel defense;
		public UILabel luck;
		public UILabel accur;
		

		public Animation animation;

		public void Set(RoCommander commander)
		{   
            RoCommander before = commander.CreateBeforeLevel();
            RoCommander origin = commander.CreateOriginCommander();
            int attackPoint = origin.currLevelUnitReg.attackDamage - before.currLevelUnitReg.attackDamage;
            int defensePoint = origin.currLevelUnitReg.defense - before.currLevelUnitReg.defense;
            int healthPoint = origin.currLevelUnitReg.maxHealth - before.currLevelUnitReg.maxHealth;
            int luckPoint = origin.currLevelUnitReg.luck - before.currLevelUnitReg.luck;
            int accPoint = origin.currLevelUnitReg.accuracy - before.currLevelUnitReg.accuracy;

            UISetter.SetLabel(attackStat, Localization.Get("1"));
            UISetter.SetLabel(healthStat, Localization.Get("4"));
            UISetter.SetLabel(defenseStat, Localization.Get("2"));
			UISetter.SetLabel(luckStat, Localization.Get("3"));
			UISetter.SetLabel(accStat, Localization.Get("5"));

            UISetter.SetSprite(imageLabel, "ma_img_levelup");
			UISetter.SetLabel(attack, string.Format("+{0}",attackPoint.ToString()));
			UISetter.SetLabel(defense, string.Format("+{0}",defensePoint.ToString()));
			UISetter.SetLabel(health, string.Format("+{0}",healthPoint.ToString()));
			UISetter.SetLabel(luck, string.Format("+{0}",luckPoint.ToString()));
			UISetter.SetLabel(accur, string.Format("+{0}",accPoint.ToString()));
		}

        public void Set(string resourceId, StatType statType, int stat)
        {
            UISetter.SetLabel(attackStat, "");
            UISetter.SetLabel(defenseStat, "");
			UISetter.SetLabel(luckStat, "");
			UISetter.SetLabel(accStat, "");
            UISetter.SetLabel(attack, "");
            UISetter.SetLabel(defense, "");
			UISetter.SetLabel(luck, "");
			UISetter.SetLabel(accur, "");
           
            string name = "";
            if (statType == StatType.ATK) name = Localization.Get("1");
            else if (statType == StatType.DEF) name = Localization.Get("2");
            else if (statType == StatType.HP) name = Localization.Get("4");
            else if (statType == StatType.ACCUR) name = Localization.Get("5");
            else if (statType == StatType.LUCK) name = Localization.Get("3");
            else if (statType == StatType.CRITR) name = Localization.Get("6");
            else if (statType == StatType.CRITDMG) name = Localization.Get("8");
            else if (statType == StatType.MOB) name = Localization.Get("7");
            UISetter.SetLabel(healthStat, name);

            UISetter.SetSprite(imageLabel, "ma_img_statsup", true);
            UISetter.SetLabel(health, statType == StatType.CRITDMG || statType == StatType.CRITR ? " + " + Localization.Format("5781", stat) : " + " + stat.ToString());

        }
        public IEnumerator StartRemoveCount()
        {
            animation.Play("CommanderLevelUp");
            while (animation.IsPlaying("CommanderLevelUp"))
            {
                yield return null;
            }
            UISetter.SetActive(root, false);
        }
    }

    // ── 카드 확대 보기 ──
    // 카드를 화면 중앙으로 옮겨 확대하고 90도 회전해 가로 화면에 꽉 차게 보여준다.

    public void OnCardScaleEvent()
    {
        if (bTouchEnable == false)
        {
            bScaleCard = !bScaleCard;

            if (bScaleCard == true)
            {
                CardScaleUpSize();
            }
            else
            {
                CardScaleDownSize();
            }
        }
    }

    public void CardScaleUpSize()
    {
        ForwardBackKeyEvent.DTouchLock();

        bTouchEnable = true;
        UISetter.SetActive(Tab, false);

        iTween.MoveTo(card.gameObject, iTween.Hash("position", new Vector3(0.0f, 0.0f, -100.0f),
                                              "islocal", true,
                                              "time", 0.2f));

        iTween.ScaleTo(card.gameObject, iTween.Hash("scale", new Vector3(device_aspectX, device_aspectY, 1),
                                               "islocal", true,
                                               "time", 0.2f));

        iTween.RotateTo(card.gameObject, iTween.Hash("z", 90,
                                                "islocal", true,
                                                "oncomplete", "OnCompScaleUp",
                                                "onCompleteTarget", gameObject,
                                                "time", 0.2f));
    }

    void OnCompScaleUp()
    {
        bTouchEnable = false;
        isZoom = true;
        btnZoomExit.SetActive(true);
    }

    void OnCompScaleDown()
    {

        bTouchEnable = false;

        btnZoomExit.SetActive(false);

        ForwardBackKeyEvent.DTouchUnLock();

        UISetter.SetActive(Tab, true);
    }


    public void CardScaleDownSize()
    {
        bTouchEnable = true;

        iTween.MoveTo(card.gameObject, iTween.Hash("position", new Vector3(-400, -30, 0.0f),
                                              "islocal", true,
                                              "time", 0.2f));

        iTween.ScaleTo(card.gameObject, iTween.Hash("scale", new Vector3(1, 1, 1),
                                               "islocal", true, "time", 0.2f));

        iTween.RotateTo(card.gameObject, iTween.Hash("z", 0, "islocal", true,
                                                "oncomplete", "OnCompScaleDown",
                                                "onCompleteTarget", gameObject,
                                                "time", 0.2f));
    }
    /// <summary>재료 / 메달 / 선물이 부족할 때 획득처 안내 팝업을 연다.</summary>
    public void SetNavigationOpen(EStorageType storageType, string id)
    {
        if (naviPopUp != null)
            return;

        ItemExchangeDataRow itemData = regulation.itemExchangeDtbl.Find(row => row.type == storageType && row.typeidx == id);
        if (itemData != null)
        {
            naviPopUp = UIPopup.Create<UINavigationPopUp>("NavigationPopUp");
            naviPopUp.Init(storageType, id, itemData);
            if (storageType == EStorageType.Part) naviPopUp.title.text = Localization.Get("8023");
            else if (storageType == EStorageType.Medal) naviPopUp.title.text = Localization.Get("5608");
            else if (storageType == EStorageType.Food) naviPopUp.title.text = Localization.Get("8023");
        }
    }

    private void SetExchangeOpen()
    {
        UISimplePopup.Create<UIMedalExchangePopup>("MedalExchangePopup").Set(commander);
    }

    /// <summary>
    /// 상세 팝업 진입점. 좌우 이동용 지휘관 목록을 모드에 맞게 걸러낸 뒤 팝업을 연다.
    /// (육성: 보유 지휘관만, 모집: 미보유 지휘관만)
    /// </summary>
    public void InitOpenPopup(string strId, CommanderDetailType type, List<RoCommander> list = null)
    {
        if (bEnterKeyEnable == false)
        {
			SendOnInitToInnerParts ();
            bEnterKeyEnable = true;

            if (list != null)
            {
                commanderList = list;

                if(type == CommanderDetailType.Training) commanderList.RemoveAll(comm => comm.state != ECommanderState.Nomal);
                else if (type == CommanderDetailType.Recruit) commanderList.RemoveAll(comm => comm.state != ECommanderState.Undefined);
            }

            InitAndOpenCard();
            Set(strId, type);

            OpenPopupShow();

            UISetter.SetActive(uiCommanderLeft.spine, false);
			selectWeaponSlot = 1;
            this.type = type;
            SetActiveItemStatus(false);
            unitRoot.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 2;
            costumeScrollView.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
            cardRoot.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 3;
            skillScrollView.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
            favorStepListView.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
            commanderGiftListView.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
            scenarioTitleListView.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 1;
            nextCommander.GetComponent<UIPanel>().depth = gameObject.GetComponent<UIPanel>().depth + 2;
        }
    }

    public void PopUpOpenEnd()
    {
        StartCoroutine(SetCommanderDetailSpineUnit());
    }

    public void ZoomExit()
    {
        if (isZoom)
        {
            isZoom = false;

            this.CardScaleDownSize();
        }
    }

    /// <summary>재화(다이아 / 골드 / 반지) 부족 안내 팝업.</summary>
    public void NotEnough(MultiplePopUpType type)
    {
        if (type == MultiplePopUpType.NOTENOUGH_CASH)
        {
            UISimplePopup.CreateBool(true, "1031", "1032", null, "5348", "1000").onClick = sender =>
            {
                string key = sender.name;
                if (key == "OK")
                {
                    UIManager.instance.world.mainCommand.OpenDiamondShop();
                }

            };
        }
        else if (type == MultiplePopUpType.NOTENOUGH_GOLD)
        {
            UISimplePopup.CreateBool(true, "1029", "1030", null, "1001", "1000").onClick = sender =>
            {
                string key = sender.name;
                if (key == "OK")
                {
                    UIManager.instance.world.camp.GoNavigation("MetroBank");
                }
            };
        }
        else if (type == MultiplePopUpType.NOTENOUGH_RING)
        {
            UISimplePopup.CreateBool(true, "20099", "20104", null, "5348", "1000").onClick = sender =>
            {
                string key = sender.name;
                if (key == "OK")
                {
                    UIManager.instance.world.mainCommand.OpenDiamondShop();
                }

            };
        }
    }

    public void CloseBuyContents()
    {
        SetCostume(commander.currentCostume.ToString(), false);
        UISetter.SetActive(buyContents, false);
    }

	public void DecreaseItemStart()
	{
		skillLevelUpContents.DecreaseItemStart();
	}

	public void DecreaseItemEnd()
	{
		skillLevelUpContents.DecreaseItemEnd();
	}

	public void AddItemStart()
	{
		skillLevelUpContents.AddItemStart();
	}

	public void AddItemEnd()
	{
		skillLevelUpContents.AddItemEnd();
	}

	public void ItemCountMax()
	{
		skillLevelUpContents.ItemCountMax();
	}

	public void CloseSkillContents()
	{
		UISetter.SetActive(skillLevelUpContents, false);
	}

	public void SetInputValue()
	{
		skillLevelUpContents.SetInputValue();
	}

    /// <summary>코스튬 구매 확인 파츠.</summary>
    [System.Serializable]
    public class UIBuyContents : UIInnerPartBase
    {
        public CommanderCostumeListItem item;

        public UILabel itemCount;
        public GameObject itemCountRoot;

        private CommanderCostumeDataRow costumeData;


        public override void OnInit(UIPanelBase parent)
        {
            base.OnInit(parent);
            UISetter.SetActive(root, false);
        }

        public void Set(CommanderCostumeDataRow data)
        {
            UISetter.SetActive(root, true);
            item.SetBuy(data);
            costumeData = data;
        }

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            UICommanderDetail uiParent = parent as UICommanderDetail;

            string key = sender.name;
            if (key.StartsWith("BuyCancleBtn"))
            {
                SoundManager.PlaySFX("BTN_Negative_001");

            }
            else if (key.StartsWith("BuyOkBtn"))
            {
                SoundManager.PlaySFX("BTN_Buy_001", false);
                if (costumeData.gid == EPriceType.Cash)
                {
                    if (localUser.cash < costumeData.sellPrice)
                    {
                        uiParent.NotEnough(MultiplePopUpType.NOTENOUGH_CASH);
                    }
                    else
                    {
                        RemoteObjectManager.instance.RequestBuyCommanderCostume(costumeData.cid, costumeData.ctid);                        
                    }
                }
                else if (costumeData.gid == EPriceType.Gold)
                {
                    if (localUser.gold < costumeData.sellPrice)
                    {
                        uiParent.NotEnough(MultiplePopUpType.NOTENOUGH_GOLD);
                    }
                    else
                    {
                        RemoteObjectManager.instance.RequestBuyCommanderCostume(costumeData.cid, costumeData.ctid);
                    }
                }



            }

            uiParent.SetCostume(uiParent.commander.currentCostume.ToString(), false);
            UISetter.SetActive(root, false);
        }


    }

	/// <summary>
	/// 스킬 레벨업 파츠. 목표 레벨을 +/- 버튼(누르고 있으면 연속 증감)이나 직접 입력으로 정하고,
	/// 구간별 비용을 합산해 한 번에 레벨업을 요청한다.
	/// </summary>
	[System.Serializable]
	public class UISkillLevelUpContents : UIInnerPartBase
	{
		public UICommanderSkill skillItem;
		public UILabel goldCost;
		public GameObject decreaseBtn;
		public GameObject addBtn;
		public UILabel title;

		public UILabel inputLabel;
		public UIInput input;

		Dictionary<int, int> totalCostList;
		int upLevel;
		string commanderId;
		int maxLevel;
		int minLevel;
		int price;
		int skillIndex;
		UIPanelBase parentPanel;

		bool isPress;

		public override void OnInit(UIPanelBase parent)
		{
			base.OnInit(parent);
			parentPanel = parent;
			totalCostList = new Dictionary<int, int> ();
			UISetter.SetActive (root, false);
		}

		public void Set(RoCommander commander, int index)
		{
			totalCostList.Clear ();
			skillIndex = index;
			UISetter.SetLabel (title, Localization.Get ("19090"));

			SkillDataRow skillData = regulation.skillDtbl[commander.unitReg.skillDrks [index - 1]];
			regulation.skillCostDtbl.ForEach (row => 
				{
					totalCostList.Add(row.level, row.typeCost[index - 1]);
				});
			int level = commander.GetSkillLevel(index);
			int limitLevel = regulation.skillCostDtbl.Find(row => row.typeLimitLevel[index - 1] == commander.level + 1).level;

			commanderId = commander.id;
			maxLevel = limitLevel;
			upLevel = level + 1;
			minLevel = upLevel;
			isPress = false;
			skillItem.SetSkill (skillData,level, 0, 0, true, 0);
			SetValue ();
		}

		public override void OnClick(GameObject sender, UIPanelBase parent)
		{
			string key = sender.name;
			if (key == "SkillLevelUpCancleBtn") 
			{
				UISetter.SetActive(root, false);
			}
			else if (key == "SkillLevelUpBtn")
			{
				SoundManager.PlaySFX("SE_Sale_001",false);

				SkillLevelUpCheck ();
			}
		}

		void SkillLevelUpCheck()
		{
			if (localUser.gold < GetCost(minLevel, upLevel)) //골드
            {
                UISimplePopup.CreateBool(true, "1029", "1030", null, "1001", "1000").onClick = go =>
                {
                    string keyName = go.name;
                    if (keyName == "OK")
                    {
                        uiWorld.camp.GoNavigation("MetroBank");
                    }
                };
            }
            else
            {
                SoundManager.PlaySFX("SE_UpgradeSkill_001");
				UISetter.SetActive(root, false);

				network.RequestCommanderSkillLevelUp(commanderId, skillIndex, upLevel - (minLevel - 1));
            }
		}

		private void SetValue()
		{
			input.value = "";
			int cost = GetCost (minLevel, upLevel);
			UISetter.SetColor(goldCost, localUser.gold < cost ? Color.red : Color.white);
			UISetter.SetLabel(inputLabel, upLevel);
			UISetter.SetLabel(goldCost, cost);
			UISetter.SetButtonEnable(decreaseBtn, upLevel > minLevel);
			UISetter.SetButtonGray(addBtn, upLevel < maxLevel);
		}

		public void DecreaseItemStart()
		{
			parentPanel.StartCoroutine(ItemCalculation(-1));
		}

		public void DecreaseItemEnd()
		{
			isPress = false;
			if(ItemCheck(-1))
			{
				upLevel--;
				SetValue();
			}
		}

		public void AddItemStart()
		{
			if (upLevel >= maxLevel) 
			{
				NetworkAnimation.Instance.CreateFloatingText (Localization.Get ("19092"));
				return;
			}

			parentPanel.StartCoroutine(ItemCalculation(1));
		}

		public void AddItemEnd()
		{
			isPress = false;

			if(ItemCheck(1))
			{
				upLevel++;
				SetValue();
			}
		}

		public void ItemCountMax()
		{
			upLevel = maxLevel;
			SetValue();
		}

		private bool ItemCheck(int value)
		{
			if(value > 0)
			{
				if(upLevel < maxLevel)
					return true;
			}
			else
			{
				if(upLevel > minLevel)
					return true;
			}
			return false;
		}

		private IEnumerator ItemCalculation(int value)
		{
			float speed = 0.05f;
			isPress = true;
			yield return new WaitForSeconds(1.0f);

			while(ItemCheck(value) && isPress)
			{
				upLevel = upLevel + value;
				SetValue();
				yield return new WaitForSeconds(speed);

			}
			yield return true;
		}

		public void SetInputValue()
		{
			int inputLevel;
			if (!int.TryParse(input.value, out inputLevel) || inputLevel <= minLevel)
				upLevel = minLevel;
			else if (inputLevel > maxLevel)
				upLevel = maxLevel;
			else
				upLevel = inputLevel;

			SetValue();
		}

		public void SetLimitLength(int limit)
		{
			input.characterLimit = limit;
		}

		int GetCost(int minLevel, int level)
		{
			int cost = 0;

			foreach(var data in totalCostList)
			{
				if (minLevel - 1 <= data.Key && data.Key <= level - 1)
					cost += data.Value;
			}
			return cost;
		}
	}


    // 최상단 팝업일 때만 지휘관 보이스를 켠다
    protected override void OnEnablePopup()
    {
        UISetter.SetVoice(uiCommanderLeft.spine, true);        
    }

    protected override void OnDisablePopup()
    {
        UISetter.SetVoice(uiCommanderLeft.spine, false);
    }    
}
