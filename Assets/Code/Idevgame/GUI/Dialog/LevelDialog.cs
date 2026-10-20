// Modified for NUKE HOUR iOS/resource-free adaptation, 2026. Original notices retained.
﻿using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using Idevgame.GameState.DialogState;
using Excel2Json;
using System.Collections.Generic;

public class LevelDialogState:CommonDialogState<LevelDialog>
{
    public override string DialogName { get { return "LevelDialog"; } }
    public LevelDialogState(MainDialogMgr stateMgr):base(stateMgr)
    {

    }

    public override void OnAction(DialogAction dialogAction, object data)
    {
        switch (dialogAction)
        {
            case DialogAction.Close:
            case DialogAction.BackButton:
                ChangeState(null);
                break;
            case DialogAction.Previous:
                if (previousS != null)
                    ChangeState(previousS);
                else
                    ChangeState(Main.Ins.DialogStateManager.MainMenuState);
                break;
            default:
                break;
        }
    }
}

//关卡选择界面
public class LevelDialog : Dialog {
    [SerializeField]
    GameObject rootMenu;
    [SerializeField]
    Image background;
    [SerializeField]
    Button Yes;
    [SerializeField]
    Button Cancel;
    [SerializeField]
    Text Task;
    bool normalLevel;//是普通关卡还是资料片关卡
    public override void OnDialogStateEnter(BaseDialogState ownerState, BaseDialogState previousDialog, object data)
    {
        base.OnDialogStateEnter(ownerState, previousDialog, data);
        normalLevel = (bool)data;
        Init();
    }
    Dictionary<string, Button> levelBtns = new Dictionary<string, Button>();
    Button selectedBtn;
    Text Description;
    Sprite previewSprite;
    Texture2D ownedPreviewTexture;
    void Init()
    {
        BuildDescription();
        Yes.onClick.AddListener(() =>
        {
            OnEnterLevel();
        });
        Cancel.onClick.AddListener(() =>
        {
            OnPreviousPress();
        });

        // Show the complete catalog; progression gates entry rather than visibility.
        List<LevelData> levels = normalLevel ? DataMgr.Ins.GetLevelDatas() : CombatData.Ins.Chapter.LoadAll();
        levels.Sort((a, b) => a.Id.CompareTo(b.Id));
        select = null;
        foreach (LevelData lev in levels)
        {
            if (lev == null) continue;
            Idevgame.Util.LevelUtils.AddGridItem(lev, rootMenu.transform, OnSelectLevel);
            Transform row = rootMenu.transform.GetChild(rootMenu.transform.childCount - 1);
            Text label = row.GetComponentInChildren<Text>();
            bool unlocked = IsUnlocked(lev);
            label.text = lev.Name + (unlocked ? "" : "（未解锁）");
            if (!unlocked) label.color = new Color(0.6f, 0.6f, 0.6f, 1);
            if (unlocked) select = lev;
        }
        if (select == null && levels.Count > 0) select = levels[0];
        for (int i = 0; i < rootMenu.transform.childCount; i++) {
            Transform tri = rootMenu.transform.GetChild(i);
            Button btn = tri.GetComponent<Button>();
            levelBtns.Add(tri.name, btn);
        }
        if (select != null) OnSelectLevel(select);
        else Yes.interactable = false;
    }

