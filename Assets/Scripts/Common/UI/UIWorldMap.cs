using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Shared.Regulation;

/// <summary>
/// 월드맵(스테이지 선택) 화면.
/// 챕터별 스테이지 목록을 표시하고, 스테이지를 누르면 전투 준비 화면으로 넘어간다.
/// 이전/다음 챕터 이동, 챕터 별 보상(지휘관), 탐사 팝업, 챕터 BGM을 처리한다.
/// 챕터 정보는 서버에 요청한 뒤 응답에서 _Set()으로 그린다.
/// </summary>
public class UIWorldMap : UIPopup
{
    public UITexture background1;
	public UITexture background2;
    public UIDefaultListView stageListView;

	public UIScrollView stageScrollView;

    public GameObject previousMapButton;
	public GameObject previousMapBadge;
    public GameObject nextMapButton;
	public GameObject nextMapBadge;
    public GameObject nextMapLock;

	public UILabel worldName;

	public string selectStageId;

	// 챕터 별 보상 (별을 모두 모으면 받는 지휘관)
	public UILabel commanderRate;
	public UILabel commanderName;
	public UISprite commanderSprite;
	public GameObject commanderRewardBtn;
	public GameObject commanderRewardStarRoot;
	public GameObject commanderRewardClearRoot;
	public TweenScale tweenScale;
	public UIPlayTween tween;

	// 화면 전환(페이드) 연출
	public TweenAlpha inTweener = null;
	public TweenAlpha outTweener = null;

    public string currentWorldMapId { get; private set; }
	private const string tutorialWorldMapId = "0";

	// 스테이지 맵은 가로로 두 화면 크기이며, 진행 중인 스테이지가 있는 쪽으로 스크롤해 연다
	private const float MapPageWidth = 1280f;

	// 연출 중 중복 열기 / 닫기 방지
	bool bBackKeyEnable = false;
	bool bEnterKeyEnable = false;

	[HideInInspector]
	public UIGetCommander getCommander;

    private UIExplorationPopup _explorationPopup;
    public GameObject explorationBadge;

    public RoWorldMap currentWorldMap
    {
        get { return localUser.FindWorldMap(currentWorldMapId); }
    }

    public override void OnClick(GameObject sender)
    {
        base.OnClick(sender);
        string key = sender.name;
        if (stageListView.Contains(key))
        {
			selectStageId = stageListView.GetPureId(key);
			var stage = currentWorldMap.FindStage(selectStageId);
			StartStage(stage, sender);
        }
        else if (key == "PreviousMap")
        {
            var worldMap = localUser.FindWorldMapByOffset(currentWorldMapId, -1);
            MoveWorldMap(worldMap.id);
        }
        else if (key == "NextMap")
        {
            MoveNextWorldMap();
        }
        else if (key == "CommanderReward")
        {
			GetCommanderReward();
        }
        else if (key == "Exploration")
        {
            if (_explorationPopup == null)
            {
                UISetter.SetActive(explorationBadge, false);
                _explorationPopup = UIPopup.Create<UIExplorationPopup>("ExplorationPopup");
            }
        }
        else
        {
            Debug.LogError("NO : " + key);
        }
    }

