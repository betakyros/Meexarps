﻿using System.Collections.Generic;
using UnityEngine;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Video;
using TMPro;

public class main : MonoBehaviour
{
    public Text gameStateText;
    public TextMeshProUGUI welcomeInstructionsText;
    private static List<QuestionCategory> questionCategories;
    private static QuestionCategory seasonalQuestionCategory;
    private static string[] anonymousNames;
    //(question, left answer, right answer)
    private static List<string[]> wouldYouRathers;
    private static string[] friendshipTips;
    private int friendshipTipIndex;
    private GameState gameState;
    public List<GameObject> welcomePanels;
    public GameObject splashScreenPanel;
    public GameObject storyPanel;
    public GameObject backgrounds;
    public GameObject welcomeScreenPanel;
    public GameObject selectRoundNumberPanel;
    public GameObject wouldYouRatherPanel;
    public GameObject answerQuestionsPanel;
    public GameObject votingPanel;
    public GameObject resultsPanel;
    public GameObject endScreenPanel;
    public GameObject errorPanel;
    public GameObject audienceScoreCard;
    public Canvas canvas;
//    public AudioSource introAudioSource;
    public AudioSource mainLoopAudioSource;
    public AudioSource blipAudioSource;
    public AudioSource stampAudioSource;
    public AudioSource resultsBeepAudioSource;
    public AudioSource[] welcomeScreenAudioSources;
    public AudioSource[] thinkingAudioSources;
    private AudioSource[] currentAudioSources;
    private int currentNumLevelToPlay;
    private float originalBlipVolume;
    private float baseVolume = .06f;
    private float onVolume = .06f;
    private float offVolume = 0f;
    public RawImage instructionVideo;
    public RawImage introInstructionVideo;
    public VideoPlayer introVp;
    public VideoPlayer vp;
    //should make a different sound per person
    public AudioClip[] blips;
    public AudioClip debouncedNoise;
    public Animator[] animators;
    public GameObject roundCounter;
    public SocketClientFranklin socket;

    private int currentCategoryIndex;
    private int selectedCategory;
    private List<int> sentCategoryIndices;
    private int currentQuestionIndex;
    private int currentWouldYouRatherIndex;
    private string gameCode;
    private static int VIP_PLAYER_NUMBER = 0;
    private static int AUDIENCE_THRESHOLD = 6;
    private static int AUDIENCE_ALIEN_NUMBER = 6;
    private Dictionary<int, int> audienceWouldYouRathers;
    private bool writeMyOwnQuestions = false;
    private Dictionary<string, bool> options = new Dictionary<string, bool>();
    private float optionsVolume;

    private bool isWinterHolidaySeason = System.DateTime.Today.Month == 1 || System.DateTime.Today.Month == 12;

    //don't foret to check if the airconsole script is disabled
    private static bool isAirconsole = false;

    // Start is called before the first frame update
    void Start()
    {
        if(isWinterHolidaySeason)
        {
            GameObject.FindWithTag("SplashScreenImage").gameObject.SetActive(false);
        } else
        {
            GameObject.FindWithTag("WinterSplashScreenImage").gameObject.SetActive(false);
        }
        StartAllLevels(welcomeScreenAudioSources);
        ExceptionHandling.SetupExceptionHandling(errorPanel);
        InitializeOptions();
        var newLinesRegex = new Regex(@"\r\n|\n|\r", RegexOptions.Singleline);
        string rawQuestions;
        string rawNsfwQuestions;
        string rawAnonymousNames;
        string rawWouldYouRathers;
        string rawFriendshipTips;
        string rawHolidayQuestions = null;
        string rawHolidayWouldYouRathers = null;
        //read the resources
        //if playing on AirConsole
        if (TextAssetsContainer.isWebGl)
        {
            rawQuestions = TextAssetsContainer.rawQuestionsText;
            rawNsfwQuestions = TextAssetsContainer.rawNsfwQuestionsText;
            rawAnonymousNames = TextAssetsContainer.rawAnonymousNamesText;
            rawWouldYouRathers = TextAssetsContainer.rawWouldYouRatherText;
            rawFriendshipTips = TextAssetsContainer.rawFriendshipTipsText;
            if(isWinterHolidaySeason)
            {
                rawHolidayQuestions = TextAssetsContainer.rawHolidayQuestionsText;
                rawHolidayWouldYouRathers = TextAssetsContainer.rawHolidayWouldYouRathersText;
            }
        }
        //if playing locally
        else
        {

            rawQuestions = Resources.GetQuestions();
            rawNsfwQuestions = Resources.GetNsfwQuestions();
            rawAnonymousNames = Resources.GetAnonymousNames();
            rawWouldYouRathers = Resources.GetWouldYouRathers();
            rawFriendshipTips = Resources.GetFriendshipTips();
            if(isWinterHolidaySeason)
            {
                rawHolidayWouldYouRathers = Resources.GetHolidayWouldYouRathers();
                rawHolidayQuestions = Resources.GetHolidayQuestions();
            }
        }
        questionCategories = new List<QuestionCategory>();
        ParseQuestions(rawQuestions, rawNsfwQuestions, newLinesRegex);
        anonymousNames = newLinesRegex.Split(rawAnonymousNames);
        friendshipTips = newLinesRegex.Split(rawFriendshipTips);
        string[] wouldYouRathersLines = newLinesRegex.Split(rawWouldYouRathers);
        List<string[]> tempWouldYouRathers = new List<string[]>();
        foreach (string s in wouldYouRathersLines)
        {
            tempWouldYouRathers.Add(s.Split('|'));
        }
        //randomize the order of the resources
        questionCategories.Shuffle();
        anonymousNames.Shuffle();
        friendshipTips.Shuffle();
        tempWouldYouRathers.Shuffle();
        wouldYouRathers = tempWouldYouRathers;

        //seasonal content
        if (isWinterHolidaySeason)
        {
            //questions
            ParseSeasonalQuestions(rawHolidayQuestions, newLinesRegex);

            //wouldyourathers
            string[] seasonalWouldYouRathersLines = newLinesRegex.Split(rawHolidayWouldYouRathers);
            List<string[]> tempSeasonalWouldYouRathers = new List<string[]>();
            foreach (string s in seasonalWouldYouRathersLines)
            {
                tempSeasonalWouldYouRathers.Add(s.Split('|'));
            }
            tempSeasonalWouldYouRathers.Shuffle();
            wouldYouRathers.InsertRange(0, tempSeasonalWouldYouRathers);
        }

        blips.Shuffle();
        if (isAirconsole)
        {
            AirConsole.instance.onReady += OnReady;
            AirConsole.instance.onMessage += OnMessage;
            AirConsole.instance.onConnect += OnConnect;
            AirConsole.instance.onDisconnect += OnDisconnect;
            AirConsole.instance.onAdComplete += OnAdComplete;
            AirConsole.instance.onAdShow += OnAdShow;
        } else
        {
            socket.getSocketIoCommunicator().Instance.On("phoneMessage", (data) =>
            {
                Debug.Log("phoneMessage");
                Debug.Log(data);
                Debug.Log(JToken.Parse(data));
                JToken parsedData = JToken.Parse(data);
                Debug.Log((int)parsedData["clientId"]);
                Debug.Log(parsedData["data"]);

                OnMessage((int)parsedData["clientId"], parsedData["data"]);
            });
        }
        gameState = new GameState();
        currentQuestionIndex = 0;
        currentWouldYouRatherIndex = 0;
        audienceWouldYouRathers = new Dictionary<int, int>();
        StartCoroutine(waitThreeSecondsThenDisplayWelcomeScreen());
        ChangeBackground(3);
    }

