
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 모든 UI 화면(패널)의 공통 베이스 클래스.
/// - 서버 데이터(localUser, regulation)와 UIManager에 접근하는 단축 프로퍼티 제공
/// - NGUI UIPanel depth 정렬(최상단 이동, depth 정규화) 관리
/// - 클릭 잠금(LockClick)과 클릭 이벤트 전달
/// - 리플렉션으로 하위 파츠(UIInnerPartBase)를 자동 수집해 Init/Refresh/Click 이벤트를 전파
/// </summary>
public class UIPanelBase : MonoBehaviour
{
    // ── 공통 접근자: 하위 UI에서 싱글톤 경로를 매번 쓰지 않도록 단축 프로퍼티로 제공 ──
    protected RoLocalUser localUser
    {
        get
        {
            return RemoteObjectManager.instance.localUser;
        }
    }

    protected Shared.Regulation.Regulation regulation
    {
        get
        {
            return RemoteObjectManager.instance.regulation;
        }
    }

    protected RemoteObjectManager network
    {
        get
        {
            return RemoteObjectManager.instance;
        }
    }

    protected UIManager.World uiWorld
    {
        get
        {
            return UIManager.instance.world;
        }
    }

    protected UIManager.Battle uiBattle
    {
        get
        {
            return UIManager.instance.battle;
        }
    }
    
	// 열린 순서대로 쌓이는 UI 목록 (마지막 노드가 최상단 UI)
	static private LinkedList<GameObject> _openedUIList = 
		new LinkedList<GameObject>();
    // holdDepth가 켜진 패널 목록: depth 정규화 때 원래 depth를 유지할 대상
    static private Dictionary<UIPanel, UIPanelBase> _holdDepthTargetDict =
        new Dictionary<UIPanel, UIPanelBase>();
    // 정규화하기 전 각 패널의 최초 depth 기록
    static private Dictionary<UIPanel, int> _originalDepthDict =
        new Dictionary<UIPanel, int>();
    public delegate void OnClickDelegate (GameObject sender);
    public OnClickDelegate onClick;

	// 클릭 잠금 카운터. 네트워크 요청 중 중복 클릭을 막는 용도이며,
	// 여러 곳에서 겹쳐 잠글 수 있도록 bool 대신 카운트로 관리
	private int _lockClickCount = 0;
    
	public bool blockClick{ 
		get { return _lockClickCount > 0; } }

	// true면 활성화되어도 최상단 정렬 대상에서 제외 (HUD처럼 고정된 UI)
	public bool dontMoveToFront = false;
    // true면 depth 정규화 때도 원래 depth 유지
    public bool holdDepth = false;
	public GameObject root;
	public UIPanel panel;
	public int originalDepth;
    // 이 패널에 포함된 하위 파츠 목록 (Awake에서 리플렉션으로 자동 수집)
    protected List<UIInnerPartBase> _innerPartsList =
        new List<UIInnerPartBase>();

    protected bool _popupState = false;

    public bool isActive { 
        get 
        {
            return (root == null) ? gameObject.activeSelf : root.activeSelf; 
        } 
    }

    /// <summary>root의 UIPanel과 원래 depth를 기록하고, 하위 파츠를 수집한다.</summary>
    virtual protected void Awake()
    {
		if(root != null)
		{
			panel = root.GetComponent<UIPanel>();
			if(panel != null)
				originalDepth = panel.depth;

			if (holdDepth) {
				if (panel != null) {
					_holdDepthTargetDict.Add(panel, this);
				} else {
					Debug.LogError("null : " + Utility.GetGameObjectPath(gameObject));
				}
			}
		}
        _innerPartsList = _CollectInnerParts();
    }

	/// <summary>클릭 잠금. 반드시 UnlockClick과 짝을 맞춰 호출한다.</summary>
	public void LockClick()
	{
		_lockClickCount++;
	}
	/// <summary>클릭 잠금 해제. 카운트가 음수가 되면 경고를 남기고 0으로 보정한다.</summary>
	public void UnlockClick()
	{
		_lockClickCount--;
		if (_lockClickCount < 0) {
			Debug.LogWarning("invalid state");
			_lockClickCount = 0;
		}
	}
	virtual protected void OnEnable()
	{
		if (dontMoveToFront)
			return;
	}

	/// <summary>비활성화 시 depth를 원래 값으로 되돌리고, 열린 UI 목록에서 제거한다. (최근 것부터 역순 탐색)</summary>
	virtual protected void OnDisable()
	{
		if(panel != null)
			panel.depth = originalDepth;
		if (dontMoveToFront)
			return;
		LinkedListNode<GameObject> node = _openedUIList.Last;
		while (node != null) {
			if(node.Value == root){
				_openedUIList.Remove(node);
				break;
			}
			node = node.Previous;
		}
	}

    /// <summary>팝업 스택 최상단 여부를 설정하고, 해당 콜백(OnEnablePopup/OnDisablePopup)을 호출한다.</summary>
    protected void SetPopupState(bool state)
    {
        _popupState = state;

        if (_popupState)
            OnEnablePopup();
        else
            OnDisablePopup();
    }

    /**
     * 팝업이 최상단에 위치 할때
     */
    virtual protected void OnEnablePopup() { }

    /**
     * 팝업이 최상단이 아니게 될때
     */
    virtual protected void OnDisablePopup() { }

