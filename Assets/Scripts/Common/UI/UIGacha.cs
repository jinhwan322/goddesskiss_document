using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Shared.Regulation;
using Spine.Unity;

/// <summary>
/// 가챠(뽑기) 팝업.
/// 상단 배너로 가챠 종류를 고르고, 1회 / 10회 / 무료 뽑기를 한다.
/// - 뽑기 전 재화 확인(10회 뽑기 확인, 재화 부족 시 상점·획득처 안내)
/// - 획득 가능 보상과 확률 목록, 확정 보상(천장) 진행도
/// - 상자 개봉 연출: 보상 등급(일반 / 프리미엄 / 무지개)에 따라 상자 애니메이션과 효과음을 바꾼다
/// </summary>
public class UIGacha : UIPopup 
{
	public GEAnimNGUI AnimNpc;
	public GEAnimNGUI AnimNormal;
	public GEAnimNGUI AnimPremium;
	public GEAnimNGUI AnimTitle;
	public GEAnimNGUI AnimBlock;

	bool bBackKeyEnable  = false;
	bool bEnterKeyEnable = false;


    public UITimer premiumRemainTime;
    public UILabel premiumOnceCash;
    public UILabel premiumTenTimeCash;
	public UILabel premiumOnceCashLabel;
	public UILabel premiumTenTimeCashLabel;
	public UILabel premiumOnceCashFreeLabel;

    public UISprite premiumOnceCashIcon;
    public UISprite premiumTenTimeCashIcon;

    public GameObject premiumOnceButton;
	public GameObject premiumTenTimeButton;
    public GameObject premiumFreeOnceButton;

	public GameObject gachaRewardBtn;
	public UILabel gachaDescription;

    public TypeARewardList typeATargetList;
    public TypeBRewardList typeBTargetList;
    private GameObject targetList;
    public BoxOpenSingle singleBox;
	public BoxOpenMultiple multipleBox;

	public UIDefaultListView bannerListView;

    public UISpineAnimation spineAnimation;
	public UIProgressBar pilotProgress;
	public UILabel pilotProgressLabel;
	public GameObject effectRoot;

	public UILabel gachaName;
	public UISprite bigBanner;
	public GameObject gachaTimeRoot;

	GameObject infoPopUp;
	string selectType;     // 선택한 가챠 타입 (gachaDtbl 키)

	// 기본 가챠 타입 (그 외는 이벤트 가챠)
	private const string GachaTypeGold = "1";
	private const string GachaTypePremium = "2";

    void Start()
    {
        UISetter.SetSpine(spineAnimation, "n_012");
    }

	/// <summary>지휘관 획득 연출 팝업을 연다.</summary>
	public UICommanderComplete CreateCommanderComplete(CommanderCompleteType _type, string _idx)
	{
		UICommanderComplete uiComplete = UIPopup.Create<UICommanderComplete>("CommanderComplete");
		uiComplete.Init(_type, _idx);

		return uiComplete;
	}


    /// <summary>버튼 클릭 처리. 처리 후 항상 하위 파츠(보상 목록, 상자 연출)에도 전달한다.</summary>
    public override void OnClick(GameObject sender)
    {
        base.OnClick(sender);
        string key = sender.name;
        if (key == "Close")
        {
			if (bBackKeyEnable == false)
				this.Close();
        }
		else if (key == "OpenPremiumRewardList")     // 획득 가능 보상 목록 열기/닫기
		{
            if (targetList == null || !targetList.activeSelf)
				_OpenRewardList(selectType);
            else
                _CloseRewardList();
        }
		else if (key == "InfoBtn")
		{
			if (infoPopUp == null)
			{
				UISimplePopup popup = UISimplePopup.CreateOK("InformationPopup");
				popup.Set(true, "1911", "13028", "", "1001", "", "");
				infoPopUp = popup.gameObject;
			}
		}
		else if (key.StartsWith("Banner-"))           // 가챠 종류 선택
		{
			SelectGacha(key.Substring(key.IndexOf("-") + 1));
		}
        else if (key.StartsWith("PremiumFree-"))      // 무료 뽑기 (PremiumFree-횟수)
		{
			_RequestGachaOpenBox(selectType, _FindCount(key), true);
        }
		else if (key.StartsWith("Premium-"))          // 유료 뽑기 (Premium-횟수)
		{
			_RequestGachaOpenBox(selectType, _FindCount(key), false);
		}
		else if (key == "Close-BoxOpenSingle" || key == "Close-BoxOpenMultiple")   // 상자 연출이 끝나면 메인 화면 복귀
		{
			this.AnimNormal.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
			this.AnimNpc.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
			this.AnimPremium.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		}
		else if (key == "GachaBox")                   // 튜토리얼 용도
		{
			singleBox.Open();
		}

        SendOnClickToInnerParts(sender);
    }