    private IEnumerator<WaitForSeconds> waitThreeSecondsThenDisplayWelcomeScreen()
    {
        yield return new WaitForSeconds(3);

        InvokeRepeating("UpdateLoadingScreenTips", 0f, 10f);

        SetVolumeForLevel(welcomeScreenAudioSources, 1);
        FadeSplashScreen fadeSplashScreenScript = splashScreenPanel.AddComponent<FadeSplashScreen>();
        fadeSplashScreenScript.Setup(3.0f);
        yield return new WaitForSeconds(3);
        Image[] storyPanels = storyPanel.GetComponentsInChildren<Image>(true);
        int numStoryScreens = 0;
        for(int i = 0; i < numStoryScreens - 1; i++)
        {
            yield return new WaitForSeconds(3);
            storyPanels[i].gameObject.SetActive(false);
            storyPanels[i+1].gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(3);
        FadeSplashScreen fadeStoryScreenScript = storyPanel.AddComponent<FadeSplashScreen>();
        /*
        if(storyPanel.activeSelf) {
            InvokeRepeating("UpdateLoadingScreenTips", 0f, 10f);
        }
        */
        fadeStoryScreenScript.Setup(3.0f);
        sendWelcomeScreenInfo(-1, -1);
    }

    public void UpdateLoadingScreenTips()
    {
        GameObject.FindWithTag("loadingScreenTips").GetComponent<TextMeshProUGUI>().text = friendshipTips[friendshipTipIndex++ % friendshipTips.Length];
    }

    private void InitializeOptions()
    {
        options.Add("nsfwQuestions", false);
        options.Add("anonymousNames", false);
        optionsVolume = 50;
        originalBlipVolume = blipAudioSource.volume;
    }

    void ParseQuestions(string rawQuestions, string rawNsfwQuestions, Regex newLinesRegex)
    {
        string[] questionsLines = newLinesRegex.Split(rawQuestions);
        string[] nsfwQuestionsLines = newLinesRegex.Split(rawNsfwQuestions);
        addLinesToCategory(questionsLines, false);
        addLinesToCategory(nsfwQuestionsLines, true);
    }
    void ParseSeasonalQuestions(string rawQuestions, Regex newLinesRegex)
    {
        string[] questionsLines = newLinesRegex.Split(rawQuestions);
        addLinesToSeasonalCategory(questionsLines, false);
    }

    private static void addLinesToCategory(string[] lines, bool isNsfw)
    {
        string currentCategoryName = null;
        List<string> questions = null;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i] == "---")
            {
                if (currentCategoryName != null)
                {
                    questionCategories.Add(new QuestionCategory(questions.ToArray(), currentCategoryName, isNsfw));
                }
                i++;
                currentCategoryName = lines[i];
                questions = new List<string>();
            }
            else
            {
                questions.Add(lines[i]);
            }

        }
    }

    private static void addLinesToSeasonalCategory(string[] lines, bool isNsfw)
    {
        List<string> questions = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            questions.Add(lines[i]);
        }
        seasonalQuestionCategory = new QuestionCategory(questions.ToArray(), lines[0], isNsfw);
    }

    void OnReady(string code)
    {
        gameCode = code;
        welcomeInstructionsText.text = "Navigate to airconsole.com and enter <size=39><b>" + code.Replace(" ", "") + "</b></size> to join!";
    }

    void Update()
    {
        if(null == gameState)
        {
            Debug.Log("Gamestate is now null!");
        }
    }

    void OnConnect(int from)
    {
        if (gameState.players.ContainsKey(from))
        {
            SendCurrentScreenForReconnect(from, gameState.players[from].playerNumber);
        } else
        {
            SendInitialRequestToPhone();
        }
    }

    void OnDisconnect(int from)
    {
        removeDeviceIdFromAlienSelections(from);
        BroadcastSelectedAliens(gameState.alienSelections);
    }

    void OnMessage(int from, JToken data)
    {
        string action = data["action"].ToString();
        bool playSound = true;
        if(gameState.players.ContainsKey(from))
        {
            string debouncedState = gameState.players[from].LogDebounce(debouncedNoise, blipAudioSource);
            if (debouncedState.Equals("DISABLED"))
            {
                return;
            }
            else if (debouncedState.Equals("DEBOUNCED"))
            {
                SendMessageToPhone(from, new JsonAction("debounced", new string[] { " " }));
                StartCoroutine(Wait5secondsThenUnblock(gameState.players[from]));
                return;
            }
        }

        if ("websocketInitialConnect".Equals(action))
        {
            OnConnect(from);
        }
        else if ("sendWelcomeInfo".Equals(action))
        {
            string name = ReplaceWhiteSpaceWithNormalSpace(data["info"]["name"].ToString().Trim());
            KeyValuePair<int, Player> currentPlayer = gameState.GetPlayerByName(name);

            if (!(currentPlayer.Value == null))
            {
                //prevent players from using the same name
                if (gameState.phoneViewGameState.Equals(PhoneViewGameState.SendStartGameScreen))
                {
                    SendMessageToPhone(from, new JsonAction("nameAlreadyTaken", new string[] { " " }));
                    return;
                }
                //reconnect case
                else
                {
                    Player cp = currentPlayer.Value;
                    gameState.players.Remove(currentPlayer.Key);
                    gameState.players.Add(from, new Player(name, from, cp.playerNumber, 0,
                        cp.points, cp.numWrong, animators[cp.alienNumber],
                        cp.bestFriendPoints, cp.alienNumber));
                    SendCurrentScreenForReconnect(from, cp.playerNumber);
                }
            }
            //new player
            else if (gameState.GetNumberOfPlayers() < 6)
            {
                int selectedAlien = data["info"]["selectedAlien"].ToObject<int>();
                //remove the nbsp
                name = Regex.Replace(name, @"\u00a0", " ");
                int newPlayerNumber = gameState.GetNumberOfPlayers();
                if (selectedAlien > -1)
                {
                    gameState.alienSelections[selectedAlien] = new[] { -1, newPlayerNumber };
                }
                else
                {
                    //select the next available alien
                    for (int i = 0; i < 6; i++)
                    {
                        if (!gameState.alienSelections.ContainsKey(i))
                        {
                            selectedAlien = i;
                            gameState.alienSelections.Add(i, new[] { -1, newPlayerNumber });
                            break;
                        }
                    }
                }
                Player p = new Player(name, from, newPlayerNumber, 0, animators[selectedAlien], selectedAlien);

                if (p.playerNumber == 0)
                {
                    SendIsVip(p);
                }

                Image[] playerIcons = welcomeScreenPanel.GetComponentsInChildren<Image>(true);
                List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
                TextMeshProUGUI[] myTexts = playerIconsList[gameState.GetNumberOfPlayers()].GetComponentsInChildren<TextMeshProUGUI>();
                myTexts[0].text = p.nickname;

                //move the headresearcher label behind the alien icon
                Image image = playerIconsList[gameState.GetNumberOfPlayers()].GetComponentsInChildren<Image>(true)[1];
                image.gameObject.SetActive(true);
                updatePlayerAnimator(image.GetComponentInChildren<Animator>(), p);

                gameState.players.Add(from, p);
                sendWelcomeScreenInfo(from, selectedAlien);
                SendMessageToPhone(from, new JsonAction("confirmNameSubmission", new string[] { " " }));
                if (gameState.phoneViewGameState == PhoneViewGameState.SendStartGameScreen)
                {
                    SendMessageToPhone(from, new JsonAction("sendStartGameScreen", new string[] { " " }));
                    SendMessageToVip(new JsonAction("allPlayersAreNotReady", gameState.whoIsNotReady().ToArray()));

                }
                else
                {
                    SendCurrentScreenForReconnect(from, p.playerNumber);
                }
            }
            // audience
            else
            {
                KeyValuePair<int, Player> audiencePlayer = gameState.GetAudienceByName(name);
                if (!(audiencePlayer.Value == null))
                {
                    //prevent players from using the same name
                    if (gameState.phoneViewGameState.Equals(PhoneViewGameState.SendStartGameScreen))
                    {
                        SendMessageToPhone(from, new JsonAction("nameAlreadyTaken", new string[] { " " }));
                        return;
                    }
                    //reconnect case
                    else
                    {
                        gameState.audienceMembers.Remove(audiencePlayer.Key);
                        Player ap = audiencePlayer.Value;
                        //todo hard code the audience animator to 7 or give them no animations
                        gameState.audienceMembers.Add(from, new Player(name, from, ap.playerNumber, 0,
                            ap.points, ap.numWrong, ap.myAnimator, ap.bestFriendPoints,
                            ap.alienNumber));
                        SendCurrentScreenForReconnect(from, ap.playerNumber);
                    }
                }
                //new Audience
                else
                {
                    int audienceNumber = 10 + gameState.GetNumberOfAudience();
                    //todo hard code the audience animator to 7 or give them no animations
                    Player p = new Player(name, from, audienceNumber, 0, animators[1], 6);
                    gameState.audienceMembers.Add(from, p);

                    if (gameState.phoneViewGameState == PhoneViewGameState.SendStartGameScreen)
                    {
                        GameObject.FindWithTag("AudienceCounter").GetComponentInChildren<TextMeshProUGUI>().text = "<color=white>Audience: " + gameState.audienceMembers.Count + "</color>";
                        SendMessageToPhone(from, new JsonAction("sendStartGameScreen", new string[] { " " }));
                        sendWelcomeScreenInfo(from, -1);
                        SendMessageToPhone(from, new JsonAction("confirmNameSubmission", new string[] { " " }));
                    }
                    else
                    {
                        SendCurrentScreenForReconnect(from, p.playerNumber);
                    }
                }
            }
        }
        else if ("checkName".Equals(action))
        {
            playSound = false;
            string name = ReplaceWhiteSpaceWithNormalSpace(data["info"]["name"].ToString().Trim());
            KeyValuePair<int, Player> currentPlayer = gameState.GetPlayerByName(name);
            KeyValuePair<int, Player> audiencePlayer = gameState.GetAudienceByName(name);
            List<string> payload = new List<string>();
            if (currentPlayer.Value == null && audiencePlayer.Value == null)
            {
                if(gameState.GetNumberOfPlayers() < 6)
                {
                    payload.Add("You will join as a new Player!");
                    SendMessageToPhone(from, new JsonAction("checkNameResponse", new string[] { "You will join as a new Player!" }));
                } else
                {
                    SendMessageToPhone(from, new JsonAction("checkNameResponse", new string[] { "You will join as a new Audience Member!" }));
                }
            } else if (currentPlayer.Value != null)
            {
                SendMessageToPhone(from, new JsonAction("checkNameResponse", new string[] { "You will Reconnect as a Player!" }));
            } else if (audiencePlayer.Value != null)
            {
                SendMessageToPhone(from, new JsonAction("checkNameResponse", new string[] { "You will Reconnect as an Audience Member!" }));
            } else
            {
                SendMessageToPhone(from, new JsonAction("checkNameResponse", new string[] { "An error occured, Sorry! Please email Meexarps@gmail.com" }));
                Debug.Log("An error occured in checkname");
            }

        }
        else if ("forceAdvance".Equals(action))
        {
            if(gameState.tvViewGameState != TvViewGameState.WelcomeScreen)
            {
                CancelInvoke();
                roundCounter.SetActive(false);
                welcomeScreenPanel.SetActive(false);
                selectRoundNumberPanel.SetActive(false);
                wouldYouRatherPanel.SetActive(false);
                answerQuestionsPanel.SetActive(false);
                votingPanel.SetActive(false);
                deactivateResultsPanel();
                StopAllCoroutines();
                StartCoroutine("SendEndScreen2");
            }
            /*
            switch (gameState.tvViewGameState)
            {
                case TvViewGameState.WelcomeScreen:
                    gameState.tvViewGameState = TvViewGameState.SubmitQuestionsScreen;
                    gameState.numRoundsPerGame = 1;
                    ExitWelcomeScreen();
                    break;
                case TvViewGameState.SubmitQuestionsScreen:
                    List<string> randomQuestions = new List<string>();
                    randomQuestions.Add(GetRandomQuestion());
                    randomQuestions.Add(GetRandomQuestion());
                    randomQuestions.Add(GetRandomQuestion());
                    gameState.GetCurrentRound().questions = randomQuestions;
                    PrepareSendQuestions();
                    break;
                case TvViewGameState.AnswerQuestionsScreen:
                    foreach(Player p in gameState.players.Values)
                    {
                        bool hasAnswers = false;
                        foreach(Answers a in gameState.GetCurrentRound().answers)
                        {
                            if(a.playerNumber == p.playerNumber)
                            {
                                hasAnswers = true;
                            }
                        }
                        if(!hasAnswers)
                        {
                            List<string> emptyAnswers = new List<string>();
                            emptyAnswers.Add("");
                            emptyAnswers.Add("");
                            emptyAnswers.Add("");
                            gameState.GetCurrentRound().answers.Add(
                                new Answers(emptyAnswers.ToArray(), gameState.players[from].playerNumber, GenerateAnonymousPlayerName()));
                        }
                    }
                    StartCoroutine(EndAnswerQuestionsPhase(2));
                    break;
                case TvViewGameState.VotingScreen:
                    StartCoroutine(CalculateVoting(false));
                    break;
                case TvViewGameState.RoundResultsScreen:
                    if (gameState.GetCurrentRoundNumber() == gameState.numRoundsPerGame - 1)
                    {
                        SendEndScreen2();
                    } else
                    {
                        StartRound();
                    }
                    break;
                case TvViewGameState.EndGameScreen:
                    break;
                default:
                    break;
            }
            */
        }
        else if ("sendSelectAlien".Equals(action))
        {
            playSound = false;
            int selectedAlien = data["info"]["selectedAlien"].ToObject<int>();
            if (gameState.alienSelections.ContainsKey(selectedAlien))
            {
                SendMessageToPhone(from, new JsonAction("sendAlienSelectionFailed", new[] { ""}));
            } else
            {
                //remove the previous selection
                removeDeviceIdFromAlienSelections(from);
                if(selectedAlien > -1)
                {
                    gameState.alienSelections.Add(selectedAlien, new[] { from, -1 });
                }
                BroadcastSelectedAliens(gameState.alienSelections);
            }

        }
        else if ("sendRetrieveOptions".Equals(action))
        {
            playSound = false;

            if (from == GetVipDeviceId())
            {
                SendMessageToPhone(GetVipDeviceId(), new JsonAction("sendRetrieveOptions", new[] { options["nsfwQuestions"].ToString(), optionsVolume.ToString(), options["anonymousNames"].ToString() }));
            }
        }
        else if ("sendSaveOptions".Equals(action))
        {
            playSound = false;

            if (from == GetVipDeviceId())
            {
                //options["nsfwQuestions"] = data["info"]["nsfwQuestions"].ToObject<bool>();
                options["anonymousNames"] = data["info"]["anonymousNames"].ToObject<bool>();
                optionsVolume = data["info"]["optionsVolume"].ToObject<float>();
                onVolume = optionsVolume / 50.0f * baseVolume;
                SetVolumeForLevel(currentAudioSources, currentNumLevelToPlay);
                blipAudioSource.volume = originalBlipVolume * optionsVolume / 50.0f;
            }
        }
        else if ("requestWelcomeScreenInfo".Equals(action))
        {
            SendInitialRequestToPhone();
            playSound = false;
        }
        else if("sendReady".Equals(action))
        {
            if (!isAudienceMember(from))
            {
                Player currentPlayer = gameState.players[from];
                currentPlayer.isReady = true;
                List<string> playersWhoAreNotReady = gameState.whoIsNotReady();
                if (playersWhoAreNotReady.Count == 0)
                {
                    SendMessageToVip(new JsonAction("allPlayersAreReady", new string[] { }));
                }
                else
                {
                    SendMessageToVip(new JsonAction("allPlayersAreNotReady", playersWhoAreNotReady.ToArray()));
                }
            }
        }
        else if ("sendStartGame".Equals(action))
        {
            //gameState.numRoundsPerGame = data["info"]["roundCount"].ToObject<int>();

            //Stop the loading screen tips
            CancelInvoke();
            if(isAirconsole)
            {
                AirConsole.instance.ShowAd();
            } else
            {
                ContinueSendStartGame();
            }
        }
        else if ("sendHoverRoundCount".Equals(action))
        {
            playSound = false;
            string roundDetails = data["info"].ToObject<string>();

            selectRoundNumberPanel.GetComponentsInChildren<TextMeshProUGUI>()[1].text = roundDetails;
        }
        else if ("sendSetRoundCount".Equals(action))
        {
            gameState.tvViewGameState = TvViewGameState.SubmitQuestionsScreen;
            List<int> tempListOfQuestionWriters = new List<int>();
            foreach (string questionWriterName in data["info"].ToObject<List<string>>())
            {
                int currentPlayerNumber = gameState.GetPlayerByName(questionWriterName).Value.playerNumber;
                tempListOfQuestionWriters.Add(currentPlayerNumber);
            }
            gameState.questionWriters = tempListOfQuestionWriters;
            ExitWelcomeScreen();
        }
        else if ("sendSubmitWouldYouRather".Equals(action))
        {
            List<string> wouldYouRather = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                wouldYouRather.Add(ReplaceWhiteSpaceWithNormalSpace(property.Value.ToString()));
            }
            wouldYouRathers.Insert(currentWouldYouRatherIndex, wouldYouRather.ToArray());
        }
        else if ("sendWouldYouRatherAnswer".Equals(action))
        {
            int indexOfFirstIcon = 5;
            int leftOrRight = data["info"].ToObject<int>();
            int playerNumber;
            if (isAudienceMember(from))
            {
                int leftAudience = 0;
                int rightAudience = 0;
                audienceWouldYouRathers[from] = leftOrRight;
                foreach (int i in audienceWouldYouRathers.Values)
                {
                    if (i == 0)
                    {
                        leftAudience++;
                    }
                    else
                    {
                        rightAudience++;
                    }
                }
                SetAudienceWouldYouRatherCounters(leftAudience, rightAudience);
            }
            else
            {
                playerNumber = gameState.players[from].playerNumber;
                Image[] images = wouldYouRatherPanel.GetComponentsInChildren<Image>(true);
                List<Image> iconTags = getPlayerIconTags(images, "WouldYouRatherPlayerIcon");
                float xOffset = canvas.GetComponent<RectTransform>().rect.width * 0.2f/* * canvas.scaleFactor*/;
                if (leftOrRight == 0)
                {
                    movePlayerIcon(iconTags[playerNumber], -1 * xOffset);
                }
                else
                {
                    movePlayerIcon(iconTags[playerNumber], xOffset);
                }
            }
        }
        else if ("sendCategory".Equals(action))
        {
            int buttonNumber = data["info"]["buttonNumber"].ToObject<int>();
            selectedCategory = sentCategoryIndices[buttonNumber - 1];
            SendRetrieveQuestions(from, buttonNumber == 6);
        }
        else if ("sendRequestAnotherQuestion".Equals(action))
        {
            playSound = false;
            string elementId = data["info"]["elementId"].ToString();
            string nextQuestion;
            if (writeMyOwnQuestions)
            {
                nextQuestion = GetRandomQuestion();
            }
            else
            {
                nextQuestion = GetNextQuestion();
            }
            SendMessageToPhone(from, new JsonAction("sendAnotherQuestion", new string[] { elementId, nextQuestion }));
        }
        else if ("sendDecidedQuestions".Equals(action))
        {
            List<string> myQuestions = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                myQuestions.Add(ReplaceWhiteSpaceWithNormalSpace(property.Value.ToString()));
            }
            gameState.GetCurrentRound().questions = myQuestions;
            StartCoroutine(PrepareSendQuestions());
        }
        else if ("sendAnswers".Equals(action))
        {
            if (!isAudienceMember(from))
            {
                List<string> myAnswers = new List<string>();
                foreach (JProperty property in ((JObject)(data["info"])).Properties())
                {
                    myAnswers.Add(ReplaceWhiteSpaceWithNormalSpace(property.Value.ToString()));
                }
                gameState.GetCurrentRound().answers.Add(
                    new Answers(myAnswers.ToArray(), gameState.players[from].playerNumber, GenerateAnonymousPlayerName()));

                int tilesOffset = 1;
                Player currentPlayer = gameState.players[from];
                /*
                answerQuestionsPanel.GetComponentsInChildren<Text>()[tilesOffset + currentPlayer.playerNumber].text = currentPlayer.nickname + "\n\n<color=green>Has Submitted</color>";
                */
                Image[] playerIcons = answerQuestionsPanel.GetComponentsInChildren<Image>(true);
                List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
                Image gridBackground = getPlayerIconTags(playerIcons, "AnswerQuestionsGridBackground")[0];


                float newLocalX = (gridBackground.GetComponent<RectTransform>().rect.width * .74f) /* canvas.scaleFactor*/;
                movePlayerIcon(playerIconsList[currentPlayer.playerNumber], newLocalX);

                if (HasEveryoneSubmittedAnswers())
                {
                    gameState.GetCurrentRound().answers.Sort((a, b) => a.anonymousPlayerName.CompareTo(b.anonymousPlayerName));
                    StartCoroutine(EndAnswerQuestionsPhase(2));
                }
            }
        }
        else if ("sendSkipInstructions".Equals(action))
        {
            if(storyPanel.activeSelf)
            {
                StopCoroutine(waitThreeSecondsThenDisplayWelcomeScreen());
                storyPanel.SetActive(false);
                sendWelcomeScreenInfo(-1, -1);
                InvokeRepeating("UpdateLoadingScreenTips", 0f, 10f);
            }
            if (instructionVideo.gameObject.GetComponent<CameraZoom>() != null)
            {
                Destroy(instructionVideo.gameObject.GetComponent<CameraZoom>());
            }
            if (introInstructionVideo.gameObject.GetComponent<CameraZoom>() != null)
            {
                Destroy(introInstructionVideo.gameObject.GetComponent<CameraZoom>());
            }
        }
        else if ("sendVoting".Equals(action))
        {
            Dictionary<string, string> myVotes = new Dictionary<string, string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                string anonymousName = property.Value.ToString();
                string playerName = property.Name.ToString();
                myVotes.Add(anonymousName, playerName);
            }
            if(isAudienceMember(from))
            {
                Dictionary<int, Dictionary<string, string>> audienceVotes = gameState.GetCurrentRound().audienceVotes;
                int audiencePlayerNumber = gameState.audienceMembers[from].playerNumber;
                if (!audienceVotes.ContainsKey(audiencePlayerNumber))
                {
                    audienceVotes.Add(audiencePlayerNumber, myVotes);
                } else
                {
                    audienceVotes[audiencePlayerNumber] = myVotes;
                }
            }
            else
            {
                Dictionary<int, Dictionary<string, string>> votes = gameState.GetCurrentRound().votes;
                Player currentPlayer = gameState.players[from];
                votes.Add(currentPlayer.playerNumber, myVotes);
                List<string> playersWhoHaventSubmitted = calculatePlayersWhoHaventSubmitted(votes);
                foreach (int playerNum in votes.Keys)
                {
                    SendMessageToPhone(gameState.GetPlayerByPlayerNumber(playerNum).deviceId,
                        new JsonAction("sendWaitScreen", playersWhoHaventSubmitted.ToArray()));
                }

                if (HasEveryoneVoted())
                {
                    bool shouldWaitForAudience = false;
                    foreach (Player audienceMember in gameState.audienceMembers.Values)
                    {
                        if (!gameState.GetCurrentRound().audienceVotes.ContainsKey(audienceMember.playerNumber))
                        {
                            shouldWaitForAudience = true;
                            SendMessageToPhone(audienceMember.deviceId,
                                new JsonAction("forceCollectAnswers", new[] { "false" }));
                        }
                    }
                    StartCoroutine(CalculateVoting(shouldWaitForAudience));
                }
            }
        }
        else if ("sendNextRound".Equals(action))
        {
            StartRound();
        }
        else if ("sendShowEndScreen".Equals(action))
        {
            StartCoroutine("SendEndScreen2");
        }
        else if ("sendPlayAgain".Equals(action))
        {
            StopCoroutine("SendEndScreen2");
            resultsBeepAudioSource.Pause();
            gameState.ResetGameState();
            endScreenPanel.SetActive(false);
            //StartRound();
            StopAllLevels(thinkingAudioSources);
            StartAllLevels(welcomeScreenAudioSources);
            if(isAirconsole)
            {
                AirConsole.instance.ShowAd();
            } else
            {
                ContinueSendStartGame();
            }
        }
        if(gameState.players.ContainsKey(from))
        {
            if (playSound) {
                //play a sound to confirm the input
                blipAudioSource.PlayOneShot(blips[gameState.players[from].playerNumber], Random.Range(.5f, 1f));
            } 
        }
    }

    private void SendInitialRequestToPhone()
    {
        if (storyPanel.activeSelf)
        {
            BroadcastToAllPhones(new JsonAction("sendSkipIntro", new string[] { " " }));
        }
        else
        {
            sendWelcomeScreenInfo(-1, -1);
        }
    }

    private static string ReplaceWhiteSpaceWithNormalSpace(string name)
    {
        return Regex.Replace(name, @"\s+", " ");
    }

    void OnAdShow()
    {
        StopAllLevels(welcomeScreenAudioSources);
    }

    void OnAdComplete(bool adWasShown)
    {
        if(adWasShown)
        {
            StartAllLevels(welcomeScreenAudioSources);
        }
        ContinueSendStartGame();
    }

    private void ContinueSendStartGame()
    {
        GameObject gameObject = GameObject.Find("WelcomeScreenPanel");
        if(gameObject != null)
        {
            gameObject.SetActive(false);
        }
        selectRoundNumberPanel.SetActive(true);
        selectRoundNumberPanel.GetComponentsInChildren<TextMeshProUGUI>()[0].text = "<color=white>The Head Researcher (<color=#325EFB>" +
            gameState.GetPlayerByPlayerNumber(0).nickname + "</color>) is selecting a game length.</color>";
        //selectRoundNumberPanel.GetComponentsInChildren<Text>()[1].text =
        //    "With " + gameState.GetNumberOfPlayers() + " players it will take about  " + (3 + gameState.GetNumberOfPlayers()) + " minutes per round. Note: With fewer rounds, some players will not get to submit questions.";
        //BroadcastToAllPhones(new JsonAction("selectRoundCountView", new[] { gameState.GetNumberOfPlayers() + "" }));
        SetVolumeForLevel(welcomeScreenAudioSources, 2);
        List<string> playersNamesAndAlienNumbers = gameState.GetPlayerNamesInNumberOrder();
        playersNamesAndAlienNumbers.AddRange(gameState.GetAlienNumberInNumberOrder());
        SendMessageToVip(new JsonAction("selectRoundCountView", playersNamesAndAlienNumbers.ToArray()));
        gameState.phoneViewGameState = PhoneViewGameState.SendSelectRoundNumberScreen;
        /*
        introAudioSource.Stop();
        mainLoopAudioSource.Play();
        StartCoroutine(ShowIntroInstrucitons(2));

        */
    }

    private IEnumerator<WaitForSeconds> Wait5secondsThenUnblock(Player p)
    {
        yield return new WaitForSeconds(5);
        p.isDebounced = false;
    }

    private List<string> calculatePlayersWhoHaventSubmitted(Dictionary<int, Dictionary<string, string>> votes)
    {
        //Send everyone who has already submitted the list of players who have not yet submitted
        List<string> playersWhoHaventSubmitted = new List<string>();
        foreach (Player p in gameState.players.Values)
        {
            if (!votes.ContainsKey(p.playerNumber))
            {
                playersWhoHaventSubmitted.Add(p.nickname);
            }
        }

        return playersWhoHaventSubmitted;
    }

    private void BroadcastSelectedAliens(Dictionary<int, int[]> alienSelections)
    {
        string selectedAliens = "";
        foreach (int alienNumber in alienSelections.Keys)
        {
            selectedAliens += alienNumber;
        }
        BroadcastToAllPhones(new JsonAction("sendAlienSelectionSuccess", new[] { selectedAliens }));
    }

    private void removeDeviceIdFromAlienSelections(int from)
    {
        Dictionary<int, int[]> alienSelections = gameState.alienSelections;
        foreach (KeyValuePair<int, int[]> alienSelection in alienSelections)
        {
            if (alienSelection.Value[0] == from)
            {
                alienSelections.Remove(alienSelection.Key);
                break;
            }
        }
    }

    //change this to an alien themed transition
    private IEnumerator<WaitForSeconds> PrepareSendQuestions()
    {
        //display element. make it alien themed
        Animator[] wouldYouRatherAnimators = wouldYouRatherPanel.GetComponentsInChildren<Animator>();
        Animator questionsSubmittedPanel = wouldYouRatherAnimators[wouldYouRatherAnimators.Length-1];
        questionsSubmittedPanel.SetBool("Open", true);
        yield return new WaitForSeconds(4.5f);
        questionsSubmittedPanel.SetBool("Open", false);
        yield return new WaitForSeconds(2.5f);

        //Stop the would you rathers
        CancelInvoke();
        wouldYouRatherPanel.SetActive(false);
        answerQuestionsPanel.SetActive(true);

        //display rotating friendship tips
        InvokeRepeating("UpdateLoadingScreenTips", 0f, 10f);

        // inititalize the grid
        Image[] playerIcons = answerQuestionsPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
        for (int i = 5; i >= gameState.GetNumberOfPlayers(); i--)
        {
            playerIconsList[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            playerIconsList[i].gameObject.SetActive(true);
            updatePlayerAnimator(playerIconsList[i].GetComponentInChildren<Animator>(), i);
        }
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            playerIconsList[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = gameState.GetPlayerByPlayerNumber(i).nickname;
        }

        SendQuestions();
    }

    private void ExitWelcomeScreen()
    {
        StopAllLevels(welcomeScreenAudioSources);
        //introAudioSource.Stop();
        StartAllLevels(thinkingAudioSources);
        StartCoroutine(ShowIntroInstrucitons(2));
    }

    private void SetAudienceWouldYouRatherCounters(int leftAudience, int rightAudience)
    {
        TextMeshProUGUI[] wouldYouRatherTexts = wouldYouRatherPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        List<TextMeshProUGUI> wouldYouRatherAudienceTags = getTextTags(wouldYouRatherTexts, "audienceText");
        wouldYouRatherAudienceTags[0].transform.parent.gameObject.SetActive(true);
        wouldYouRatherAudienceTags[1].transform.parent.gameObject.SetActive(true);
        wouldYouRatherAudienceTags[0].text = "" + leftAudience;
        wouldYouRatherAudienceTags[1].text = "" + rightAudience;
    }

    private void sendWelcomeScreenInfo(int from, int alienNumber)
    {
        BroadcastSelectedAliens(gameState.alienSelections);
        List<string> playerNames = gameState.GetPlayerNamesInNumberOrder();
        List<string> playerAlienNumber = gameState.GetAlienNumberInNumberOrder();
        List<string> payload = new List<string>();
        int currNumPlayers = playerNames.Count;
        string hasGameStarted = gameState.tvViewGameState == TvViewGameState.WelcomeScreen ? "NOT_STARTED" : "STARTED";
        payload.Add(hasGameStarted);
        if (currNumPlayers < 6)
        {
            payload.Add("" + playerNames.Count);
            payload.AddRange(playerNames);
            payload.AddRange(playerAlienNumber);
            BroadcastToAllPhones(new JsonAction("sendWelcomeScreenInfo", payload.ToArray()));
        } else
        {
            payload.Add("full");
            payload.AddRange(playerNames);
            payload.AddRange(playerAlienNumber);
            BroadcastToAllPhones(new JsonAction("sendWelcomeScreenInfo", payload.ToArray()));
        }
        int currNumAudience = gameState.GetNumberOfAudience();

        if (from > 0)
        {
            if (currNumAudience == 0)
            {
                Player myPlayer = gameState.players[from];
                sendWelcomeScreenInfoDetails(from, alienNumber, myPlayer);

            }
            else 
            {
                Player myPlayer = gameState.audienceMembers[from];
                sendWelcomeScreenInfoDetails(from, AUDIENCE_ALIEN_NUMBER, myPlayer);
            }
        }
    }

    private void sendWelcomeScreenInfoDetails(int from, int alienNumber, Player myPlayer)
    {
        string vipName = gameState.GetPlayerByPlayerNumber(0).nickname;
        List<string> playerNames = gameState.GetPlayerNamesInNumberOrder();
        List<string> alienNumbers = gameState.GetAlienNumberInNumberOrder();
        List<string> payload = new List<string>();
        payload.Add(myPlayer.nickname);
        payload.Add("" + myPlayer.playerNumber);
        payload.Add("" + alienNumber);
        payload.Add(vipName);
        payload.AddRange(playerNames);
        payload.AddRange(alienNumbers);
        SendMessageToPhone(from, new JsonAction("sendWelcomeScreenInfoDetails",
            payload.ToArray()));
    }

    private void SendIsVip(Player currentPlayer)
    {
        SendMessageToPhone(currentPlayer.deviceId, new JsonAction("setIsVip", new[] { "" }));
    }

    private bool isAudienceMember(int from)
    {
        return gameState.audienceMembers.ContainsKey(from);
    }

    private int GetVipDeviceId()
    {
        if(getNumPlayers() == 0)
        {
            return -1;
        }
        return gameState.GetPlayerByPlayerNumber(0).deviceId;
    }

    private void SendMessageToPlayersAndSendWaitScreenToAudience(JsonAction jsonAction)
    {
        foreach (Player p in gameState.players.Values)
        {
            SendMessageToPhone(p.deviceId, jsonAction);
        }

        foreach (Player p in gameState.audienceMembers.Values)
        {
            sendWaitScreenToPlayer(p);
        }
    }

    private void sendWaitScreenToPlayer(Player p)
    {
        SendMessageToPhone(p.deviceId, new JsonAction("sendWaitScreen", new string[] { }));
    }

    private void SendMessageToVipAndSendWaitScreenToEveryoneElse(JsonAction jsonAction)
    {
        foreach(Player p in gameState.players.Values)
        {
            if(p.playerNumber == VIP_PLAYER_NUMBER)
            {
                SendMessageToPhone(p.deviceId, jsonAction);
            }
            else
            {
                sendWaitScreenToPlayer(p);
            }
        }

        foreach(Player p in gameState.audienceMembers.Values)
        {
            sendWaitScreenToPlayer(p);
        }
    }

    private void SendMessageIfVipElseSendWaitScreen(int from, int currentPlayerNumber, JsonAction jsonAction)
    {
        if (currentPlayerNumber == VIP_PLAYER_NUMBER)
        {
            SendMessageToPhone(from, jsonAction);
        }
        else
        {
            SendMessageToPhone(from, new JsonAction("sendWaitScreen", new string[] {}));
        }
    }

    private void SendMessageToVip(JsonAction jsonAction)
    {
        foreach (Player p in gameState.players.Values)
        {
            if (p.playerNumber == VIP_PLAYER_NUMBER)
            {
                SendMessageToPhone(p.deviceId, jsonAction);
            }
        }
    }

    private void SendCurrentScreenForReconnect(int from, int currentPlayerNumber)
    {
        //null if they don't exist or are not a player
        Player currentPlayer = gameState.GetPlayerByPlayerNumber(currentPlayerNumber);
        //null if they don't exist or are not an audience
        Player currentAudience = gameState.GetAudienceByPlayerNumber(currentPlayerNumber);
        bool isVip = currentPlayerNumber == VIP_PLAYER_NUMBER;
        bool isPlayer = gameState.players.ContainsKey(from);
        bool isAudience = gameState.audienceMembers.ContainsKey(from);
        if (isPlayer)
        {
            Player myPlayer = gameState.players[from];
            sendWelcomeScreenInfoDetails(from, myPlayer.alienNumber, myPlayer);
        }
        if (isAudience)
        {
            Player myPlayer = gameState.audienceMembers[from];
            sendWelcomeScreenInfoDetails(from, myPlayer.alienNumber, myPlayer);
        }


        if (isVip)
        {
            SendIsVip(gameState.GetPlayerByPlayerNumber(currentPlayerNumber));
        }
        switch (gameState.phoneViewGameState)
        {
            case PhoneViewGameState.SendStartGameScreen:
                SendMessageToPhone(from, new JsonAction("sendStartGameScreen", new string[] { " " }));

                break;
            case PhoneViewGameState.SendSelectRoundNumberScreen:
                List<string> playersNames = gameState.GetPlayerNamesInNumberOrder();
                playersNames.AddRange(gameState.GetAlienNumberInNumberOrder());
                SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber,
                    new JsonAction("selectRoundCountView", playersNames.ToArray())
                );
                break;
            case PhoneViewGameState.SendSkipInstructionsScreen:
                SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber,
                    new JsonAction("sendSkipInstructions", new string[] { " " }));
                break;
            case PhoneViewGameState.SendWaitScreen:
                SendMessageToPhone(from, new JsonAction("sendWaitScreen", new string[] {}));
                break;
            case PhoneViewGameState.SendWouldYouRather:
                if (currentPlayerNumber == gameState.GetCurrentWritingPlayer().playerNumber)
                //if (currentPlayer.Value.playerNumber == gameState.GetCurrentRoundNumber())
                {
                    SendSelectCategory(from);
                }
                else
                {
                    Image[] playerIcons = wouldYouRatherPanel.GetComponentsInChildren<Image>(true);
                    if (isPlayer)
                    {
                        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
                        int playerNum = currentPlayer.playerNumber;

                        playerIconsList[playerNum].gameObject.SetActive(true);
                        updatePlayerAnimator(playerIconsList[playerNum].GetComponentInChildren<Animator>(), playerNum);
                        playerIconsList[playerNum].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = gameState.GetPlayerByPlayerNumber(playerNum).nickname;
                    } else if(isAudience)
                    {
                        int playerIconOffset = 14;
                        playerIcons[playerIconOffset + 6].gameObject.SetActive(true);
                    }
                    string waitingForPlayerName = gameState.GetCurrentWritingPlayer().nickname;
                    List<string> payload = new List<string>();
                    payload.Add(waitingForPlayerName);
                    payload.Add("true");
                    SendMessageToPhone(from, new JsonAction("sendWouldYouRather", payload.ToArray()));
                }
                break;
            case PhoneViewGameState.SendQuestions:
                if (isPlayer)
                {
                    Image[] playerIcons = answerQuestionsPanel.GetComponentsInChildren<Image>(true);
                    List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
                    int playerNum = currentPlayer.playerNumber;

                    playerIconsList[playerNum].gameObject.SetActive(true);
                    updatePlayerAnimator(playerIconsList[playerNum].GetComponentInChildren<Animator>(), playerNum);
                    playerIconsList[playerNum].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = gameState.GetPlayerByPlayerNumber(playerNum).nickname;
                }
                if (gameState.GetCurrentRound().hasPlayerSubmittedAnswer(currentPlayerNumber)) {
                    SendMessageToPhone(from, new JsonAction("sendWaitScreen", new string[] { }));
                } else
                {
                    SendQuestions(from);
                }
                break;
            case PhoneViewGameState.SendVoting:
                if (gameState.GetCurrentRound().votes.ContainsKey(currentPlayerNumber) || gameState.GetCurrentRound().audienceVotes.ContainsKey(currentPlayerNumber))
                {
                    List<string> playersWhoHaventSubmitted = calculatePlayersWhoHaventSubmitted(gameState.GetCurrentRound().votes);
                    SendMessageToPhone(from, new JsonAction("sendWaitScreen", playersWhoHaventSubmitted.ToArray()));
                }
                else
                {
                    SendVoting(from);
                }
                break;
            case PhoneViewGameState.SendNextRoundScreen:
                sendPersonalRoundResultsForReconnect(currentPlayer, currentAudience);
                if (isVip)
                {
                    SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber, new JsonAction("sendNextRoundScreen", new string[] { " " }));
                }
                break;
            case PhoneViewGameState.SendAdvanceToResultsScreen:
                sendPersonalRoundResultsForReconnect(currentPlayer, currentAudience);
                if (isVip)
                {
                    SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber, new JsonAction("sendAdvanceToResultsScreen", new string[] { " " }));
                }
                break;
            case PhoneViewGameState.SendPersonalRoundResultsScreen:
                sendPersonalRoundResultsForReconnect(currentPlayer, currentAudience);
                break;
            case PhoneViewGameState.SendEndScreen:
                string currentBestFriend = CalculateBestFriend(currentPlayer);

                SendMessageToPhone(currentPlayer.deviceId, new JsonAction("sendEndScreen", new string[] { "" + currentPlayer.points,
                    (currentPlayer.numWrong + currentPlayer.points).ToString(), currentBestFriend }));
                break;
            default:
                SendMessageToPhone(from, new JsonAction("sendWaitScreen", new string[] { " " }));
                break;
        }
    }

    private void sendPersonalRoundResultsForReconnect(Player currentPlayer, Player currentAudience)
    {
        Player toSend;
        Dictionary<string, string> votesToUse;
        if (currentPlayer == null)
        {
            toSend = currentAudience;
            if(gameState.GetCurrentRound().audienceVotes.ContainsKey(toSend.playerNumber))
            {
                votesToUse = gameState.GetCurrentRound().audienceVotes[toSend.playerNumber];
            } else
            {
                return;
            }
        }
        else
        {
            toSend = currentPlayer;
            if(gameState.GetCurrentRound().votes.ContainsKey(toSend.playerNumber))
            {
                votesToUse = gameState.GetCurrentRound().votes[toSend.playerNumber];
            } else
            {
                return;
            }
        }
        SendSinglePlayerPersonalizedRoundResults(
            gameState.GetCurrentRound().answers,
            votesToUse,
            toSend,
            true);
    }

    /* Possible actions to send */
    public void SendWaitScreenToEveryone()
    {
        BroadcastToAllPhones(new JsonAction("sendWaitScreen", new string[] {}));
        gameState.phoneViewGameState = PhoneViewGameState.SendWaitScreen;
    }

    //todo remove parameter
    private IEnumerator<WaitForSeconds> ShowIntroInstrucitons(float seconds)
    {
        selectRoundNumberPanel.GetComponentsInChildren<Image>(true)[1].gameObject.SetActive(true);
        //flash the instructions
        SendMessageToVipAndSendWaitScreenToEveryoneElse(new JsonAction("sendSkipInstructions", new string[] {}));
        //AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendSkipInstructions", new string[] { " " })));
        gameState.phoneViewGameState = PhoneViewGameState.SendSkipInstructionsScreen;

        introVp.url = System.IO.Path.Combine(Application.streamingAssetsPath + "/", "meexarpsTutorial.mp4");

        introVp.Prepare();
        introVp.SetDirectAudioVolume(0, 1);
        yield return new WaitForSeconds(1);
        while (!introVp.isPrepared)
        {
            yield return new WaitForSeconds(1);
            break;
        }
        introInstructionVideo.texture = introVp.texture;
        introInstructionVideo.gameObject.SetActive(true);
        CameraZoom instructionsCz = introInstructionVideo.gameObject.AddComponent<CameraZoom>();
        instructionsCz.Setup(.5f, 86f, false, false, false, true, false);
        introVp.Play();
        //vp.Pause();
        yield return new WaitForSeconds(1);
        while (null != instructionsCz)
        {
            yield return new WaitForSeconds(1);
        }
        introVp.Pause();
        yield return new WaitForSeconds(1);
        introInstructionVideo.gameObject.SetActive(false);
        selectRoundNumberPanel.GetComponentsInChildren<Image>(true)[1].gameObject.SetActive(false);
        Destroy(instructionsCz);
        //AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendVoting", new string[] { " " })));
        selectRoundNumberPanel.SetActive(false);
        StartRound();
    }

    //startRound sends one person a SendRetrieveQuestions message and sends the others would you rathers until the questions are complete
    public void StartRound()
    {
        SetVolumeForLevel(thinkingAudioSources, 1);
        ChangeBackground(1);
        //reset the "Answer Set #" counter
        anonymousPlayerNumberCount = 0;
        anonymousPlayerNumbers.Shuffle(gameState.GetNumberOfPlayers());
        gameState.rounds.Add(new Round());

        //if starting from previous game
        deactivateResultsPanel();
        //set isOpen to false
        wouldYouRatherPanel.SetActive(true);

        //also sets to true
        gameState.updateRoundCounter(roundCounter);
        moveRoundCounter(false);

        //display only the active players without the current question writer
        int playerIconOffset = 14;
        Image[] playerIcons = wouldYouRatherPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");

        //i = 6 also disables audience icon
        //for (int i = 6; i >= gameState.GetNumberOfPlayers(); i--)
        for (int i = 5; i >= gameState.GetNumberOfPlayers(); i--)
        {
            //playerIcons[playerIconOffset + i].gameObject.SetActive(false);
            playerIconsList[i].gameObject.SetActive(false);
        }
        //reset all active players to active
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            //playerIcons[playerIconOffset + i].gameObject.SetActive(true);
            playerIconsList[i].gameObject.SetActive(true);
            //GameObject g = playerIconsList[i].GetComponentInChildren<Animator>().gameObject;
            updatePlayerAnimator(playerIconsList[i].GetComponentInChildren<Animator>(), i);
        }
        if (gameState.audienceMembers.Count > 0)
        {
            playerIcons[playerIconOffset + 6].gameObject.SetActive(true);
        }
        //also set the current player's icon to inactive
        //playerIcons[playerIconOffset + gameState.GetCurrentRoundNumber()].gameObject.SetActive(false);
        playerIconsList[gameState.GetCurrentWritingPlayer().playerNumber].gameObject.SetActive(false);

        /*
        //set the current player's icon to true
        int currentPlayerIconPanelOffset = 14;
        for (int i = 0; i < 6; i++)
        {
            if(i == gameState.GetCurrentRoundNumber())
            {
                playerIcons[currentPlayerIconPanelOffset + i].gameObject.SetActive(true);
            } else
            {
                playerIcons[currentPlayerIconPanelOffset + i].gameObject.SetActive(false);
            }
        }
        */
        //Set the current player's name
        Text[] wouldYouRatherTexts = wouldYouRatherPanel.GetComponentsInChildren<Text>(true);
        string playerName = gameState.GetCurrentWritingPlayer().nickname;
        getPlayerIconTags(playerIcons, "Banner")[0].GetComponentInChildren<TextMeshProUGUI>().text = "It's " + playerName + "'s turn to write questions!";
        
        Text[] playerTexts = wouldYouRatherPanel.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            playerIconsList[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = gameState.GetPlayerByPlayerNumber(i).nickname;
        }

        //controllers in the retrieve questions state will ignore would you rathers
        int currentPlayerTurnDeviceId = gameState.GetCurrentWritingPlayer().deviceId;
        SendSelectCategory(currentPlayerTurnDeviceId);

        gameState.phoneViewGameState = PhoneViewGameState.SendWouldYouRather;
        gameState.tvViewGameState = TvViewGameState.SubmitQuestionsScreen;
        InvokeRepeating("SendWouldYouRather", 0f, 20f);
    }

    private void deactivateResultsPanel()
    {
        Animator nextActionBannerAnimator = resultsPanel.GetComponentsInChildren<Animator>()[2];
        nextActionBannerAnimator.SetBool("isOpen", false);
        GameObject banner = GameObject.FindWithTag("Banner");
        if(banner != null)
        {
            RectTransform rectTransform = banner.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, -.1f);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
        }
        resultsPanel.SetActive(false);
    }

    private void updatePlayerAnimator(Animator a, int playerNumber)
    {
        updatePlayerAnimator(a, gameState.GetPlayerByPlayerNumber(playerNumber));
    }

    private void updatePlayerAnimator(Animator a, Player p)
    {
        a.runtimeAnimatorController = p.myAnimator.runtimeAnimatorController;
    }

    public static List<Image> getPlayerIconTags(Image[] playerIcons, string tagName)
    {
        List<Image> playerIconsList = new List<Image>();
        foreach (Image i in playerIcons)
        {
            if (i.tag == tagName)
            {
                playerIconsList.Add(i);
            }
        }

        return playerIconsList;
    }

    private static List<TextMeshProUGUI> getTextTags(TextMeshProUGUI[] texts, string tagName)
    {
        List<TextMeshProUGUI> playerIconsList = new List<TextMeshProUGUI>();
        foreach (TextMeshProUGUI i in texts)
        {
            if (i.tag == tagName)
            {
                playerIconsList.Add(i);
            }
        }

        return playerIconsList;
    }

    public void SendSelectCategory(int deviceId)
    {
        //-1 is write my own, -2 is seasonal
        KeyValuePair<string, int>[] categoriesToSend = new KeyValuePair<string, int>[] {
            isWinterHolidaySeason ? GetSeasonalCategoryName() : GetNextCategoryName(),
            GetNextCategoryName(),
            GetNextCategoryName(),
            GetNextCategoryName(),
            GetNextCategoryName(),
            new KeyValuePair<string, int> ("I'll write my own", -1)
        };
        List<int> currentSentCategoryIndices = new List<int>();
        currentSentCategoryIndices.Add(categoriesToSend[0].Value);
        currentSentCategoryIndices.Add(categoriesToSend[1].Value);
        currentSentCategoryIndices.Add(categoriesToSend[2].Value);
        currentSentCategoryIndices.Add(categoriesToSend[3].Value);
        currentSentCategoryIndices.Add(categoriesToSend[4].Value);
        currentSentCategoryIndices.Add(categoriesToSend[5].Value);

        sentCategoryIndices = currentSentCategoryIndices;

        string[] stringifiedCategoriesToSend = new string[]
        {
            categoriesToSend[0].Key,
            categoriesToSend[1].Key,
            categoriesToSend[2].Key,
            categoriesToSend[3].Key,
            categoriesToSend[4].Key,
            categoriesToSend[5].Key
        };

        SendMessageToPhone(deviceId, new JsonAction("sendSelectCategory", stringifiedCategoriesToSend));
    }

    private KeyValuePair<string, int> GetNextCategoryName()
    {
        QuestionCategory questionCategory;
        int tempCurrentCategoryIndex;
        if(options["nsfwQuestions"])
        {
            tempCurrentCategoryIndex = currentCategoryIndex;
            questionCategory = questionCategories[currentCategoryIndex++ % questionCategories.Count];
        } else
        {
            do
            {
                tempCurrentCategoryIndex = currentCategoryIndex;
                questionCategory = questionCategories[currentCategoryIndex++ % questionCategories.Count];
            } while (questionCategory.isNsfw);
        }
        return new KeyValuePair<string, int>(questionCategory.categoryName, tempCurrentCategoryIndex);
    }
    //seasonal questions will always be the first cateogry
    private KeyValuePair<string, int> GetSeasonalCategoryName()
    {
        return new KeyValuePair<string, int>(seasonalQuestionCategory.categoryName, -2);
    }

    public void SendRetrieveQuestions(int deviceId, bool localWriteMyOwnQuestions)
    {
        string[] questionsToSend;
        if (localWriteMyOwnQuestions)
        {
            questionsToSend = new string[] {
                "",
                "",
                "",
                "I'll write my own"
            };
            writeMyOwnQuestions = true;
        }
        else
        {
            questionsToSend = new string[] {
                GetNextQuestion(),
                GetNextQuestion(),
                GetNextQuestion(),
                selectedCategory == -2 ? seasonalQuestionCategory.categoryName : questionCategories[selectedCategory % questionCategories.Count].categoryName
            };
            writeMyOwnQuestions = false;
        }
        SendMessageToPhone(deviceId, new JsonAction("sendRetrieveQuestions", questionsToSend));
    }

    public void SendWouldYouRather()
    {
        WouldYouRatherTimer oldWouldYouRatherTimer = wouldYouRatherPanel.GetComponent<WouldYouRatherTimer>();
        if (null == oldWouldYouRatherTimer)
        {
            Destroy(oldWouldYouRatherTimer);
        }
        WouldYouRatherTimer wyrt = wouldYouRatherPanel.AddComponent<WouldYouRatherTimer>();
        wyrt.SetTimerText(wouldYouRatherPanel.GetComponentsInChildren<TextMeshProUGUI>()[1]);
        //reset the icons
        int playerIconOffset = 5;
        Image[] playerIconPanels = wouldYouRatherPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconTags = getPlayerIconTags(playerIconPanels, "WouldYouRatherPlayerIcon");
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            if (i == gameState.GetCurrentWritingPlayer().playerNumber) {
                //don't do anything for the current player
                continue;
            }
            movePlayerIcon(playerIconTags[i], 0);
        }
        audienceWouldYouRathers.Clear();
        if(gameState.audienceMembers.Count != 0)
        {
            SetAudienceWouldYouRatherCounters(0, 0);
        }
        TextMeshProUGUI[] wouldYouRatherTexts = wouldYouRatherPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        string[] currentWouldYouRather = wouldYouRathers[currentWouldYouRatherIndex++ % wouldYouRathers.Count];
        wouldYouRatherTexts[0].text = currentWouldYouRather[0];
        //left answer
        wouldYouRatherTexts[2].text = currentWouldYouRather[1];
        //right answer
        wouldYouRatherTexts[3].text = currentWouldYouRather[2];
        //Maybe send the possible answers here
        string waitingForPlayerName = gameState.GetCurrentWritingPlayer().nickname;
        BroadcastToAllPhones(new JsonAction("sendWouldYouRather", new[] { waitingForPlayerName }));
    }

    private void movePlayerIcon(Image playerIcon, float x)
    {
        //        playerIcon.transform.localPosition = new Vector2(x, playerIcon.transform.localPosition.y);
        int transformIndex = 1;
        Transform iconContainerTransform = playerIcon.GetComponentsInChildren<Transform>()[transformIndex].transform;
        iconContainerTransform.localPosition = new Vector2(x, iconContainerTransform.transform.localPosition.y);
        /*
        Transform myTextTransform = playerIcon.GetComponentInChildren<Text>().transform;
        Transform myIconTransform = playerIcon.GetComponentsInChildren<Image>()[1].transform;
        //myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.50f * canvas.scaleFactor, myTransform.position.y);
        myTextTransform.localPosition = new Vector2(x, myTextTransform.localPosition.y);
        myIconTransform.localPosition = new Vector2(x, myIconTransform.localPosition.y);
        */
    }

    public void SendQuestions()
    {
        SendQuestions(-1);
    }

    public void SendQuestions(int playerDeviceId)
    {
        string[] questionsToSend = gameState.GetCurrentRound().questions.ToArray();

        if (playerDeviceId < 0)
        {
            Image[] playerIcons = answerQuestionsPanel.GetComponentsInChildren<Image>(true);
            List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
            //reset all play player icons
            foreach(Image i in playerIconsList)
            {
                movePlayerIcon(i, 0);
            }
            SendMessageToPlayersAndSendWaitScreenToAudience(new JsonAction("sendQuestions", questionsToSend));
            foreach(Player audience in gameState.audienceMembers.Values)
            {
                SendMessageToPhone(audience.deviceId, new JsonAction("sendVotingTutorialScreen", questionsToSend));
            }
            gameState.phoneViewGameState = PhoneViewGameState.SendQuestions;
            gameState.tvViewGameState = TvViewGameState.AnswerQuestionsScreen;
        }
        //reconnect case
        else
        {
            if (isAudienceMember(playerDeviceId))
            {
                SendMessageToPhone(playerDeviceId, new JsonAction("sendVotingTutorialScreen", questionsToSend));
            }
            else
            {
                SendMessageToPhone(playerDeviceId, new JsonAction("sendQuestions", questionsToSend));
            }
        }
    }

    public void SendVoting()
    {
        //show instruction will show skippable instructions, then send the voting screen to the phones, then dozoom
        StartCoroutine(ShowInstrucitons(2));

    }

    //send voting screen for reconnecting players
    public void SendVoting(int playerDeviceId)
    {
        List<string[]> playerNames = new List<string[]>();
        List<string> anonymousPlayerNames = new List<string>();
        List<string> playerAnswers = new List<string>();
        int myPlayerNumber = -1;
        if (!isAudienceMember(playerDeviceId))
        {
            myPlayerNumber = gameState.players[playerDeviceId].playerNumber;
        }
        List<Answers> answersList = gameState.GetCurrentRound().answers;

        //add each playerName
        foreach (Answers a in answersList)
        {
            if(a.playerNumber != myPlayerNumber)
            {
                Player p = gameState.GetPlayerByPlayerNumber(a.playerNumber);
                string nickName = p.nickname;
                string alienNumber = "" + p.alienNumber; 
                playerNames.Add(new[] { nickName, alienNumber });
            }
        }
        //add each anonymous name
        for (int i = 0; i < answersList.Count; i++)
        {
            Answers answers = answersList[i];
            if (answers.playerNumber != myPlayerNumber)
            {
                anonymousPlayerNames.Add(answers.anonymousPlayerName);
            }
        }

        //add the set of answers to display as help text
        /*TODO this is not in the same order as the anonymous names.*/
        for (int i = 0; i < answersList.Count; i++)
        {
            Answers answers = answersList[i];
            if (answers.playerNumber != myPlayerNumber)
            {
                playerAnswers.Add(string.Join("~~NEWLINE~~", answers.text));
            }
        }

        //send data to the phones
        List<string> listToSend = new List<string>();
        playerNames.Shuffle();

        //anonymousPlayerNames.Shuffle();
        List<string> pNames = new List<string>();
        List<string> alienNumbers = new List<string>();
        foreach (string[] playerName in playerNames)
        {
            pNames.Add(playerName[0]);
            alienNumbers.Add(playerName[1]);
        }
        listToSend.AddRange(pNames);
        listToSend.AddRange(anonymousPlayerNames);
        /*TODO this is not in the same order as anonymous player names*/
        listToSend.AddRange(playerAnswers);
        listToSend.AddRange(alienNumbers);
        SendMessageToPhone(playerDeviceId, new JsonAction("sendVoting", listToSend.ToArray()));
    }

    //todo remove parameter
    private IEnumerator<WaitForSeconds> ShowInstrucitons(float seconds)
    {
        //setup and show the answer panels
        List<string> playerNames = new List<string>();
        List<string> anonymousPlayerNames = new List<string>();

        foreach (Player p in gameState.players.Values)
        {
            playerNames.Add(p.nickname);
        }

        //gameState.GetCurrentRound().answers.Shuffle();

        List<Answers> answersList = gameState.GetCurrentRound().answers;
        GameObject votingPanelGrid = GameObject.FindGameObjectWithTag("VotingPanelGrid");
        //re-enable all panels
        Image[] votingPanelCards = votingPanelGrid.GetComponentsInChildren<Image>(true);
        int gridOffset = 1;
        for (int i = 0; i < 6; i++)
        {
            votingPanelCards[gridOffset + i].gameObject.SetActive(true);
        }
        for (int i = 0; i < answersList.Count; i++)
        {
            Answers answers = answersList[i];
            anonymousPlayerNames.Add(answers.anonymousPlayerName);
            TextMeshProUGUI myTitle = votingPanelGrid.GetComponentsInChildren<TextMeshProUGUI>()[2*i];
            TextMeshProUGUI myQandA = votingPanelGrid.GetComponentsInChildren<TextMeshProUGUI>()[2*i + 1];
            //todo set the text size to the same size as the panel
            myTitle.text = answers.anonymousPlayerName;
            myQandA.text = "";
            //myTitle.fontSizeMax = 25;
        }
        votingPanel.GetComponentsInChildren<TextMeshProUGUI>()[1].text = gameState.GetCurrentRound().PrintQuestions();
        
        //flash the instructions
        if (gameState.GetCurrentRoundNumber() == 0)
        {
            SendMessageToVipAndSendWaitScreenToEveryoneElse(new JsonAction("sendSkipInstructions", new string[] {  }));
            //            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendSkipInstructions", new string[] { " " })));
            gameState.phoneViewGameState = PhoneViewGameState.SendSkipInstructionsScreen;

            Image[] images = votingPanel.GetComponentsInChildren<Image>();
            vp.url = System.IO.Path.Combine(Application.streamingAssetsPath + "/", "meexarpsGuessingTutorial.mp4");

            vp.Prepare();
            //if we update the video, update this ratio
            vp.SetDirectAudioVolume(0, onVolume*3);
            yield return new WaitForSeconds(1);
            while (!vp.isPrepared)
            {
                yield return new WaitForSeconds(1);
                break;
            }
            instructionVideo.texture = vp.texture;
            instructionVideo.gameObject.SetActive(true);
            instructionVideo.gameObject.GetComponent<RectTransform>().SetAsLastSibling();
            CameraZoom instructionsCz = instructionVideo.gameObject.AddComponent<CameraZoom>();
            instructionsCz.Setup(.5f, 20f, false, false, false, true, false);
            vp.Play();
            //vp.Pause();
            yield return new WaitForSeconds(1);
            vp.Play();
            while(null != instructionsCz)
            {
                yield return new WaitForSeconds(1);
            }
            vp.Pause();
            yield return new WaitForSeconds(1);
            instructionVideo.gameObject.SetActive(false);
            Destroy(instructionsCz);
            //reset the position of the child panels
            //questions panel
            images[1].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
            //votingpanelgrid
            images[2].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        } else
        {
            yield return new WaitForSeconds(1);
        }

        //send data to the phones
        foreach (Player p in gameState.players.Values)
        {
            SendVoting(p.deviceId);
        }
        foreach (Player p in gameState.audienceMembers.Values)
        {
            SendVoting(p.deviceId);
        }
        /*
        List<string> listToSend = new List<string>();
        listToSend.AddRange(playerNames);
        listToSend.AddRange(anonymousPlayerNames);
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendVoting", listToSend.ToArray())));
        */
        gameState.phoneViewGameState = PhoneViewGameState.SendVoting;

        StartCoroutine(DoZoom(2));

    }

    //todo remove parameter
    private IEnumerator<WaitForSeconds> DoZoom(float seconds)
    {
        int gridLayoutOffset = 2;
        votingPanel.GetComponentsInChildren<Image>()[gridLayoutOffset].GetComponentInChildren<GridLayoutGroup>().enabled = false;
        AutoResizeGrid autoResizeGrid = FindObjectsOfType(typeof(AutoResizeGrid))[2] as AutoResizeGrid;
        autoResizeGrid.enabled = false;
        int panelOffset = 3;
        int numPlayersAtStart = gameState.GetCurrentRound().answers.Count;
        GameObject[] votingGridQnA = GameObject.FindGameObjectsWithTag("votingTextQnA");

        for (int i = 0; i < numPlayersAtStart; i++)
        {
            TextMeshProUGUI myQnAText = votingGridQnA[i].GetComponentInChildren<TextMeshProUGUI>();
            myQnAText.text ="";

            int answerDisplayDuration = 4;
            //Camera zoom will make the current panel the last element, so we don't need to add i
            CameraZoom cz = votingPanel.GetComponentsInChildren<Image>()[panelOffset].gameObject.AddComponent<CameraZoom>();
            cz.Setup(1f, answerDisplayDuration * 3f, true, true, false);

            //temporarily increase the max size
            //myTitle.fontSizeMax = 60;
            //myQnAText.fontSizeMax = 40;

            List<Answers> answersList = gameState.GetCurrentRound().answers;
            Answers answers = answersList[i];

            //create the short text to replace the text with questions on minimize
            StringBuilder shortTextSb = new StringBuilder();

            //wait one second after pause before displaying anything
            yield return new WaitForSeconds(2);

            for (int j = 0; j < answers.text.Length; j++)
            {
                string answer = answers.text[j];

                string newText = myQnAText.text + "<color=black>" + gameState.GetCurrentRound().questions[j] + "</color>\n<size=4>\n</size><b>" + answer + "</b>";

                myQnAText.text = newText;
                shortTextSb.Append(j+1 + ". <color=black>" + answer + "</color>");
                if (j < answers.text.Length - 1)
                {
                    myQnAText.text = myQnAText.text + "<size=8>\n\n</size>";
                    shortTextSb.Append("<size=8>\n\n</size>");
                }
                //wait answerDisplayDuration seconds between each answer to give it a punch
                yield return new WaitForSeconds(answerDisplayDuration);
            }
            myQnAText.text = shortTextSb.ToString();

            //reset the max size
           // myTitle.fontSizeMax = 25;
            //myQnAText.fontSizeMax = 20;
            yield return new WaitForSeconds(2);
            Destroy(cz);
        }
        Image[] playerIcons = votingPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "AnswerQuestionsPane");
        //reset the ordering of the panels
        for (int i = 0; i < 6 - numPlayersAtStart; i++)
        {
            playerIconsList[i].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }
        votingPanel.GetComponentsInChildren<Image>()[gridLayoutOffset].GetComponentInChildren<GridLayoutGroup>().enabled = true;
        autoResizeGrid.enabled = true;
    }

    private void debugOnPhone(string key, string value)
    {
        Debug.Log(key + ": " + value);
        JsonAction jsonAction = new JsonAction("debug", new string[] { key + ": " + value });
        BroadcastToAllPhones(jsonAction);
    }
    private void sendEveryonePersonalizedRoundResults(List<Answers> answerList)
    {
        Round currentRound = gameState.GetCurrentRound();
        
        //todo dedupe this code
        //players
        foreach (KeyValuePair<int, Dictionary<string, string>> playerVote in currentRound.votes)
        {
            Player currentPlayer = gameState.GetPlayerByPlayerNumber(playerVote.Key);
            SendSinglePlayerPersonalizedRoundResults(answerList, playerVote.Value, currentPlayer, false);
        }
        //audience
        foreach (KeyValuePair<int, Dictionary<string, string>> audienceVote in currentRound.audienceVotes)
        {
            Player currentAudiencePlayer = gameState.GetAudienceByPlayerNumber(audienceVote.Key);
            SendSinglePlayerPersonalizedRoundResults(answerList, audienceVote.Value, currentAudiencePlayer, false);
        }
    }

    private void SendSinglePlayerPersonalizedRoundResults(List<Answers> answerList, Dictionary<string, string> playerVote, Player currentPlayer, bool isReconnect)
    {
        //construct the answer set
        List<string> personalRoundResults = new List<string>();
        //anon names
        foreach (Answers a in answerList)
        {
            if (currentPlayer.playerNumber.Equals(a.playerNumber))
            {
                //skip
            }
            else
            {
                personalRoundResults.Add(a.anonymousPlayerName);
            }
        }
        //guess
        List<string> guesses = new List<string>();
        foreach (Answers a in answerList)
        {
            if (currentPlayer.playerNumber.Equals(a.playerNumber))
            {
                //skip
            }
            else
            {
                if (playerVote.ContainsKey(a.anonymousPlayerName))
                {
                    guesses.Add(playerVote[a.anonymousPlayerName]);
                }
                else
                {
                    guesses.Add("~~~None~~~");
                }
            }
        }
        //actual
        List<string> actual = new List<string>();
        List<string> actualAlienNumbers = new List<string>();
        foreach (Answers a in answerList)
        {
            if (currentPlayer.playerNumber.Equals(a.playerNumber))
            {
                //skip
            }
            else
            {
                int playerNum = a.playerNumber;
                Player p = gameState.GetPlayerByPlayerNumber(playerNum);
                actual.Add(p.nickname);
                actualAlienNumbers.Add("" + p.alienNumber);
            }
        }

        incrementBestFriends(currentPlayer, guesses, actual);
        personalRoundResults.AddRange(guesses);
        personalRoundResults.AddRange(actual);
        personalRoundResults.AddRange(actualAlienNumbers);

        if (isReconnect)
        {
            personalRoundResults.Add("~~~reconnect~~~");
        }
        else
        {
            personalRoundResults.Add("~~~notReconnect~~~");
        }


        //send the current player their answerset
        SendMessageToPhone(currentPlayer.deviceId, new JsonAction("sendPersonalRoundResults", personalRoundResults.ToArray()));
    }

    private static void incrementBestFriends(Player currentPlayer, List<string> guesses, List<string> actual)
    {
        //calculate best friends
        for (int i = 0; i < guesses.Count; i++)
        {
            if (guesses[i].Equals(actual[i]))
            {
                currentPlayer.IncrementBestFriendCounter(guesses[i]);
            }
        }
    }

    public IEnumerator<WaitForSeconds> EndAnswerQuestionsPhase(int count)
    {
        Image[] playerIcons = answerQuestionsPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "WouldYouRatherPlayerIcon");
        SetVolumeForLevel(thinkingAudioSources, 2);

        //everyone should cheer
        foreach (Player p in gameState.players.Values)
        {
            playerIconsList[p.playerNumber].GetComponentInChildren<Animator>().SetBool("isCheer", true);
            //playerIconsList[p.playerNumber].GetComponentInChildren<Animator>().SetBool("isCheer", false);
            //p.playAnimation(MeexarpAction.Cheer);
        }
        int cheerAnimationLength = 1;
        int waitAnimationLength = 3;
        yield return new WaitForSeconds(cheerAnimationLength);


        foreach (Player p in gameState.players.Values)
        {
            playerIconsList[p.playerNumber].GetComponentInChildren<Animator>().SetBool("isCheer", false);
        }
        yield return new WaitForSeconds(waitAnimationLength);

        //Stop the loading screen tips
        CancelInvoke();

        answerQuestionsPanel.SetActive(false);
        votingPanel.SetActive(true);
        ChangeBackground(2);
        //TODO shuffle answers
        SendVoting();
        gameState.phoneViewGameState = PhoneViewGameState.SendVoting;
        gameState.tvViewGameState = TvViewGameState.VotingScreen;
    }

        //todo remove parameter
    public IEnumerator<WaitForSeconds> CalculateVoting(bool shouldWaitForAudience)
    {
        //SetVolumeForLevel(thinkingAudioSources, 2);
        if (shouldWaitForAudience)
        {
            yield return new WaitForSeconds(10);
        }
        //autocollect responses from each audience member
        foreach (Player audienceMember in gameState.audienceMembers.Values)
        {
            if (!gameState.GetCurrentRound().audienceVotes.ContainsKey(audienceMember.playerNumber))
            {
                shouldWaitForAudience = true;
                SendMessageToPhone(audienceMember.deviceId,
                    new JsonAction("forceCollectAnswers", new[] { "true" }));
            }
        }
        if (shouldWaitForAudience)
        {
            yield return new WaitForSeconds(1);
        }
        votingPanel.SetActive(false);
        resultsPanel.SetActive(true);
        moveRoundCounter(true);
        //SendWaitScreenToEveryone();
        //AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendPersonalRoundResults", new string[] { " " })));
        gameState.phoneViewGameState = PhoneViewGameState.SendPersonalRoundResultsScreen;
        gameState.tvViewGameState = TvViewGameState.RoundResultsScreen;

        //set the anonymous names of each box
        List<Answers> answersList = gameState.GetCurrentRound().answers;
        int countAtBeginingOfRound = answersList.Count;
        GameObject resultsPanelGrid = GameObject.FindGameObjectWithTag("ResultsPanelGrid");
        Image[] resultsPanelCards = resultsPanelGrid.GetComponentsInChildren<Image>(true);
        int rPanelOffset = 1;

        for (int i = 0; i < countAtBeginingOfRound; i++)
        {
            Answers answers = answersList[i];
            GameObject currentResultPanel = resultsPanelCards[i + rPanelOffset].gameObject;
            TextMeshProUGUI myTitle = currentResultPanel.GetComponentsInChildren<TextMeshProUGUI>()[0];
            TextMeshProUGUI myQandA = currentResultPanel.GetComponentsInChildren<TextMeshProUGUI>()[1];

            myTitle.text = answers.anonymousPlayerName;
            myQandA.text = "";
        }

        //send each individual their personalized results
        sendEveryonePersonalizedRoundResults(answersList);

        //give some time for the context switch
        yield return new WaitForSeconds(2);

        resultsPanel.GetComponentsInChildren<Image>()[0].GetComponentInChildren<GridLayoutGroup>().enabled = false;
        AutoResizeGrid autoResizeGrid = FindObjectsOfType(typeof(AutoResizeGrid))[3] as AutoResizeGrid;
        autoResizeGrid.enabled = false;

        List<Answers> answerList = gameState.GetCurrentRound().answers;
        for (int i = 0; i < countAtBeginingOfRound; i++)
        {
            List<string> namesOfCorrectPeople = new List<string>();
            List<string> namesOfCorrectPeopleForZoomInView = new List<string>();
            Dictionary<string, List<string>> wrongGuessNameToPlayerNames = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> wrongGuessNameToPlayerNamesForZoomInView = new Dictionary<string, List<string>>();
            //0 is sad, 1 is cheer
            Dictionary<Player, MeexarpAction> playerAnimations = new Dictionary<Player, MeexarpAction>();


            int numberOfCorrectAudienceGuesses = 0;
            int numberOfWrongAudienceGuesses = 0;

            Answers answers = answerList[i];
            string anonymousPlayerName = answers.anonymousPlayerName;
            string targetPlayerName = gameState.GetPlayerByPlayerNumber(answers.playerNumber).nickname;
            foreach (KeyValuePair<int, Dictionary<string, string>> playerVote in gameState.GetCurrentRound().votes)
            {
                Player p = gameState.GetPlayerByPlayerNumber(playerVote.Key);

                if (p.playerNumber == answers.playerNumber)
                {
                    //ignore voting for yourself
                }
                else if (playerVote.Value.ContainsKey(anonymousPlayerName))
                {
                    string currentGuess = playerVote.Value[anonymousPlayerName];
                    if (playerVote.Value.ContainsKey(anonymousPlayerName))
                    {
                        if (currentGuess == targetPlayerName)
                        {
                            namesOfCorrectPeopleForZoomInView.Add("<color=green>" + p.nickname + "</color>");
                            namesOfCorrectPeople.Add("<b><color=green>" + p.nickname + "</color></b>");
                            p.points++;
                            //playerAnimations.Add(p, MeexarpAction.Cheer);

                            //keep track of total score
                            gameState.totalCorrectGuesses++;
                        }
                        else
                        {
                            p.numWrong++;
                            if(!wrongGuessNameToPlayerNames.ContainsKey(currentGuess))
                            {
                                wrongGuessNameToPlayerNamesForZoomInView.Add(currentGuess, new List<string>());
                                wrongGuessNameToPlayerNames.Add(currentGuess, new List<string>());
                            }
                            wrongGuessNameToPlayerNamesForZoomInView[currentGuess].Add("<color=red>" + p.nickname + "</color>");
                            wrongGuessNameToPlayerNames[currentGuess].Add("<b><color=red>" + p.nickname + "</color></b>");
                            //playerAnimations.Add(p, MeexarpAction.Sad);
                            //keep track of total score
                            gameState.totalWrongGuesses++;
                        }
                    }
                } else
                {
                    if (!wrongGuessNameToPlayerNames.ContainsKey("none"))
                    {
                        wrongGuessNameToPlayerNamesForZoomInView.Add("none", new List<string>());
                        wrongGuessNameToPlayerNames.Add("none", new List<string>());
                    }
                    wrongGuessNameToPlayerNamesForZoomInView["none"].Add("<color=red>" + p.nickname + "</color>");
                    wrongGuessNameToPlayerNames["none"].Add("<b><color=red>" + p.nickname + "</color></b>");
                    playerAnimations.Add(p, MeexarpAction.Sad);
                    //keep track of total score
                    p.numWrong++;
                    gameState.totalWrongGuesses++;
                }
            }
            //calculate audience votes
            foreach (KeyValuePair<int, Dictionary<string, string>> playerVote in gameState.GetCurrentRound().audienceVotes)
            {
                Player p = gameState.GetAudienceByPlayerNumber(playerVote.Key);
                if (playerVote.Value.ContainsKey(anonymousPlayerName))
                {
                    string currentGuess = playerVote.Value[anonymousPlayerName];
                    if (playerVote.Value.ContainsKey(anonymousPlayerName))
                    {
                        if (currentGuess == targetPlayerName)
                        {
                            numberOfCorrectAudienceGuesses++;
                            p.points++;
                        }
                        else
                        {
                            p.numWrong++;
                            numberOfWrongAudienceGuesses++;
                        }
                    }
                }
            }
            StringBuilder correctVotesSB = new StringBuilder();
            StringBuilder correctVotesStringSb = new StringBuilder();
            if (namesOfCorrectPeople.Count > 0)
            {
                correctVotesStringSb.Append(System.String.Join(", ", namesOfCorrectPeopleForZoomInView.GetRange(0, namesOfCorrectPeople.Count - 1)));
                correctVotesSB.Append(System.String.Join(", ", namesOfCorrectPeople.GetRange(0, namesOfCorrectPeople.Count - 1)));
                if (namesOfCorrectPeople.Count != 1)
                {
                    correctVotesStringSb.Append(" and ");
                    correctVotesSB.Append(" and ");
                }
                correctVotesStringSb.Append(namesOfCorrectPeopleForZoomInView[namesOfCorrectPeople.Count - 1]);
                correctVotesSB.Append(namesOfCorrectPeople[namesOfCorrectPeople.Count - 1]);
            } else
            {
                correctVotesStringSb.Append("No one");
                correctVotesSB.Append("No one");
            }
            correctVotesSB.Append(" guessed correctly!");

            correctVotesStringSb.Append(" guessed correctly!");

            StringBuilder wrongVotesSb = new StringBuilder();
            StringBuilder wrongVotesForZoomInViewSb = new StringBuilder();
            int wrongVotesCount = 0;
            foreach (KeyValuePair<string, List<string>> wrongVotes in wrongGuessNameToPlayerNames)
            {
                StringBuilder currentSb = new StringBuilder();
                if("none".Equals(wrongVotes.Key))
                {
                    continue;
                }
                currentSb.Append(System.String.Join(", ", wrongVotes.Value.GetRange(0, wrongVotes.Value.Count - 1)));
                if(wrongVotes.Value.Count != 1) {
                    currentSb.Append(" and ");
                }
                currentSb.Append(wrongVotes.Value[wrongVotes.Value.Count - 1]);
                currentSb.Append(" incorrectly guessed ");
                currentSb.Append(wrongVotes.Key);
                currentSb.Append("\n");
                currentSb.Append("\n");
                wrongVotesSb.Append(currentSb.ToString());
                wrongVotesCount++;
            }

            if (wrongGuessNameToPlayerNames.ContainsKey("none"))
            {
                StringBuilder currentSb = new StringBuilder();
                List<string> noneVoters = wrongGuessNameToPlayerNames["none"];
                bool hasOneNoneVoter = noneVoters.Count == 1;
                currentSb.Append(System.String.Join(", ", noneVoters.GetRange(0, noneVoters.Count - 1)));
                if (!hasOneNoneVoter)
                {
                    currentSb.Append(" and ");
                }
                currentSb.Append(noneVoters[noneVoters.Count - 1]);
                if(hasOneNoneVoter)
                {
                    currentSb.Append(" has ");
                } else
                {
                    currentSb.Append(" have ");
                }
                currentSb.Append("no idea who ");
                currentSb.Append(anonymousPlayerName);
                currentSb.Append(" is!");
                currentSb.Append("\n");
                currentSb.Append("\n");
                wrongVotesSb.Append(currentSb.ToString());
                wrongVotesCount++;
            }

            //todo unduplicate this code
            List<string> wrongVotesLines = new List<string>();
            foreach (KeyValuePair<string, List<string>> wrongVotes in wrongGuessNameToPlayerNamesForZoomInView)
            {
                StringBuilder currentSb = new StringBuilder();
                if ("none".Equals(wrongVotes.Key))
                {
                    continue;
                }
                currentSb.Append(System.String.Join(", ", wrongVotes.Value.GetRange(0, wrongVotes.Value.Count - 1)));
                if (wrongVotes.Value.Count != 1)
                {
                    currentSb.Append(" and ");
                }
                currentSb.Append(wrongVotes.Value[wrongVotes.Value.Count - 1]);
                currentSb.Append(" incorrectly guessed ");
                currentSb.Append(wrongVotes.Key);
                wrongVotesLines.Add(currentSb.ToString());
            }

            if (wrongGuessNameToPlayerNames.ContainsKey("none"))
            {
                StringBuilder currentSb = new StringBuilder();
                List<string> noneVoters = wrongGuessNameToPlayerNamesForZoomInView["none"];
                bool hasOneNoneVoter = noneVoters.Count == 1;
                currentSb.Append(System.String.Join(", ", noneVoters.GetRange(0, noneVoters.Count - 1)));
                if (!hasOneNoneVoter)
                {
                    currentSb.Append(" and ");
                }
                currentSb.Append(noneVoters[noneVoters.Count - 1]);
                if (hasOneNoneVoter)
                {
                    currentSb.Append(" has ");
                }
                else
                {
                    currentSb.Append(" have ");
                }
                currentSb.Append("no idea who ");
                currentSb.Append(anonymousPlayerName);
                currentSb.Append(" is!");
                wrongVotesLines.Add(currentSb.ToString());
            }

            //an offset for the the questions tile
            int playerTileOffset = 1;

            TextMeshProUGUI myTitle = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>()[playerTileOffset];
            TextMeshProUGUI myQandAs = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>()[playerTileOffset + 1];

            //temporarily increase max size
           // myTitle.fontSizeMax = 60;
            //myQandAs.fontSizeMax = 40;

            //display all results panel
            int resultsPanelOffset = playerTileOffset + 12;
            Animator titleAndGridContainerAnimator = resultsPanel.GetComponentsInChildren<Animator>()[0];
            Animator rightAndWrongPanelAnimator = resultsPanel.GetComponentsInChildren<Animator>()[1];
            TextMeshProUGUI rightAndWrongPanelTitle = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>(true)[resultsPanelOffset];
            TextMeshProUGUI rightAndWrongPanelRightAndWrong = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>(true)[resultsPanelOffset+1];
            TextMeshProUGUI rightAndWrongPanelAudienceRightAndWrong = resultsPanel.GetComponentsInChildren<TextMeshProUGUI>(true)[resultsPanelOffset+2];

            rightAndWrongPanelTitle.text ="";
            rightAndWrongPanelRightAndWrong.text = "";
            rightAndWrongPanelAudienceRightAndWrong.text = "";

            //first, display the questions and answers again
            int playerPanelTileOffset = 2;
            myTitle.text = anonymousPlayerName + "'s answers";
            CameraZoom cz = resultsPanel.GetComponentsInChildren<Image>()[playerPanelTileOffset].gameObject.AddComponent<CameraZoom>();

            int zoomInTime = 1;
            float waitForContextSeconds = 1.5f;
            int waitForReadSeconds = 4;
            float waitForEachAnswer = .5f;

            float totalWaitTime = 2 * waitForContextSeconds + 2 * waitForReadSeconds + (3 + wrongVotesCount) * waitForEachAnswer;
            cz.Setup(zoomInTime, totalWaitTime, true, true, false, false, true);

            rightAndWrongPanelTitle.text = "<b>" + anonymousPlayerName +
                " is ???" + " </b>";

            rightAndWrongPanelAnimator.SetBool("Open", true);
            yield return new WaitForSeconds(zoomInTime);
            yield return new WaitForSeconds(waitForContextSeconds);
            for (int j = 0; j < answers.text.Length; j++)
            {
                yield return new WaitForSeconds(waitForEachAnswer);
                string answer = answers.text[j];

                myQandAs.text = myQandAs.text + "<color=black>" + gameState.GetCurrentRound().questions[j] + "</color><size=4>\n\n</size><color=#4A6EEF>" + answer + "</color>";
                if (j < answers.text.Length - 1)
                {
                    myQandAs.text = myQandAs.text +"<size=8>\n\n</size>";
                }
            }
            yield return new WaitForSeconds(waitForReadSeconds);

            string tileTitle = anonymousPlayerName + " is... \n<color=blue>" + targetPlayerName + "</color>";
            rightAndWrongPanelTitle.text = "<b>" + tileTitle + "</b>";
            yield return new WaitForSeconds(waitForContextSeconds);

            foreach (string s in wrongVotesLines)
            {
                rightAndWrongPanelRightAndWrong.text = rightAndWrongPanelRightAndWrong.text + "<size=7>\n\n</size>" + s;
                yield return new WaitForSeconds(waitForEachAnswer);
            }
            rightAndWrongPanelRightAndWrong.text = rightAndWrongPanelRightAndWrong.text 
                + "<size=7>\n\n</size>" + correctVotesStringSb.ToString();
            if(numberOfCorrectAudienceGuesses > 0)
            {
                rightAndWrongPanelAudienceRightAndWrong.text = rightAndWrongPanelAudienceRightAndWrong.text + 
                    "<color=green>" + numberOfCorrectAudienceGuesses + "</color> Correct Audience Members";
                gameState.totalAudienceCorrectGuesses += numberOfCorrectAudienceGuesses;
            }
            if(numberOfWrongAudienceGuesses > 0)
            {
                rightAndWrongPanelAudienceRightAndWrong.text = rightAndWrongPanelAudienceRightAndWrong.text +
                    "<size=15>\n\n</size><color=red>" + numberOfWrongAudienceGuesses + "</color> Wrong Audience Members";
                gameState.totalAudienceWrongGuesses += numberOfWrongAudienceGuesses;

            }

            //play each animation
            foreach (KeyValuePair<Player, MeexarpAction> playerAnimation in playerAnimations)
            {
                playerAnimation.Key.playAnimation(playerAnimation.Value);
            }

            yield return new WaitForSeconds(waitForReadSeconds);

            //yield return new WaitForSeconds(zoomInTime);
            string audienceGuessesString = numberOfCorrectAudienceGuesses + numberOfWrongAudienceGuesses == 0 ? "" : "\n\n<color=green>" + numberOfCorrectAudienceGuesses + " </color>/<color=red>" + numberOfWrongAudienceGuesses + "</color> Audience";

            myTitle.text = "<b>" + tileTitle + "</b>";
            myQandAs.text = wrongVotesSb.ToString() + correctVotesSB.ToString() + audienceGuessesString;

            //myTitle.fontSizeMax = 25;
            //myQandAs.fontSizeMax= 20;

            //reveal it on phones
            BroadcastToAllPhones(new JsonAction("sendRevealNextPersonalRoundResult", new string[] { targetPlayerName }));

            //close results side panel
            rightAndWrongPanelAnimator.SetBool("Open", false);
            //titleAndGridContainerAnimator.SetBool("Open", false);

            //wait for the shrink
            yield return new WaitForSeconds(zoomInTime);

            //1 second buffer
            yield return new WaitForSeconds(1);
            Destroy(cz);
        }
        Image[] playerIcons = resultsPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "AnswerQuestionsPane");

        //reset the position of the other panels
        for (int i = 0; i < 6 - countAtBeginingOfRound; i++)
        {
            playerIconsList[i].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }
        resultsPanel.GetComponentsInChildren<Image>()[0].GetComponentInChildren<GridLayoutGroup>().enabled = true;
        autoResizeGrid.enabled = true;

        GameObject.FindWithTag("Banner").GetComponentInChildren<TextMeshProUGUI>().text = "Waiting for <color=#325EFB>" +
                gameState.GetPlayerByPlayerNumber(0).nickname + "</color> to continue";

        Animator nextActionBannerAnimator = resultsPanel.GetComponentsInChildren<Animator>()[2];
        nextActionBannerAnimator.SetBool("isOpen", true);

        if (gameState.GetCurrentRoundNumber() == gameState.GetNumRoundsPerGame() - 1)
        {
            SendMessageToVip(new JsonAction("sendAdvanceToResultsScreen", new string[] { " " }));
//            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendAdvanceToResultsScreen", new string[] { " " })));
            gameState.phoneViewGameState = PhoneViewGameState.SendAdvanceToResultsScreen;
        }
        else
        {
            SendMessageToVip(new JsonAction("sendNextRoundScreen", new string[] { " " }));
            //AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendNextRoundScreen", new string[] { " " })));
            gameState.phoneViewGameState = PhoneViewGameState.SendNextRoundScreen;
        }
        
    }

    private void moveRoundCounter(bool moveUp)
    {
        RectTransform roundCounterTransform = roundCounter.GetComponent<RectTransform>();
        Vector2 origAnchorMin = roundCounterTransform.anchorMin;
        Vector2 origAnchorMax = roundCounterTransform.anchorMax;
        if (moveUp)
        {
            roundCounterTransform.anchorMin = new Vector2(origAnchorMin.x, .75f);
            roundCounterTransform.anchorMax = new Vector2(origAnchorMax.x, 1f);
        } else
        {
            roundCounterTransform.anchorMin = new Vector2(origAnchorMin.x, .65f);
            roundCounterTransform.anchorMax = new Vector2(origAnchorMax.x, .9f);
        }
    }

    public System.Collections.IEnumerator SendEndScreen2()
    {
        gameState.phoneViewGameState = PhoneViewGameState.SendEndScreen;
        gameState.tvViewGameState = TvViewGameState.EndGameScreen;
        roundCounter.SetActive(false);
        deactivateResultsPanel();
        endScreenPanel.SetActive(true);
        foreach (Player p in gameState.players.Values)
        {
            string currentBestFriend = CalculateBestFriend(p);

            SendMessageToPhone(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, (p.numWrong + p.points).ToString(), currentBestFriend }));
        }

        foreach (Player p in gameState.audienceMembers.Values)
        {
            string currentBestFriend = CalculateBestFriend(p);

            SendMessageToPhone(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, (p.numWrong + p.points).ToString(), currentBestFriend }));
        }

        int offset = 0;
        float correctPercent = ((float)gameState.totalCorrectGuesses) / ((float)(gameState.totalCorrectGuesses + gameState.totalWrongGuesses)) * 100.0f;
        string[] friendshipStatusArray = { "Archnemeses", "Enemies", "Strangers", "Acquaintances", "Just \"Ok\" Friends", "Friends", "Best Friends" };
        string[] resultsStatuses = { "The experiment was successful! The power of friendship is truly amazing!" ,
        "The experiment went ok. We have not proven the power of friendship, but have gathered valuable data!" };


        string friendshipStatus = CaluclateFriendshipStatus(correctPercent, friendshipStatusArray);
        bool didWeWin = correctPercent >= 60.0f;
        string resultStatus = didWeWin ? resultsStatuses[0] : resultsStatuses[1];
        if (didWeWin)
        {
            ChangeBackground(4);
        } else
        {
            ChangeBackground(5);
        }

        bool shouldShowAudienceScorecard = false;
        if (audienceScoreCard != null)
        {
            if (gameState.audienceMembers.Count == 0)
            {
                audienceScoreCard.SetActive(false);
            }
            else
            {
                shouldShowAudienceScorecard = true;
                audienceScoreCard.SetActive(true);
                GameObject.FindWithTag("PercentCorrectAndStatusContainer").gameObject.GetComponent<RectTransform>().anchorMax
                    = new Vector2(.7f, .95f);
            }
        }

        TextMeshProUGUI[] textMeshPros = endScreenPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

        //set percentCorrectText
        //empty strings so we can apply animation
        textMeshPros[offset].text = "";
        textMeshPros[offset + 1].text = "";
        textMeshPros[offset + 2].text = "";
        textMeshPros[offset + 3].text = "";
        textMeshPros[offset + 4].text = "";
        textMeshPros[offset + 6].text = "";
        textMeshPros[offset + 7].text = "";

        float waitForContext = 2.0f;

        yield return new WaitForSeconds(waitForContext);
        resultsBeepAudioSource.Play();
        yield return StartCoroutine(textMeshPros[offset].gameObject
            .AddComponent<charByCharText>().WriteText("Collectively, your team identified"));
        resultsBeepAudioSource.Pause();

        yield return new WaitForSeconds(waitForContext);
        stampAudioSource.Play();
        textMeshPros[offset+1].text = "<b><size=90> " + correctPercent.ToString("n2") + "%</size> </b>";
        yield return new WaitForSeconds(waitForContext);

        resultsBeepAudioSource.Play();
        yield return StartCoroutine(textMeshPros[offset+2].gameObject
            .AddComponent<charByCharText>().WriteText("of your teammates correctly!"));
        resultsBeepAudioSource.Pause();

        //set friendship status
        yield return new WaitForSeconds(waitForContext);
        resultsBeepAudioSource.Play();
        yield return textMeshPros[offset + 3].gameObject
            .AddComponent<charByCharText>().WriteText("You have achieved the status of");
        resultsBeepAudioSource.Pause();

        yield return new WaitForSeconds(waitForContext);
        stampAudioSource.Play();
        textMeshPros[offset + 4].text = "<b><size=60> " + friendshipStatus + " </size></b> ";
        yield return new WaitForSeconds(waitForContext);
        
        //set result text
        textMeshPros[offset + 6].text = resultStatus;
        yield return new WaitForSeconds(waitForContext);

        textMeshPros[offset + 7].text = "Look at your phone for personal results   <sprite=0>";
        yield return new WaitForSeconds(waitForContext);

        if(shouldShowAudienceScorecard)
        {
            //set audience scores
            //first sort the audience scores
            List<Player> sortedAudienceScores = new List<Player>(gameState.audienceMembers.Values);
            sortedAudienceScores.Sort((pair1, pair2) => pair2.points.CompareTo(pair1.points));
            //display the top 5 audience members
            StringBuilder audienceScoresSb = new StringBuilder(100);
            int numAudienceScoresToDisplay = System.Math.Min(5, sortedAudienceScores.Count);
            audienceScoresSb.Append("Audience Scores");
            if (sortedAudienceScores.Count > 5)
            {
                audienceScoresSb.Append("\n<size=12>(top 5)</size>");
            }
            for (int i = 0; i < numAudienceScoresToDisplay; i++)
            {
                audienceScoresSb.Append("\n<size=20>" + sortedAudienceScores[i].nickname + "    " + sortedAudienceScores[i].points + "</size>");
            }

            audienceScoreCard.GetComponentInChildren<TextMeshProUGUI>().text = audienceScoresSb.ToString();
        }
    }

    private static string CaluclateFriendshipStatus(float correctPercent, string[] friendshipStatusArray)
    {
        string friendshipStatus;
        switch (correctPercent)
        {
            case float f when (f >= 90):
                friendshipStatus = friendshipStatusArray[6];
                break;
            case float f when (f >= 70):
                friendshipStatus = friendshipStatusArray[5];
                break;
            case float f when (f >= 60):
                friendshipStatus = friendshipStatusArray[4];
                break;
            case float f when (f >= 40):
                friendshipStatus = friendshipStatusArray[3];
                break;
            case float f when (f >= 30):
                friendshipStatus = friendshipStatusArray[2];
                break;
            case float f when (f >= 20):
                friendshipStatus = friendshipStatusArray[1];
                break;
            default:
                friendshipStatus = friendshipStatusArray[0];
                break;
        }

        return friendshipStatus;
    }

    public void SendEndScreen()
    {

        //display points
        int playerCounter = 0;
        foreach (Player p in gameState.players.Values)
        {
            StringBuilder pointsSB = new StringBuilder(100);

            string[] pointSuffixStrings = new string[] { "gazillion", "bajillion", "trazillion", "million", "foshizillion", "mozillion" };

            pointSuffixStrings.Shuffle<string>();

            pointsSB.Append(p.nickname);
            pointsSB.Append(" has ");
            pointsSB.Append(p.points);
            pointsSB.Append(" ");
            pointsSB.Append(pointSuffixStrings[p.playerNumber]);
            pointsSB.Append(" points");
            pointsSB.Append("\n");
            int tilesOffset = 2;
            endScreenPanel.GetComponentsInChildren<TextMeshProUGUI>()[tilesOffset + playerCounter].text = pointsSB.ToString();
            playerCounter++;
            string currentBestFriend = CalculateBestFriend(p);

            SendMessageToPhone(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, currentBestFriend }));

        }
        StringBuilder audiencePointsSB = new StringBuilder(100);

        foreach (Player p in gameState.audienceMembers.Values)
        {

            audiencePointsSB.Append(p.nickname);
            audiencePointsSB.Append(" has ");
            audiencePointsSB.Append(p.points);
            audiencePointsSB.Append(" ");
            audiencePointsSB.Append(" points");
            audiencePointsSB.Append("\n");
            endScreenPanel.GetComponentsInChildren<TextMeshProUGUI>()[0].text = audiencePointsSB.ToString();
            string currentBestFriend = CalculateBestFriend(p);

            SendMessageToPhone(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, currentBestFriend }));

        }
        
        gameState.phoneViewGameState = PhoneViewGameState.SendEndScreen;
    }

    private string CalculateBestFriend(Player p)
    {
        int currentBestFriendPoints = 0;
        List<string> currentBestFriends = new List<string>();
        currentBestFriends.Add("No one!");
        //calculate best friends
        foreach (Player otherPlayer in gameState.players.Values)
        {
            Dictionary<string, int> myBfp = p.bestFriendPoints;
            int myPoints = myBfp.ContainsKey(otherPlayer.nickname) ? myBfp[otherPlayer.nickname] : 0;
            Dictionary<string, int> theirBfp = otherPlayer.bestFriendPoints;
            int theirPoints = theirBfp.ContainsKey(p.nickname) ? theirBfp[p.nickname] : 0;
            if(myPoints + theirPoints > 0)
            {
                if (myPoints + theirPoints > currentBestFriendPoints)
                {
                    currentBestFriends.Clear();
                    currentBestFriends.Add(otherPlayer.nickname);
    //                currentBestFriend = otherPlayer.nickname;
                    currentBestFriendPoints = myPoints + theirPoints;
                }
                else if (myPoints + theirPoints == currentBestFriendPoints)
                {
                    currentBestFriends.Add(otherPlayer.nickname);
                }
            }
        }

        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < currentBestFriends.Count; i++)
        {
            if(i != 0)
            {
                sb.Append("~~split~~");
            }
            sb.Append(currentBestFriends[i]);
        }
        return sb.ToString();
    }

    private int anonymousNameCounter = 0;
    private int anonymousPlayerNumberCount = 0;
    private int[] anonymousPlayerNumbers = new int[] {1, 2, 3, 4, 5, 6 };
    public string GenerateAnonymousPlayerName()
    {   
        if(!options["anonymousNames"])
        {
            return "Personality " + anonymousPlayerNumbers[anonymousPlayerNumberCount++]; 
        }

        return anonymousNames[(anonymousNameCounter++)%anonymousNames.Length];
    }

    public bool HasEveryoneSubmittedAnswers()
    {
        return gameState.GetCurrentRound().answers.Count == gameState.GetNumberOfPlayers();
    }

    public bool HasEveryoneVoted()
    {
        return gameState.GetCurrentRound().votes.Count == gameState.GetNumberOfPlayers();
    }

    private string GetNextQuestion()
    {
        QuestionCategory qc = selectedCategory == -2 ? seasonalQuestionCategory : questionCategories[selectedCategory % questionCategories.Count];
        return qc.questions[currentQuestionIndex++ % qc.questions.Length];
    }
    private string GetRandomQuestion()
    {
        QuestionCategory qc;
        do
        {
            qc = questionCategories[Random.Range(0, questionCategories.Count)];
        } while ((!options["nsfwQuestions"] && qc.isNsfw));
        return qc.questions[Random.Range(0, qc.questions.Length)];
    }

    public int getNumPlayers()
    {
        return gameState.GetNumberOfPlayers();
    }

    private void StartAllLevels(AudioSource[] audioSources)
    {
        audioSources[0].volume = onVolume;
        foreach (AudioSource a in audioSources)
        {
            a.Play();
        }
    }

    private void SetVolumeForLevel(AudioSource[] audioSources, int numLevelToPlay)
    {
        currentAudioSources = audioSources;
        currentNumLevelToPlay = numLevelToPlay;
        for(int i = 0; i < audioSources.Length; i++)
        {
            if(i == numLevelToPlay)
            {
                audioSources[i].volume = onVolume;
            } else
            {
                audioSources[i].volume = offVolume;
            }
        }
    }

    private void StopAllLevels(AudioSource[] audioSources)
    {
        foreach (AudioSource a in audioSources)
        {
            a.volume = offVolume;
            a.Stop();
        }
    }

    private void ChangeBackground(int numBackground)
    {
        Image[] backgroundImages = backgrounds.GetComponentsInChildren<Image>(true);

        for (int i = 0; i < backgroundImages.Length; i++){
            if(i == numBackground)
            {
                backgroundImages[i].gameObject.SetActive(true);
            } else
            {
                backgroundImages[i].gameObject.SetActive(false);
            }
        }
    }
    private void BroadcastToAllPhones(JsonAction jsonAction)
    {
        if(isAirconsole)
        {
            AirConsole.instance.Broadcast(jsonAction);
        }
        else
        {
            
            JObject msg = new JObject();
            msg.Add("action", "broadcast");
            msg.Add("data", JToken.FromObject(jsonAction));
            socket.SendWebSocketMessage(msg.ToString());
            
            socket.SendWebSocketMessage(JToken.FromObject(msg).ToString());
        }
    }
    private void SendMessageToPhone(int from, JsonAction jsonAction)
    {
        if (isAirconsole)
        {
            AirConsole.instance.Message(from, jsonAction);
        } else
        {
            
            JObject msg = new JObject();
            msg.Add("action", "message");
            msg.Add("from", "" + from);
            msg.Add("data", JToken.FromObject(jsonAction));
            
            socket.SendWebSocketMessage(JToken.FromObject(msg).ToString());
        }
    }

    public int getCurrentRoundNumberOfAnswers()
    {
        return gameState.GetCurrentRound().answers.Count;
    }
}

