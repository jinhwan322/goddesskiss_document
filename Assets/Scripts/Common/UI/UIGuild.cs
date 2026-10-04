using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Shared.Regulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// 길드(연합) 메인 팝업.
/// - 미가입: 길드 목록/검색, 가입 신청, 길드 생성
/// - 가입: 길드 정보·공지, 길드원 목록(정렬, 길드장의 퇴출/위임/보좌관 임명), 게시판, 상점, 전투 파견, 협동 전투
/// - 점령전: 단계별 상태 표시, 신청, 남은 시간 타이머
/// </summary>
public class UIGuild : UIPopup
{
	public GEAnimNGUI AnimBG;
	public GEAnimNGUI AnimNpc;
	public GEAnimNGUI AnimInfo;
	public GEAnimNGUI AnimTitle;
	public GEAnimNGUI AnimBlock;


    bool bBackKeyEnable = false;
    bool bEnterKeyEnable = false;

    public UIDefaultListView guildListView;
    public UIDefaultListView guildMemberListView;
    public UIFlipSwitch join;
    public UIFlipSwitch create;

    public GameObject joinGuild;
    public GameObject createGuild;
    public GameObject guildMain;
    public GameObject guildJoinCreate;

    public UIInput searchGuildName;
    public UILabel guildNotice;
    public UILabel guildCount;
    public UILabel guildName;
    public UILabel guildWorld;
    public UISprite guildEmblem;
    public UILabel guildLevel;
    public UILabel guildPoint;
    public GameObject btnManage;
    public GameObject btnSkill;
    public GameObject btnApprove;
    public GameObject btnModify;
    public UILabel sortName;

    public UIInput createGuildName;
    public UIToggle toggleFree;
    public UIToggle toggleApprove;
    public UILabel minLevelLabel;
    public UISprite selectEmblem;
    public UILabel createCostLabel;

    public GameObject guildNoticeRoot;

	private UIGuildBoard guildBoard;
	public GameObject guildBoardBtn;
	public GameObject guildBoardBadge;

    private int minLevel;
    private int emblemIdx;
    private ESortType sortType = ESortType.Undefined;
    /// <summary>현재 표시 중인 화면</summary>
    private enum GuildTab
    {
        Join,   // 길드 가입(목록/검색)
        Create, // 길드 생성
        Main    // 길드 메인(가입된 상태)
    }
    private GuildTab uiGuildState = GuildTab.Join;

    // 길드원 등급 (서버 memberGrade 값)
    private const int GRADE_MEMBER = 0;
    private const int GRADE_MASTER = 1;
    private const int GRADE_SUB_MASTER = 2;

    /// <summary>길드원 관리 팝업에서 선택한 동작</summary>
    private enum MemberAction
    {
        None,
        Deport,           // 퇴출
        DelegateMaster,   // 길드장 위임
        AppointSubMaster, // 보좌관 임명
        FireSubMaster     // 보좌관 해임
    }
   
    public UISpineAnimation spineAnimation;
   
    public static readonly string guildItemIdPrefix = "GuildItem-";
    public static readonly string memberItemIdPrefix = "MemberItem-";
    private int guildCreateCost;

	public GameObject conquestJoinBtn;
	public GameObject conquestBtn;
	public Protocols.ConquestInfo conquestInfo;
	public UITimer conquestTimer;
	public TimeData conquestTime;
	public UILabel conquestStateLabel;
	public GameObject conquestBadge;
	public UIConquestHistoryPopup historyPopup;

    List<RoGuild> guildList;
	public List<Protocols.GuildMember.MemberData> memberList;
    [HideInInspector]
	public UIGuildTroopDispatchPopup dispatch;
	private UISecretShopPopup secretShop;
    [SerializeField]
    private GameObject contentsRoot;
    [SerializeField]
    private GameObject dispatchPrefab;

    // 길드 NPC 스파인과 길드 생성 비용 초기화
    void Start()
    {
        UISetter.SetSpine(spineAnimation, "n_010");
        guildCreateCost = int.Parse(RemoteObjectManager.instance.regulation.defineDtbl["GUILD_CREATION_PRICE"].value);
    }