    void BuildDescription()
    {
        if (Description != null) return;
        var panel = new GameObject("MapDescriptionPanel", typeof(RectTransform), typeof(Image));
        panel.layer = gameObject.layer;
        var rect = panel.GetComponent<RectTransform>();rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(.36f, .19f);rect.anchorMax = new Vector2(.95f, .47f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var shade = panel.GetComponent<Image>();shade.color = new Color(.035f, .03f, .025f, .82f);shade.raycastTarget = false;
        var text = new GameObject("MapDescription", typeof(RectTransform), typeof(Text));text.layer = gameObject.layer;
        var body = text.GetComponent<RectTransform>();body.SetParent(rect, false);body.anchorMin = Vector2.zero;body.anchorMax = Vector2.one;
        body.offsetMin = new Vector2(24, 20);body.offsetMax = new Vector2(-24, -20);
        Description = text.GetComponent<Text>();Description.font = Task.font;Description.fontSize = 26;
        Description.color = new Color(.96f, .92f, .81f);Description.alignment = TextAnchor.UpperLeft;
        Description.raycastTarget = false;Description.supportRichText = false;
        Description.horizontalOverflow = HorizontalWrapMode.Wrap;Description.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void ReleasePreview()
    {
        if (background != null) background.sprite = null;
        DisposePreview(previewSprite);DisposePreview(ownedPreviewTexture);
        previewSprite = null;ownedPreviewTexture = null;
    }
    void DisposePreview(UnityEngine.Object item)
    {
        if (item == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) { DestroyImmediate(item);return; }
#endif
        Destroy(item);
    }
    protected override void OnDestroy() { ReleasePreview();base.OnDestroy(); }

    bool IsUnlocked(LevelData lev)
    {
        return lev != null && lev.Id <= (normalLevel ? GameStateMgr.Ins.gameStatus.Level : CombatData.Ins.Chapter.level);
    }

    LevelData select;
    void OnSelectLevel(LevelData lev)
    {
        ReleasePreview();
        Texture2D loadingTexture = null;
        if (!string.IsNullOrEmpty(lev.BgTexture))
        {
            if (normalLevel)
            {
                Texture2D preview = global::Meteor.Launcher.ContentResources.Load<Texture2D>(lev.BgTexture);
                if (preview != null) loadingTexture = preview;
              }
            else
            {
                for (int i = 0; i < CombatData.Ins.Chapter.resPath.Count; i++)
                {
                    if (CombatData.Ins.Chapter.resPath[i].EndsWith(lev.BgTexture + ".jpg"))
                    {
                        byte[] array = System.IO.File.ReadAllBytes(CombatData.Ins.Chapter.resPath[i]);
                        Texture2D tex = new Texture2D(0, 0, TextureFormat.ARGB32, false);
                        tex.LoadImage(array);
                        loadingTexture = tex;
                        ownedPreviewTexture = tex;
                        break;
                    }
                }
            }
        }

        if (loadingTexture != null) {
            background.sprite = previewSprite = Sprite.Create(loadingTexture, new Rect(0, 0, loadingTexture.width, loadingTexture.height), Vector2.zero);
            background.color = Color.white;
        }
        if (loadingTexture != null)
        {
            Utility.Expand(background, loadingTexture.width, loadingTexture.height);
            Control("Image").GetComponent<RectTransform>().sizeDelta = background.GetComponent<RectTransform>().sizeDelta;
        }
        else
        {
            background.sprite = null;
            background.color = Color.clear;
        }
        select = lev;
        Task.text = select.Name + (IsUnlocked(select) ? "" : "\n未解锁");
        Task.resizeTextForBestFit = true;Task.resizeTextMinSize = 20;Task.resizeTextMaxSize = 32;
        Yes.interactable = IsUnlocked(select);
        Description.text = select.Name + "\n" + (String.IsNullOrEmpty(select.Desc) ? "暂无地图介绍" : select.Desc)
            + (String.IsNullOrEmpty(select.Mission) ? "" : "\n\n关卡目标：" + select.Mission);
        if (selectedBtn != null) {
            selectedBtn.image.color = new Color(1, 1, 1, 0);
            selectedBtn = null;
        }
        selectedBtn = levelBtns[lev.Name];
        selectedBtn.image.color = new Color(144.0f / 255.0f, 104.0f / 255.0f, 104.0f / 255.0f, 104.0f / 255.0f);
    }

    void OnEnterLevel()
    {
        if (select != null && IsUnlocked(select))
        {
            //单机全部是普通关卡对待.
            if (normalLevel)
            {
                U3D.LoadLevel(select, LevelMode.SinglePlayerTask, GameMode.Normal);
            }
            else
            {
                DlcMng.Ins.PlayDlc(CombatData.Ins.Chapter, select.Id);
            }
            if (background.material == null)
                OnBackPress();
        }
    }
}