public static class IListExtensions
{
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts)
    {
        Shuffle(ts, ts.Count);
    }

    public static void Shuffle<T>(this IList<T> ts, int numToShuffle)
    {
        var count = numToShuffle;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}

[System.Serializable]
public class JsonAction
{
    public string action;
    //TODO data should really be a json payload
    public string[] data;

    public JsonAction(string a, string[] d)
    {
        action = a;
        data = d;
    }
}

//copied from https://stackoverflow.com/questions/36239705/serialize-and-deserialize-json-and-json-array-in-unity/36244111#36244111
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

class Player
{
    public string nickname { get; set; }
    public int deviceId { get; set; }
    public int playerNumber { get; set; }
    private int avatarId { get; set; }
    public int points { get; set; }
    public int numWrong { get; set; }
    public Animator myAnimator { get; set; }
    public Dictionary<string, int> bestFriendPoints { get; set; }
    public int alienNumber { get; set; }
    public bool isReady { get; set; }
    public System.DateTime[] debounceLog { get; set; }
    private int debounceMaxInputsPerSec = 4;
    private int currentDebounceIndex = 0;
    public bool isDebounced { get; set; }

public Player(string n, int d, int pn, int a, Animator an, int selectedAlien)
    {
        nickname = n;
        deviceId = d;
        playerNumber = pn;
        avatarId = a;
        points = 0;
        numWrong = 0;
        myAnimator = an;
        bestFriendPoints = new Dictionary<string, int>();
        alienNumber = selectedAlien; //todo i dont think i need this
        isReady = false;
        isDebounced = false;
        debounceLog = new System.DateTime[debounceMaxInputsPerSec];
    }