    /// <summary>
    /// 버튼 클릭 처리. 버튼 이름(key)으로 분기한다.
    /// 목록 아이템 버튼은 "버튼명-길드ID" 형식이라 '-' 뒤의 ID로 대상 길드를 찾는다.
    /// </summary>
	public override void OnClick(GameObject sender)
    {
        base.OnClick(sender);

        string key = sender.name;

        if (key.StartsWith(memberItemIdPrefix))
        {
            OpenMemberManagePopup(key.Replace(memberItemIdPrefix, ""));
            return;
        }

        RoGuild guild = null;
        if (key.Contains("-"))
        {
            string id = key.Substring(key.IndexOf("-") + 1);
            if (!string.IsNullOrEmpty(id))
                guild = guildList.Find(list => list.gidx == int.Parse(id));
        }

        if (key == "Close")
        {
            if (bBackKeyEnable == false)
                this.Close();
        }
        else if (key == "Join")
        {
            uiGuildState = GuildTab.Join;
            SetTab();
        }
        else if (key == "Create")
        {
            uiGuildState = GuildTab.Create;
            SetTab();
        }
        else if (key == "Search")
        {
            if (!string.IsNullOrEmpty(searchGuildName.value))
                network.RequestSearchGuild(searchGuildName.value);
            else
                NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("110217"));
        }
        else if (key == "Refresh")
        {
            network.RequestGuildList();
        }
        else if (key.StartsWith("BtnJoin-"))    // 자유 가입
        {
            if (guild != null)
                network.RequestFreeJoinGuild(guild.gidx);
        }
        else if (key.StartsWith("BtnApply-"))   // 가입 신청
        {
            if (guild != null)
                network.RequestApplyGuildJoin(guild.gidx);
        }
        else if (key.StartsWith("BtnCancel-"))  // 가입 신청 취소
        {
            if (guild == null)
                return;

            UISimplePopup.CreateBool(true, Localization.Get("110224"), guild.gnm,
                Localization.Get("110225"), Localization.Get("1304"), Localization.Get("1305")
            ).onClick = popupSender =>
            {
                if (popupSender.name == "OK")
                    network.RequestCancelGuildJoin(guild.gidx);
            };
        }
        else if (key == "BtnSort")              // 길드원 정렬 기준 순환: 레벨 → 최근 접속 → 포인트
        {
            if (sortType == ESortType.Level) sortType = ESortType.Time;
            else if (sortType == ESortType.Time) sortType = ESortType.PBPoint;
            else if (sortType == ESortType.PBPoint) sortType = ESortType.Level;

            PlayerPrefs.SetInt("GuildMemberSortType", (int)sortType);
            SetMemberList();
        }
        else if (key == "BtnDispatch")          // 전투 파견
        {
            OpenDispatchPopup();
        }
        else if (key == "BtnConquest")          // 점령전
        {
            OnConquestBtnClicked();
        }
        else if (key == "EnterConquest")        // 점령전 신청
        {
            OnConquestJoinBtnClicked();
        }
        else if (key == "BtnManage" || key == "BtnSkill")   // 길드 관리(길드장) / 길드 스킬 보기(길드원)
        {
            UIPopup.Create<UIGuildManagePopup>("GuildManagePopup").InitAndOpenGuildManage();
        }
        else if (key == "BtnApprove")           // 가입 승인(길드장, 보좌관)
        {
            network.RequestManageGuildJoinMember();
        }
        else if (key == "BtnPoint")
        {
            UIPopup.Create<UIGuildMemberPointPopUp>("GuildMemberPointPopUp").InitAndOpenMemberList(memberList);
        }
        else if (key == "BtnEmblem")            // 길드 생성: 엠블럼 선택
        {
            UIImageListPopup recv = UISimplePopup.Create<UIImageListPopup>("ImageListPopup");
            recv.Set(true, Localization.Get("110020"), null, null, null, Localization.Get("1000"), null);
            recv.SetItemGuildEmblem();
            recv.onClick = popupSender =>
            {
                string popupKey = popupSender.name;
                if (popupKey.StartsWith("Emblem_"))
                {
                    emblemIdx = int.Parse(popupKey.Replace("Emblem_", ""));
                    SetCreateGuild();
                }
            };
        }
        else if (key == "BtnPlus")              // 길드 생성: 가입 최소 레벨 + (내 레벨까지)
        {
            if (minLevel < localUser.level)
            {
                minLevel++;
                SetCreateGuild();
            }
        }
        else if (key == "BtnMinus")             // 길드 생성: 가입 최소 레벨 - (길드 건물 오픈 레벨까지)
        {
            if (minLevel > localUser.FindBuilding(EBuilding.Guild).firstLevelReg.userLevel)
            {
                minLevel--;
                SetCreateGuild();
            }
        }
        else if (key == "BtnCreate")
        {
            CreateGuild();
        }
        else if (key == "BtnModify")            // 공지 수정
        {
            OpenNoticeEditPopup();
        }
        else if (key == "ShopBtn")              // 길드 상점
        {
            if (secretShop == null)
            {
                secretShop = UIPopup.Create<UISecretShopPopup>("SecretShopPopup");
                secretShop.Init(EShopType.GuildShop);
            }
        }
        else if (key == "GuildBoard")
        {
            network.RequestGetGuildBoard(0);
        }
        else if (key == "BtnCooperateBattle")   // 협동 전투 (길드 레벨 제한)
        {
            int enableLevel = int.Parse(regulation.defineDtbl["COOPERATE_BATTLE_OPEN_GUILD_LEVEL"].value);
            if (localUser.guildInfo.level < enableLevel)
            {
                NetworkAnimation.Instance.CreateFloatingText(Localization.Format("110089", enableLevel));
                return;
            }

            network.RequestCooperateBattleInfo();
        }
    }

    /// <summary>길드 공지 수정 팝업 (최대 60자)</summary>
    private void OpenNoticeEditPopup()
    {
        UIReceiveUserString recv = UISimplePopup.Create<UIReceiveUserString>("InputUserStringBig");
        recv.SetDefault(localUser.guildInfo.notice);
        recv.Set(true, Localization.Get("110027"), null, null, Localization.Get("110040"), Localization.Get("1000"), null);
        recv.SetLimitLength(60);
        recv.onClick = popupSender =>
        {
            string popupKey = popupSender.name;
            if (popupKey == "OK")
            {
                network.RequestUpdateGuildInfo(4, recv.input.value);   // 4: 공지 변경
                recv.ClosePopup();
            }
            else if (popupKey == "Close")
            {
                recv.ClosePopup();
            }
        };
    }

    #region 길드원 관리 팝업

    /// <summary>
    /// 길드원 정보 / 관리 팝업 (길드장 전용).
    /// 팝업 하나에서 [관리 메뉴] → [확인 화면] → 서버 요청 순서로 화면을 바꿔 가며 진행한다.
    /// 메뉴 버튼: Btn-1 퇴출, Btn-2 길드장 위임, Btn-3 보좌관 임명(보좌관이면 해임)
    /// </summary>
    private void OpenMemberManagePopup(string unoText)
    {
        if (localUser.guildInfo.memberGrade != GRADE_MASTER)
            return;

        Protocols.GuildMember.MemberData member = memberList.Find(list => list.uno.ToString() == unoText);
        if (member == null || member.uno.ToString() == localUser.uno)
            return;

        MemberAction action = MemberAction.None;
        UIGuildMemberInfoPopup popup = UISimplePopup.Create<UIGuildMemberInfoPopup>("GuildMemberInfoPopup");
        ShowMemberMenu(popup, member);

        popup.onClick = popupSender =>
        {
            string popupKey = popupSender.name;

            if (popupKey == "OK")
            {
                popup.Close();
                RequestMemberAction(action, member.uno);
            }
            else if (popupKey == "Cancel")      // 확인 화면에서 취소하면 관리 메뉴로 돌아간다
            {
                action = MemberAction.None;
                ShowMemberMenu(popup, member);
            }
            else if (popupKey == "Btn-1")
            {
                action = MemberAction.Deport;
                ShowMemberConfirm(popup, member, Localization.Get("110061"), Localization.Get("110062"), Localization.Get("110061"), Localization.Get("1000"));
            }
            else if (popupKey == "Btn-2")
            {
                action = MemberAction.DelegateMaster;
                ShowMemberConfirm(popup, member, Localization.Get("110057"), Localization.Get("110058"), Localization.Get("110060"), Localization.Get("1000"));
            }
            else if (popupKey == "Btn-3")
            {
                if (member.memberGrade == GRADE_MEMBER)
                {
                    action = MemberAction.AppointSubMaster;
                    ShowMemberConfirm(popup, member, "110093", "110097", "110060", "1000");
                }
                else
                {
                    action = MemberAction.FireSubMaster;
                    ShowMemberConfirm(popup, member, "110101", "110104", "110111", "1000");
                }
            }
        };
    }

    /// <summary>관리 메뉴 화면: 퇴출 / 길드장 위임 / 보좌관 임명(보좌관이면 해임) 버튼</summary>
    private void ShowMemberMenu(UIGuildMemberInfoPopup popup, Protocols.GuildMember.MemberData member)
    {
        popup.Set(true, Localization.Get("110052"), null, null, null, null, Localization.Get("1001"));
        popup.SetInfo(member.name, member.world, member.thumnail, member.level, Utility.GetTimeString(member.lastTime),
            Localization.Get("110061"),     // 퇴출
            Localization.Get("110054"),     // 길드장 위임
            member.memberGrade == GRADE_MEMBER ? Localization.Get("110092") : Localization.Get("110100"));   // 보좌관 임명 / 해임
    }

    /// <summary>선택한 관리 동작의 확인 화면</summary>
    private void ShowMemberConfirm(UIGuildMemberInfoPopup popup, Protocols.GuildMember.MemberData member,
        string title, string message, string okLabel, string cancelLabel)
    {
        popup.Set(true, title, message, null, okLabel, cancelLabel, null);
        popup.SetInfo(member.name, member.world, member.thumnail, member.level, Utility.GetTimeString(member.lastTime), null, null, null);
    }

    private void RequestMemberAction(MemberAction action, int uno)
    {
        switch (action)
        {
            case MemberAction.Deport:
                network.RequestDeportGuildMember(uno);
                break;
            case MemberAction.DelegateMaster:
                network.RequestDelegatingGuild(uno);
                break;
            case MemberAction.AppointSubMaster:
                network.RequestAppointSubMaster(uno);
                break;
            case MemberAction.FireSubMaster:
                network.RequestFireSubMaster(uno);
                break;
        }
    }

    #endregion

    /// <summary>가입 여부에 따라 길드 메인 또는 길드 목록을 다시 그리고, 열려 있는 파견/상점 팝업도 갱신한다.</summary>
    public override void OnRefresh()
    {
        base.OnRefresh();
		UISetter.SetActive(guildBoardBtn, localUser.IsExistGuild());
		if (localUser.IsExistGuild())
		{
			InitAndOpenGuildInfo(memberList);
		}
		else
		{
			if (uiGuildState == GuildTab.Join) InitAndOpenGuildList(guildList);
		}
		if(dispatch != null)
		{
			dispatch.Set();
		}
		if(secretShop != null)
		{
			secretShop.OnRefresh();
		}
    }
    
