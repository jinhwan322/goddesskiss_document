using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cache;
using Newtonsoft.Json;

using Shared.Regulation;

using RoomDecorator;
using RoomDecorator.Data;

/// <summary>
/// 메인 로비 공통 UI (상단 유저 정보·재화, 하단 메뉴, 채팅, 사이드 메뉴).
/// 캠프(로비)와 월드맵 화면 모두에서 유지되며, 화면에 따라 표시 영역과 NGUI depth를 바꾼다.
/// - 재화 표시 / 충전 팝업(VIP 충전 횟수 확인), 탄약·연료 자동 충전 타이머와 로컬 푸시
/// - 메뉴 버튼(우편, 업적, 이벤트, 그룹, 데이트, 기숙사 등) 진입
/// - 메인 지휘관 스파인 연출과 보이스
/// </summary>
public class UIMainCommand : UIPopup {
    public UIUser user;
    public UIChatting chat;

	public GameObject uiRoot;
	public GameObject purchasePopup;

    public GameObject Top;
	public GameObject LeftMainSpine;

	public GameObject Right;

    public GameObject BottomLeft;
    public GameObject BottomRight;

    public GameObject campBtn;
    public GameObject worldMapBtn;

    public GameObject StageInfo;

    public MessageTextUp2D floatingText;

    private int campDepth;
    private int worldMapDepth;

    public UITimer bulletTimer;
	public UITimer oilTimer;
    public UILabel bullet;
    public UILabel key;
    public UILabel challenge;
    public UILabel worldChallenge;
    public UILabel sweep;
    public UILabel waveDuelTicket;
    public UILabel cooperateBattleTicket;
	public UILabel eventRaidTicket;
	public UILabel oil;

    public GameObject goldBtn;

    public GameObject CallBtn;
    public UICallMessage callMessage;

	public UIPanel bulletRoot;
    public GameObject keyRoot;
    public GameObject challengeRoot;
    public GameObject worldChallengeRoot;
    public GameObject sweepRoot;
	public GameObject otherRoot;
	public GameObject eventRaidRoot;
	public UIPanel oilRoot;
	public GameObject goldRoot;
	public UISprite otherIcon;
	public UILabel otherCount;
    public GameObject waveDuelTicketRoot;
    public GameObject cooperateBattleTicketRoot;

    public GameObject badgeMail;
    public UILabel mailCount;

    public GameObject badgeChat;
	public GameObject badgeWorldMap;
	public GameObject badgeCarnival1;
    public GameObject badgeCarnival2;
    public GameObject badgeCarnival3;
    public GameObject badgeGroup;
    public GameObject badgeFirstPayment;

    public GameObject campRoot;
    public GameObject worldMapRoot;
    public GameObject commonRoot;
    public UILabel worldName;
    public GameObject contentsMenu;
    public GameObject firstPayment;
    public UISprite achievement;
    public UILabel achievementLabel;
    public UILabel gameCenterLabel;

    public GameObject joinAttendCarnivalBtn;
    public UISprite bottomLeftBg;
    public UIGrid bottomLeftGrid;

    public bool enableUIAnimation = true;    
    private TimeData bulletReChargeTime;
	private TimeData oilReChargeTime;

    // 다이아 상점
    private UIDiamondShop diamondShop;
    private UIVipInfo vipInfo;

    UIPanel campPanel;
    UIPanel worldMapPanel;
    UIPanel rootPanel;
    UIPanel commonRootPanel;
    UIPanel campRootPanel;
    UIPanel chatRootPanel;
    UIPanel sideMenuPanel;
    UIPanel contentsMenuRootPanel;
    UIPanel worldMapRootPanel;
    UIPanel callMessagePanel;
    GameObject notiBlock;
    UIFirstPaymentPopup firstPaymentPopup;

    public UIPanel naviRootPanel;
    public UIPanel naviListPanel;

    public GameObject webEvent;
    public GameObject badgeWebEvent;    

    [HideInInspector]
    public GameObject sideMenu;
	UISideMenu uiSideMenu;

    public UIMainbanner banner;

    public UISpineAnimationTest spineTest;
    private bool _enableMainCommander;

    private int _openDiaShopType = 1;
    public UISprite worldMapNaviBtn;
	public GameObject worldMapNaviRoot;
	public GameObject worldMapNaviParent;
	public UIDefaultListView worldMapNaviListView;
	public GUIAnimNGUI m_SecondaryButton;