	/// <summary>
	/// 다음 챕터로 이동. 마지막 챕터면 안내 팝업,
	/// 이동할 수 없으면 원인(현재 챕터 미클리어 / 레벨 부족)을 안내한다.
	/// </summary>
	private void MoveNextWorldMap()
	{
		RoWorldMap currWorldMap = currentWorldMap;
		RoWorldMap nextWorldMap = localUser.FindWorldMapByOffset(currentWorldMapId, 1);
		if (nextWorldMap == null)
		{
			UISimplePopup.CreateOK(true, "5386", "6042", "6037", "1001");
			return;
		}

		if (currWorldMap.canMoveNextMap)
		{
			MoveWorldMap(nextWorldMap.id);
		}
		else if (currWorldMap.stageCount > currWorldMap.clearStageCount)
		{
			NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Format("6039"));
		}
		else if (nextWorldMap.dataRow.unlockUserLevel > localUser.level)
		{
			NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Format("99999", nextWorldMap.dataRow.unlockUserLevel));
		}
	}

	/// <summary>챕터 별 보상 팝업 (보상 지휘관 획득)</summary>
	public void GetCommanderReward()
	{
		if (getCommander == null)
		{
			getCommander = UIPopup.Create<UIGetCommander>("GetCommander");
			getCommander.Init(currentWorldMap);
		}
	}

	/// <summary>
	/// 스테이지 진입. 열린 전투 스테이지면 적 정보로 전투 데이터를 만들어 전투 준비 화면을 연다.
	/// 초소/창고는 도전 횟수가 남아 있을 때만 들어갈 수 있다.
	/// </summary>
	/// <param name="sender">버튼으로 눌렀을 때만 안내 문구를 띄운다 (자동 진입은 null)</param>
	public void StartStage(RoWorldMap.Stage _stage, GameObject sender = null)
	{
		EStageTypeIdRange type = _stage.data.type;
		if (_stage.isOpen && type != EStageTypeIdRange.PowerPlant)
		{
			bool isBuilding = type == EStageTypeIdRange.GuardPost || type == EStageTypeIdRange.Storage;
			if (isBuilding && !_stage.canBattle)
				return;

			BattleData data = BattleData.Create(EBattleType.Plunder);
			data.defender = _stage.GetEnemy();
			data.stageId = _stage.id;
			data.worldId = currentWorldMapId;
			data.IsCleared = int.Parse(_stage.id) <= localUser.lastClearStage;
			uiWorld.readyBattle.InitAndOpenReadyBattle(data);
		}
		else if (!_stage.canBattle && sender != null)
		{
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("5351"));
		}
		else if (!_stage.isOpen && sender != null)
		{
            NetworkAnimation.Instance.CreateFloatingText(new Vector3(0, -0.5f, 0), Localization.Get("5767"));
        }
		else
		{
            Debug.LogError("type = " + type);
        }
	}

	/// <summary>전투 결과 화면의 "다음 스테이지" 버튼: 다음 스테이지로 바로 진입한다.</summary>
	public void NextStage(string _id)
	{
		var stage = currentWorldMap.FindNextStage(_id);
		StartStage(stage);
	}

    public override void OnRefresh()
    {
        base.OnRefresh();
		RefreshWorldMapReward();
    }

    /// <summary>
    /// 서버에서 받은 챕터 정보로 화면을 그린다.
    /// 배경, 스테이지 목록, 이전/다음 버튼, 스크롤 위치(진행 중인 스테이지가 있는 페이지), 별 보상 상태를 설정한다.
    /// </summary>
    public void _Set(RoWorldMap worldMap)
    {
        if (worldMap == null) {
            Debug.LogError("invalid state");
            return;
        }
		tweenScale.enabled = false;
        currentWorldMapId = worldMap.id;
        UISetter.SetLabel(title, worldMap.name);
		UISetter.SetActive(previousMapButton, worldMap.canMovePreviousMap);
		UISetter.SetActive(nextMapButton, !worldMap.id.Equals(tutorialWorldMapId));
        UISetter.SetActive(nextMapLock, !worldMap.canMoveNextMap);
        UISetter.SetActive(explorationBadge, localUser.explorationDtbl.hasCompleteState);

        var background = Utility.LoadTexture(worldMap.dataRow.backgroundImageId);
        UISetter.SetTexture(background1, background);
		UISetter.SetTexture(background2, background);

		ScrollToCurrentPage();

        stageListView.Init(worldMap.stageList, "Stage-");

		// 별을 모두 모았는데 보상을 받지 않았으면 보상 버튼을 강조
		if (worldMap.starCount >= worldMap.maxStarCount && !worldMap.rwd)
			tweenScale.PlayForward();

		UISetter.SetLabel(UIManager.instance.world.mainCommand.worldName, Localization.Get(worldMap.name));

		// 다음에 도전할 스테이지가 이 챕터에 있으면 선택 표시
		int nextStageId = localUser.lastClearStage + 1;
		if (localUser.FindWorldMapByStage(nextStageId.ToString()) != null)
			stageListView.SetSelection(nextStageId.ToString(), true);

		stageScrollView.enabled = !worldMap.id.Equals(tutorialWorldMapId);
		RefreshWorldMapReward();
    }

	/// <summary>
	/// 스크롤을 처음으로 되돌린 뒤, 진행 중인 스테이지가 오른쪽 페이지에 있으면 한 페이지 이동한다.
	/// 마지막으로 플레이한 스테이지(또는 다음 도전 스테이지)가 기준이며, 그 외에는 튜토리얼 챕터만 왼쪽 페이지를 보여준다.
	/// </summary>
	private void ScrollToCurrentPage()
	{
		stageScrollView.MoveRelative(-stageScrollView.transform.localPosition);

		int targetStage = localUser.lastPlayStage != -1 ? localUser.lastPlayStage : localUser.lastClearStage + 1;
		bool isProgressChapter = localUser.lastPlayStage != -1
			|| (localUser.FindWorldMapByStage((targetStage + 1).ToString()) != null
				&& int.Parse(currentWorldMap.id) == int.Parse(localUser.FindLastOpenedWorldMap().id));

		bool moveToSecondPage = isProgressChapter
			? (targetStage - 4) % 20 >= 10
			: int.Parse(currentWorldMap.id) != 0;

		if (moveToSecondPage)
			stageScrollView.MoveRelative(new Vector3(-MapPageWidth, 0, 0));

		stageScrollView.UpdatePosition();
		localUser.lastPlayStage = -1;
	}

	/// <summary>챕터 별 보상 영역 표시 (모은 별 / 최대 별, 보상 지휘관, 수령 여부)</summary>
	public void RefreshWorldMapReward()
	{
		CommanderDataRow commander = regulation.commanderDtbl[currentWorldMap.dataRow.c_idx];
		UISetter.SetLabel(commanderRate, string.Format("{0}/{1}", currentWorldMap.starCount, currentWorldMap.maxStarCount));
		UISetter.SetLabel(commanderName, commander.nickname);
		UISetter.SetSprite(commanderSprite, commander.worldMapRewardId);
		UISetter.SetActive(commanderRewardStarRoot, !currentWorldMap.rwd);
		UISetter.SetActive(commanderRewardClearRoot, currentWorldMap.rwd);
		UISetter.SetScale(commanderRewardStarRoot, new Vector3(1, 1, 1));
	}

	public void StartRewardAnimation()
	{
		tween.resetOnPlay = true;
		tween.tweenGroup = 1;
		tween.Play(true);
	}

	public void EndRewardAnimation()
	{
		tween.resetOnPlay = true;
		tween.tweenGroup = 2;
		tween.Play(true);
	}

