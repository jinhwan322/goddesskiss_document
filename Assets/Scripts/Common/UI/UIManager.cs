using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cache;

/// <summary>
/// UI 전체를 관리하는 싱글톤 매니저.
/// - 게임 상태(월드/전투/튜토리얼/시나리오)에 따라 UI 그룹(World, Battle, Scenario)을 전환
/// - 각 화면은 처음 접근할 때 생성(지연 생성)해 초기 로딩 시간과 메모리를 절약
/// - 생성된 패널을 panelList로 모아 데이터가 바뀌면 일괄 갱신(RefreshOpenedUI)
/// </summary>
public class UIManager : MonoBehaviour {

    /// <summary>현재 게임 상태. 어떤 UI 그룹을 사용할지 결정한다.</summary>
    public enum EState
    {
        World,
        Battle,
        Tutorial,
        Scenario,
        Editor
    }

    //------------------------------------------------------//

    public static bool hasInstance { get { return _singleton != null; } }

    private static UIManager _singleton = null;
    /// <summary>싱글톤 접근자. 씬에서 찾아보고, 없으면 새로 생성한다.</summary>
    public static UIManager instance
    {
        get
        {
            if (_singleton == null) {
                _singleton = GameObject.FindObjectOfType<UIManager>();
                if (_singleton == null) {
                    GameObject go = new GameObject("_UIManager");
                    _singleton = go.AddComponent<UIManager>();
                    if (_singleton == null) {
                        Debug.LogError("invalid state");
                        throw new System.NullReferenceException();
                    }
                }
                _singleton._Init();
            }
            return _singleton;
        }
    }

    [HideInInspector]
    public EState state;

    public World world { get; private set; }
    public Battle battle { get; private set; }
    public Scenario scenario { get; private set; }  
    
    /// <summary>현재 상태에 맞는 대화(시나리오 대사) 매니저를 반환한다.</summary>
    public ClassicRpgManager DialogMgr
    {
        get
        {
            switch (state)
            {
                case EState.World:
                    if (world != null)
                    {
                        return world.dialogMgr;
                    }
                    break;
                case EState.Battle:
                case EState.Tutorial:
                    if (battle != null)
                    {
                        return battle.DialogMgr;
                    }
                    break;
                case EState.Scenario:
                    if (scenario != null)
                    {
                        return scenario.DialogMgr;
                    }
                    break;
            }

            return null;
        }
    }

    /// <summary>현재 상태에 해당하는 UI 그룹.</summary>
    public Group current{
        get{
            switch (state)
            {
                case EState.World:
                    return world;
                case EState.Tutorial:
                case EState.Battle:
                    return battle;      
                case EState.Scenario:
                    return scenario;
            }
            return null;
        }
    }

    /// <summary>캐시와 현재 UI 그룹을 정리한다. (씬 전환 시 호출)</summary>
    public void Release()
    {
        StopAllCoroutines();

        CacheItemPocket.Release();
        CacheManager.instance.CleanUp();

        if (current != null)
            current.Release();
    }

    // 중복 인스턴스 방지: 이미 싱글톤이 있으면 자신을 제거
    void Awake()
    {
        if (_singleton != null && _singleton != this) {
            DestroyImmediate(this);
        } else {
            _singleton = this;
            _Init();
        }
    }

    void OnDestroy()
    {
        if (_singleton == this) {
            _singleton = null;            
        }
    }

    void OnLevelWasLoaded(int level)
    {
        Debug.Log("OnLevelWasLoaded : " + Application.loadedLevelName);
    }

    // 갱신 요청이 있으면 프레임 끝에서 한 번만 처리
    void LateUpdate()
    {
        if (needRefreshUI) {
            _RefreshOpenedUI();
            needRefreshUI = false;
        }
    }

    private bool needRefreshUI = false;
    /// <summary>
    /// UI 갱신 요청. 바로 갱신하지 않고 플래그만 세워 두고 LateUpdate에서 한 번에 처리한다.
    /// 한 프레임에 여러 번 호출되어도 갱신은 한 번만 일어나도록 하기 위함.
    /// </summary>
    public void RefreshOpenedUI()
    {
        needRefreshUI = true;
    }
    
