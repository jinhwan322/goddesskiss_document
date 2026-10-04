using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//UIRoot(0번을 사용한다. UIRoot.list[0])와 UICamera만 구성되어 있으면, 언제든 호출시 화면에 나타나야함.
/// <summary>
/// 팝업 UI 베이스 클래스 (UIPanelBase 상속).
/// - 정적 Create()로 프리팹을 로드해 어디서든 즉시 생성
/// - openedPopups 스택으로 최상단 팝업 추적
/// - WaitResult() 코루틴으로 사용자 선택을 기다리는 흐름 제어
/// - 닫을 때 파괴할지, 비활성화 후 재사용할지 선택
/// </summary>
public class UIPopup : UIPanelBase {	

    // onClick 콜백이 실행 중인 팝업 (콜백 안에서 자신을 참조할 때 사용)
    static public UIPopup current = null;
	// 현재 활성화된 팝업 수
	static private int _popupCount = 0;

    // 열린 팝업 스택. 0번 인덱스가 최상단 팝업
    static public List<UIPopup> openedPopups = new List<UIPopup>();

    public UILabel title;
    public UILabel message;

	// 사용자가 선택(버튼 클릭)을 마쳤는지 여부 - WaitResult 종료 조건
	protected bool _isSelected = false;
    // 선택 후 자동으로 닫을지 여부
    protected bool _autoDestroy = true;
    // true면 닫을 때 Destroy하지 않고 비활성화만 해서 재사용
    private bool _recyclable = false;
	protected bool _isClosed = false;
    // WaitResult로 대기 중인 코루틴 수. 0일 때만 자동으로 닫는다
    protected int _waitRoutineCount = 0;

    public delegate void OnCloseDelegate();
    public OnCloseDelegate onClose;
    
    /// <summary>씬 전환 시 팝업 스택을 초기화한다.</summary>
    static public void InitUIPopup()
    {
        openedPopups.Clear();
    }
	
	override protected void OnEnable()
	{
		base.OnEnable ();
		++_popupCount;
        _isClosed = false;
	}

	override protected void OnDisable()
	{
		base.OnDisable ();
		--_popupCount;
	}

    /// <summary>
    /// 파생 팝업(전용 UI)은 기본적으로 재사용하고, 기본 UIPopup(메시지 팝업)은 자동 파괴하도록 설정한다.
    /// 에디터에서는 뒤로가기 키 처리 컴포넌트가 빠졌는지 검사한다.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        if(this.GetType() != typeof(UIPopup)){
            _autoDestroy = false;
            _recyclable = true;
        }
#if UNITY_EDITOR
        if (GetComponent<ForwardBackKeyEvent>() == null) {
            Debug.LogWarning("missing BackKeyEvent : " + Utility.GetGameObjectPath(gameObject));
        }