    public GameObject dormitoryBtn;

    // 재화 충전 버튼 이름 → VIP 충전 타입
    private static readonly Dictionary<string, EVipRechargeType> RechargeButtons = new Dictionary<string, EVipRechargeType>
    {
        { "BulletRecharge", EVipRechargeType.Bullet },
        { "KeyRecharge", EVipRechargeType.Key },
        { "ChallengeRecharge", EVipRechargeType.Challenge },
        { "WorldChallengeRecharge", EVipRechargeType.WorldDuelTicket },
        { "SweepRecharge", EVipRechargeType.SweepTicket },
        { "EventRaidRecharge", EVipRechargeType.EventRaidTicket },
        { "OilRecharge", EVipRechargeType.Oil },
    };

    // 전용 표시 영역이 있는 재화 (그 외 재화는 otherRoot에 아이콘과 수량으로 표시)
    private static readonly HashSet<EGoods> DedicatedResourceViews = new HashSet<EGoods>
    {
        EGoods.Bullet, EGoods.Key, EGoods.Challenge, EGoods.WorldDuelTicket, EGoods.SweepTicket,
        EGoods.WaveDuelTicket, EGoods.CooperateBattleTicket, EGoods.EventRaidChallenge, EGoods.FreeCash, EGoods.FreeGold
    };

    /// <summary>UI 루트에서 사이드 메뉴와 공지 블록을 찾고, 충전 타이머와 패널 depth 참조를 준비한다.</summary>
    void Start()
    {
        uiRoot = this.gameObject.transform.parent.gameObject;
        sideMenu = uiRoot.transform.Find("SideMenu").gameObject;
		uiSideMenu = sideMenu.GetComponent<UISideMenu> ();

        notiBlock = uiRoot.transform.Find("Block").gameObject;
        campDepth = 0;
        worldMapDepth = 0;
        bulletReChargeTime = TimeData.Create();
		oilReChargeTime = TimeData.Create();
        callMessage.Init();
        SettingDepth();
        
        UISetter.SetActive(webEvent, false);
        ResizeBottomLeftMenu();

        uiSideMenu.FirstOpenMenu();
    }

    /// <summary>화면 전환 시 depth를 바꿀 패널들을 미리 찾아 둔다.</summary>
    void SettingDepth()
    {
        campPanel = uiWorld.camp.GetComponent<UIPanel>();
        worldMapPanel = uiWorld.worldMap.GetComponent<UIPanel>();
        rootPanel = gameObject.GetComponent<UIPanel>();
        commonRootPanel = commonRoot.GetComponent<UIPanel>();
        campRootPanel = campRoot.GetComponent<UIPanel>();
        chatRootPanel = chat.GetComponent<UIPanel>();
        sideMenuPanel = sideMenu.GetComponent<UIPanel>();
        contentsMenuRootPanel = contentsMenu.GetComponent<UIPanel>();
        worldMapRootPanel = worldMapRoot.GetComponent<UIPanel>();
        callMessagePanel = callMessage.scrollView.GetComponent<UIPanel>();
    }

    /// <summary>(개발 빌드) 콜 버튼을 1.5초 누르고 있으면 튜토리얼 디버그 UI를 연다.</summary>
    IEnumerator TutorialPressChecker()
    {
        yield return new WaitForSeconds(1.5f);

        #if !LIVE_MODE        

        GameObject uiRoot = UIRoot.list[0].gameObject;
        Transform tfTutorial = uiRoot.transform.Find("Tutorial");
        if (tfTutorial != null)
        {            
            tfTutorial.gameObject.SetActive(true);
        }       
        #endif

    }


    public void OnPress(GameObject sender)
    {
        if (sender.name == "CallBtn")
        {
            StopCoroutine("TutorialPressChecker");
            StartCoroutine("TutorialPressChecker");
        }
    }

    public void OnRelease(GameObject sender)
    {
        if (sender.name == "CallBtn")
        {
            StopCoroutine("TutorialPressChecker");
        }
    }