    /// <summary>현재 그룹의 패널 중 활성화된 패널만 OnRefresh()를 호출한다.</summary>
    private void _RefreshOpenedUI()
    {
        List<UIPanelBase> panelList = null;
        if (world != null) {
            panelList = world.panelList;
        } else if (battle != null) {
            panelList = battle.panelList;
        }
        if (panelList == null) {
            Debug.LogError("invalid state");
            return;
        }

        panelList.ForEach(target => {
            if (target.isActive) {
                target.OnRefresh();
            }
        });
    }

    /// <summary>상태별 UI 묶음의 베이스. 패널 목록 관리, 전체 닫기, 초기화를 담당한다.</summary>
    public abstract class Group
    {
        public List<UIPanelBase> panelList{ get; protected set;}
        protected abstract void _Init();
        public virtual bool isValid { get; protected set; }

        public void CloseAll()
        {
            panelList.ForEach(panel => UISetter.SetActive(panel, false));
        }

        /// <summary>
        /// 패널을 한 프레임에 하나씩 켜고, 4프레임 뒤에 다시 끄는 방식으로 초기화한다.
        /// 모든 패널의 초기화(Awake/OnEnable)를 미리 실행하되, 한 프레임에 부하가 몰리지 않도록 분산하기 위함.
        /// </summary>
        public IEnumerator InitCoroutine()
        {
            _Init();
            panelList = UIPanelBase.FindContainedPanelBase(this, false);

            int openIdx = 0;
            int closeIdx = -4;
            while (openIdx < panelList.Count || closeIdx < panelList.Count) {
                if (openIdx >= 0 && openIdx < panelList.Count) {
                    UISetter.SetActive(panelList[openIdx], true);
                }
                if (closeIdx >= 0 && closeIdx < panelList.Count) {
                    UISetter.SetActive(panelList[closeIdx], false);
                }
                openIdx++;
                closeIdx++;
                yield return null;
            }

            isValid = true;
        }

        virtual public void Release() { }        
    }

    /// <summary>
    /// 월드(로비) 화면 UI 그룹.
    /// 메인 화면(camp, mainCommand, worldMap, dailyBonus)은 _Init에서 미리 생성하고,
    /// 나머지 화면은 프로퍼티에 처음 접근할 때 생성해 panelList에 등록한다. (지연 생성 + 캐싱)
    /// </summary>
    [System.Serializable]
    public class World : Group
    {
        string prefix = "Prefabs/UI/World/";
        Transform parent = UIRoot.list[0].transform;

		public GameObject noticePopUp;

        public UICamp camp;
        public UIMainCommand mainCommand;

        // ── 지연 생성 화면들 ──
        // 패턴: private 필드 + Create 메서드(없으면 프리팹 생성 후 panelList 등록)
        //       + public 프로퍼티(접근 시 생성) + exist 프로퍼티(생성 여부 확인)
        private UIMetroBank _metroBank;
        private UIMetroBank CreateUIMetroBank()
        {
            if (_metroBank == null)
            {
                _metroBank = Utility.LoadAndInstantiateGameObject<UIMetroBank>(prefix + "MetroBank", parent);
                panelList.Add(_metroBank);
            }
            return _metroBank;
        }
        public UIMetroBank metroBank { get { return CreateUIMetroBank(); } }
		public bool existMetroBank { get { return _metroBank != null; } }

        public UIReward achievement;
        public UIReward mission;
        private UIWarHome _warHome;
        private UIWarHome CreateUIWarHome()
        {
            if (_warHome == null)
            {
                _warHome = Utility.LoadAndInstantiateGameObject<UIWarHome>(prefix + "Mission_1", parent);
                panelList.Add(_warHome);
            }
            return _warHome;
        }
        public UIWarHome warHome { get { return CreateUIWarHome(); } }
		public bool existWarHome { get { return _warHome != null; } }