	/// <summary>뽑기 요청. 유료 뽑기는 재화 확인을 먼저 거친다.</summary>
	/// <param name="free">무료 뽑기 (재화 확인 없음)</param>
	/// <param name="confirm">10회 뽑기 확인 팝업을 이미 거쳤는지</param>
	private void _RequestGachaOpenBox(string type, int count, bool free, bool confirm = false)
    {
        var costData = regulation.FindGachaCost(type, count);
        if (costData == null)
		{
            Debug.LogError("invalid state : " + type + ", " + count);
            return;
        }

		if (!free && !CheckGachaCost(type, count, costData, confirm))
			return;

		network.RequestGachaOpenBox(type, count);
	}

	/// <summary>
	/// 뽑기 재화 확인.
	/// - 골드 / 다이아: 10회 뽑기는 확인 팝업을 먼저 띄우고, 부족하면 골드 획득처 / 다이아 상점으로 안내
	/// - 그 외 재화(뽑기권 등): 부족하면 획득처 안내 팝업
	/// </summary>
	/// <returns>바로 요청해도 되면 true, 팝업을 띄웠으면 false</returns>
	private bool CheckGachaCost(string type, int count, GachaCostDataRow costData, bool confirm)
	{
		bool isGold = costData.priceType == EGoods.FreeGold;
		bool isCash = costData.priceType == EGoods.FreeCash;

		if (isGold || isCash)
		{
			if (!confirm && count == 10)
			{
				string messageKey = isGold ? "13024" : "13023";
				UISimplePopup.CreateBool(false, Localization.Get("1303"), Localization.Format(messageKey, costData.cost), null,
					Localization.Get("1304"), Localization.Get("1305")).onClick = sender =>
				{
					if (sender.name == "OK")
						_RequestGachaOpenBox(type, count, false, true);
				};
				return false;
			}

			if (isGold && costData.cost > localUser.gold)
			{
				UISimplePopup.CreateBool(true, "1029", "1030", null, "1304", "1305").onClick = sender =>
				{
					if (sender.name == "OK")
						uiWorld.camp.GoNavigation("MetroBank");
				};
				return false;
			}

			if (isCash && costData.cost > localUser.cash)
			{
				UISimplePopup.CreateBool(true, "1031", "1032", null, "1304", "1305").onClick = sender =>
				{
					if (sender.name == "OK")
						UIManager.instance.world.mainCommand.OpenDiamondShop();
				};
				return false;
			}
			return true;
		}

		GoodsDataRow goods = regulation.FindGoodsServerFieldName(((int)costData.priceType).ToString());
		int userCnt = localUser.resourceList[goods.serverFieldName];
		if (costData.cost > userCnt)
		{
			ItemExchangeDataRow itemData = regulation.itemExchangeDtbl.Find(row => row.type == EStorageType.Goods && row.typeidx == goods.type);
			UINavigationPopUp popup = UIPopup.Create<UINavigationPopUp>("NavigationPopUp");
			popup.Init(EStorageType.Goods, goods.type, itemData);
			popup.title.text = Localization.Get("5608");
			return false;
		}
		return true;
	}


	/// <summary>버튼 이름 끝의 "-숫자"에서 뽑기 횟수를 읽는다. (읽지 못하면 1회)</summary>
	private int _FindCount(string key)
	{
		int idx = key.LastIndexOf("-");
		if (idx < 0) {
			Debug.LogError(key);
			return 1;
		}
        int count = 1;
        if (!int.TryParse(key.Substring(idx + 1), out count)) {
            Debug.LogError(key);
            return 1;
        }
        return count;
    }

    public override void OnRefresh()
    {
        base.OnRefresh();
        _Set();
        SendOnRefreshToInnerParts();
    }

    /// <summary>가챠 팝업 진입점. 배너 목록을 만들고 첫 번째 가챠를 선택한 상태로 연다.</summary>
    public void InitAndOpenGacha()
    {
		if( bEnterKeyEnable == false )
		{
			bEnterKeyEnable = true;

			SetBannerList();
	        _Set();
            UISetter.SetActive(typeATargetList.root, false);
            UISetter.SetActive(typeBTargetList.root, false);
	        UISetter.SetActive(singleBox.root, false);
	        UISetter.SetActive(multipleBox.root, false);
			OpenPopupShow();
			SendOnInitToInnerParts();
		}
    }

	public void RefreshGacha()
	{
		_Set();
	}

	void RefreshBannerList()
	{
		if(bannerListView.itemList.Count > 0)
		{
			bannerListView.itemList.ForEach(row => 
				{
					UIGachaBannerListItem item = row as UIGachaBannerListItem;
					item.OnRefresh();
				});
		}
	}

	/// <summary>뽑기 버튼의 가격(재화 아이콘 / 수량)과 횟수 문구 표시</summary>
	private void _SetCost(UISprite costIcon, UILabel label, string gachaType, int count)
    {
        var costData = regulation.FindGachaCost(gachaType, count);
        if (costData == null) {
            Debug.LogError("invalid state");
            return;
        }

		var goodsData = regulation.goodsDtbl[((int)costData.priceType).ToString()];
		UISetter.SetSprite(costIcon, goodsData.iconId);

		UILabel countLabel = count == 1 ? premiumOnceCashLabel : premiumTenTimeCashLabel;
		UISetter.SetLabel(countLabel, Localization.Format("13004", count));
        UISetter.SetLabel(label, costData.cost);
    }