    /// <summary>메인 로비 버튼 처리. 재화 충전 버튼은 RechargeButtons 표로 처리한다.</summary>
    public override void OnClick(GameObject sender)
    {
        base.OnClick(sender);
        string key = sender.name;

        EVipRechargeType rechargeType;
        if (RechargeButtons.TryGetValue(key, out rechargeType))
        {
            OpenVipRechargePopUp(rechargeType);
            return;
        }

        if (key == "UserDetail")
        {
            uiWorld.userDetail.InitOpenPopup();
        }
        else if (key == "Achievement")
        {
            uiWorld.achievement.Set(EReward.Achievement);
            uiWorld.achievement.Open();
        }
        else if (key == "Mail")
        {
            network.RequestMailList();
        }
        else if (key == "Link-CashShop")
        {
            OpenDiamondShop();
        }
        else if (key == "Link-GoldShop")
        {
            OpenMetroBank();
        }
        else if (key == "ToCamp")
        {
            uiWorld.worldMap.Close();
        }
        else if (key == "ToWorldMap")
        {
            uiWorld.worldMap.InitAndOpenWorldMap();
        }
        else if (key == "BtnCalendar")      // 출석 보상
        {
            uiWorld.dailyBonus.InitAndOpenDailyBonus();
        }
        else if (key == "BtnCarnival")      // 이벤트
        {
            network.RequestGetCarnivalList(ECarnivalCategory.Basic);
        }
        else if (key == "BtnReward")
        {
            network.RequestGetCarnivalList(ECarnivalCategory.Reward);
        }
        else if (key == "BtnNewUser")       // 신규 유저 출석
        {
            network.RequestGetCarnivalList(ECarnivalCategory.JoinAttend);
        }
        else if (key == "BtnGroup")
        {
            uiWorld.group.InitAndOpenGroup();
        }
        else if (key == "BtnDate")
        {
            uiWorld.dateMode.InitAndOpenDateMode();
        }
        else if (key == "BtnWebEvent")
        {
            RemoteObjectManager.instance.RequestStartWebEvent();
        }
        else if (key == "WorldMapNaviOpenBtn")
        {
            OpenWorldMapNavigation();
        }
        else if (key == "BtnDormitory")
        {
            OpenDormitory();
        }
		else if (worldMapNaviListView.Contains(key))    // 월드맵 챕터 바로가기
		{
			string id = worldMapNaviListView.GetPureId(key);
			if (!uiWorld.worldMap.currentWorldMapId.Equals(id))
				uiWorld.worldMap.MoveWorldMap(id);
		}
        else if (key == "FirstPayment")
        {
            OpenAndInitFirstPaymentPopup();
        }
    }

    /// <summary>기숙사(방 꾸미기): 내 기숙사 주인 정보로 초기화하고 층 정보를 요청한다.</summary>
    private void OpenDormitory()
    {
        localUser.dormitory.InitCommanders();

        RoDormitory.User owner = new RoDormitory.User();
        owner.name = localUser.nickname;
        owner.level = localUser.level;
        owner.uno = localUser.uno;
        owner.isMaster = true;
        DormitoryInitData.Instance.Set(owner);

        network.RequestGetDormitoryFloorInfo();
    }

    void OpenMetroBank()
    {
        uiWorld.camp.GoNavigation("MetroBank");
    }

    /// <summary>상단 유저 정보와 재화(현재/최대) 표시, 뱃지, 이벤트 버튼 상태를 갱신한다.</summary>
    public void Set()
    {
        if (user != null)
        {
            user.Set(localUser);
            UISetter.SetLabel(bullet, string.Format("{0}/{1}", localUser.bullet, regulation.GetUserLevelDataRow(localUser.level).maxBullet));
            SetGoodsLabel(key, localUser.opener, EGoods.Key);
            SetGoodsLabel(challenge, localUser.challenge, EGoods.Challenge);
            SetGoodsLabel(worldChallenge, localUser.worldDuelTicket, EGoods.WorldDuelTicket);
            SetGoodsLabel(sweep, localUser.sweepTicket, EGoods.SweepTicket);
            SetGoodsLabel(waveDuelTicket, localUser.waveDuelTicket, EGoods.WaveDuelTicket);
            UISetter.SetLabel(cooperateBattleTicket, string.Format("{0}/{1}", localUser.cooperateBattleTicket, regulation.defineDtbl["COOPERATE_BATTLE_COUNT"].value));
            SetGoodsLabel(eventRaidTicket, localUser.eventRaidTicket, EGoods.EventRaidChallenge);
            SetGoodsLabel(oil, localUser.oil, EGoods.Oil);
        }

		RoBuilding building = localUser.FindBuilding(EBuilding.MetroBank);
		UISetter.SetButtonEnable(goldBtn, localUser.level >= building.firstLevelReg.userLevel);
        BadgeControl();    
        SetPaymentControl();   
        if (webEvent != null || localUser.badgeCarnivalTabList[3].Count != 0)
        {          
            UISetter.SetActive(webEvent, localUser.webEventUrls != null && localUser.webEventUrls.Count > 0);
            UISetter.SetActive(joinAttendCarnivalBtn, localUser.badgeCarnivalTabList[3].Count != 0);
          
            ResizeBottomLeftMenu();
            bottomLeftGrid.Reposition();
           
            UISetter.SetActive(badgeWebEvent, localUser.badgeWebEvent);
        }  

        UISetter.SetActive(dormitoryBtn, localUser.level >= localUser.dormitory.config.openLevel);
    }