        private UIReward _mail;
        private UIReward CreateUIReward()
        {
            if (_mail == null)
            {
                _mail = Utility.LoadAndInstantiateGameObject<UIReward>(prefix + "Mail", parent);
                panelList.Add(_mail);
            }
            return _mail;
        }
        public UIReward mail { get { return CreateUIReward(); } }

        private UIUserDetail _userDetail;
        private UIUserDetail CreateUIUserDetail()
        {
            if (_userDetail == null)
            {
                _userDetail = Utility.FindOrCreateGameObject<UIUserDetail>(prefix + "UserDetail", parent);
                panelList.Add(_userDetail);
            }
            return _userDetail;
        }
        public UIUserDetail userDetail { get { return CreateUIUserDetail(); } }

		public UIHeadQuarters _headQuarters;
        private UIHeadQuarters CreateUIHeadQuarters()
        {
            if (_headQuarters == null)
            {
                _headQuarters = Utility.FindOrCreateGameObject<UIHeadQuarters>(prefix + "HeadQuarters", parent);
                panelList.Add(_headQuarters);
            }
            return _headQuarters;
        }
        public UIHeadQuarters headQuarters { get { return CreateUIHeadQuarters(); } }
        public bool existHeadQuarters { get { return _headQuarters != null; } }

        private UIShop _shop;
        private UIShop CreateUIShop()
        {
            if (_shop == null)
            {
                _shop = Utility.FindOrCreateGameObject<UIShop>(prefix + "Shop", parent);
                panelList.Add(_shop);
            }
            return _shop;
        }
        public UIShop shop { get { return CreateUIShop(); } }
        
		private UIStorage _storage;
		private UIStorage CreateUIStorage()
		{
			if (_storage == null)
			{
				_storage = Utility.FindOrCreateGameObject<UIStorage>(prefix + "Storage", parent);
				panelList.Add(_storage);
			}
			return _storage;
		}
		public UIStorage storage { get { return CreateUIStorage(); } }
        
        private UIReadyBattle _readyBattle;
        private UIReadyBattle CreateUIReadyBattle()
        {
            if (_readyBattle == null)
            {
                _readyBattle = Utility.FindOrCreateGameObject<UIReadyBattle>(prefix + "ReadyBattle", parent);
                panelList.Add(_readyBattle);
            }
            return _readyBattle;
        }
        public UIReadyBattle readyBattle { get { return CreateUIReadyBattle(); } }
		public bool existReadyBattle { get { return _readyBattle != null; } }

		private UICommanderList _commanderList;
		private UICommanderList CreateUICommanderList()
		{
			if (_commanderList == null)
			{
				_commanderList = Utility.FindOrCreateGameObject<UICommanderList>(prefix + "CommanderList", parent);
				panelList.Add(_commanderList);
			}
			return _commanderList;
		}
		public UICommanderList commanderList { get { return CreateUICommanderList(); } }
		public bool existCommanderList { get { return _commanderList != null; } }

        private UIRankingBattle _rankingBattle;
        private UIRankingBattle CreateUIRankingBattle()
        {
            if (_rankingBattle == null)
            {
                _rankingBattle = Utility.FindOrCreateGameObject<UIRankingBattle>(prefix + "RankingBattle", parent);
                panelList.Add(_rankingBattle);
            }
            return _rankingBattle;
        }
        public UIRankingBattle rankingBattle { get { return CreateUIRankingBattle(); } }
        public bool existRankingBattle { get { return _rankingBattle != null; } }

        public UIWorldMap worldMap;

        private UILevelUp _levelUp;
        private UILevelUp CreateUILevelUp()
        {
            if (_levelUp == null)
            {
                _levelUp = Utility.FindOrCreateGameObject<UILevelUp>(prefix + "LevelUpPopUp", parent);
                panelList.Add(_levelUp);
            }
            return _levelUp;
        }
        public UILevelUp levelUp { get { return CreateUILevelUp(); } }

		private UIAnnihilationMap _annihilationMap;
		private UIAnnihilationMap CreateUIAnnihilationMap()
		{
			if (_annihilationMap == null)
			{
				_annihilationMap = Utility.FindOrCreateGameObject<UIAnnihilationMap>(prefix + "AnnihilationMap", parent);
				panelList.Add(_annihilationMap);
			}
			return _annihilationMap;
		}
		public UIAnnihilationMap annihilationMap { get { return CreateUIAnnihilationMap(); } }
		public bool existAnnihilationMap { get { return _annihilationMap != null; } }