    /// <summary>
    /// 선택한 가챠 정보 표시: 배너, 설명, 가격, 무료 뽑기 가능 여부와 다음 무료까지 남은 시간, 확정 보상 진행도.
    /// 상단 재화 표시도 이 가챠의 뽑기 재화로 바꾼다.
    /// </summary>
    private void _Set()
    {
		RoGacha gacha = localUser.gacha[selectType];
		GachaDataRow gachaData = regulation.gachaDtbl[selectType];
		GachaCostDataRow costData = regulation.FindGachaCost(gachaData.type, 1);
        UISetter.SetActive(premiumFreeOnceButton, gacha.canOpenFreeBox);
        UISetter.SetActive(premiumOnceButton, !gacha.canOpenFreeBox);

		UISetter.SetSprite(bigBanner, Localization.Get(gachaData.banner));
		UISetter.SetLabel(gachaName, Localization.Get(gachaData.name));
		UISetter.SetActive(gachaDescription, gachaData.eventComment != "0");
		UISetter.SetLabel(gachaDescription, Localization.Get(gachaData.eventComment));
		UISetter.SetActive(gachaTimeRoot, gachaData.count != 0);
		UISetter.SetActive(pilotProgress, gachaData.bonusCount != 0 || gachaData.type == GachaTypePremium);

		// 기본 가챠(골드 / 프리미엄)는 배너가 작아서 설명과 보상 버튼을 위로 올린다
		bool isBasicGacha = gachaData.type == GachaTypeGold || gachaData.type == GachaTypePremium;
		bigBanner.height = isBasicGacha ? 277 : 320;
		gachaDescription.transform.localPosition = new Vector3(gachaDescription.transform.localPosition.x, isBasicGacha ? -70 : -110, 0);
		gachaRewardBtn.transform.localPosition = new Vector3(gachaRewardBtn.transform.localPosition.x, isBasicGacha ? -30 : -72, 0);

		_SetCost(premiumOnceCashIcon, premiumOnceCash, gachaData.type, 1);
		_SetCost(premiumTenTimeCashIcon, premiumTenTimeCash, gachaData.type, 10);

		// 무료 뽑기 횟수 제한이 있으면 남은 횟수도 표시
		if(gachaData.count > 1)
			UISetter.SetLabel(premiumOnceCashFreeLabel, string.Format("{0}\n{1}/{2}", Localization.Get("13007"), gacha.remainCount, gachaData.count));
		else
			UISetter.SetLabel(premiumOnceCashFreeLabel, Localization.Get("13007"));
				
        if (premiumRemainTime != null) {
            // 타이머가 끝났을 때 문구: 오늘 무료 횟수를 다 썼으면 / 무료 뽑기 가능
			if (gacha.remainCount <= 0) {
                premiumRemainTime.SetFinishString(Localization.Get("6015"));
            } else {
                premiumRemainTime.SetFinishString(Localization.Get("6016"));
            }
            premiumRemainTime.RegisterOnFinished(ui => {
				UISetter.SetActive(premiumFreeOnceButton, gacha.canOpenFreeBox);
				UISetter.SetActive(premiumOnceButton, !gacha.canOpenFreeBox);
            });
			premiumRemainTime.SetLabelFormat(string.Format("{0} ", Localization.Get("13005")), "");
            premiumRemainTime.Set(gacha.freeOpenTime);
        }
		uiWorld.mainCommand.SetResourceView(((int)costData.priceType).ToString());
		RefreshBannerList();
		SetPilotProgress();
    }

	/// <summary>보유한 가챠 목록으로 배너를 만든다. (정렬 순서대로, 첫 번째 가챠 선택)</summary>
	void SetBannerList()
	{
		List<GachaDataRow> gachaList = new List<GachaDataRow>();
		foreach(var key in localUser.gacha.Keys)
		{
			gachaList.Add(regulation.gachaDtbl[key]);
		}
		gachaList.Sort((a, b) => {
			return a.sort.CompareTo(b.sort);
		});
			
		selectType = gachaList[0].type;
		bannerListView.InitGachaBannerList(gachaList, "Banner-");
		bannerListView.SetSelection(selectType, true);
	}

	/// <summary>
	/// 확정 보상(천장) 진행도. 정해진 횟수를 채우면 메인 보상이 확정된다.
	/// 프리미엄 가챠는 define 값(TREASURE_PILOT_RATE), 그 외 가챠는 가챠별 bonusCount를 기준으로 한다.
	/// </summary>
	void SetPilotProgress()
	{
		RoGacha gacha = localUser.gacha[selectType];
		GachaDataRow gachaData = regulation.gachaDtbl[selectType];
		int maxRate = gachaData.type == GachaTypePremium
			? int.Parse(regulation.defineDtbl["TREASURE_PILOT_RATE"].value) - 1
			: gachaData.bonusCount - 1;

		UISetter.SetProgress(pilotProgress, maxRate > 0 ? (float)gacha.pilotRate / maxRate : 0f);
		if (gacha.pilotRate >= maxRate)
		{
			UISetter.SetLabel(pilotProgressLabel, Localization.Format("13027", Localization.Get(gachaData.mainReward)));
			UISetter.SetActive(effectRoot, true);
		}
		else
		{
			UISetter.SetLabel(pilotProgressLabel, Localization.Format("13026", gacha.pilotRate, maxRate, Localization.Get(gachaData.mainReward)));
			UISetter.SetActive(effectRoot, false);
		}
	}