    public Player(string n, int d, int pn, int a, int p, int nw, Animator an, Dictionary<string, int> bfp, int selectedAlien)
    {
        nickname = n;
        deviceId = d;
        playerNumber = pn;
        avatarId = a;
        points = p;
        numWrong = nw;
        myAnimator = an;
        bestFriendPoints = bfp;
        alienNumber = selectedAlien;
        isReady = false;
        isDebounced = false;
        debounceLog = new System.DateTime[debounceMaxInputsPerSec];
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("", 100);
        sb.Append("Nickname: " + nickname + "\n");
        sb.Append("deviceId: " + deviceId + "\n");
        sb.Append("playerNumber: " + playerNumber + "\n");
        sb.Append("avatarId: " + avatarId + "\n");
        sb.Append("points: " + points + "\n");
        return sb.ToString();
    }

    public void playAnimation(MeexarpAction meexarpActions)
    {
        if (meexarpActions == MeexarpAction.Cheer)
        {
            myAnimator.SetBool("isCheer", true);
            myAnimator.SetBool("isCheer", false);
        }
        else if (meexarpActions == MeexarpAction.Sad)
        {
            myAnimator.SetBool("isSad", true);
            myAnimator.SetBool("isSad", false);
        }
    }

    public void IncrementBestFriendCounter(string playerName)
    {
        if(bestFriendPoints.ContainsKey(playerName))
        {
            bestFriendPoints[playerName]++;
        } else
        {
            bestFriendPoints.Add(playerName, 1);
        }
    }