#region 길드 미가입: 가입 / 생성
    /// <summary>길드 목록 화면으로 열기. 생성 옵션(최소 레벨, 엠블럼)도 초기값으로 되돌린다.</summary>
    public void InitAndOpenGuildList(List<RoGuild> list)
    {
        uiGuildState = GuildTab.Join;
        UISetter.SetActive(guildNoticeRoot, false);
        UISetter.SetActive(guildJoinCreate, true);
        UISetter.SetActive(guildMain, false);

        InitGuildList(list);

        if (guildListView != null)guildListView.scrollView.SetDragAmount(0, 0, false);

        minLevel = localUser.FindBuilding(EBuilding.Guild).firstLevelReg.userLevel;
        emblemIdx = 0;
        
        Open();
    }

    /// <summary>가입 가능 길드 목록에서 제거한다. (폐쇄 신청된 길드 등)</summary>
    public void RemoveGuildList(int gidx)
    {
        RoGuild guild = guildList.Find(row => row.gidx == gidx);
        if (guild != null)
        {
            guildList.Remove(guild);
            OnRefresh();
        }
    }

    public void InitGuildList(List<RoGuild> list)
    {
        if (list != null)
        {
            guildList = list;
            SetGuildList();
        }        
    }

    /// <summary>길드 목록 표시. 내가 가입 신청한 길드를 맨 위로 올린다.</summary>
    public void SetGuildList()
    {
        if (uiGuildState == GuildTab.Join)
        {
            if (guildListView != null)
            {
                //내가 가입신청한 길드가 최상단으로
                RoGuild reqItem;
                for (int i = 0; i < guildList.Count; i++)
                {
                    if (guildList[i].list == "req")
                    {
                        reqItem = guildList[i];
                        guildList.RemoveAt(i);
                        guildList.Insert(0, reqItem);
                        break;
                    }
                }

                guildListView.Init(guildList, guildItemIdPrefix);
            }
        }
        SetTab();
    }

    /// <summary>가입 신청 상태가 바뀐 길드를 갱신한다.</summary>
    public void ChangeGuildItemState(int idx, string listType)
    {
        RoGuild guild = guildList.Find(list => list.gidx == idx);

        if (guild != null)
        {
            guild.list = listType;
            SetGuildList();
        }
    }

    /// <summary>가입 조건(자유 가입 / 신청 후 승인)이 바뀐 길드를 갱신한다.</summary>
    public void ChangeGuildItemType(int idx, int type)
    {
        RoGuild guild = guildList.Find(list => list.gidx == idx);

        if (guild != null)
        {
            guild.gtyp = type;
            SetGuildList();
        }
    }

    /// <summary>가입 / 생성 탭 전환</summary>
    private void SetTab()
    {
        UISetter.SetFlipSwitch(join, uiGuildState == GuildTab.Join);
        UISetter.SetFlipSwitch(create, uiGuildState == GuildTab.Create);

        UISetter.SetActive(joinGuild, uiGuildState == GuildTab.Join);
        UISetter.SetActive(createGuild, uiGuildState == GuildTab.Create);

        if (uiGuildState == GuildTab.Create)
            SetCreateGuild();
    }

    /// <summary>길드 생성 옵션(가입 최소 레벨, 엠블럼, 생성 비용) 표시</summary>
    private void SetCreateGuild()
    {
        UISetter.SetLabel(minLevelLabel, "Lv "+minLevel);
        UISetter.SetSpriteWithSnap(selectEmblem, "union_mark_" + string.Format("{0:00}", emblemIdx), true);
        UISetter.SetLabel(createCostLabel, guildCreateCost);
    }

    /// <summary>
    /// 길드 생성: 다이아 → 이름 길이(2~10자) → 사용 가능 문자 순서로 검사한 뒤 확인 팝업을 띄운다.
    /// </summary>
    private void CreateGuild()
    {
        string name = createGuildName.value;
        int joinType = toggleFree.value ? 1 : 2;   // 1: 자유 가입, 2: 신청 후 승인

        if (localUser.cash < guildCreateCost)
        {
            NotEnough(MultiplePopUpType.NOTENOUGH_CASH);
        }
        else if (string.IsNullOrEmpty(name) || name.Length < 2 || name.Length > 10)
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("80030"));
        }
        else if (!Utility.PossibleChar(name))
        {
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("7144"));
        }
        else
        {
            UIGuildCreateConfirmPopup recv = UISimplePopup.Create<UIGuildCreateConfirmPopup>("GuildCreateConfirmPopup");
            recv.Set(true, Localization.Get("110023"), string.Format(Localization.Get("110024"), guildCreateCost),
                null, Localization.Get("110026"), Localization.Get("1000"), null);
            recv.SetInfo(name, emblemIdx, minLevel, joinType, guildCreateCost);
            recv.onClick = popupSender =>
            {
                string popupKey = popupSender.name;
                if (popupKey == "OK")
                {
                    network.RequestCreateGuild(name, joinType, minLevel, emblemIdx);
                    recv.ClosePopup();
                }
                else if (popupKey == "Close")
                {
                    recv.ClosePopup();
                }
            };
        }
    }
