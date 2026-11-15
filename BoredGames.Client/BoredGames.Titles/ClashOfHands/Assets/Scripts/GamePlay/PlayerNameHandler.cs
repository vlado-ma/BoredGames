using Assets.Scripts;
using Assets.Scripts.BoredGames.API;
using Assets.Scripts.GamePlay;
using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.BoredGames.API.Responses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameHandler : MonoBehaviour
{
    [SerializeField] public Canvas _playerNameDialog;
    [SerializeField] public Canvas _mainMenuCanvas;
    [SerializeField] public TMP_InputField _playerNameInput;
    [SerializeField] public TextMeshProUGUI _validationMessage;
    
    private Button continueButton;
    public static string GameInstanceIdParamKey = "gameInstanceId";

    private void Awake()
    {
        Dictionary<string, string> queryParams = Utils.GetQueryParams();

        if (queryParams.ContainsKey(GameInstanceIdParamKey))
        {
            string value = queryParams[GameInstanceIdParamKey];
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            GameState.Instance.GameId = value;
        }
    }

    void Start()
    {
        GameObject continueButtonObject = GameObject.Find("ContinueButton");
        if (continueButtonObject != null)
        {
            continueButton = continueButtonObject.GetComponent<Button>();
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnButtonClick);
            }
        }

        _playerNameInput.onValueChanged.AddListener(OnInputValueChanged);
    }

    void OnInputValueChanged(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _validationMessage.gameObject.SetActive(false);
        }
        else
        {
            _validationMessage.gameObject.SetActive(true);
        }
    }

    void OnButtonClick()
    {
        StartCoroutine(CreateSessionThenJoinCoroutine());
    }

    private IEnumerator CreateSessionThenJoinCoroutine()
    {
        GameState.Instance.PlayerName = _playerNameInput.text;
        PlayerDetailsResponse session = null;

        yield return StartCoroutine(
            BoredGamesAPIClient.Instance.CreatePlayerSession(
                response => session = response
            )
        );

        if (session == null)
        {
            yield break;
        }

        GameState.Instance.PlayerId = session.id;
        GameState.Instance.PlayerName = session.nickName;

        if (!GameState.Instance.IsGameCreated)
        {
            _playerNameDialog.gameObject.SetActive(false);
            _mainMenuCanvas.gameObject.SetActive(true);
            yield break;
        }

        if (!BoredGamesSocketClient.Instance.IsConnected())
        {
            BoredGamesSocketClient.Instance.SetupConnection();
        }

        var joinSucceeded = false;

        yield return StartCoroutine(
            BoredGamesAPIClient.Instance.JoinGame(
                onSuccess: _ => joinSucceeded = true,
                onFailure: errorMessage =>
                {
                    GameState.Instance.Error = errorMessage;
                    GameManager.Instance.CheckGameStatus();
                }
            )
        );

        _playerNameDialog.gameObject.SetActive(false);
        _mainMenuCanvas.gameObject.SetActive(false);

        if (joinSucceeded)
        {
            GameManager.Instance.CheckGameStatus();
        }
    }

    void OnDestroy()
    {
        continueButton.onClick.RemoveListener(OnButtonClick);
        _playerNameInput.onValueChanged.RemoveListener(OnInputValueChanged);
    }
}