    public string LogDebounce(AudioClip debouncedBlip, AudioSource blipAudioSource)
    {
        if(isDebounced)
        {
            return "DISABLED";
        }
        if(debounceLog[currentDebounceIndex] == null)
        {
            debounceLog[currentDebounceIndex++] = System.DateTime.Now;
            return "ENABLED";
        }

        System.DateTime oldestInput = debounceLog[currentDebounceIndex % debounceMaxInputsPerSec];
        System.DateTime now = System.DateTime.Now;
        if (now.Subtract(oldestInput).TotalSeconds > 1)
        {
            debounceLog[currentDebounceIndex % debounceMaxInputsPerSec] = now;
            currentDebounceIndex = ++currentDebounceIndex % debounceMaxInputsPerSec;
            return "ENABLED";
        } else
        {
            blipAudioSource.PlayOneShot(debouncedBlip, Random.Range(.5f, 1f));
            isDebounced = true;
            return "DEBOUNCED";
        }
    }
}

[System.Serializable]
public class Answers
{
    public string[] text { get; set; }
    public int playerNumber { get; }
    public string anonymousPlayerName { get; }

    public Answers(string[] t, int pn, string apn)
    {
        text = t;
        playerNumber = pn;
        anonymousPlayerName = apn;
    }

    public string displayVotePanel()
    {
        StringBuilder sb = new StringBuilder("", 100);
        sb.Append(anonymousPlayerName);
        sb.Append("\n");
        foreach (string answer in text)
        {
            sb.Append(answer);
            sb.Append("\n");
        }
        return sb.ToString();

    }
    
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("", 100);
        sb.Append("Player Number: " + playerNumber + "\n");
        foreach (string answer in text)
        { 
            sb.Append("\t" + answer);
            sb.Append("\n");
        }
        sb.Append("Anonymous Player Name: " + anonymousPlayerName);
        return sb.ToString();
    }
}