    /// <summary>재화 수량을 "현재/최대 충전량" 형식으로 표시한다.</summary>
    private void SetGoodsLabel(UILabel label, int current, EGoods goods)
    {
        UISetter.SetLabel(label, string.Format("{0}/{1}", current, regulation.FindGoodsServerFieldName((int)goods).rechargeMax));
    }

    /// <summary>좌하단 메뉴 배경 너비를 버튼 수에 맞춘다.</summary>
    private void ResizeBottomLeftMenu()
    {
        bottomLeftBg.width = 120 + (bottomLeftGrid.GetChildList().Count * 110);
    }

    public override void OnRefresh()
    {
        base.OnRefresh();
        Set();
        chat.OnRefresh();
        if(diamondShop != null)
        {
            diamondShop.SetUserData();
        }

        if(sideMenu != null)
        {
			uiSideMenu.SetBadge();
        }

        if (firstPaymentPopup != null)
            firstPaymentPopup.OnRefresh();
    }

    /// <summary>월드맵 화면용 배치: 캠프 메뉴를 숨기고 스테이지 정보, 챕터 바로가기, 탄약 표시를 켠다.</summary>
    public void OnWorldMap()
    {
        SetResourceView(EGoods.Bullet);
		network.RequestBulletCharge();
        UISetter.SetActive(worldMapBtn, false);
        UISetter.SetActive(campBtn, true);
        UISetter.SetActive(chat, false);
        UISetter.SetActive(StageInfo, true);
        UISetter.SetActive(worldMapRoot, true);
        UISetter.SetActive(campRoot, false);
        UISetter.SetActive(banner, false);
        UISetter.SetActive(worldMapNaviParent, true);

        Set();
		SetWorldMapNaviList();
        Open();
        DepthControl(false);

        localUser.UserLevelUpCheck();

        if(callMessage != null)callMessage.transform.localPosition = new Vector2(10, -100);
    }

    /// <summary>캠프(로비) 화면용 배치: 채팅, 사이드 메뉴, 메인 지휘관을 켜고 배너 정보와 로비 BGM을 불러온다.</summary>
    public void OnCamp()
    {
        commonRoot.GetComponent<PanelDepthControl>().OnDisable();
        chat.GetComponent<PanelDepthControl>().OnDisable();
        SetResourceView(EGoods.Bullet);
        UISetter.SetActive(campBtn, false);
        UISetter.SetActive(worldMapBtn, true);
        UISetter.SetActive(chat, true);
        UISetter.SetActive(StageInfo, false);
        UISetter.SetActive(worldMapRoot, false);
        UISetter.SetActive(campRoot, true);
        UISetter.SetActive(sideMenu, true);
        UISetter.SetActive(contentsMenu, true);
        UISetter.SetActive(banner, false);
        UISetter.SetActive(worldMapNaviParent, false);

        EnableMainCommanderSpine(!RemoteObjectManager.instance.localUser.isEnableTutorial);

		network.RequestOnCamp();

        Set();
        Open();
        DepthControl(true);

        localUser.UserLevelUpCheck();
        RemoteObjectManager.instance.RequestGetRotationBannerInfo();
        CacheManager.instance.SoundPocketCache.Create("Pocket_BGM_World");

        if (callMessage != null) callMessage.transform.localPosition = new Vector2(10, -230);
    }

    public void EnableMainCommanderSpine(bool isEnable)
    {
        UISetter.SetActive(spineTest, isEnable);

        if (isEnable)
        {
            if (enableUIAnimation)
            {
                StartAnimation();
            }
        }
    }