    private void _CloseRewardList()
    {
        UISetter.SetActive(targetList, false);
    }

	/// <summary>
	/// 획득 가능 보상 목록. 가챠 UI 타입에 따라
	/// A형(보상 종류별 확률 + 보상 목록) 또는 B형(보상·수량별 개별 확률)으로 보여준다.
	/// </summary>
	private void _OpenRewardList(string type)
    {
        GachaDataRow gachaData = regulation.gachaDtbl[type];
        var list = _CreateGettableRewardList(type);
        if (gachaData.ui == 0)
        {
            targetList = typeATargetList.root;
            Dictionary<ERewardType, float> probabilityList = new Dictionary<ERewardType, float>();

            foreach(var data in localUser.gacha[type].gachaRatingTypeA)
            {
                probabilityList.Add(data.Key, data.Value.rating);
            }

            typeATargetList.Set(list, probabilityList);
            UISetter.SetActive(targetList, true);
            typeATargetList.rewardList.ResetPosition();
        }
        else
        {
            targetList = typeBTargetList.root;
            typeBTargetList.Set(list, localUser.gacha[type]);
            UISetter.SetActive(targetList, true);
            typeBTargetList.rewardList.ResetPosition();
        }
    }

	/// <summary>
	/// 가챠 보상 테이블을 표시용 목록으로 바꾼다.
	/// A형은 같은 보상을 하나로 묶고 가능한 수량들을 모아 두고, B형은 보상·수량 조합마다 한 줄씩 만든다.
	/// </summary>
	private List<GettableRewardData> _CreateGettableRewardList(string type)
    {
        GachaDataRow gachaData = regulation.gachaDtbl[type];
        var list = regulation.FindGachaRewardList(type);

        Dictionary<ERewardType, Dictionary<string, List<int>>> cntDict = new Dictionary<ERewardType, Dictionary<string, List<int>>>();
        Dictionary<ERewardType, HashSet<string>> createdRewardSet = new Dictionary<ERewardType, HashSet<string>>();
        var uniqueList = new List<GettableRewardData>();
       
        list.ForEach(row =>
            {
                if (!cntDict.ContainsKey(row.rewardType))
                    cntDict.Add(row.rewardType, new Dictionary<string, List<int>>());
                if (!cntDict[row.rewardType].ContainsKey(row.rewardId))
                    cntDict[row.rewardType].Add(row.rewardId, new List<int>());
                cntDict[row.rewardType][row.rewardId].Add(row.rewardCount);
            });

        if (gachaData.ui == 0)
        {
            list.ForEach(row =>
                {
                    if (createdRewardSet.ContainsKey(row.rewardType) && createdRewardSet[row.rewardType].Contains(row.rewardId))
                    {
                        return;
                    }
                    if (!createdRewardSet.ContainsKey(row.rewardType))
                        createdRewardSet.Add(row.rewardType, new HashSet<string>());
                    createdRewardSet[row.rewardType].Add(row.rewardId);
                    uniqueList.Add(new GettableRewardData()
                        {
                            rewardData = row,
                            countList = cntDict[row.rewardType][row.rewardId]
                        });
                });
        }
        else
        {
            list.ForEach(row =>
                {
                    for(int i = 0; i < cntDict[row.rewardType][row.rewardId].Count; i++)
                    {
                        int count = cntDict[row.rewardType][row.rewardId][i];
                        List<int> cList = new List<int>();
                        cList.Add(count);
                        uniqueList.Add(new GettableRewardData()
                            {
                                rewardData = row,
                                countList = cList
                            });
                    }
                });
        }

        return uniqueList;
    }

    /// <summary>뽑기 결과 연출 시작. 1개면 단일 상자, 여러 개면 10연 상자 연출을 재생한다.</summary>
    public void OpenBox(List<BoxData> list)
    {
        if (list == null) {
            Debug.LogError("invalid state");
            return;
        }

		this.AnimNormal.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimNpc.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimPremium.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);