public class QuestionCategory
{
    public string[] questions { get; set; }
    public string categoryName { get; set; }
    public bool isNsfw { get; set; }

    public QuestionCategory(string[] qs, string cName, bool nsfw)
    {
        questions = qs;
        categoryName = cName;
        isNsfw = nsfw;
    }
}

class Round
{
    public List<string> questions { get; set; }
    public List<Answers> answers { get; set; }
    
    //<deviceId, <anonymousName, playerName>>
    public Dictionary<int, Dictionary<string, string>> votes { get; set; }
    public Dictionary<int, Dictionary<string, string>> audienceVotes { get; set; }

    public Round()
    {
        questions = new List<string>();
        answers = new List<Answers>();
        votes = new Dictionary<int, Dictionary<string, string>>();
        audienceVotes = new Dictionary<int, Dictionary<string, string>>();
    }

    public string PrintQuestions()
    {
        StringBuilder sb = new StringBuilder("", 100);
        for (int i = 0; i < questions.Count; i++)
        {
            string question = questions[i];
            sb.Append(question);
            if(i <questions.Count - 1)
            sb.Append("<size=10>\n\n</size>");
        }
        return sb.ToString();

    }

    public Answers getAnswerByAnonymousName(string anonName)
    {
        foreach(Answers a in answers)
        {
            if(a.anonymousPlayerName == anonName)
            {
                return a;
            }
        }
        return null;
    }
    /*
        public Round(List<string> q, List<Answers> a)
        {
            questions = q;
            answers = a;
        }
        */
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder("", 100);
        sb.Append("Questions \n");
        foreach (string question in questions)
        {
            sb.Append("\t" + question);
            sb.Append("\n");
        }