        private UISeaRobberSweep _seaRobberSweep;
        private UISeaRobberSweep CreateUISeaRobberSweep()
        {
            if (_seaRobberSweep == null)
            {
                _seaRobberSweep = Utility.FindOrCreateGameObject<UISeaRobberSweep>(prefix + "SeaRobberSweep", parent);
                panelList.Add(_seaRobberSweep);
            }
            return _seaRobberSweep;
        }
        public UISeaRobberSweep seaRobberSweep { get { return CreateUISeaRobberSweep(); } }

        private UISituation _situation;
        private UISituation CreateUISituation()
        {
            if (_situation == null)
            {
                _situation = Utility.FindOrCreateGameObject<UISituation>(prefix + "Situation", parent);
                panelList.Add(_situation);
            }
            return _situation;
        }
        public UISituation situation { get { return CreateUISituation(); } }
		public bool existSituation { get { return _situation != null; } }

        public UIDailyBonus dailyBonus;

        private UIGacha _gacha;
        private UIGacha CreateUIGacha()
        {
            if (_gacha == null)
            {
                _gacha = Utility.FindOrCreateGameObject<UIGacha>(prefix + "Gacha", parent);
                panelList.Add(_gacha);
            }
            return _gacha;
        }
        public UIGacha gacha { get { return CreateUIGacha(); } }
		public bool existGacha { get { return _gacha != null; } }

		private UICommanderDetail _commanderDetail;
        private UICommanderDetail CreateUICommanderDetail()
        {
            if (_commanderDetail == null)
            {
                _commanderDetail = Utility.FindOrCreateGameObject<UICommanderDetail>(prefix + "CommanderDetail", parent);
                panelList.Add(_commanderDetail);
            }
            return _commanderDetail;
        }
        public UICommanderDetail commanderDetail { get { return CreateUICommanderDetail(); } }
        public bool existCommanderDetail { get { return _commanderDetail != null; } }

        private UIUnitUpgradeComplete _unitUpgradeComplete;
        private UIUnitUpgradeComplete CreateUIUnitUpgradeComplete()
        {
            if (_unitUpgradeComplete == null)
            {
                _unitUpgradeComplete = Utility.FindOrCreateGameObject<UIUnitUpgradeComplete>(prefix + "UIUnitUpgradeComplete", parent);
                panelList.Add(_unitUpgradeComplete);
            }
            return _unitUpgradeComplete;
        }
        public UIUnitUpgradeComplete unitUpgradeComplete { get { return CreateUIUnitUpgradeComplete(); } }
      
        private UIRaid _raid;
        private UIRaid CreateUIRaid()
        {
            if (_raid == null)
            {
                _raid = Utility.FindOrCreateGameObject<UIRaid>(prefix + "Raid", parent);
                panelList.Add(_raid);
            }
            return _raid;
        }
        public UIRaid raid { get { return CreateUIRaid(); } }
		public bool existRaid { get { return _raid != null; } }

        private UIGuild _guild;
        private UIGuild CreateUIGuild()
        {
            if (_guild == null)
            {
                _guild = Utility.LoadAndInstantiateGameObject<UIGuild>(prefix + "Guild", parent);
                panelList.Add(_guild);
            }
            return _guild;
        }
        public UIGuild guild { get { return CreateUIGuild(); } }

        private UIPreDeckSetting _preDeckSetting;
        private UIPreDeckSetting CreateUIPreDeckSetting()
        {
            if (_preDeckSetting == null)
            {
                _preDeckSetting = Utility.FindOrCreateGameObject<UIPreDeckSetting>(prefix + "PreDeckSetting", parent);
                panelList.Add(_preDeckSetting);
            }
            return _preDeckSetting;
        }
        public UIPreDeckSetting preDeckSetting { get { return CreateUIPreDeckSetting(); } }
        public bool existPreDeckSetting { get { return _preDeckSetting != null; } }