#endregion

#region 길드 게시판 / 파견
	/// <summary>게시판 페이지 데이터를 표시한다. 게시판 팝업이 없으면 먼저 만들고, 새 글 뱃지를 끈다.</summary>
	public void OpenGuildBoard(int page, int maxPage, List<Protocols.GuildBoardData> list)
	{
		if(guildBoard == null)
		{
			guildBoard = UIPopup.Create<UIGuildBoard>("GuildBoard");
			guildBoard.InitAndOpenGuildBoard();
		}
		guildBoard.SetChattingData(page, maxPage, list);
		localUser.badgeGuild = false;
		UISetter.SetActive(guildBoardBadge, false);
		UIManager.instance.RefreshOpenedUI();
	}

	public void CloseGuildBoard()
	{
		if(guildBoard != null)
			guildBoard.Close();
	}

	/// <summary>전투 파견 팝업. 처음 한 번만 프리팹을 생성해 contentsRoot 아래에 붙이고, 이후에는 재사용한다.</summary>
	public void OpenDispatchPopup()
	{
        if (dispatch != null && dispatch.isActive)
            return;

        if(dispatch == null)
        {
            GameObject go = GameObject.Instantiate(dispatchPrefab) as GameObject;
            dispatch = go.GetComponent<UIGuildTroopDispatchPopup>();
            dispatch.transform.parent = contentsRoot.transform;
            dispatch.transform.localScale = new Vector3(1, 1, 1);
            dispatch.transform.localPosition = new Vector3(0, 0, 0);
        }
        dispatch.Init();
	}

	public void CloseDispatchPopup()
	{
		if(dispatch != null)
		{
			dispatch.ClosePopup();
		}
	}