        if (list.Count == 1) {
            singleBox.Set(list[0]);

            UISetter.SetActive(singleBox.root, true);
            singleBox.StartAnimation();
        } else {
            multipleBox.Set(list);

            UISetter.SetActive(multipleBox.root, true);
			multipleBox.StartAnimation();
        }
    }
	
	/// <summary>가챠 종류를 선택하고 화면을 갱신한다.</summary>
	private void SelectGacha(string type)
	{
		selectType = type;
		bannerListView.SetSelection(selectType, true);
		_Set();
	}

	public void GachaSelectCashType()
	{
		SelectGacha(GachaTypePremium);
	}

	public void GachaSelectGoldType()
	{
		SelectGacha(GachaTypeGold);
	}

    /// <summary>다른 화면에서 특정 가챠로 바로 이동할 때 사용한다.</summary>
    public void GoNavigationGacha(string _selectType)
    {
        SelectGacha(_selectType);
    }

    /// <summary>서버에서 받은 뽑기 결과 하나 (상자 하나에 표시할 보상)</summary>
    public class BoxData
    {
        public EGachaAnimationType gachaType { get; set; }
        public ERewardType rewardType { get; set; }
		public CommanderCompleteType getType { get; set; }
        public string rewardId { get; set; }
        public int rewardCount { get; set; }
		public bool isNew { get; set; }

        //아래 두 필드는 메달 상승 연출시 필요.
        //타입이 지휘관일 경우만. 중첩시 적용 전 로컬에서 갖고있던 메달 갯수.
        public int currentCommanderMedal { get; set; }
        //타입이 지휘관일 경우만. 지휘관 중첩시 적용된 메달 갯수.
        public int getCommanderMedal { get; set; }

        public GoodsDataRow goodsData
        {
            get
            {
                if (rewardType != ERewardType.Goods) {
                    return null;
                }
                return RemoteObjectManager.instance.regulation.goodsDtbl[rewardId];
            }
        }

		public PartDataRow partsData
		{
			get
			{
				if (rewardType != ERewardType.UnitMaterial) {
					return null;
				}
				return RemoteObjectManager.instance.regulation.partDtbl[rewardId];
			}
		}

        public CommanderDataRow commanderData
        {
            get
            {
				if (rewardType != ERewardType.Medal && rewardType != ERewardType.Commander) {
                    return null;
                }
                return RemoteObjectManager.instance.regulation.commanderDtbl[rewardId];
            }
        }

        public CommanderCostumeDataRow costumeData
        {
            get
            {
                if (rewardType != ERewardType.Costume)
                {
                    return null;
                }
                return RemoteObjectManager.instance.regulation.commanderCostumeDtbl[rewardId];
            }
        }

        public EquipItemDataRow equipItemData
        {
            get
            {
                if (rewardType != ERewardType.Item)
                    return null;

                return RemoteObjectManager.instance.regulation.equipItemDtbl.Find(row => row.key == rewardId);
            }
        }
    }

    /// <summary>획득 가능 보상 목록의 한 줄 (보상과 획득 가능한 수량들)</summary>
    public class GettableRewardData
    {
        public GachaRewardDataRow rewardData { get; set; }
		public List<int> countList { get; set; }
    }

    /// <summary>A형 보상 목록: 보상 종류별 확률 + 보상 목록</summary>
    [System.Serializable]
    public class TypeARewardList : UIInnerPartBase
    {
        public UIDefaultListView probabilityList;
        public UIDefaultListView rewardList;

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            base.OnClick(sender, parent);
            string key = sender.name;
            if (key == "Close-PremiumGoodsList") 
            {
                SoundManager.PlaySFX ("EFM_SelectButton_002", false);
                UISetter.SetActive(root, false);
            }
        }

        public void Set(List<GettableRewardData> list, Dictionary<ERewardType, float> probability)
        {
            SoundManager.PlaySFX ("EFM_OpenPopup_001", false);
            rewardList.CreateItem(list.Count, "Reward-", (item, index) => {
                UIGoods ui = item as UIGoods;
                var data = list[index];
                UISetter.SetGameObjectName(ui.gameObject, data.rewardData.rewardId);
                ui.Set(data.rewardData, data.countList);
            });

            probabilityList.InitGachaProbabilityList(probability);
        }
    }

    /// <summary>B형 보상 목록: 보상 · 수량 조합별 확률</summary>
    [System.Serializable]
    public class TypeBRewardList : UIInnerPartBase
    {
        public UIDefaultListView rewardList;

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            base.OnClick(sender, parent);
            string key = sender.name;
            if (key == "Close-PremiumGoodsList") 
			{
				SoundManager.PlaySFX ("EFM_SelectButton_002", false);
                UISetter.SetActive(root, false);
            }
        }

        public void Set(List<GettableRewardData> list, RoGacha gacha)
        {
			SoundManager.PlaySFX ("EFM_OpenPopup_001", false);

            rewardList.CreateItem(list.Count, "Reward-", (item, index) => {
                UIGoods ui = item as UIGoods;
                var data = list[index];

                float probability = 0.0f;

                if(gacha.gachaRatingTypeB.ContainsKey(data.rewardData.rewardType))
                {
                    Protocols.GachaRatingDataTypeB gachaRewardTypeList = gacha.gachaRatingTypeB[data.rewardData.rewardType];

                    if(gachaRewardTypeList.list.ContainsKey(data.rewardData.rewardId))
                    {
                        Dictionary<int, float> gachaRewardList = gachaRewardTypeList.list[data.rewardData.rewardId];
                        if(gachaRewardList.ContainsKey(data.countList[0]))
                            probability = gachaRewardList[data.countList[0]];
                    }
                }
                
                UISetter.SetGameObjectName(ui.gameObject, data.rewardData.rewardId);
                ui.Set(data.rewardData, data.countList, probability);
            });
        }
    }

    #region 상자 개봉 연출

    private const string NormalBoxOpenAnimationName = "Gacha_Box_Ani_01";
    private const string PremiumBoxOpenAnimationName = "Gacha_Box_Ani_02";
    private const string RainbowBoxOpenAnimationName = "Gacha_Box_Ani_03";

    /// <summary>보상 종류별 연출 등급: 지휘관 = 무지개, 메달·장비·코스튬 = 프리미엄, 그 외 = 일반</summary>
    private static EGachaAnimationType GetAnimationGrade(ERewardType rewardType)
    {
        if (rewardType == ERewardType.Commander)
            return EGachaAnimationType.RainBow;
        if (rewardType == ERewardType.Medal || rewardType == ERewardType.Item || rewardType == ERewardType.Costume)
            return EGachaAnimationType.Premium;
        return EGachaAnimationType.Normal;
    }

    private static string GetOpenAnimationName(EGachaAnimationType grade)
    {
        if (grade == EGachaAnimationType.Normal)
            return NormalBoxOpenAnimationName;
        if (grade == EGachaAnimationType.Premium)
            return PremiumBoxOpenAnimationName;
        return RainbowBoxOpenAnimationName;
    }

    /// <summary>보상 종류에 맞게 상자 내용을 채운다.</summary>
    private static void SetBoxReward(UIGachaBox box, BoxData data)
    {
        if (data.rewardType == ERewardType.Goods)
            box.Set(data.gachaType, data.goodsData, data.rewardCount);
        else if (data.rewardType == ERewardType.UnitMaterial)
            box.Set(data.gachaType, data.partsData, data.rewardCount);
        else if (data.rewardType == ERewardType.Costume)
            box.Set(data.gachaType, data.costumeData);
        else if (data.rewardType == ERewardType.Medal || data.rewardType == ERewardType.Commander)
            box.Set(data);
        else if (data.rewardType == ERewardType.Item)
            box.Set(data.gachaType, data.equipItemData, data.rewardCount);
    }

    /// <summary>연출 등급에 맞는 효과음과 스파인 애니메이션을 재생한다.</summary>
    private static void PlayOpenEffect(EGachaAnimationType grade, SkeletonAnimation premiumSpine, SkeletonAnimation rainbowSpine)
    {
        if (grade == EGachaAnimationType.RainBow)
        {
            SoundManager.PlaySFX("SE_PremiumGacha_001");
            rainbowSpine.state.SetAnimation(0, "gacha_02", false);
        }
        else if (grade == EGachaAnimationType.Premium)
        {
            SoundManager.PlaySFX("SE_PremiumGacha_001");
            premiumSpine.state.SetAnimation(0, "n_012_gacha", false);
        }
        else
        {
            SoundManager.PlaySFX("SE_NormalGacha_001");
        }
    }

    /// <summary>1회 뽑기 상자 연출: 등급별 상자 애니메이션 → 상자 등장 → 터치해서 개봉</summary>
    [System.Serializable]
    public class BoxOpenSingle : UIInnerPartBase
    {
        public UIGachaBox box;
		public GameObject skipButton;
		public GameObject closeButton;
		public SkeletonAnimation premiumSpine;
		public SkeletonAnimation rainbowSpine;
		public Animation normalBoxAnimation;
		public Animation premiumBoxAnimation;
		public Animation rainbowBoxAnimation;
        public BoxData currentBoxData { get; private set; }
        public Coroutine currAnimationRoutine { get; private set; }

		private EGachaAnimationType grade;
		private Animation curAnimation;
		private string curAnimationName;

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            base.OnClick(sender, parent);
            string key = sender.name;
            if (key == "Close-BoxOpenSingle")
                UISetter.SetActive(root, false);
			else if (key == "SingleSkip")
				Skip();
			else if (key == "SingleOpen")
				Open();
        }

        /// <summary>보상을 상자에 채우고, 보상 등급에 맞는 상자 연출을 고른다.</summary>
        public void Set(BoxData data)
        {
            currentBoxData = data;
            SetBoxReward(box, data);
            StopAnimation();

			box.GetType(data.getType);
            box.openAnimation.Stop();

			UISetter.SetActive(normalBoxAnimation.gameObject, false);
			UISetter.SetActive(premiumBoxAnimation.gameObject, false);
			UISetter.SetActive(rainbowBoxAnimation.gameObject, false);

			grade = GetAnimationGrade(data.rewardType);
			curAnimation = grade == EGachaAnimationType.Normal ? normalBoxAnimation
				: grade == EGachaAnimationType.Premium ? premiumBoxAnimation
				: rainbowBoxAnimation;
			curAnimationName = GetOpenAnimationName(grade);
        }

        public void StartAnimation()
        {
            currAnimationRoutine = parentPanelBase.StartCoroutine(_PlayOpenAnimation());
        }

        public void StopAnimation()
        {
            if (currAnimationRoutine == null)
                return;
            parentPanelBase.StopCoroutine(currAnimationRoutine);
            currAnimationRoutine = null;
        }

		/// <summary>상자 연출 건너뛰기: 애니메이션을 마지막 프레임으로 보낸다.</summary>
		public void Skip()
		{
			if (curAnimation.IsPlaying(curAnimationName))
			{
				SoundManager.StopSFX();
				Utility.SampleAnimationEnd(curAnimation, curAnimationName);
				UISetter.SetActive(curAnimation.gameObject, false);
			}
		}

		public void Open()
		{
			box.BoxOpen();
			box.BroadcastMessage("MarkAsChanged", SendMessageOptions.DontRequireReceiver);
		}

        /// <summary>상자 연출 재생 → 상자 표시 → 사용자가 상자를 열 때까지 대기 → 닫기 버튼 표시</summary>
        private IEnumerator _PlayOpenAnimation()
        {
			UISetter.SetActive(box, false);
			UISetter.SetActive(closeButton, false);
			UISetter.SetActive(curAnimation.gameObject, true);

			PlayOpenEffect(grade, premiumSpine, rainbowSpine);

			UISetter.SetActive(skipButton, true);
			curAnimation.Play(curAnimationName);
			while (curAnimation.IsPlaying(curAnimationName))
				yield return null;

			UISetter.SetActive(skipButton, false);
			UISetter.SetActive(curAnimation.gameObject, false);

			SoundManager.PlaySFX("SE_Gacha10_001");
			UISetter.SetActive(box, true);
			box.BroadcastMessage("MarkAsChanged", SendMessageOptions.DontRequireReceiver);
			while (box.background.isActiveAndEnabled)
				yield return null;

			UISetter.SetActive(closeButton, true);
        }
    }

    /// <summary>
    /// 10회 뽑기 상자 연출: 가장 높은 등급으로 상자 애니메이션 → 상자 10개가 차례로 등장
    /// → 모두 열기(또는 하나씩 터치) → 닫기 버튼 표시
    /// </summary>
    [System.Serializable]
    public class BoxOpenMultiple : UIInnerPartBase
    {
        public UIDefaultListView boxList;
        public GameObject skipButton;
		public GameObject openButton;
        public GameObject closeButton;
        public List<BoxData> currentBoxDataList { get; private set; }
        public Coroutine currAnimationRoutine { get; private set; }
		public SkeletonAnimation premiumSpine;
		public SkeletonAnimation rainbowSpine;
		public Animation normalBoxAnimation;
		public Animation premiumBoxAnimation;
		public Animation rainbowBoxAnimation;

		private EGachaAnimationType grade;
		private Animation curAnimation;
		private string curAnimationName;

        public override void OnClick(GameObject sender, UIPanelBase parent)
        {
            base.OnClick(sender, parent);
			UIGacha gacha = parent as UIGacha;
            string key = sender.name;
            if (key == "Close-BoxOpenMultiple")
			{
                UISetter.SetActive(root, false);
				gacha.SetPilotProgress();
            }
			else if (key == "MultipleSkip")
			{
                Skip();
			}
			else if (key == "MultipleOpen")
			{
				Open();
			}
        }

        /// <summary>상자마다 보상을 채우고, 가장 높은 보상 등급으로 상자 연출을 고른다.</summary>
        public void Set(List<BoxData> dataList)
        {
            currentBoxDataList = dataList;
            StopAnimation();

			grade = EGachaAnimationType.Normal;
			boxList.CollectExistItem("GachaBox_");
			for (int i = 0; i < boxList.itemList.Count; i++)
			{
				UIGachaBox box = boxList.itemList[i] as UIGachaBox;
				BoxData data = dataList[i];
				SetBoxReward(box, data);

				EGachaAnimationType boxGrade = GetAnimationGrade(data.rewardType);
				if (boxGrade > grade)
					grade = boxGrade;

				box.GetType(data.getType);
				box.openAnimation.Stop();
				UISetter.SetActive(box, false);
			}

			UISetter.SetActive(normalBoxAnimation.gameObject, false);
			UISetter.SetActive(premiumBoxAnimation.gameObject, false);
			UISetter.SetActive(rainbowBoxAnimation.gameObject, false);

			curAnimation = grade == EGachaAnimationType.Normal ? normalBoxAnimation
				: grade == EGachaAnimationType.Premium ? premiumBoxAnimation
				: rainbowBoxAnimation;
			curAnimationName = GetOpenAnimationName(grade);
        }

        /// <summary>연출 건너뛰기: 상자 애니메이션을 끝내고 상자 10개를 바로 등장 완료 상태로 둔다.</summary>
        public void Skip()
        {
			Utility.SampleAnimationEnd(curAnimation, curAnimationName);
			StopAnimation();

            for (int idx = 0; idx < currentBoxDataList.Count; ++idx) {
                var box = boxList.itemList[idx] as UIGachaBox;
				UISetter.SetActive(box, true);
				Utility.SampleAnimationEnd(box.openAnimation, box.moveAnimationName);
            }

			UISetter.SetActive(openButton, true);
            UISetter.SetActive(skipButton, false);
			StartAnimation(parentPanelBase.StartCoroutine(_BoxOpenCheck()));
        }

		/// <summary>모두 열기 버튼</summary>
		public void Open()
		{
			StopAnimation();
			UISetter.SetActive(openButton, false);
			StartAnimation(parentPanelBase.StartCoroutine(_PlayOpenBoxAnimation()));
		}

		public void StartAnimation()
		{
			currAnimationRoutine = parentPanelBase.StartCoroutine(_PlayOpenAnimation());
		}

		public void StartAnimation(Coroutine routine)
        {
			currAnimationRoutine = routine;
        }

		public void StopAnimation()
        {
            if (currAnimationRoutine == null)
                return;
            parentPanelBase.StopCoroutine(currAnimationRoutine);
            currAnimationRoutine = null;
        }

        /// <summary>상자 연출 재생 → 상자를 한 프레임씩 차례로 등장 → 열기 대기</summary>
        private IEnumerator _PlayOpenAnimation()
        {
			UISetter.SetActive(closeButton, false);
			UISetter.SetActive(curAnimation.gameObject, true);
			PlayOpenEffect(grade, premiumSpine, rainbowSpine);

			UISetter.SetActive(skipButton, true);
			curAnimation.Play(curAnimationName);
			while (curAnimation.IsPlaying(curAnimationName))
				yield return null;
			UISetter.SetActive(curAnimation.gameObject, false);

			for (int idx = 0; idx < currentBoxDataList.Count; ++idx)
			{
				var box = boxList.itemList[idx] as UIGachaBox;
				UISetter.SetActive(box, true);
				SoundManager.PlaySFX("SE_Gacha10_001");

				box.openAnimation.Play(box.moveAnimationName);
				yield return null;
				box.BroadcastMessage("MarkAsChanged", SendMessageOptions.DontRequireReceiver);

				if (!isActive)
					break;
			}

			UISetter.SetActive(skipButton, false);
			UISetter.SetActive(openButton, true);

			StartAnimation(parentPanelBase.StartCoroutine(_BoxOpenCheck()));
        }

		/// <summary>사용자가 상자를 하나씩 모두 열 때까지 기다린 뒤 닫기 버튼을 보여준다.</summary>
		private IEnumerator _BoxOpenCheck()
		{
			for (int idx = 0; idx < currentBoxDataList.Count; ++idx)
			{
				var box = boxList.itemList[idx] as UIGachaBox;

				while (box.background.isActiveAndEnabled)
					yield return null;

				if (!isActive)
					break;
			}

			UISetter.SetActive(openButton, false);
			UISetter.SetActive(closeButton, true);
		}

		/// <summary>모두 열기: 아직 열지 않은 상자를 0.2초 간격으로 차례로 연다.</summary>
		private IEnumerator _PlayOpenBoxAnimation()
		{
			for (int idx = 0; idx < currentBoxDataList.Count; ++idx)
			{
				var box = boxList.itemList[idx] as UIGachaBox;

				if (box.background.isActiveAndEnabled)
					yield return new WaitForSeconds(0.2f);

				box.BoxOpen();
				box.BroadcastMessage("MarkAsChanged", SendMessageOptions.DontRequireReceiver);
				while (box.background.isActiveAndEnabled)
					yield return null;

				if (!isActive)
					break;
			}

			UISetter.SetActive(openButton, false);
			UISetter.SetActive(closeButton, true);
		}
	}

    #endregion

	void OpenPopupShow()
	{
		Open();
	}

	public override void Open()
	{
		base.Open();
		this.OnAnimOpen();
	}
	
	/// <summary>닫기: 상단 재화 표시를 기본(탄약)으로 되돌리고 퇴장 연출을 시작한다.</summary>
	public override void Close()
	{
		bBackKeyEnable = true;
		uiWorld.mainCommand.SetResourceView(EGoods.Bullet);
		this.HidePopup();
	}
	IEnumerator OnEventHidePopup()
	{
		yield return new WaitForSeconds(0.8f);
		
		base.Close();

		bBackKeyEnable = false;
		bEnterKeyEnable = false;

	}
	
	void HidePopup()
	{
		OnAnimClose();
		
		StartCoroutine( OnEventHidePopup() );
	}
	
	void OnAnimOpen()
	{
        this.AnimNormal.Reset();
        this.AnimNpc.Reset();
        this.AnimPremium.Reset();
        this.AnimTitle.Reset();
        this.AnimBlock.Reset();

        UIManager.instance.SetPopupPositionY(gameObject);

		this.AnimNormal.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimNpc.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimPremium.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimTitle.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimBlock.MoveIn(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		
	}
	
	void OnAnimClose()
	{
		this.AnimNormal.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimNpc.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimPremium.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimTitle.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		this.AnimBlock.MoveOut(GUIAnimSystemNGUI.eGUIMove.SelfAndChildren);
		
	}

    public void AnimOpenFinish()
    {
        UIManager.instance.EnableCameraTouchEvent(true);
    }

    protected override void OnEnablePopup()
    {
        UISetter.SetVoice(spineAnimation, true);        
    }

    protected override void OnDisablePopup()
    {
        UISetter.SetVoice(spineAnimation, false);
    }    
}