    /// <summary>
    /// 캠프 / 월드맵 패널 위에 로비 UI가 올라오도록 depth를 다시 맞춘다.
    /// 월드맵에서는 재화, 사이드 메뉴, 챕터 바로가기가 월드맵보다 위에 오도록 단계별로 높인다.
    /// </summary>
    private void DepthControl(bool camp)
    {
        campDepth = campPanel.depth + 1;
        worldMapDepth = worldMapPanel.depth + 1;
        if(camp)
        {
            rootPanel.depth = campDepth;
            commonRootPanel.depth = campDepth;
			bulletRoot.depth = campDepth + 1;
            campRootPanel.depth = campDepth;
            chatRootPanel.depth = campDepth;
            sideMenuPanel.depth = campDepth;
            callMessagePanel.depth = campDepth + 1;
            contentsMenuRootPanel.depth = campDepth;
        }
        else
        {
            rootPanel.depth = worldMapDepth;
            commonRootPanel.depth = worldMapDepth + 1;
			bulletRoot.depth = worldMapDepth + 2;
            worldMapRootPanel.depth = worldMapDepth + 2;
            sideMenuPanel.depth = worldMapDepth + 3;
            naviRootPanel.depth = worldMapDepth + 4;
            naviListPanel.depth = worldMapDepth + 5;
            callMessagePanel.depth = worldMapDepth + 4;
        }
    }

	/// <summary>메인 지휘관 스파인을 화면 안으로 들여오고, 도착하면 보이스를 켠다.</summary>
	public void StartLeftMainSpine()
	{
        iTween.MoveTo(LeftMainSpine, iTween.Hash("x", 0, "time", 1.0, "delay", 0.0, "islocal", true, "oncomplete", "OnStartLeftMainSpine", "oncompletetarget", this.gameObject));
	}

    public void OnStartLeftMainSpine()
    {
        _enableMainCommander = true;
        EnableMainCommanderVoice(true);        
    }

    public void EndAnimation()
    {
        _enableMainCommander = true;

        GameObject spineTarget = spineTest.spine.target;
        if (spineTarget != null)
        {
            spineTarget.GetComponent<UIInteraction>().StartInteraction();
            EnableMainCommanderVoice(true);
        }        
    }

    /// <summary>
    /// 메인 지휘관 보이스 on/off. 캠프 화면만 열려 있고(다른 팝업 없음) 지휘관이 화면 안에 있을 때만 켠다.
    /// </summary>
    public void EnableMainCommanderVoice(bool enable)
    {
        // 캠프가 아닌 경우 보이스 비활성화
        if (UIPopup.openedPopups.Count != 1 || !(UIPopup.openedPopups[0] is UICamp))
        {
            enable = false;
        }
        
        if (!_enableMainCommander)
        {
            enable = false;
        }

        spineTest.enableVoice = enable;        
    }

    /// <summary>메인 지휘관 스파인을 화면 밖으로 내보내고 보이스를 끈다.</summary>
    public void CloseLeftMainSpine()
	{
        iTween.MoveTo(LeftMainSpine, iTween.Hash("x", -550, "time", 1.8, "delay", 0.0, "islocal", true, "oncomplete", "OnCloseLeftMainSpine", "oncompletetarget", this.gameObject));
	}

    public void OnCloseLeftMainSpine()
    {
        _enableMainCommander = false;
        EnableMainCommanderVoice(false);        
    }

    /// <summary>로비 진입 연출: 상단 / 좌측 지휘관 / 하단 메뉴가 순서대로 들어온다. (최초 1회)</summary>
    public void StartAnimation()
    {
        enableUIAnimation = false;
        iTween.MoveFrom(Top, iTween.Hash("y", 5,"time", 1.0,"delay",0.5));
		iTween.MoveFrom(LeftMainSpine, iTween.Hash("x", -5,"time", 1.0,"delay",1.1, "oncomplete", "EndAnimation", "oncompletetarget", this.gameObject));
        iTween.MoveFrom(BottomLeft, iTween.Hash("x", -5,"time", 1.0,"delay",0.9));
        iTween.MoveFrom(BottomRight, iTween.Hash("x", 5,"time", 1.0,"delay",0.9));
    }

    public void TopAnchorAnimation()
    {
        iTween.MoveFrom(Top, iTween.Hash("y", 5,"time", 1.0,"delay",0));
    }

