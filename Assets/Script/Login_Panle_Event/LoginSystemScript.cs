﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using AssemblyCSharp;
using LitJson;
using System.Collections.Generic;
using cn.sharesdk.unity3d;


public class LoginSystemScript : MonoBehaviour {
	

	//public ShareSDK shareSdk;
	private GameObject panelCreateDialog;

	public Toggle agreeProtocol;

	public Text versionText;

	private int tapCount = 0;//点击次数
	public GameObject watingPanel;

  public GameObject agreement;
  public GameObject wechatLogin;
  public GameObject guestLogin;
  public bool canGuestLogin;
  public bool canWechatLogin;


	void Start () {
		CustomSocket.getInstance().hasStartTimer = false;
		CustomSocket.getInstance ().Connect ();
		ChatSocket.getInstance ().Connect ();
		GlobalDataScript.isonLoginPage = true;
		SocketEventHandle.getInstance ().UpdateGlobalData += UpdateGlobalData;
		SocketEventHandle.getInstance ().LoginCallBack += LoginCallBack;
		SocketEventHandle.getInstance ().RoomBackResponse += RoomBackResponse;
		versionText.text ="版本号：" +Application.version;

#if UNITY_IPHONE
    canGuestLogin = GlobalDataScript.inAppPay;
    canWechatLogin = GlobalDataScript.getInstance().wechatOperate.isWechatValid();
    agreement.SetActive(!canGuestLogin);
    guestLogin.SetActive(canGuestLogin);
    if (canGuestLogin) {
      wechatLogin.SetActive(canWechatLogin);
    }
#endif
  }

  // Update is called once per frame
  void Update () {
		if(Input.GetKey(KeyCode.Escape)){ //Android系统监听返回键，由于只有Android和ios系统所以无需对系统做判断
			if (panelCreateDialog == null) {
				panelCreateDialog = Instantiate (Resources.Load("Prefab/Panel_Exit")) as GameObject;
				panelCreateDialog.transform.parent = gameObject.transform;
				panelCreateDialog.transform.localScale = Vector3.one;
				//panelCreateDialog.transform.localPosition = new Vector3 (200f,150f);
				panelCreateDialog.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 0f);
				panelCreateDialog.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 0f);
			}
		}

#if UNITY_IPHONE
    if (canGuestLogin != GlobalDataScript.inAppPay) {
      canGuestLogin = GlobalDataScript.inAppPay;
      agreement.SetActive(!canGuestLogin);
      guestLogin.SetActive(canGuestLogin);
      if (canGuestLogin) {
        wechatLogin.SetActive(canWechatLogin);
      } else {
        wechatLogin.SetActive(true);
      }
    }

    if (canWechatLogin != GlobalDataScript.getInstance().wechatOperate.isWechatValid()) {
      canWechatLogin = GlobalDataScript.getInstance().wechatOperate.isWechatValid();

      if (canGuestLogin) {
        wechatLogin.SetActive(canWechatLogin);
      } else {
        wechatLogin.SetActive(true);
      }
    }
#endif
  }

	public void login(){
    SoundCtrl.getInstance().playSoundUI(true);
		
		if (!CustomSocket.getInstance ().isConnected) {
			CustomSocket.getInstance ().Connect ();
			ChatSocket.getInstance ().Connect();
			tapCount = 0;
			return;
		}



		GlobalDataScript.reinitData ();//初始化界面数据
		if (agreeProtocol.isOn) {
			doLogin ();
			watingPanel.SetActive(true);
		} else {
			MyDebug.Log ("请先同意用户使用协议");
			TipsManagerScript.getInstance ().setTips ("请先同意用户使用协议");
		}

		tapCount += 1;
		Invoke ("resetClickNum", 10f);

	}

  public void guestLoginCallback() {
#if UNITY_IPHONE
    GlobalDataScript.getInstance().wechatOperate.debugLogin();
#endif
  }

	public void doLogin(){
		//GlobalDataScript.getInstance ().wechatOperate.login ();
        CustomSocket.getInstance().sendMsg(new LoginRequest(null));
	}

  public void UpdateGlobalData(ClientResponse response) {
    if (int.Parse(response.message) == 1) {
      GlobalDataScript.inAppPay = true;
    } else {
      GlobalDataScript.inAppPay = false;
    }
  }

	public void LoginCallBack(ClientResponse response){
		if (watingPanel != null) {
			watingPanel.SetActive(false);
		}
	
		SoundCtrl.getInstance ().stopBGM ();
		SoundCtrl.getInstance ().playBGM ();
		if (response.status == 1) {
			if (GlobalDataScript.homePanel != null) {
				GlobalDataScript.homePanel.GetComponent<HomePanelScript> ().removeListener ();
				Destroy (GlobalDataScript.homePanel);
			}


			if (GlobalDataScript.gamePlayPanel != null) {
				GlobalDataScript.gamePlayPanel.GetComponent<MyMahjongScript> ().exitOrDissoliveRoom ();
			}

			GlobalDataScript.loginResponseData = JsonMapper.ToObject<AvatarVO> (response.message);
			ChatSocket.getInstance ().sendMsg (new LoginChatRequest(GlobalDataScript.loginResponseData.account.uuid));
			panelCreateDialog = Instantiate (Resources.Load("Prefab/Panel_Home")) as GameObject;
			panelCreateDialog.transform.parent = GlobalDataScript.getInstance().canvsTransfrom;
			panelCreateDialog.transform.localScale = Vector3.one;
			panelCreateDialog.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 0f);
			panelCreateDialog.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 0f);
			GlobalDataScript.homePanel = panelCreateDialog;
			removeListener ();
			Destroy (this);
			Destroy (gameObject);
		}
	}

	private void removeListener(){
		SocketEventHandle.getInstance ().UpdateGlobalData -= UpdateGlobalData;
		SocketEventHandle.getInstance ().LoginCallBack -= LoginCallBack;
		SocketEventHandle.getInstance ().RoomBackResponse -= RoomBackResponse;
	}

	private void RoomBackResponse(ClientResponse response){

		watingPanel.SetActive(false);

		if (GlobalDataScript.homePanel != null) {
			GlobalDataScript.homePanel.GetComponent<HomePanelScript> ().removeListener ();
			Destroy (GlobalDataScript.homePanel);
		}


		if (GlobalDataScript.gamePlayPanel != null) {
			GlobalDataScript.gamePlayPanel.GetComponent<MyMahjongScript> ().exitOrDissoliveRoom ();
		}
		GlobalDataScript.reEnterRoomData = JsonMapper.ToObject<RoomJoinResponseVo> (response.message);

		for (int i = 0; i < GlobalDataScript.reEnterRoomData.playerList.Count; i++) {
			AvatarVO itemData =	GlobalDataScript.reEnterRoomData.playerList [i];
			if (itemData.account.openid == GlobalDataScript.loginResponseData.account.openid) {
				GlobalDataScript.loginResponseData.account.uuid = itemData.account.uuid;
        GlobalDataScript.loginResponseData.isOnLine = true;
				ChatSocket.getInstance ().sendMsg (new LoginChatRequest(GlobalDataScript.loginResponseData.account.uuid));
				break;
			}
		}

		GlobalDataScript.gamePlayPanel =  PrefabManage.loadPerfab ("Prefab/Panel_GamePlay");
		removeListener ();
		Destroy (this);
		Destroy (gameObject);
  }


	private void resetClickNum(){
		tapCount = 0;
	}


}