        // conversation
        public ClassicRpgManager dialogMgr;

        private UISecretShop _secretShop;
        private UISecretShop CreateUISecretShop()
        {
            if (_secretShop == null)
            {
                _secretShop = Utility.LoadAndInstantiateGameObject<UISecretShop>(prefix + "SecretShop", parent);
                panelList.Add(_secretShop);
            }
            return _secretShop;
        }
        public UISecretShop secretShop { get { return CreateUISecretShop(); } }
		public bool existSecretShop { get { return _secretShop != null; } }

        private UIVipShop _vipShop;
        private UIVipShop CreateUIVipShop()
        {
            if (_vipShop == null)
            {
                _vipShop = Utility.LoadAndInstantiateGameObject<UIVipShop>(prefix + "VipShop", parent);
                panelList.Add(_vipShop);
            }
            return _vipShop;
        }
        public UIVipShop vipShop { get { return CreateUIVipShop(); } }
        public bool existVipShop { get { return _vipShop != null; } }

        private UIAlarm _alarm;
		private UIAlarm CreateUIAlarm()
		{
			if (_alarm == null)
			{
				_alarm = Utility.LoadAndInstantiateGameObject<UIAlarm>(prefix + "Alarm", parent);
				panelList.Add(_alarm);
			}
			return _alarm;
		}
		public UIAlarm alarm { get { return CreateUIAlarm(); } }
        
        private UIVipGacha _vipGacha;
        private UIVipGacha CreateUIVipGacha()
        {
            if(_vipGacha == null)
            {
                _vipGacha = Utility.LoadAndInstantiateGameObject<UIVipGacha>(prefix + "VipGacha", parent);
                panelList.Add(_vipGacha);
            }
            return _vipGacha;
        }
        public UIVipGacha vipGacha { get { return CreateUIVipGacha(); } }

		private UICarnival _carnival;
		private UICarnival CreateUICarnival()
		{
			if(_carnival == null)
			{
				_carnival = Utility.LoadAndInstantiateGameObject<UICarnival>(prefix + "Carnival", parent);
				panelList.Add(_carnival);
			}
			return _carnival;
		}
		public UICarnival carnival { get { return CreateUICarnival(); } }
		public bool existCarnival { get { return _carnival != null; } }

        private UIGroup _group;
        private UIGroup CreateUIGroup()
        {
            if (_group == null)
            {
                _group = Utility.LoadAndInstantiateGameObject<UIGroup>(prefix + "Group", parent);
                panelList.Add(_group);
            }
            return _group;
        }
        public UIGroup group { get { return CreateUIGroup(); } }
        public bool existGroup { get { return _group != null; } }

        private UIConquestMap _conquestMap;
		private UIConquestMap CreateUIConquestMap()
		{
			if(_conquestMap == null)
			{
				_conquestMap = Utility.LoadAndInstantiateGameObject<UIConquestMap>(prefix + "ConquestMap", parent);
				panelList.Add(_conquestMap);
			}
			return _conquestMap;
		}
		public UIConquestMap conquestMap { get { return CreateUIConquestMap(); } }
		public bool existConquestMap { get { return _conquestMap != null; } }

        private UICooperateBattle _cooperateBattle;
        private UICooperateBattle CreateUICooperateBattle()
        {
            if (_cooperateBattle == null)
            {
                _cooperateBattle = Utility.LoadAndInstantiateGameObject<UICooperateBattle>(prefix + "CooperateBattle", parent);
                panelList.Add(_cooperateBattle);
            }

            return _cooperateBattle;
        }

        public UICooperateBattle cooperateBattle { get { return CreateUICooperateBattle(); } }
        public bool existCooperateBattle { get { return _cooperateBattle != null; } }

        private UIDateMode _dateMode;
        private UIDateMode CreateUIDateMode()
        {
            if (_dateMode == null)
            {
                _dateMode = Utility.LoadAndInstantiateGameObject<UIDateMode>(prefix + "DateMode", parent);
                panelList.Add(_dateMode);
            }
            return _dateMode;
        }
        public UIDateMode dateMode { get { return CreateUIDateMode(); } }
		public bool existDateMode { get { return _dateMode != null; } }