	/// <summary>탄약 수량을 표시하고, 최대치 미만이면 다음 충전까지의 타이머를 돌린다.</summary>
	public void BulletControl()
	{
		UserLevelDataRow row = RemoteObjectManager.instance.regulation.GetUserLevelDataRow(localUser.level);
		UISetter.SetLabel(this.bullet, string.Format("{0}/{1}", localUser.bullet, row.maxBullet));
		if(localUser.bullet < row.maxBullet && localUser.bulletRemain > 0)
		{
			UISetter.SetActive(bulletTimer, true);
			bulletReChargeTime.SetByDuration(localUser.bulletRemain);
			UISetter.SetTimer(bulletTimer, bulletReChargeTime);

			bulletTimer.RegisterOnFinished(ui =>
				{
					localUser.BulletCharge();
				});
		}
		else
		{
			UISetter.SetActive(bulletTimer, false);

		}
	}

	/// <summary>탄약이 가득 차면 '충전 완료' 로컬 푸시를 취소하고, 아니면 가득 차는 시각에 맞춰 예약한다.</summary>
	public void MaxBulletCheck()
	{
		UserLevelDataRow row = RemoteObjectManager.instance.regulation.GetUserLevelDataRow(localUser.level);
		if(localUser.bullet >= row.maxBullet)
		{
			UISetter.SetActive(bulletTimer, false);

            network.CancelLocalPush(ELocalPushType.BulletFullCharge);
        } 
        else
        {
            if(!localUser.useBullet)
            {
                if(localUser.tempBullet != localUser.bullet)network.ScheduleLocalPush(ELocalPushType.BulletFullCharge, (int)bulletReChargeTime.GetRemain());
            }
        }

        localUser.tempBullet = localUser.bullet;

    }

	/// <summary>연료(이벤트 전투 입장 재화) 수량과 충전 타이머 표시</summary>
	public void OilControl()
	{
		GoodsDataRow row = RemoteObjectManager.instance.regulation.FindGoodsServerFieldName(((int)EGoods.Oil).ToString());        
        UISetter.SetLabel(oil, string.Format("{0}/{1}", localUser.oil, row.rechargeMax));
		if(localUser.oil < row.rechargeMax && localUser.oilRemain > 0)
		{
            UISetter.SetActive(oilTimer, true);
			oilReChargeTime.SetByDuration(localUser.oilRemain);
			UISetter.SetTimer(oilTimer, oilReChargeTime);

			oilTimer.RegisterOnFinished(ui =>
				{
					localUser.OilCharge();
				});
		}
		else
		{
			UISetter.SetActive(oilTimer, false);
		}
	}

    /// <summary>첫 결제 이벤트 버튼 표시 (0: 미결제, 1: 결제 후 보상 미수령, 2: 완료)</summary>
    public void SetPaymentControl()
    {
        UISetter.SetActive(badgeFirstPayment, localUser.statistics.firstPayment == 1);
        UISetter.SetActive(firstPayment, localUser.statistics.firstPayment < 2 && !worldMapRoot.activeSelf);
    }

    public void BadgeControl()
    {
        UISetter.SetActive(badgeWorldMap, localUser.badgeWorldMap || localUser.explorationDtbl.hasCompleteState);
        UISetter.SetActive(badgeMail, localUser.badgeNewMailCount > 0);
        UISetter.SetLabel(mailCount, localUser.badgeNewMailCount);
        UISetter.SetActive(badgeCarnival1, localUser.badgeCarnivalComplete[1] || localUser.badgeCarnival(ECarnivalCategory.Basic));
        UISetter.SetActive(badgeCarnival2, localUser.badgeCarnivalComplete[2] || localUser.badgeCarnival(ECarnivalCategory.Reward));
        UISetter.SetActive(badgeCarnival3, localUser.badgeCarnivalComplete[3]);
        UISetter.SetActive(badgeGroup, localUser.badgeGroupCount > 0);
    }