#endregion

#region 점령전
	/// <summary>
	/// 점령전 상태를 반영한다. 참여 가능한 길드면 신청 버튼을 화면 밖(x = -800)에서 밀어 넣고,
	/// 남은 시간이 끝나면 점령전 맵을 닫고 상태를 다시 요청한다.
	/// </summary>
	public void SetConquestState(Protocols.ConquestInfo info)
	{
		conquestInfo = info;
		int conquestEnableLevel = int.Parse(regulation.defineDtbl["GUILD_OCCUPY_LEVEL"].value);
		localUser._conquestTeam = info.side;

		UISetter.SetActive(conquestJoinBtn, localUser.guildInfo.occupy == 1);
		UISetter.SetActive(conquestBtn, localUser.guildInfo.occupy == 1);

		SetConquestStateLabel();

		if(localUser.guildInfo.occupy == 0 || localUser.guildInfo.level < conquestEnableLevel)
			return;

		if(conquestJoinBtn.transform.localPosition.x == -800)
		{
			iTween.MoveTo(conquestJoinBtn, iTween.Hash("x", -465,
				"islocal", true,
				"time", 0.2f,
				"delay", 1.0f,
				"easeType", iTween.EaseType.linear));
		}
			
		if(conquestInfo.remain > 0)
		{
			conquestTime = TimeData.Create();
			conquestTime.SetByDuration(conquestInfo.remain);
			UISetter.SetTimer(conquestTimer, conquestTime);
			conquestTimer.SetLabelFormat("", Localization.Get("5821"));
			conquestTimer.RegisterOnFinished(ui=>{
				if(UIManager.instance.world.existConquestMap && UIManager.instance.world.conquestMap.isActive)
				{
					if(conquestInfo.state == EConquestState.Join)
						NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110349"));
					else if(conquestInfo.state == EConquestState.Setting)
						NetworkAnimation.Instance.CreateFloatingText(Localization.Format("110350"));
					UIManager.instance.world.conquestMap.ClosePopUp();
				}
				network.RequestGetConquestInfo();
			});
		}
		if(localUser.lastConquestReplayPoint != 0)
			CreateConquestHistoryPopup(localUser.lastConquestReplayPoint);
	}

	/// <summary>점령전 단계(없음/신청/매칭/배치/전투)와 신청 여부에 맞는 안내 문구를 표시한다. 미신청이면 운영진에게 뱃지를 띄운다.</summary>
	public void SetConquestStateLabel()
	{
		UISetter.SetActive(conquestBadge, false);
		switch(conquestInfo.state)
		{
		case EConquestState.None:
				UISetter.SetLabel(conquestStateLabel, Localization.Get("110359"));
			break;
		case EConquestState.Join:
			{
				if(conquestInfo.sign == 0)
				{
					if(localUser.guildInfo.memberGrade == GRADE_MEMBER)
						UISetter.SetLabel(conquestStateLabel, Localization.Get("110354"));
					else
					{
						UISetter.SetActive(conquestBadge, true);
						UISetter.SetLabel(conquestStateLabel, Localization.Get("110309"));
					}
				}
				else
					UISetter.SetLabel(conquestStateLabel, Localization.Get("110355"));
			}
			break;
		case EConquestState.Match:
			{
				if(conquestInfo.sign == 0)
					UISetter.SetLabel(conquestStateLabel, Localization.Get("110116"));
				else
					UISetter.SetLabel(conquestStateLabel, Localization.Get("110356"));
			}
			break;
		case EConquestState.Setting:
			if(conquestInfo.sign == 0)
				UISetter.SetLabel(conquestStateLabel, Localization.Get("110116"));
			else
				UISetter.SetLabel(conquestStateLabel, Localization.Get("110357"));
			break;
		case EConquestState.Battle:
				UISetter.SetLabel(conquestStateLabel, Localization.Get("110358"));
			break;
		}
	}

	/// <summary>점령전 신청: 신청 기간에 아직 신청하지 않았을 때, 운영진(길드장/보좌관)만 신청할 수 있다.</summary>
	void OnConquestJoinBtnClicked()
	{
		if (conquestInfo.state != EConquestState.Join || conquestInfo.sign != 0)
			return;

		if (localUser.guildInfo.memberGrade == GRADE_MEMBER)
			NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110360"));
		else
			network.RequestConquestJoin();
	}

	/// <summary>점령전 버튼: 단계와 신청/참여 여부에 따라 랭킹 보기, 배치 화면 진입, 안내 문구 중 하나를 처리한다.</summary>
	void OnConquestBtnClicked()
	{
		int conquestEnableLevel = int.Parse(regulation.defineDtbl["GUILD_OCCUPY_LEVEL"].value);

		if(localUser.guildInfo.level < conquestEnableLevel)
		{
			NetworkAnimation.Instance.CreateFloatingText(Localization.Format("110344", conquestEnableLevel));
			return;
		}

		if(conquestInfo == null)
		{
			NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110366"));
			Close();
			return;
		}

		switch(conquestInfo.state)
		{
		case EConquestState.None:
		case EConquestState.Join:
		case EConquestState.Match:
			network.RequestGetGuildRanking(1);
			break;
		case EConquestState.Setting:
			if(conquestInfo.join == 0)
			{
				network.RequestGetGuildRanking(1);
				NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110314"));
			}
			else if(conquestInfo.sign == 0 && conquestInfo.join == 1)
			{
				network.RequestGetGuildRanking(1);
				NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110315"));
			}
			else if(conquestInfo.sign == 1 && conquestInfo.join == 1)
				network.RequestGetConquestTroop();
			break;
		case EConquestState.Battle:
			NetworkAnimation.Instance.CreateFloatingText(Localization.Get("110313"));
			break;
		}
	}

	/// <summary>점령전 정보를 받지 못했을 때 점령전 버튼을 숨긴다.</summary>
	public void SetConquestError()
	{
		UISetter.SetActive(conquestBtn, false);
		UISetter.SetActive(conquestJoinBtn, false);
	}

	/// <summary>점령전 기록(랭킹) 팝업을 열고 목록을 표시한다.</summary>
	public void CreateConquestHistoryPopup(int type, List<Protocols.GuildRankingInfo> list)
	{
		if(historyPopup == null)
		{
			historyPopup = UIPopup.Create<UIConquestHistoryPopup>("ConquestHistoryPopup");
			historyPopup.Init();
		}
		historyPopup.SetRankingList(type, list);
	}

	/// <summary>지난 점령전 결과(보상 포인트)가 있으면 기록 팝업을 한 번 띄운다.</summary>
	public void CreateConquestHistoryPopup(int point)
	{
		localUser.lastConquestReplayPoint = 0;
		if(historyPopup == null)
		{
			historyPopup = UIPopup.Create<UIConquestHistoryPopup>("ConquestHistoryPopup");
			historyPopup.Init(point);
		}
	}