        private UIWaveBattle _waveBattle;
        private UIWaveBattle CreateUIWaveBattle()
        {
            if (_waveBattle == null)
            {
                _waveBattle = Utility.FindOrCreateGameObject<UIWaveBattle>(prefix + "WaveBattle", parent);
                panelList.Add(_waveBattle);
            }
            return _waveBattle;
        }
        public UIWaveBattle waveBattle { get { return CreateUIWaveBattle(); } }
        public bool existWaveBattle { get { return _waveBattle != null; } }

        private UILaboratory _laboratory;
        private UILaboratory CreateUILaboratory()
        {
            if (_laboratory == null)
            {
                _laboratory = Utility.FindOrCreateGameObject<UILaboratory>(prefix + "Laboratory", parent);
                panelList.Add(_laboratory);
            }
            return _laboratory;
        }
        public UILaboratory laboratory { get { return CreateUILaboratory(); } }
        public bool existLaboratory { get { return _laboratory != null; } }

        private UIWeaponResearch _weaponResearch;
        private UIWeaponResearch CreateUIWeaponResearch()
        {
            if (_weaponResearch == null)
            {
                _weaponResearch = Utility.FindOrCreateGameObject<UIWeaponResearch>(prefix + "WeaponResearch", parent);

                panelList.Add(_weaponResearch);
            }
            return _weaponResearch;
        }
        public UIWeaponResearch weaponResearch { get { return CreateUIWeaponResearch(); } }
        public bool existWeaponResearch { get { return _weaponResearch != null; } }

		private UIEventBattle _eventBattle;
		private UIEventBattle CreateUIEventBattle()
		{
			if (_eventBattle == null)
			{
				_eventBattle = Utility.FindOrCreateGameObject<UIEventBattle>(prefix + "EventBattle", parent);
				panelList.Add(_eventBattle);
			}
			return _eventBattle;
		}
		public UIEventBattle eventBattle { get { return CreateUIEventBattle(); } }
		public bool existEventBattle { get { return _eventBattle != null; } }

        private UIInfinityBattle _infinityBattle;
        private UIInfinityBattle CreateUIInfinityBattle()
        {
            if (_infinityBattle == null)
            {
                _infinityBattle = Utility.FindOrCreateGameObject<UIInfinityBattle> (prefix + "InfinityBattle", parent);

                panelList.Add(_infinityBattle);
            }
            return _infinityBattle;
        }
        public UIInfinityBattle infinityBattle { get { return CreateUIInfinityBattle(); } }
        public bool existInfinityBattle { get { return _infinityBattle != null; } }

        /**
         * M02_WorldMap 이 모두 초기화가 완료된 상태여부
         */
        public bool onStart = false;

        /// <summary>로비 진입 시 항상 필요한 메인 화면만 미리 생성한다.</summary>
        protected override void _Init()
        {
           string prefix = "Prefabs/UI/World/";
           Transform parent = UIRoot.list[0].transform;
            camp = Utility.LoadAndInstantiateGameObject<UICamp>(prefix + "Camp", parent);
            mainCommand = Utility.FindOrCreateGameObject<UIMainCommand>(prefix + "MainCommand", parent);
            worldMap = Utility.FindOrCreateGameObject<UIWorldMap>(prefix + "WorldMap", parent);
            dailyBonus = Utility.FindOrCreateGameObject<UIDailyBonus>(prefix + "DailyBonus", parent);
                            
            dialogMgr = CacheManager.instance.UiCache.Create<ClassicRpgManager>("ClassicRpgManager", parent);
            dialogMgr.gameObject.SetActive(false);

#if ENABLE_LOG
            //Utility.LoadAndInstantiateGameObject("Prefabs/Etc/ConsoleRemoteServer", parent);
#endif
        }        
    }