#endif
    }

    /// <summary>팝업 클릭 처리. 등록된 onClick을 실행한 뒤, 대기 중인 코루틴이 없으면 자동으로 닫는다.</summary>
	public override void OnClick (GameObject sender)
    {
        if (onClick != null) 
		{
            current = this;
            base.OnClick(sender);
            current = null;
            if (_autoDestroy && _waitRoutineCount <= 0) {
                Close();
            }
            return;
        }
	}   

	/// <summary>
	/// 팝업 결과를 기다리는 코루틴. 호출하는 쪽에서 yield return popup.WaitResult(); 형태로 사용한다.
	/// - 재사용 팝업: 팝업이 비활성화될 때까지 대기
	/// - 일회성 팝업: 선택 완료(_isSelected)까지 대기한 뒤 자동으로 닫음
	/// </summary>
	public Coroutine WaitResult()
	{
		return StartCoroutine (_WaitResult ());
	}

	private IEnumerator _WaitResult()
	{
        if (!_recyclable) {
            ++_waitRoutineCount;
            while (!_isSelected) {
                yield return null;
            }
            --_waitRoutineCount;
            if (_autoDestroy && _waitRoutineCount <= 0) {
                Close();
            }
        } else {
            while (gameObject.activeSelf) {
                yield return null;
            }
        }
		yield break;
	}   

    /// <summary>
    /// 팝업 닫기. 스택에서 제거한 뒤 다음 팝업을 최상단 상태로 전환하고 onClose 콜백을 호출한다.
    /// 재사용 팝업이 아니면 GameObject를 파괴한다. (_isClosed로 중복 호출 방지)
    /// </summary>
    public virtual void Close()
	{
		if (_isClosed)
            return;
        _isClosed = true;
        
        // 메인 화면(UIMainCommand)은 팝업 스택 관리 대상에서 제외
        if (!(this is UIMainCommand))
        {            
            SetPopupState(false);

            if(!openedPopups.Remove(this))
			{
				Debug.Log("Remove false");
			}

            if (openedPopups.Count > 0)
            {
                if (UIManager.instance.world != null)
                {
                    openedPopups[0].SendMessage("SetPopupState", true, SendMessageOptions.DontRequireReceiver);
                }
            }           
        }        

        StopAllCoroutines();
		gameObject.SetActive (false);


        if (onClose != null)
        {
            onClose();
        }

        if (!_recyclable) {
            GameObject.Destroy(gameObject);
        }
	}

    /// <summary>팝업 열기. 기존 최상단 팝업을 비활성 상태로 돌리고, 자신을 스택 맨 앞에 넣은 뒤 최상단으로 정렬한다.</summary>
    public virtual void Open()
    {
        if (root != null)
            root.SetActive(true);        

        // 메인 화면(UIMainCommand)은 팝업 스택 관리 대상에서 제외
        if (!(this is UIMainCommand))
        {
            if (openedPopups.Count > 0)
            {
                if (UIManager.instance.world != null)
                {
                    openedPopups[0].SendMessage("SetPopupState", false, SendMessageOptions.DontRequireReceiver);
                }
            }

            openedPopups.Insert(0, this);

            SetPopupState(true);            
        }

		MoveToFront();
    }

    /// <summary>닫을 때 파괴할지 설정한다. 자동 파괴를 켜면 재사용은 꺼진다.</summary>
	public void SetAutoDestroy(bool autoDestroy)
	{
		_autoDestroy = autoDestroy;
        _recyclable = _recyclable && !_autoDestroy;
	}
	
	/// <summary>닫을 때 재사용(비활성화만)할지 설정한다.</summary>
    public void SetRecyclable(bool recyclable)
    {
        _recyclable = recyclable;
    }
    
    /// <summary>
    /// Prefabs/UI/Popup/ 경로의 프리팹을 UIRoot 아래에 생성하고 상태를 초기화한다.
    /// 생성 직후 입력이 막혀 있지 않도록 UI 카메라의 터치 입력을 켠다.
    /// </summary>
	static protected UIPopup _Create(string prefabName)
	{
		const string path = "Prefabs/UI/Popup/";
		UIRoot root = UIRoot.list [0];
		if (root == null) {
			Debug.LogError("invalid state");
			return null;
		}
		GameObject go = 
			Utility.LoadAndInstantiateGameObject (
				path + prefabName, root.transform
				);
		if (go == null) {
			Debug.LogError ("invalid state");
			return null;
		}

		UIPopup popup = go.GetComponent<UIPopup> ();
		if (popup == null) {
			Debug.LogError("invalid state");
			return null;
		}
		popup._isSelected = false;
		popup._waitRoutineCount = 0;
        popup.gameObject.SetActive(true);

        UIManager.instance.EnableCameraTouchEvent(true);
        return popup;
    }
    /// <summary>팝업을 생성하고 지정한 컴포넌트 타입으로 반환한다.</summary>
    static public T Create<T>(string prefabName) where T : MonoBehaviour
    {
        UIPopup popup = _Create(prefabName);
        if (popup == null)
            return null;
        return popup.GetComponent<T>();
    }
    /// <summary>기본 UIPopup 타입으로 팝업을 생성한다.</summary>
    static public UIPopup Create(string prefabName)
    {
        return Create<UIPopup>(prefabName);
    }

    /// <summary>열기 연출: 화면 아래(y=-1000)에서 원위치로 튀어 오르듯 이동한다. (iTween easeOutBack, 0.3초)</summary>
    public void DefaultOpenAnimation()
    {
        if (root != null) {
            root.transform.localPosition = Vector3.up * -1000.0f;
        }
        iTween.MoveTo(this.gameObject, iTween.Hash("y", 0,
                                                   "islocal", true,
                                                   "time", 0.3,
                                                   "delay", 0,
                                                   "easeType", iTween.EaseType.easeOutBack));
    }

    /// <summary>닫기 연출: 아래로 내려간 뒤 완료되면 Close()를 호출한다. (iTween easeInBack, 0.3초)</summary>
    public void DefaultCloseAnimation()
    {
        iTween.MoveTo(this.gameObject, iTween.Hash("y", -1000,
                                                   "islocal", true,
                                                   "time", 0.3,
                                                   "delay", 0,
                                                   "easeType", iTween.EaseType.easeInBack,
                                                   "oncomplete", "Close",
                                                   "oncompletetarget", this.gameObject));
    }
}
