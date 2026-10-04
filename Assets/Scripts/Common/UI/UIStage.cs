using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Shared.Regulation;

/// <summary>
/// 월드맵 / 점령 맵의 스테이지 아이콘 항목.
/// 스테이지 타입(공장, 테러리스트, 초소, 창고 등)과 클리어 상태에 따라 아이콘, 별, 깃발, 사용 불가 표시를 바꾼다.
/// </summary>
public class UIStage : UIItemBase {
    public UISprite icon;
	public UILabel lbStage;

    public GameObject openRoot;
    public GameObject lockRoot;
    public GameObject selectRoot;
	public GameObject badge;
	public GameObject unUse;
	public UISprite flag;
	public GameObject starRoot;

	public UIProgressBar hpProgress;

	private static readonly Color LockedStageLabelColor = new Color(225.0f / 255.0f, 96.0f / 255.0f, 0, 1);

    public override void SetSelection(bool selected)
    {
        base.SetSelection(selected);
        UISetter.SetActive(selectRoot, selected);
    }

    public override void SetLock(bool locked)
    {
        base.SetLock(locked);
        UISetter.SetActive(lockRoot, locked);
    }

    /// <summary>초소 / 창고처럼 아이콘 없이 건물로 표시되는 스테이지인지 확인한다.</summary>
    private static bool IsBuildingStage(EStageTypeIdRange type)
    {
        return type == EStageTypeIdRange.GuardPost || type == EStageTypeIdRange.Storage;
    }

    /// <summary>
    /// 월드맵 스테이지 표시.
    /// - 건물형(초소/창고) 스테이지는 아이콘을 숨기고, 열렸지만 도전 횟수를 다 쓰면 사용 불가로 표시한다.
    /// - 공장/테러리스트는 클리어하면 깃발을, 공장/테러리스트/창고는 별 등급을 표시한다.
    /// </summary>
    public void Set(RoWorldMap.Stage stage)
    {
        var typeRow = stage.typeData;
		var localUser = RemoteObjectManager.instance.localUser;
		EStageTypeIdRange type = stage.data.type;
		bool isBuilding = IsBuildingStage(type);

		UISetter.SetActive(badge, false);
        UISetter.SetSprite(icon, typeRow.resourceId, true);

        UISetter.SetActive(openRoot, true);
		UISetter.SetColor(icon, (stage.canBattle || !isBuilding) && stage.isOpen ? Color.white : Color.gray);
		UISetter.SetColor(lbStage, stage.isOpen ? Color.white : LockedStageLabelColor);

		UISetter.SetActive(unUse, isBuilding && stage.isOpen && !stage.canBattle);
		UISetter.SetActive(icon, !isBuilding);

		// 아이콘이 없는 건물형 스테이지는 선택 표시를 아래쪽에 둔다
		selectRoot.transform.localPosition = icon.gameObject.activeSelf ? new Vector3(0, 63, 0) : new Vector3(0, -15, 0);

		bool isBattleStage = type == EStageTypeIdRange.Factory || type == EStageTypeIdRange.Terrorist;
		UISetter.SetActive(starRoot, isBattleStage || type == EStageTypeIdRange.Storage);
		UISetter.SetRank(starRoot, stage.star);
		UISetter.SetActive(flag, isBattleStage && localUser.lastClearStage >= int.Parse(stage.id));
		UISetter.SetSprite(flag, stage.typeData.battleCount == 3 ? "stage_nomal_blue" : "stage_nomal_skyblue");

		lbStage.text = string.Format("{0} - {1}", stage.data.worldMapId, stage.data.order);
    }

	/// <summary>점령 맵 스테이지 표시. 남은 체력(%)과 클리어 여부를 보여준다.</summary>
	public void Set(RoScramble.Stage stage)
	{
		UISetter.SetActive(openRoot, true);
		UISetter.SetActive(lockRoot, !stage.isOpen);
		UISetter.SetColor(icon, !stage.clear && stage.isOpen ? Color.white : Color.gray);
		UISetter.SetActive(hpProgress, stage.isOpen);
		UISetter.SetProgress(hpProgress, (float)stage.hp / 100.0f);

		UISetter.SetActive(unUse, stage.clear);
	}
}