    /// <summary>전투/튜토리얼 UI 그룹. 전투 로직(AbstractBattle)과 전투 UI를 묶어 한 곳에서 접근하게 한다.</summary>
    [System.Serializable]
    public class Battle : Group
    {        
        protected AbstractBattle battleMain;
        protected UIBattleMain uiBattleMain;
        protected ClassicRpgManager dialogMgr;
        protected UIBattleResult battleResult;        

        public AbstractBattle Main { get { return battleMain; } }
        public UIBattleMain MainUI { get { return uiBattleMain; } }
        public ClassicRpgManager DialogMgr { get { return dialogMgr; } }
        public UIBattleResult BattleResult { get { return battleResult; } }
        public Shared.Battle.Simulator Simulator { get { return battleMain.Simulator; }  }
        public AbstractBattle.ViewData ViewData { get { return Main.viewData; } }


        public AbstractBattle.BattleTroopViewData LeftView
        {
            get { return Main.viewData.leftView; }
        }

        public AbstractBattle.BattleTroopViewData RightView
        {
            get { return Main.viewData.rightView; }
        }        

        /// <summary>
        /// 전투 UI, 결과 팝업, 대화 매니저를 캐시에서 생성하고,
        /// 씬에 있는 전투 메인(M03_Battle 또는 M04_Tutorial)을 찾아 연결한다.
        /// </summary>
        protected override void _Init()
        {
            Transform parent = UIRoot.list[0].transform;
            uiBattleMain = CacheManager.instance.UiCache.Create<UIBattleMain>("Battle", parent);
            uiBattleMain.name = "Battle";
            
            battleResult = CacheManager.instance.UiCache.Create<UIBattleResult>("BattleResultPopUp", parent);
            battleResult.name = "BattleResultPopUp";
            battleResult.gameObject.SetActive(false);

            dialogMgr = CacheManager.instance.UiCache.Create<ClassicRpgManager>("ClassicRpgManager", parent);
            dialogMgr.gameObject.SetActive(false);
            
            if (battleMain == null)
            {
                battleMain = GameObject.FindObjectOfType<M03_Battle>();
            }

            if (battleMain == null)
            {
                battleMain = GameObject.FindObjectOfType<M04_Tutorial>();
            }

            if (uiBattleMain == null)
            {
                uiBattleMain = battleMain.ui;
            }                   

            if (battleResult == null)
            {
                battleResult = GameObject.FindObjectOfType<UIBattleResult>();
            }

            panelList = UIPanelBase.FindContainedPanelBase(this, false);
        }        
    }


    /// <summary>시나리오(스토리 연출) UI 그룹.</summary>
    [System.Serializable]
    public class Scenario : Group
    {        
        protected ClassicRpgManager dialogMgr;                
        public ClassicRpgManager DialogMgr { get { return dialogMgr; } }

        /// <summary>시나리오 연출용 대화 매니저만 생성한다.</summary>
        protected override void _Init()
        {
            Transform parent = UIRoot.list[0].transform;            
            dialogMgr = CacheManager.instance.UiCache.Create<ClassicRpgManager>("ClassicRpgManager", parent);
            dialogMgr.gameObject.SetActive(false);
                        

            panelList = UIPanelBase.FindContainedPanelBase(this, false);
        }        
    } 
    
    /// <summary>MonoBehaviour가 아닌 클래스(Group 등)도 코루틴을 실행할 수 있도록 대신 실행해 준다.</summary>
    public void StartLocalCoroutine(IEnumerator routine)
    {        
        StartCoroutine(routine);
    }  

    public void StopLocalCoroutine(IEnumerator routine)
    {
        StopCoroutine(routine);
    }

    /// <summary>UI 카메라의 마우스/터치 입력을 켜거나 끈다. (연출 중 입력 차단 등에 사용)</summary>
    public void EnableCameraTouchEvent(bool isEnable)
    {
        GameObject uiCamera = GameObject.Find("Camera");
		if(uiCamera.GetComponent<UICamera>() == null)
			return;
        uiCamera.GetComponent<UICamera>().useMouse = isEnable;
        uiCamera.GetComponent<UICamera>().useTouch = isEnable;

        Debug.Log("EnableCameraTouchEvent:" + isEnable);
    }
}