        sb.Append("Answers \n");
        foreach (Answers answer in answers)
        {
            sb.Append("\t" + answer.ToString());
            sb.Append("\n");
        }
        return sb.ToString();
    }

    public bool hasPlayerSubmittedAnswer(int playerNumber)
    {
        foreach(Answers a in answers)
        {
            if(a.playerNumber == playerNumber)
            {
                return true;
            }
        }
        return false;
    }
}

//enum State { WELCOME, GIVE_QUESTION, WOULD_YOU_RATHER, };

class GameState
{
    //Dictionary<deviceId, Player>
    public Dictionary<int, Player> players { get; set; }
    //alienNumber, [from, playerNumber]. 
    //If either from or playernumber is defined the alien is taken
    public Dictionary<int, int[]> alienSelections { get; set; }
    public Dictionary<int, Player> audienceMembers { get; set; }
    public List<Round> rounds { get; set; }
    public PhoneViewGameState phoneViewGameState;
    public TvViewGameState tvViewGameState;
    public List<int> questionWriters { get; set; }
    public int totalCorrectGuesses { get; set; }
    public int totalWrongGuesses { get; set; }
    public int totalAudienceCorrectGuesses { get; set; }
    public int totalAudienceWrongGuesses { get; set; }

    public GameState()
    {
        players = new Dictionary<int, Player>();
        alienSelections = new Dictionary<int, int[]>();
        audienceMembers = new Dictionary<int, Player>();
        rounds = new List<Round>();
        tvViewGameState = TvViewGameState.WelcomeScreen;
        phoneViewGameState = PhoneViewGameState.SendStartGameScreen;
        ResetGuesses();
    }