    /// <summary>
    /// 상단 재화 표시 영역을 현재 화면에 맞게 바꾼다.
    /// 탄약·다이아·골드는 기본 표시, 콘텐츠 입장권은 전용 영역, 그 외 재화는 공용 영역(otherRoot)에 표시한다.
    /// </summary>
    public void SetResourceView(EGoods goods)
    {
		UISetter.SetAlpha(bulletRoot, goods == EGoods.Bullet || goods == EGoods.FreeCash || goods == EGoods.FreeGold ? 1.0f : 0.0f);
		UISetter.SetAlpha(oilRoot, goods == EGoods.EventRaidChallenge ? 1.0f : 0.0f);
        UISetter.SetActive(keyRoot, goods == EGoods.Key);
        UISetter.SetActive(challengeRoot, goods == EGoods.Challenge);
        UISetter.SetActive(worldChallengeRoot, goods == EGoods.WorldDuelTicket);
        UISetter.SetActive(sweepRoot, goods == EGoods.SweepTicket);
        UISetter.SetActive(waveDuelTicketRoot, goods == EGoods.WaveDuelTicket);
        UISetter.SetActive(cooperateBattleTicketRoot, goods == EGoods.CooperateBattleTicket);
		UISetter.SetActive(eventRaidRoot, goods == EGoods.EventRaidChallenge);
		UISetter.SetActive(goldRoot, goods != EGoods.EventRaidChallenge);

		bool useOtherView = !DedicatedResourceViews.Contains(goods);
		UISetter.SetActive(otherRoot, useOtherView);
		if (useOtherView)
		{
			GoodsDataRow data = regulation.FindGoodsServerFieldName(((int)goods).ToString());
			UISetter.SetSprite(otherIcon, data.iconId);
			UISetter.SetLabel(otherCount, localUser.resourceList[data.serverFieldName]);
		}
    }

	/// <summary>재화 ID(문자열)로 표시 영역을 바꾼다.</summary>
	public void SetResourceView(string id)
	{
		SetResourceView((EGoods)int.Parse(id));
	}

	/// <summary>이벤트 전투 화면용 재화 표시: 이벤트 중에는 연료를, 이벤트가 끝나면 탄약을 보여준다.</summary>
	public void SetEventBattleResourceView(bool eventRaidChallenge, bool eventEnd)
	{
		UISetter.SetAlpha(bulletRoot, eventEnd ? 1.0f : 0.0f);
		UISetter.SetAlpha(oilRoot, !eventEnd ? 1.0f : 0.0f);
		UISetter.SetActive(keyRoot, false);
		UISetter.SetActive(challengeRoot, false);
		UISetter.SetActive(sweepRoot, false);
		UISetter.SetActive(waveDuelTicketRoot, false);
		UISetter.SetActive(cooperateBattleTicketRoot, false);
		UISetter.SetActive(eventRaidRoot, eventRaidChallenge && !eventEnd);
		UISetter.SetActive(goldRoot, !eventRaidChallenge || eventEnd);
		UISetter.SetActive(otherRoot, false);
	}

    public void SetEnableButton(EGoods goods, bool state)
    {
        if(goods == EGoods.FreeGold)
        {
            UISetter.SetButtonEnable(goldBtn, state);
        }

    }

    public void CreateFloatingText(Vector3 _position, string _text)
    {
        floatingText.initAndStartMoving(_position, _text);
    }

    public void OpenVipInfo(bool isShop)
    {
        if(vipInfo == null)
        {
            vipInfo = UIPopup.Create<UIVipInfo>("vipInfo");
            vipInfo.Init(isShop);
        }
    }

    /// <summary>다이아 상점 열기: 상품 목록을 먼저 요청하고, 응답에서 OpenAndInitDiamondShop()으로 연다.</summary>
    /// <param name="type">처음 보여줄 상점 탭</param>
    public void OpenDiamondShop(int type = 1)
    {
        _openDiaShopType = type;

        RemoteObjectManager.instance.RequestGetCashShopList();
    }

    public void OpenAndInitDiamondShop()
    {
        if (diamondShop == null)
        {
            diamondShop = UIPopup.Create<UIDiamondShop>("DiamondShop");            
        }

        diamondShop.initTabIdx = _openDiaShopType;
        diamondShop.InitAndOpenCashShop();
    }
    public void OpenAndInitFirstPaymentPopup()
    {
        if (firstPaymentPopup == null)
        {
            firstPaymentPopup = UIPopup.Create<UIFirstPaymentPopup>("FirstPaymentPopup");            
            firstPaymentPopup.Init();
        }
        else
            firstPaymentPopup.OnRefresh();
    }