#endregion

#region 길드 가입 상태: 길드 메인
    /// <summary>길드 메인 화면으로 열기</summary>
    public void InitAndOpenGuildInfo(List<Protocols.GuildMember.MemberData> list)
    {
        uiGuildState = GuildTab.Main;
        UISetter.SetActive(guildNoticeRoot, true);
        UISetter.SetActive(guildJoinCreate, false);
        UISetter.SetActive(guildMain, true);

        InitGuildMemberList(list);
        
        SetGuildInfo();
        
        Open();
    }

    /// <summary>
    /// 길드 정보 표시. 폐쇄 신청 중이면 공지 대신 폐쇄 예정 시각을 보여준다.
    /// 등급별 버튼: 길드장 = 관리, 길드원·보좌관 = 스킬 보기, 길드장·보좌관 = 가입 승인·공지 수정
    /// </summary>
    public void SetGuildInfo()
    {
        if (localUser.guildInfo.state == 1)
        {
            UISetter.SetLabel(guildNotice, Localization.Get("110242") + "\n" + Localization.Get("110243") + CloseTimeString(localUser.guildInfo.closeTime));
        }
        else
        {
            if (string.IsNullOrEmpty(localUser.guildInfo.notice))
            {
                UISetter.SetLabel(guildNotice, Localization.Get("112013"));
            }
            else
            {
                UISetter.SetLabel(guildNotice, localUser.guildInfo.notice);
            }            
        }

        SetMemberCountLabel(localUser.guildInfo.count);
        UISetter.SetLabel(guildName, localUser.guildInfo.name);
        UISetter.SetLabel(guildWorld, Localization.Format("19067",localUser.guildInfo.world));
        UISetter.SetLabel(guildLevel, "Lv " + localUser.guildInfo.level);
        UISetter.SetLabel(guildPoint, localUser.guildInfo.point);
        UISetter.SetSpriteWithSnap(guildEmblem, "union_mark_" + string.Format("{0:00}", localUser.guildInfo.emblem), true);
        UISetter.SetActive(btnManage, localUser.guildInfo.memberGrade == GRADE_MASTER);
        UISetter.SetActive(btnSkill, localUser.guildInfo.memberGrade != GRADE_MASTER);
        UISetter.SetActive(btnApprove, localUser.guildInfo.memberGrade != GRADE_MEMBER);
        UISetter.SetActive(btnModify, localUser.guildInfo.memberGrade != GRADE_MEMBER);
    }

    /// <summary>길드원 수 표시 (현재 / 최대)</summary>
    private void SetMemberCountLabel(int count)
    {
        UISetter.SetLabel(guildCount, string.Format("[FF6000FF]{0}[-][4E1F00FF]/{1}[-]", count, localUser.guildInfo.maxCount));
    }

    /// <summary>길드원 목록 초기화. 마지막으로 고른 정렬 기준을 불러온다.</summary>
    public void InitGuildMemberList(List<Protocols.GuildMember.MemberData> list)
    {
        if (list != null)
        {
            sortType = (ESortType)PlayerPrefs.GetInt("GuildMemberSortType", (int)ESortType.Level);

            memberList = list;
            SetMemberList();
        }       
    }

    /// <summary>
    /// 길드원 목록 정렬 후 표시.
    /// 선택한 기준(레벨 / 최근 접속 / 포인트)으로 정렬한 뒤, 길드장을 맨 위에, 보좌관들을 그 바로 아래에 둔다.
    /// </summary>
    private void SetMemberList()
    {
        if (uiGuildState != GuildTab.Main || guildMemberListView == null)
            return;

        memberList.Sort(delegate (Protocols.GuildMember.MemberData row, Protocols.GuildMember.MemberData row1)
        {
            if (sortType == ESortType.Time)
                return row.lastTime.CompareTo(row1.lastTime);
            if (sortType == ESortType.PBPoint)
                return row1.paymentBonusPoint.CompareTo(row.paymentBonusPoint);
            return row1.level.CompareTo(row.level);   // 기본: 레벨 높은 순
        });

        // 길드장을 맨 위로
        for (int i = 0; i < memberList.Count; i++)
        {
            if (memberList[i].memberGrade == GRADE_MASTER)
            {
                MoveMember(i, 0);
                break;
            }
        }

        // 보좌관(여러 명 가능)은 길드장 바로 아래부터
        int subMasterIdx = 1;
        for (int i = 0; i < memberList.Count; i++)
        {
            if (memberList[i].memberGrade == GRADE_SUB_MASTER)
                MoveMember(i, subMasterIdx++);
        }

        guildMemberListView.Init(memberList, memberItemIdPrefix);

        if (sortType == ESortType.Time) UISetter.SetLabel(sortName, Localization.Get("1053"));
        else if (sortType == ESortType.Level) UISetter.SetLabel(sortName, Localization.Get("1034"));
        else if (sortType == ESortType.PBPoint) UISetter.SetLabel(sortName, Localization.Get("21011"));
    }

    private void MoveMember(int from, int to)
    {
        Protocols.GuildMember.MemberData item = memberList[from];
        memberList.RemoveAt(from);
        memberList.Insert(to, item);
    }

    /// <summary>새 길드원을 목록에 추가한다.</summary>
    public void AddMemberList(Protocols.GuildMember.MemberData member)
    {
        memberList.Add(member);
        SetMemberCountLabel(memberList.Count);
        SetMemberList();
    }

    /// <summary>탈퇴/퇴출된 길드원을 목록에서 제거한다.</summary>
    public void RemoveMemberList(int uno)
    {
        Protocols.GuildMember.MemberData member = memberList.Find(list => list.uno == uno);
        if (member != null)
        {
            memberList.Remove(member);
            SetMemberCountLabel(memberList.Count);
            SetMemberList();
        }
    }

    /// <summary>길드장 위임 결과 반영: 나는 길드원으로, 대상은 길드장으로 바꾼다.</summary>
    public void DelegateGuildMaster(int uno)
    {
        Protocols.GuildMember.MemberData my = memberList.Find(list => list.uno.ToString() == localUser.uno);
        if (my != null)
        {
            my.memberGrade = GRADE_MEMBER;
        }

        Protocols.GuildMember.MemberData member = memberList.Find(list => list.uno == uno);
        if (member != null)
        {
            member.memberGrade = GRADE_MASTER;
        }
    }

    /// <summary>보좌관 임명/해임 결과 반영</summary>
    public void AppointSubMaster(bool bAppoint, int uno)
    {
        Protocols.GuildMember.MemberData member = memberList.Find(list => list.uno == uno);
        if (member != null)
        {
            member.memberGrade = bAppoint ? GRADE_SUB_MASTER : GRADE_MEMBER;
        }
    }

    #endregion

    /// <summary>재화(다이아 / 골드) 부족 안내 팝업</summary>
    private void NotEnough(MultiplePopUpType type)
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
    }

    /// <summary>서버 시간(Unix 초)을 한국 시간(UTC+9) 문자열로 바꾼다.</summary>
    public string CloseTimeString(double dTime)
    {
        var time = new System.DateTime(1970, 1, 1, 0, 0, 0, 0).AddHours(9);
        return time.AddSeconds((long)(dTime)).ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>퇴장 연출(0.8초)이 끝나면 실제로 닫고, 다음에 열 때를 위해 상태를 되돌린다.</summary>
    IEnumerator OnEventHidePopup()
    {
        yield return new WaitForSeconds(0.8f);

        base.Close();

        bBackKeyEnable = false;
        bEnterKeyEnable = false;

		conquestJoinBtn.transform.localPosition = new Vector3(-800, -147, 0);

		if(secretShop != null)
		{
			secretShop.Close();
		}
    }

    void HidePopup()
    {
        OnAnimClose();

        StartCoroutine(OnEventHidePopup());
    }

    /// <summary>배경 / NPC / 정보 / 타이틀 패널 등장 연출 (GEAnim)</summary>
    void OnAnimOpen()
    {
        this.AnimBG.Reset();
        this.AnimNpc.Reset();
        this.AnimInfo.Reset();
        this.AnimTitle.Reset();
        this.AnimBlock.Reset();

        UIManager.instance.SetPopupPositionY(gameObject);

		this.AnimBG.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimNpc.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimInfo.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimTitle.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimBlock.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);

    }

    void OnAnimClose()
    {
		this.AnimBG.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimNpc.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimInfo.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimTitle.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimBlock.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);

    }

    /// <summary>열기. bEnterKeyEnable로 연출 중 중복 열기를 막는다.</summary>
    public override void Open()
    {
        if (bEnterKeyEnable == false)
        {
            bEnterKeyEnable = true;

            base.Open();
            if (guildListView != null)
                guildListView.scrollView.SetDragAmount(0, 0, false);

            if (guildMemberListView != null)
                guildMemberListView.scrollView.SetDragAmount(0, 0, false);
            this.OnAnimOpen();
        }
    }

    /// <summary>닫기. 길드에서 연 하위 화면(점령전 맵, 협동 전투, 게시판)을 함께 닫고 퇴장 연출을 시작한다.</summary>
    public override void Close()
    {
        bBackKeyEnable = true;
		if(UIManager.instance.world.existConquestMap && UIManager.instance.world.conquestMap.isActive)
			UIManager.instance.world.conquestMap.ClosePopUp();

        if (UIManager.instance.world.existCooperateBattle
            && UIManager.instance.world.cooperateBattle.isActive)
        {
            UIManager.instance.world.cooperateBattle.ClosePopup();
        }

		CloseGuildBoard();
		
        this.HidePopup();
    }

    /// <summary>등장 연출이 끝나면 터치 입력을 다시 켠다.</summary>
    public void AnimOpenFinish()
    {
        UIManager.instance.EnableCameraTouchEvent(true);
    }

    // 최상단 팝업일 때만 NPC 보이스를 켠다
    protected override void OnEnablePopup()
    {
        UISetter.SetVoice(spineAnimation, true);
    }

    protected override void OnDisablePopup()
    {
        UISetter.SetVoice(spineAnimation, false);
    }
}