#if ENABLE_TUTORIAL
	/// <summary>튜토리얼 진행 중이면 튜토리얼 챕터를 요청하고 true를 반환한다.</summary>
	private bool TryOpenTutorialMap()
	{
		if (localUser.tutorialStep >= 12)
			return false;

		network.RequestWorldMapInformation(localUser.FindWorldMap(tutorialWorldMapId).id);
		return true;
	}
#endif

    /// <summary>
    /// 월드맵 진입점. 챕터를 지정하지 않으면 마지막으로 열린 챕터를 연다.
    /// stage를 주면 서버 응답 후 StartStageNavigation()에서 해당 스테이지로 바로 진입한다.
    /// </summary>
    public void InitAndOpenWorldMap(string world = null, string stage = null)
    {
		if (bEnterKeyEnable)
			return;
		bEnterKeyEnable = true;

#if ENABLE_TUTORIAL
		if (TryOpenTutorialMap())
			return;
#endif
		RoWorldMap worldMap = string.IsNullOrEmpty(world)
			? localUser.FindLastOpenedWorldMap()
			: localUser.FindWorldMap(world);

		selectStageId = stage;
		network.RequestWorldMapInformation(worldMap.id);
    }

	/// <summary>다른 챕터로 이동 (서버에 챕터 정보 요청)</summary>
	public void MoveWorldMap(string world)
	{
		currentWorldMapId = world;
		selectStageId = null;
		network.RequestWorldMapInformation(world);
	}

	/// <summary>전투에서 돌아올 때: 마지막으로 플레이한 스테이지의 챕터를 연다.</summary>
	public void InitAndOpenLastStageWorldMap()
	{
		if (bEnterKeyEnable)
			return;
		bEnterKeyEnable = true;

#if ENABLE_TUTORIAL
		if (TryOpenTutorialMap())
			return;
#endif
		RoWorldMap worldMap = localUser.FindWorldMapByStage(localUser.lastPlayStage.ToString());
		network.RequestWorldMapInformation(worldMap.id);
	}

	public void InitAndOpenTutorialWorldMap()
	{
		if (bEnterKeyEnable)
			return;
		bEnterKeyEnable = true;

		RoWorldMap worldMap = localUser.FindWorldMap(tutorialWorldMapId);
		network.RequestWorldMapInformation(worldMap.id);
	}

	/// <summary>진입할 스테이지가 지정되어 있으면 바로 전투 준비 화면으로 이동한다. (다른 화면에서 바로가기)</summary>
	public void StartStageNavigation()
	{
		if (string.IsNullOrEmpty(selectStageId))
			return;

		var stage = currentWorldMap.FindStage(selectStageId);
		StartStage(stage);
		selectStageId = null;
	}

	/// <summary>챕터 BGM 재생. "Pocket_"으로 시작하면 사운드 포켓(묶음 리소스)에서 재생한다.</summary>
	public void SetBGM()
	{
		if (currentWorldMap.dataRow.bgm.StartsWith("Pocket_"))
			Cache.CacheManager.instance.SoundPocketCache.Create(currentWorldMap.dataRow.bgm);
		else
			Cache.CacheManager.instance.SoundCache.PlayBGM(Loading.WorldMap, currentWorldMap.dataRow.bgm);
	}

    public void CloseWorldMap()
    {
        uiWorld.worldMap.Close();
		uiWorld.camp.OpenCamp();
    }

    public override void Open()
    {
        base.Open();
        uiWorld.mainCommand.OnWorldMap();
		In();
    }

	/// <summary>닫기: 페이드 아웃 연출을 시작하고, 연출이 끝나면 CloseEnd()에서 실제로 닫는다.</summary>
	public override void Close()
	{
		if (bBackKeyEnable)
			return;

		bBackKeyEnable = true;
		Out();
		selectStageId = null;
	}

	private void In()
	{
		_Play(inTweener);
	}

	private void Out()
	{
		_Play(outTweener);
	}

	private void _Play(UITweener tweener)
	{
		tweener.enabled = true;
		tweener.ResetToBeginning();
		tweener.PlayForward();
	}

	/// <summary>페이드 아웃 완료 콜백: 실제로 닫고 캠프(로비) 화면으로 돌아간다.</summary>
	public void CloseEnd()
	{
		base.Close();
		uiWorld.camp.OpenCamp();
		bBackKeyEnable = false;
		bEnterKeyEnable = false;
	}
}