    public void ResetGuesses()
    {
        totalWrongGuesses = 0;
        totalCorrectGuesses = 0;
        totalAudienceWrongGuesses = 0;
        totalAudienceCorrectGuesses = 0;
        foreach(Player p in players.Values)
        {
            p.numWrong = 0;
            p.points = 0;
            p.bestFriendPoints.Clear();
        }
        foreach(Player p in audienceMembers.Values)
        {
            p.numWrong = 0;
            p.points = 0;
            p.bestFriendPoints.Clear();
        }
    }

    public int GetNumRoundsPerGame()
    {
        return this.questionWriters.Count;
    }

    public Player GetPlayerByPlayerNumber(int playerNumber)
    {
        foreach (Player p in players.Values)
        {
            if (playerNumber == p.playerNumber)
            {
                return p;
            }
        }
        return null;
    }

    public Player GetAudienceByPlayerNumber(int playerNumber)
    {
        foreach (Player p in audienceMembers.Values)
        {
            if (playerNumber == p.playerNumber)
            {
                return p;
            }
        }
        return null;
    }

    public KeyValuePair<int, Player> GetPlayerByName(string playerName)
    {
        foreach (KeyValuePair<int, Player> p in players)
        {
            if (playerName == p.Value.nickname)
            {
                return p;
            }
        }
        return default;
    }

    public KeyValuePair<int, Player> GetAudienceByName(string playerName)
    {
        foreach (KeyValuePair<int, Player> p in audienceMembers)
        {
            if (playerName == p.Value.nickname)
            {
                return p;
            }
        }
        return default;
    }

    public List<string> GetPlayerNamesInNumberOrder()
    {
        List<string> p = new List<string>();
        for(int i = 0; i < players.Count; i++)
        {
            p.Add(GetPlayerByPlayerNumber(i).nickname);
        }
        return p;
    }

    public List<string> GetAlienNumberInNumberOrder()
    {
        List<string> p = new List<string>();
        for (int i = 0; i < players.Count; i++)
        {
            p.Add(GetPlayerByPlayerNumber(i).alienNumber + "");
        }
        return p;
    }

    public void ResetGameState()
    {
        rounds = new List<Round>();
        ResetGuesses();
    }

    public int GetNumberOfPlayers()
    {
        return players.Count;
    }
    public Player GetCurrentWritingPlayer()
    {
        return GetPlayerByPlayerNumber(this.questionWriters[GetCurrentRoundNumber()]);
    }

    public int GetNumberOfAudience()
    {
        return audienceMembers.Count;
    }

    public Round GetCurrentRound()
    {
        return rounds[GetCurrentRoundNumber()];
    }

    public int GetCurrentRoundNumber()
    {
        return rounds.Count - 1;
    }

    public List<string> whoIsNotReady()
    {
        List<string> returnable = new List<string>();
        foreach (Player p in players.Values) {
            if(!p.isReady)
            {
                returnable.Add(p.nickname);
            }
        }
        return returnable;
    }

    public override string ToString()
    {

        StringBuilder sb = new StringBuilder("", 100);
        sb.Append("Players \n");
        foreach (Player player in players.Values)
        {
            sb.Append("\t" + player.ToString());
            sb.Append("\n");
        }

        sb.Append("Rounds \n");
        foreach (Round r in rounds)
        {
            sb.Append("\t" + r.ToString());
            sb.Append("\n");
        }
        sb.Append("Round Number: " + GetCurrentRoundNumber());
        return sb.ToString();
    }

    public void updateRoundCounter(GameObject roundCounter)
    {
        roundCounter.GetComponentsInChildren<TextMeshProUGUI>()[1].text = (GetCurrentRoundNumber() + 1) + 
            "<size=15> of " + this.GetNumRoundsPerGame() + "</size>";
        roundCounter.SetActive(true);
    }
}

enum PhoneViewGameState
{
    SendQuestions = 0,
    SendWaitScreen = 1,
    SendSelectCategory = 11,
    SendRetrieveQuestions = 2,
    SendWouldYouRather = 3,
    SendVoting = 4,
    SendPersonalRoundResultsScreen = 44,
    SendNextRoundScreen = 5,
    SendAdvanceToResultsScreen = 6,
    SendEndScreen = 7,
    SendStartGameScreen = 8,
    SendSelectRoundNumberScreen = 9,
    SendSkipInstructionsScreen = 10
}

enum TvViewGameState
{
    WelcomeScreen = 0,
    //SelectRoundCountScreen = 1,
    SubmitQuestionsScreen = 2,
    AnswerQuestionsScreen = 4,
    VotingScreen = 5,
    RoundResultsScreen = 6,
    EndGameScreen = 7
}

enum MeexarpAction
{
    Sad = 0,
    Cheer= 1
}

static class Resources
{

    public static string GetAnonymousNames()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("anonymousNames.txt"));
    }

    public static string GetQuestions()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("questions.txt"));
    }

    public static string GetNsfwQuestions()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("nsfwQuestions.txt"));
    }

    public static string GetWouldYouRathers()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("wouldYouRathers.txt"));
    }
    public static string GetHolidayWouldYouRathers()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("winterWouldYouRathers.txt"));
    }
    public static string GetHolidayQuestions()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("winterQuestions.txt"));
    }

    public static string GetFriendshipTips()
    {
        return System.IO.File.ReadAllText(prependTextResourceFilepath("friendshipTips.txt"));
    }

    private static string prependTextResourceFilepath(string filename)
    {
        return System.IO.Path.Combine(Application.streamingAssetsPath + "/", filename);
    }
}