    /// <summary>
    /// 재화 충전 팝업.
    /// 일일 초기화 시각이 지났으면 충전 횟수를 서버에서 다시 받아온 뒤 연다.
    /// 남은 충전 횟수가 없으면 VIP 등급에 따라 안내 팝업(최고 등급) 또는 다이아 상점 이동(VIP 상향)을 띄운다.
    /// 스테이지 도전 횟수 충전은 스테이지별로 횟수를 따로 관리한다.
    /// </summary>
    public void OpenVipRechargePopUp(EVipRechargeType type)
    {
		if(localUser.resetTimeData != null && localUser.resetTimeData.GetRemain() <= 0)
		{
			network.RequestVipBuyCount(type);
			return;
		}

        int typeInt = (int)type;
        VipRechargeDataRow vipRow = regulation.vipRechargeDtbl[typeInt.ToString()];
        int maxVip = regulation.vipExpDtbl[regulation.vipExpDtbl.length - 1].Idx;
        int step = 0;
        int vip = localUser.vipLevel;
        int maxRecharge = vipRow.GetMaxRechargeCount(vip);

        if(type == EVipRechargeType.StageType1 || type == EVipRechargeType.StageType2)
        {
            string stageId = uiWorld.readyBattle.battleData.stageId;

            if(localUser.stageRechargeList.ContainsKey(stageId))
                step = localUser.stageRechargeList[stageId];
        }
        else
        {
            if(localUser.resourceRechargeList.ContainsKey(typeInt.ToString()))
                step = localUser.resourceRechargeList[typeInt.ToString()];
        }

        int rechargeCnt = maxRecharge - step;
        if(localUser.vipLevel == maxVip && rechargeCnt == 0)
        {
            UISimplePopup.CreateOK(true, "5635", "12014", null, "1001");
        }
        else if(localUser.vipLevel < vipRow.startVip || rechargeCnt == 0)
        {
            UISimplePopup.CreateBool(true, "5635", "12006", null, "5348", "1000").onClick = sender =>
                {
                    string key = sender.name;
                    if (key == "OK")
                    {
                        OpenDiamondShop();
                    }
                };
        }
        else
        {
			if(purchasePopup == null)
			{
				UIResourcePurchasePopup popup =	UIPopup.Create<UIResourcePurchasePopup>("ResourcePurchasePopup");
				popup.initData(vipRow);
				purchasePopup = popup.gameObject;
			}
        }
    }

    public void SetChatPanelDepth(int depth)
    {
        if (chatRootPanel == null)
            return;

        chatRootPanel.depth = depth;
    }

    public void ResetChatPanelDepth()
    {
        if (chatRootPanel == null)
            return;

        chatRootPanel.depth = campDepth;
    }

    public void ResetGuildName()
    {
        if (user == null)
            return;        

        UISetter.SetLabel(user.guildName, GameSetting.instance.guildName ? (!string.IsNullOrEmpty(localUser.guildName) ? localUser.guildName : Localization.Get("7180")) : "");
    }

	/// <summary>월드맵 챕터 바로가기 목록 열기/닫기</summary>
	void OpenWorldMapNavigation()
	{
		UISetter.SetActive(worldMapNaviRoot, !worldMapNaviRoot.activeSelf);
		UISetter.SetSprite(worldMapNaviBtn, worldMapNaviRoot.activeSelf ? "qm-down-button" : "qm-up-button");

        uiSideMenu.CloseMenu();

		if(worldMapNaviRoot.activeSelf)
			m_SecondaryButton.MoveIn(GUIAnimSystemNGUI.eGUIMove.Self);
		else
			m_SecondaryButton.MoveOut(GUIAnimSystemNGUI.eGUIMove.Self);
	}

    public void CloseWorldMapNavigation()
    {
        if (worldMapNaviRoot.activeSelf)
        {
            UISetter.SetActive(worldMapNaviRoot, false);
            UISetter.SetSprite(worldMapNaviBtn, "qm-up-button");

            m_SecondaryButton.MoveOut(GUIAnimSystemNGUI.eGUIMove.Self);
        }
    }

	/// <summary>챕터 바로가기 목록 구성 (튜토리얼 챕터 제외)</summary>
	void SetWorldMapNaviList()
	{
		List<WorldMapDataRow> worldMapList = regulation.worldMapDtbl.FindAll(row => {return row.id != "0";});
		worldMapNaviListView.Init(worldMapList, "WorldMapNavi_");

		UISetter.SetActive(worldMapNaviRoot, false);
		UISetter.SetSprite(worldMapNaviBtn, "qm-up-button");
	}
}