    /// <summary>NGUI 버튼 클릭 진입점. 클릭이 잠겨 있으면 무시하고, 등록된 onClick 델리게이트로 전달한다.</summary>
    virtual public void OnClick(GameObject sender)
    {
        if (blockClick) return;
        if (onClick != null) onClick(sender);
    }

    /// <summary>활성화된 하위 파츠들에 클릭 이벤트를 전파한다.</summary>
    protected void SendOnClickToInnerParts(GameObject sender)
    {
        _innerPartsList.ForEach(part => {
            if (part.isActive) { 
                part.OnClick(sender, this);
            }
        });
    }
    /// <summary>모든 하위 파츠를 초기화한다.</summary>
    protected void SendOnInitToInnerParts()
    {
        _innerPartsList.ForEach(part => {
            	part.OnInit(this);
        });
    }
    /// <summary>활성화된 하위 파츠들을 갱신한다.</summary>
    protected void SendOnRefreshToInnerParts()
    {
        _innerPartsList.ForEach(part => {
            if (part.isActive) {
                part.OnRefresh();
            }
        });
    }

    /// <summary>데이터가 바뀌었을 때 화면을 갱신한다. UIManager.RefreshOpenedUI()를 통해 호출된다.</summary>
    virtual public void OnRefresh() { }
    
    /// <summary>이 UI를 열린 목록 맨 뒤(최상단)에 추가하고 전체 depth를 재정렬한다. 이미 최상단이면 무시한다.</summary>
	public void MoveToFront()
	{
        if (_openedUIList.Count > 0 && _openedUIList.Last.Value == root) 
		{
			return;
		}
		_openedUIList.AddLast (root);
        NormalizePanelDepths();
	}

    /// <summary>
    /// NGUI 패널 depth 정규화.
    /// depth 순으로 정렬한 뒤 같은 depth끼리는 같은 값으로 묶어, 0부터 연속된 값으로 다시 매긴다.
    /// UI를 여닫을수록 depth 값이 계속 커지는 문제를 막기 위함.
    /// </summary>
    public void NormalizePanelDepths()
    {
        UIRoot uiRoot = UIRoot.list[0];
        UIPanel[] list = uiRoot.GetComponentsInChildren<UIPanel>();
        int size = list.Length;

        if (size > 0) {
            System.Array.Sort(list, UIPanel.CompareFunc);

            int start = 0;
            int current = list[0].depth;

            for (int i = 0; i < size; ++i) {
                UIPanel p = list[i];
                if (!_originalDepthDict.ContainsKey(p)) {
                    _originalDepthDict.Add(p, p.depth);
                }
                if (_holdDepthTargetDict.ContainsKey(p)) {
                    p.depth = _originalDepthDict[p];
                    return;
                }

                if (p.depth == current) {
                    p.depth = start;
                } else {
                    current = p.depth;
                    p.depth = ++start;
                }
            }
        }
    }

    //3d오브젝트를 여러개 띄워야하면 사용. 갯수가 적을땐 랜더타겟이용. 이걸 사용하게 되는 상황은 최악의 상황.
    private static readonly float zPosOffset = 2.0f;
    static protected void SetPanelZPosition(UIPanel p, float originZPos)
    {
        Vector3 pos = p.transform.position;
        pos.z = originZPos - p.depth * zPosOffset;
        p.transform.position = pos;
    }

    /// <summary>이 패널이 필드로 가진 하위 UIPanelBase 목록을 반환한다.</summary>
    public List<UIPanelBase> GetPanelBaseList(bool includeSelf)
    {
        return FindContainedPanelBase(this, includeSelf);
    }

    /// <summary>
    /// 리플렉션으로 parent의 public 필드 중 UIPanelBase 타입을 모두 찾아 반환한다.
    /// UIManager의 World/Battle 그룹이 관리할 패널 목록을 자동으로 만들 때 사용한다.
    /// </summary>
    static public List<UIPanelBase> FindContainedPanelBase(object parent, bool includeParent = true)
    {
        List<UIPanelBase> list = new List<UIPanelBase>();
        if (parent == null)
            return list;

        System.Type type = parent.GetType();
        FieldInfo[] fieldList = type.GetFields();
        System.Type targetType = typeof(UIPanelBase);
        if (includeParent && type.IsSubclassOf(targetType))
            list.Add(parent as UIPanelBase);
        for (int idx = 0; idx < fieldList.Length; ++idx) {
            FieldInfo fi = fieldList[idx];
            UIPanelBase pb = fi.GetValue(parent) as UIPanelBase;
            if (pb != null) {
                list.Add(pb);
            }
        }
        return list;
    }

    /// <summary>
    /// 리플렉션으로 UIInnerPartBase 타입의 public 필드를 수집한다.
    /// 파츠를 추가해도 등록 코드를 따로 작성할 필요가 없다.
    /// </summary>
    private List<UIInnerPartBase> _CollectInnerParts()
    {
        List<UIInnerPartBase> list = new List<UIInnerPartBase>();
        System.Type type = this.GetType();
        FieldInfo[] fiList = type.GetFields();
        for (int idx = 0; idx < fiList.Length; ++idx) {
            if (fiList[idx].FieldType.IsSubclassOf(typeof(UIInnerPartBase))) {
                list.Add(fiList[idx].GetValue(this) as UIInnerPartBase);
            }
        }
        return list;
    }
}
