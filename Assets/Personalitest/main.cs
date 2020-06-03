using System.Collections.Generic;
using UnityEngine;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Video;

public class main : MonoBehaviour
{
    public Text gameStateText;
    public Text welcomeInstructionsText;
    private static List<QuestionCategory> questionCategories;
    private static string[] anonymousNames;
    //(question, left answer, right answer)
    private static List<string[]> wouldYouRathers;
    private GameState gameState;
    public List<GameObject> welcomePanels;
    public GameObject splashScreenPanel;
    public GameObject welcomeScreenPanel;
    public GameObject selectRoundNumberPanel;
    public GameObject wouldYouRatherPanel;
    public GameObject answerQuestionsPanel;
    public GameObject votingPanel;
    public GameObject resultsPanel;
    public GameObject endScreenPanel;
    public GameObject errorPanel;
    public Canvas canvas;
//    public AudioSource introAudioSource;
    public AudioSource mainLoopAudioSource;
    public AudioSource blipAudioSource;
    public AudioSource[] welcomeScreenAudioSources;
    private float onVolume = .4f;
    private float offVolume = 0f;
    public RawImage instructionVideo;
    public RawImage introInstructionVideo;
    public VideoPlayer introVp;
    public VideoPlayer vp;
    //should make a different sound per person
    public AudioClip[] blips;
    public Animator[] animators;

    private int currentCategoryIndex;
    private int selectedCategory;
    private List<int> sentCategoryIndices;
    private int currentQuestionIndex;
    private int currentWouldYouRatherIndex;
    private string gameCode;
    private static int VIP_PLAYER_NUMBER = 0;
    private static int AUDIENCE_THRESHOLD = 6;
    private Dictionary<int, int> audienceWouldYouRathers;
    private bool writeMyOwnQuestions = false;
    private Dictionary<string, bool> options = new Dictionary<string, bool>();
    
    

    // Start is called before the first frame update
    void Start()
    {
        StartAllLevels(welcomeScreenAudioSources);
        ExceptionHandling.SetupExceptionHandling(errorPanel);
        InitializeOptions();
        var newLinesRegex = new Regex(@"\r\n|\n|\r", RegexOptions.Singleline);
        string rawQuestions;
        string rawNsfwQuestions;
        string rawAnonymousNames;
        string rawWouldYouRathers;
        //read the resources
        //if playing on AirConsole
        if (TextAssetsContainer.isWebGl)
        {
            rawQuestions = TextAssetsContainer.rawQuestionsText;
            rawNsfwQuestions = TextAssetsContainer.rawNsfwQuestionsText;
            rawAnonymousNames = TextAssetsContainer.rawAnonymousNamesText;
            rawWouldYouRathers = TextAssetsContainer.rawWouldYouRatherText;
        }
        //if playing locally
        else
        {

            rawQuestions = Resources.GetQuestions();
            rawNsfwQuestions = Resources.GetNsfwQuestions();
            rawAnonymousNames = Resources.GetAnonymousNames();
            rawWouldYouRathers = Resources.GetWouldYouRathers();
        }
        questionCategories = new List<QuestionCategory>();
        ParseQuestions(rawQuestions, rawNsfwQuestions, newLinesRegex);
        anonymousNames = newLinesRegex.Split(rawAnonymousNames);
        string[] wouldYouRathersLines = newLinesRegex.Split(rawWouldYouRathers);
        List<string[]> tempWouldYouRathers = new List<string[]>();
        foreach (string s in wouldYouRathersLines)
        {
            tempWouldYouRathers.Add(s.Split('|'));
        }
        //randomize the order of the resources
        questionCategories.Shuffle();
        anonymousNames.Shuffle();
        tempWouldYouRathers.Shuffle();
        wouldYouRathers = tempWouldYouRathers;

        blips.Shuffle();

        AirConsole.instance.onReady += OnReady;
        AirConsole.instance.onMessage += OnMessage;
        AirConsole.instance.onConnect += OnConnect;
        AirConsole.instance.onDisconnect += OnDisconnect;
        gameState = new GameState();
        currentQuestionIndex = 0;
        currentWouldYouRatherIndex = 0;
        audienceWouldYouRathers = new Dictionary<int, int>();
        StartCoroutine(waitThreeSecondsThenDisplayWelcomeScreen());
    }

    private IEnumerator<WaitForSeconds> waitThreeSecondsThenDisplayWelcomeScreen()
    {
        yield return new WaitForSeconds(3);
        SetVolumeForLevel(welcomeScreenAudioSources, 1);
        FadeSplashScreen fadeSplashScreenScript = splashScreenPanel.AddComponent<FadeSplashScreen>();
        fadeSplashScreenScript.Setup();
    }

    private void InitializeOptions()
    {
        options.Add("nsfwQuestions", false);
        options.Add("anonymousNames", false);
    }

    void ParseQuestions(string rawQuestions, string rawNsfwQuestions, Regex newLinesRegex)
    {
        string[] questionsLines = newLinesRegex.Split(rawQuestions);
        string[] nsfwQuestionsLines = newLinesRegex.Split(rawNsfwQuestions);
        addLinesToCategory(questionsLines, false);
        addLinesToCategory(nsfwQuestionsLines, true);
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

    void OnReady(string code)
    {
        gameCode = code;
        welcomeInstructionsText.text = "Navigate to airconsole.com and enter <size=39><b>" + code.Replace(" ", "") + "</b></size> to join!\nPlayers can reconnect by joining and using the same name";
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
        if ("sendWelcomeInfo".Equals(action))
        {
            string name = data["info"]["name"].ToString();
            KeyValuePair<int, Player> currentPlayer = gameState.GetPlayerByName(name);

            if (!currentPlayer.Equals(default(KeyValuePair<int, Player>)))
            {
                //prevent players from using the same name
                if (gameState.phoneViewGameState.Equals(PhoneViewGameState.SendStartGameScreen))
                {
                    AirConsole.instance.Message(from, new JsonAction("nameAlreadyTaken", new string[] { " " }));
                    return;
                }
                //reconnect case
                else
                {
                    gameState.players.Remove(currentPlayer.Key);
                    gameState.players.Add(from, new Player(name, from, currentPlayer.Value.playerNumber, 0, 
                        currentPlayer.Value.points, animators[currentPlayer.Value.playerNumber], 
                        currentPlayer.Value.bestFriendPoints, currentPlayer.Value.alienNumber));
                    SendCurrentScreenForReconnect(from, currentPlayer.Value.playerNumber);
                    int selectedAlien = data["info"]["selectedAlien"].ToObject<int>();
                    sendWelcomeScreenInfo(from, selectedAlien);
                }
            }
            //new player
            else if (gameState.GetNumberOfPlayers() < 6)
            {
                int selectedAlien = data["info"]["selectedAlien"].ToObject<int>();

                //remove the nbsp
                name = Regex.Replace(name, @"\u00a0", " ");
                int newPlayerNumber = gameState.GetNumberOfPlayers();
                Player p = new Player(name, from, newPlayerNumber, 0, animators[newPlayerNumber], selectedAlien);
                if(selectedAlien > -1)
                {
                    gameState.alienSelections.Add(selectedAlien, new[] { -1, p.playerNumber });
                } else
                {
                    for(int i = 0; i < 6; i++)
                    {
                        if(!gameState.alienSelections.ContainsKey(i))
                        {
                            selectedAlien = i;
                            gameState.alienSelections.Add(i, new[] { -1, p.playerNumber });
                        }
                    }
                }
                if (p.playerNumber == 0)
                {
                    SendIsVip(p);
                }

                welcomePanels[gameState.GetNumberOfPlayers()].GetComponentInChildren<Text>().text = p.nickname;
                gameState.players.Add(from, p);
                sendWelcomeScreenInfo(from, selectedAlien);
                SendCurrentScreenForReconnect(from, p.playerNumber);
            }
            // audience
            else {
                KeyValuePair<int, Player> audiencePlayer = gameState.GetAudienceByName(name);
                if (!audiencePlayer.Equals(default(KeyValuePair<int, Player>)))
                {
                    //prevent players from using the same name
                    if (gameState.phoneViewGameState.Equals(PhoneViewGameState.SendStartGameScreen))
                    {
                        AirConsole.instance.Message(from, new JsonAction("nameAlreadyTaken", new string[] { " " }));
                        return;
                    }
                    //reconnect case
                    else
                    {
                        gameState.audienceMembers.Remove(audiencePlayer.Key);
                        //todo hard code the audience animator to 7 or give them no animations
                        gameState.audienceMembers.Add(from, new Player(name, from, audiencePlayer.Value.playerNumber, 0, 
                            audiencePlayer.Value.points, audiencePlayer.Value.myAnimator, audiencePlayer.Value.bestFriendPoints, 
                            audiencePlayer.Value.alienNumber));
                        SendCurrentScreenForReconnect(from, audiencePlayer.Value.playerNumber);
                    }
                }
                //new Audience
                else
                {
                    int audienceNumber = 10 + gameState.GetNumberOfAudience();
                    //todo hard code the audience animator to 7 or give them no animations
                    Player p = new Player(name, from, audienceNumber, 0, animators[1], -1);
                    gameState.audienceMembers.Add(from, p);
                    welcomeScreenPanel.GetComponentsInChildren<Text>()[8].text = "Audience: " + gameState.audienceMembers.Count;
                    SendCurrentScreenForReconnect(from, p.playerNumber);
                    sendWelcomeScreenInfo(from, -1);
                }
            }
        }
        else if ("forceAdvance".Equals(action))
        {
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
                    SendQuestions();
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
                    StartCoroutine(CalculateVoting(2));
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
        }
        else if ("sendSelectAlien".Equals(action))
        {
            playSound = false;
            int selectedAlien = data["info"]["selectedAlien"].ToObject<int>();
            if (gameState.alienSelections.ContainsKey(selectedAlien))
            {
                AirConsole.instance.Message(from, JsonUtility.ToJson(
                    new JsonAction("sendAlienSelectionFailed", new[] { ""})));
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
            AirConsole.instance.Message(GetVipDeviceId(), JsonUtility.ToJson(
                new JsonAction("sendRetrieveOptions", new[] { options["nsfwQuestions"].ToString(), options["anonymousNames"].ToString() })));

        }
        else if ("sendSaveOptions".Equals(action))
        {
            options["nsfwQuestions"] = data["info"]["nsfwQuestions"].ToObject<bool>();
            options["anonymousNames"] = data["info"]["anonymousNames"].ToObject<bool>();
        }
        else if ("requestWelcomeScreenInfo".Equals(action))
        {
            sendWelcomeScreenInfo(-1, -1);
            playSound = false;
        }
        else if ("sendStartGame".Equals(action))
        {
            //gameState.numRoundsPerGame = data["info"]["roundCount"].ToObject<int>();
            //GameObject.Find("WelcomeScreenPanel").SetActive(false);

            //selectRoundNumberPanel.SetActive(true);
            //selectRoundNumberPanel.GetComponentsInChildren<Text>()[1].text =
            //    "With " + gameState.GetNumberOfPlayers() + " players it will take about  " + (3 + gameState.GetNumberOfPlayers()) + " minutes per round. Note: With fewer rounds, some players will not get to submit questions.";
            //AirConsole.instance.Broadcast(new JsonAction("selectRoundCountView", new[] { gameState.GetNumberOfPlayers() + "" }));
            SetVolumeForLevel(welcomeScreenAudioSources, 2);
            SendMessageToVip(new JsonAction("selectRoundCountView", new[] { gameState.GetNumberOfPlayers() + "" }));
            gameState.phoneViewGameState = PhoneViewGameState.SendSelectRoundNumberScreen;
            /*
            introAudioSource.Stop();
            mainLoopAudioSource.Play();
            StartCoroutine(ShowIntroInstrucitons(2));
            */
        }
        else if ("sendSetRoundCount".Equals(action))
        {
            gameState.tvViewGameState = TvViewGameState.SubmitQuestionsScreen;
            gameState.numRoundsPerGame = data["info"].ToObject<int>();
            ExitWelcomeScreen();
        }
        else if ("sendSubmitWouldYouRather".Equals(action))
        {
            List<string> wouldYouRather = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                Debug.Log("property: " + property);
                wouldYouRather.Add(property.Value.ToString());
            }
            wouldYouRathers.Insert(0, wouldYouRather.ToArray());
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
                float xOffset = canvas.GetComponent<RectTransform>().rect.width * 0.2f * canvas.scaleFactor;
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
            AirConsole.instance.Message(from, new JsonAction("sendAnotherQuestion", new string[] { elementId, nextQuestion }));
        }
        else if ("sendDecidedQuestions".Equals(action))
        {
            PrepareSendQuestions();

            List<string> myQuestions = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                myQuestions.Add(property.Value.ToString());
            }
            gameState.GetCurrentRound().questions = myQuestions;
            SendQuestions();
        }
        else if ("sendAnswers".Equals(action))
        {
            List<string> myAnswers = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                myAnswers.Add(property.Value.ToString());
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
            movePlayerIcon(playerIconsList[currentPlayer.playerNumber], canvas.GetComponent<RectTransform>().rect.width * 0.4f * canvas.scaleFactor);

            if (HasEveryoneSubmittedAnswers())
            {
                StartCoroutine(EndAnswerQuestionsPhase(2));
            }

        }
        else if ("sendSkipInstructions".Equals(action))
        {
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
                gameState.GetCurrentRound().audienceVotes.Add(gameState.audienceMembers[from].playerNumber, myVotes);
            }
            else
            {
                gameState.GetCurrentRound().votes.Add(gameState.players[from].playerNumber, myVotes);

                if (HasEveryoneVoted())
                {
                    StartCoroutine(CalculateVoting(2));
                }
            }
        }
        else if ("sendNextRound".Equals(action))
        {
            StartRound();
        }
        else if ("sendShowEndScreen".Equals(action))
        {
            SendEndScreen2();
        }
        else if ("sendPlayAgain".Equals(action))
        {
            gameState.ResetGameState();
            endScreenPanel.SetActive(false);
            StartRound();
        }
        if(gameState.players.ContainsKey(from))
        {
            if (playSound) {
                //play a sound to confirm the input
                blipAudioSource.PlayOneShot(blips[gameState.players[from].playerNumber], Random.Range(.5f, 1f));
            } 
        }
    }

    private static void BroadcastSelectedAliens(Dictionary<int, int[]> alienSelections)
    {
        string selectedAliens = "";
        foreach (int alienNumber in alienSelections.Keys)
        {
            selectedAliens += alienNumber;
        }
        AirConsole.instance.Broadcast(new JsonAction("sendAlienSelectionSuccess", new[] { selectedAliens }));
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

    private void PrepareSendQuestions()
    {
        //Stop the would you rathers
        CancelInvoke();

        wouldYouRatherPanel.SetActive(false);
        answerQuestionsPanel.SetActive(true);

        //display the status of each player's submission
        /*
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            Player currentPlayer = gameState.GetPlayerByPlayerNumber(i);
            int tilesOffset = 1;
            answerQuestionsPanel.GetComponentsInChildren<Text>()[tilesOffset + i].text = currentPlayer.nickname + "\n\n<color=red>Has Not Submitted</color>";

        }
        */

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
        }
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            //            playerTexts[playerTextOffset + i].text = gameState.GetPlayerByPlayerNumber(i).nickname;
            //          playerTexts[currentPlayerTextOffset + i].text = gameState.GetPlayerByPlayerNumber(i).nickname;
            playerIconsList[i].gameObject.GetComponentInChildren<Text>().text = gameState.GetPlayerByPlayerNumber(i).nickname;
        }
    }

    private void ExitWelcomeScreen()
    {
        StopAllLevels(welcomeScreenAudioSources);
        //introAudioSource.Stop();
        mainLoopAudioSource.Play();
        StartCoroutine(ShowIntroInstrucitons(2));
    }

    private void SetAudienceWouldYouRatherCounters(int leftAudience, int rightAudience)
    {
        Text[] wouldYouRatherTexts = wouldYouRatherPanel.GetComponentsInChildren<Text>(true);
        wouldYouRatherTexts[18].gameObject.SetActive(true);
        wouldYouRatherTexts[19].gameObject.SetActive(true);
        wouldYouRatherTexts[18].text = "Audience: " + leftAudience;
        wouldYouRatherTexts[19].text = "Audience: " + rightAudience;
    }

    private void sendWelcomeScreenInfo(int from, int alienNumber)
    {
        int currNumPlayers = gameState.GetNumberOfPlayers();
        if(currNumPlayers < 6)
        {
            AirConsole.instance.Broadcast(new JsonAction("sendWelcomeScreenInfo", new[] { "" + currNumPlayers }));
            BroadcastSelectedAliens(gameState.alienSelections);
        } else
        {
            AirConsole.instance.Broadcast(new JsonAction("sendWelcomeScreenInfo", new[] { "full" }));
        }
        int currNumAudience = gameState.GetNumberOfAudience();

        if (from > 0)
        {
            if (currNumAudience == 0)
            {
                Player myPlayer = gameState.players[from];
                AirConsole.instance.Message(from, new JsonAction("sendWelcomeScreenInfoDetails", new string[] { myPlayer.nickname, "" + myPlayer.playerNumber, "" + alienNumber }));

            } else 
            {
                Player myPlayer = gameState.audienceMembers[from];
                AirConsole.instance.Message(from, new JsonAction("sendWelcomeScreenInfoDetails", new string[] { myPlayer.nickname, "" + myPlayer.playerNumber, "" + -1 }));
            }
        }
    }

    private static void SendIsVip(Player currentPlayer)
    {
        AirConsole.instance.Message(currentPlayer.deviceId, new JsonAction("setIsVip", new[] { "" }));
    }

    private bool isAudienceMember(int from)
    {
        return gameState.audienceMembers.ContainsKey(from);
    }

    private int GetVipDeviceId()
    {
        return gameState.GetPlayerByPlayerNumber(0).deviceId;
    }

    private void SendMessageToPlayersAndSendWaitScreenToAudience(JsonAction jsonAction)
    {
        foreach (Player p in gameState.players.Values)
        {
            AirConsole.instance.Message(p.deviceId, jsonAction);
        }

        foreach (Player p in gameState.audienceMembers.Values)
        {
            AirConsole.instance.Message(p.deviceId, new JsonAction("sendWaitScreen", new string[] { " " }));
        }
    }

    private void SendMessageToVipAndSendWaitScreenToEveryoneElse(JsonAction jsonAction)
    {
        foreach(Player p in gameState.players.Values)
        {
            if(p.playerNumber == VIP_PLAYER_NUMBER)
            {
                AirConsole.instance.Message(p.deviceId, jsonAction);
            }
            else
            {
                AirConsole.instance.Message(p.deviceId, new JsonAction("sendWaitScreen", new string[] { " " }));
            }
        }

        foreach(Player p in gameState.audienceMembers.Values)
        {
            AirConsole.instance.Message(p.deviceId, new JsonAction("sendWaitScreen", new string[] { " " }));
        }
    }

    private void SendMessageIfVipElseSendWaitScreen(int from, int currentPlayerNumber, JsonAction jsonAction)
    {
        if (currentPlayerNumber == VIP_PLAYER_NUMBER)
        {
            AirConsole.instance.Message(from, jsonAction);
        }
        else
        {
            AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
        }
    }

    private void SendMessageToVip(JsonAction jsonAction)
    {
        foreach (Player p in gameState.players.Values)
        {
            if (p.playerNumber == VIP_PLAYER_NUMBER)
            {
                AirConsole.instance.Message(p.deviceId, jsonAction);
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

        if (isVip)
        {
            SendIsVip(gameState.GetPlayerByPlayerNumber(currentPlayerNumber));
        }
        switch (gameState.phoneViewGameState)
        {
            case PhoneViewGameState.SendStartGameScreen:
                AirConsole.instance.Message(from, new JsonAction("sendStartGameScreen", new string[] { " " }));

                break;
            case PhoneViewGameState.SendSelectRoundNumberScreen:
                SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber,
                    new JsonAction("selectRoundCountView", new string[] { gameState.GetNumberOfPlayers() + "" })
                );
                break;
            case PhoneViewGameState.SendSkipInstructionsScreen:
                SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber,
                    new JsonAction("sendSkipInstructions", new string[] { " " }));
                break;
            case PhoneViewGameState.SendWaitScreen:
                AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
                break;
            case PhoneViewGameState.SendWouldYouRather:
                if (currentPlayerNumber == gameState.GetCurrentRoundNumber())
                //if (currentPlayer.Value.playerNumber == gameState.GetCurrentRoundNumber())
                {
                    SendSelectCategory(from);
                }
                else
                {
                    AirConsole.instance.Message(from, new JsonAction("sendWouldYouRather", new string[] { "reconnecting" }));
                }
                break;
            case PhoneViewGameState.SendQuestions:
                SendQuestions(from);
                break;
            case PhoneViewGameState.SendVoting:
                if (gameState.GetCurrentRound().votes.ContainsKey(currentPlayerNumber) || gameState.GetCurrentRound().audienceVotes.ContainsKey(currentPlayerNumber))
                {
                    AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
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
                SendMessageIfVipElseSendWaitScreen(from, currentPlayerNumber, new JsonAction("sendEndScreen", new string[] { " " }));
                break;
            default:
                AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
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
            votesToUse = gameState.GetCurrentRound().audienceVotes[toSend.playerNumber];
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
        AirConsole.instance.Broadcast(new JsonAction("sendWaitScreen", new[] { "This is a funny quote" }));
        gameState.phoneViewGameState = PhoneViewGameState.SendWaitScreen;
    }

    //todo remove parameter
    private IEnumerator<WaitForSeconds> ShowIntroInstrucitons(float seconds)
    {
        //flash the instructions
        SendMessageToVipAndSendWaitScreenToEveryoneElse(new JsonAction("sendSkipInstructions", new string[] { " " }));
        //AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendSkipInstructions", new string[] { " " })));
        gameState.phoneViewGameState = PhoneViewGameState.SendSkipInstructionsScreen;

        Image[] images = votingPanel.GetComponentsInChildren<Image>();
        introVp.url = System.IO.Path.Combine(Application.streamingAssetsPath + "/", "knowyourfriendsintrotutorial.mp4");

        introVp.Prepare();
        yield return new WaitForSeconds(1);
        while (!introVp.isPrepared)
        {
            yield return new WaitForSeconds(1);
            break;
        }
        introInstructionVideo.texture = introVp.texture;
        introInstructionVideo.gameObject.SetActive(true);
        CameraZoom instructionsCz = introInstructionVideo.gameObject.AddComponent<CameraZoom>();
        instructionsCz.Setup(.5f, 64f, false, false, false, true);
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
        Destroy(instructionsCz);
        //AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendVoting", new string[] { " " })));
        selectRoundNumberPanel.SetActive(false);
        StartRound();
    }

    //startRound sends one person a SendRetrieveQuestions message and sends the others would you rathers until the questions are complete
    public void StartRound()
    {
        //reset the "Answer Set #" counter
        anonymousPlayerNumberCount = 0;
        anonymousPlayerNumbers.Shuffle(gameState.GetNumberOfPlayers());
        gameState.rounds.Add(new Round());

        //if starting from previous game
        resultsPanel.SetActive(false);
        wouldYouRatherPanel.SetActive(true);

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
        }
        if (gameState.audienceMembers.Count > 0)
        {
            playerIcons[playerIconOffset + 6].gameObject.SetActive(true);
        }
        //also set the current player's icon to inactive
        //playerIcons[playerIconOffset + gameState.GetCurrentRoundNumber()].gameObject.SetActive(false);
        playerIconsList[gameState.GetCurrentRoundNumber()].gameObject.SetActive(false);

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
        string playerName = gameState.GetPlayerByPlayerNumber(gameState.GetCurrentRoundNumber()).nickname;
        getPlayerIconTags(playerIcons, "Banner")[0].GetComponentInChildren<Text>().text = "It's " + playerName + "'s turn to write questions!";

        int playerTextOffset = 4;
        int currentPlayerTextOffset = 13;
        Text[] playerTexts = wouldYouRatherPanel.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            //            playerTexts[playerTextOffset + i].text = gameState.GetPlayerByPlayerNumber(i).nickname;
            //          playerTexts[currentPlayerTextOffset + i].text = gameState.GetPlayerByPlayerNumber(i).nickname;
            playerIconsList[i].gameObject.GetComponentInChildren<Text>().text = gameState.GetPlayerByPlayerNumber(i).nickname;
        }

        //controllers in the retrieve questions state will ignore would you rathers
        int currentPlayerTurnDeviceId = gameState.GetPlayerByPlayerNumber(gameState.GetCurrentRoundNumber()).deviceId;
        SendSelectCategory(currentPlayerTurnDeviceId);

        gameState.phoneViewGameState = PhoneViewGameState.SendWouldYouRather;
        gameState.tvViewGameState = TvViewGameState.SubmitQuestionsScreen;
        InvokeRepeating("SendWouldYouRather", 0f, 150f);
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

    private static List<Text> getTextTags(Text[] texts, string tagName)
    {
        List<Text> playerIconsList = new List<Text>();
        foreach (Text i in texts)
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
        KeyValuePair<string, int>[] categoriesToSend = new KeyValuePair<string, int>[] {
            GetNextCategoryName(),
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

        AirConsole.instance.Message(deviceId, new JsonAction("sendSelectCategory", stringifiedCategoriesToSend));
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
                questionCategories[selectedCategory % questionCategories.Count].categoryName
            };
            writeMyOwnQuestions = false;
        }
        AirConsole.instance.Message(deviceId, new JsonAction("sendRetrieveQuestions", questionsToSend));
    }

    public void SendWouldYouRather()
    {
        WouldYouRatherTimer oldWouldYouRatherTimer = wouldYouRatherPanel.GetComponent<WouldYouRatherTimer>();
        if (null == oldWouldYouRatherTimer)
        {
            Destroy(oldWouldYouRatherTimer);
        }
        WouldYouRatherTimer wyrt = wouldYouRatherPanel.AddComponent<WouldYouRatherTimer>();
        wyrt.SetTimerText(wouldYouRatherPanel.GetComponentsInChildren<Text>()[1]);
        //reset the icons
        int playerIconOffset = 5;
        Image[] playerIconPanels = wouldYouRatherPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconTags = getPlayerIconTags(playerIconPanels, "WouldYouRatherPlayerIcon");
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            if (i == gameState.GetCurrentRoundNumber()) {
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
        Text[] wouldYouRatherTexts = wouldYouRatherPanel.GetComponentsInChildren<Text>(true);
        if(gameState.audienceMembers.Count != 0)
        {
            wouldYouRatherTexts[10].text = "0";
            wouldYouRatherTexts[11].text = "0";
        } else
        {

        }
        string[] currentWouldYouRather = wouldYouRathers[currentWouldYouRatherIndex++ % wouldYouRathers.Count];
        wouldYouRatherTexts[0].text = currentWouldYouRather[0];
        //left answer
        wouldYouRatherTexts[2].text = currentWouldYouRather[1];
        //right answer
        wouldYouRatherTexts[3].text = currentWouldYouRather[2];
        //Maybe send the possible answers here
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendWouldYouRather", new[] { " " })));
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
            gameState.phoneViewGameState = PhoneViewGameState.SendQuestions;
            gameState.tvViewGameState = TvViewGameState.AnswerQuestionsScreen;
        } else
        {
            if (isAudienceMember(playerDeviceId))
            {
                AirConsole.instance.Message(playerDeviceId, JsonUtility.ToJson(new JsonAction("sendWaitScreen", questionsToSend)));
            }
            else
            {
                AirConsole.instance.Message(playerDeviceId, JsonUtility.ToJson(new JsonAction("sendQuestions", questionsToSend)));
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
        List<string> playerNames = new List<string>();
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
                playerNames.Add(gameState.GetPlayerByPlayerNumber(a.playerNumber).nickname);
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
        listToSend.AddRange(playerNames);
        listToSend.AddRange(anonymousPlayerNames);
        /*TODO this is not in the same order as anonymous player names*/
        listToSend.AddRange(playerAnswers);
        AirConsole.instance.Message(playerDeviceId, JsonUtility.ToJson(new JsonAction("sendVoting", listToSend.ToArray())));
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

        gameState.GetCurrentRound().answers.Shuffle();

        List<Answers> answersList = gameState.GetCurrentRound().answers;
        for (int i = 0; i < answersList.Count; i++)
        {
            Answers answers = answersList[i];
            anonymousPlayerNames.Add(answers.anonymousPlayerName);
            int answerPanelOffset = 2;
            Text myText = votingPanel.GetComponentsInChildren<Text>()[answerPanelOffset + i];
            //todo set the text size to the same size as the panel
            myText.text = "\n" + answers.anonymousPlayerName + "\n\n";
        }
        votingPanel.GetComponentsInChildren<Text>()[1].text = gameState.GetCurrentRound().PrintQuestions();
        
        //flash the instructions
        if (gameState.GetCurrentRoundNumber() == 0)
        {
            SendMessageToVipAndSendWaitScreenToEveryoneElse(new JsonAction("sendSkipInstructions", new string[] { " " }));
            //            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendSkipInstructions", new string[] { " " })));
            gameState.phoneViewGameState = PhoneViewGameState.SendSkipInstructionsScreen;

            Image[] images = votingPanel.GetComponentsInChildren<Image>();
            vp.url = System.IO.Path.Combine(Application.streamingAssetsPath + "/", "knowyourfriendstutorialvideo.mp4");

            vp.Prepare();
            yield return new WaitForSeconds(1);
            while (!vp.isPrepared)
            {
                yield return new WaitForSeconds(1);
                break;
            }
            instructionVideo.texture = vp.texture;
            instructionVideo.gameObject.SetActive(true);
            CameraZoom instructionsCz = instructionVideo.gameObject.AddComponent<CameraZoom>();
            instructionsCz.Setup(1f, 16f, false, false, false, true);
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
        int numPlayersAtStart = gameState.GetNumberOfPlayers();
        for (int i = 0; i < numPlayersAtStart; i++)
        {

            int answerPanelOffset = 2;
            Text myText = votingPanel.GetComponentsInChildren<Text>()[answerPanelOffset];

            //Camera zoom will make the current panel the last element, so we don't need to add i
            CameraZoom cz = votingPanel.GetComponentsInChildren<Image>()[panelOffset].gameObject.AddComponent<CameraZoom>();
            cz.Setup(1f, 12f, true, true, false);
            List<Answers> answersList = gameState.GetCurrentRound().answers;
            Answers answers = answersList[i];

            //create the short text to replace the text with questions on minimize
            StringBuilder shortTextSb = new StringBuilder();
            shortTextSb.Append(myText.text);

            //wait one second after pause before displaying anything
            yield return new WaitForSeconds(2);

            for (int j = 0; j < answers.text.Length; j++) 
            {
                string answer = answers.text[j];

                myText.text += "<i>" + gameState.GetCurrentRound().questions[j] + "</i>\n<b>" + answer + "</b>";
                shortTextSb.Append(answer);
                if (j < answers.text.Length - 1 )
                {
                    myText.text += "<size=15>\n</size>";
                    shortTextSb.Append("<size=15>\n\n</size>");
                }
                //wait four seconds between each answer to give it a punch
                yield return new WaitForSeconds(4);
            }
            myText.text = shortTextSb.ToString();
            yield return new WaitForSeconds(2);
            Destroy(cz);
        }
        Image[] playerIcons = votingPanel.GetComponentsInChildren<Image>(true);
        List<Image> playerIconsList = getPlayerIconTags(playerIcons, "AnswerQuestionsPane");
        //reset the ordering of the panels
        for (int i = 0; i < 6 - gameState.GetNumberOfPlayers(); i++)
        {
            playerIconsList[i].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
            //votingPanel.GetComponentsInChildren<Image>()[panelOffset].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }
        votingPanel.GetComponentsInChildren<Image>()[gridLayoutOffset].GetComponentInChildren<GridLayoutGroup>().enabled = true;
        autoResizeGrid.enabled = true;
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
        foreach (Answers a in answerList)
        {
            if (currentPlayer.playerNumber.Equals(a.playerNumber))
            {
                //skip
            }
            else
            {
                int playerNum = a.playerNumber;
                actual.Add(gameState.GetPlayerByPlayerNumber(playerNum).nickname);
            }
        }

        incrementBestFriends(currentPlayer, guesses, actual);
        personalRoundResults.AddRange(guesses);
        personalRoundResults.AddRange(actual);

        if (isReconnect)
        {
            personalRoundResults.Add("~~~reconnect~~~");
        }
        else
        {
            personalRoundResults.Add("~~~notReconnect~~~");
        }


        //send the current player their answerset
        AirConsole.instance.Message(currentPlayer.deviceId, new JsonAction("sendPersonalRoundResults", personalRoundResults.ToArray()));
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
            //p.playAnimation(MeexarpAction.Cheer);
        }
        yield return new WaitForSeconds(waitAnimationLength);

        answerQuestionsPanel.SetActive(false);
        votingPanel.SetActive(true);
        //TODO shuffle answers
        SendVoting();
        gameState.phoneViewGameState = PhoneViewGameState.SendVoting;
        gameState.tvViewGameState = TvViewGameState.VotingScreen;
    }

        //todo remove parameter
    public IEnumerator<WaitForSeconds> CalculateVoting(int count)
    {
        votingPanel.SetActive(false);
        resultsPanel.SetActive(true);
        //SendWaitScreenToEveryone();
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendPersonalResults", new string[] { " " })));
        gameState.phoneViewGameState = PhoneViewGameState.SendPersonalRoundResultsScreen;
        gameState.tvViewGameState = TvViewGameState.RoundResultsScreen;

        //set the anonymous names of each box
        List<Answers> answersList = gameState.GetCurrentRound().answers;
        for (int i = 0; i < answersList.Count; i++)
        {
            Answers answers = answersList[i];
            int resultsPanelOffset = 1;
            Text myText = resultsPanel.GetComponentsInChildren<Text>()[resultsPanelOffset + i];
            //todo set the text size to the same size as the panel
            myText.text = answers.anonymousPlayerName;
        }

        //send each individual their personalized results
        sendEveryonePersonalizedRoundResults(answersList);

        //give some time for the context switch
        yield return new WaitForSeconds(2);

        resultsPanel.GetComponentsInChildren<Image>()[0].GetComponentInChildren<GridLayoutGroup>().enabled = false;
        AutoResizeGrid autoResizeGrid = FindObjectsOfType(typeof(AutoResizeGrid))[3] as AutoResizeGrid;
        autoResizeGrid.enabled = false;

        int increasedFontSize = 23;
        List<Answers> answerList = gameState.GetCurrentRound().answers;
        for (int i = 0; i < answerList.Count; i++)
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
                            namesOfCorrectPeople.Add("<b><size=" + increasedFontSize + "><color=green>" + p.nickname + "</color></size></b>");
                            p.points++;
                            playerAnimations.Add(p, MeexarpAction.Cheer);

                            //keep track of total score
                            gameState.totalCorrectGuesses++;
                        }
                        else
                        {
                            if(!wrongGuessNameToPlayerNames.ContainsKey(currentGuess))
                            {
                                wrongGuessNameToPlayerNamesForZoomInView.Add(currentGuess, new List<string>());
                                wrongGuessNameToPlayerNames.Add(currentGuess, new List<string>());
                            }
                            wrongGuessNameToPlayerNamesForZoomInView[currentGuess].Add("<color=red>" + p.nickname + "</color>");
                            wrongGuessNameToPlayerNames[currentGuess].Add("<b><size=" + increasedFontSize + "><color=red>" + p.nickname + "</color></size></b>");
                            playerAnimations.Add(p, MeexarpAction.Sad);
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
                    wrongGuessNameToPlayerNames["none"].Add("<b><size=" + increasedFontSize + "><color=red>" + p.nickname + "</color></size></b>");
                    playerAnimations.Add(p, MeexarpAction.Sad);
                    //keep track of total score
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
            correctVotesSB.Append(" correctly guessed that ");
            correctVotesSB.Append(anonymousPlayerName);
            correctVotesSB.Append(" is ");
            correctVotesSB.Append(targetPlayerName);

            correctVotesStringSb.Append(" correctly guessed that ");
            correctVotesStringSb.Append(anonymousPlayerName);
            correctVotesStringSb.Append(" is ");
            correctVotesStringSb.Append(targetPlayerName);

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
                currentSb.Append(" incorrectly guessed that ");
                currentSb.Append(anonymousPlayerName);
                currentSb.Append(" is ");
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
                currentSb.Append(" incorrectly guessed that ");
                currentSb.Append(anonymousPlayerName);
                currentSb.Append(" is ");
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

            Text myText = resultsPanel.GetComponentsInChildren<Text>()[playerTileOffset];


            //display all results panel
            int resultsPanelOffset = playerTileOffset + 6;
            Animator titleAndGridContainerAnimator = resultsPanel.GetComponentsInChildren<Animator>()[0];
            Animator rightAndWrongPanelAnimator = resultsPanel.GetComponentsInChildren<Animator>()[1];
            Text rightAndWrongPanelTitle = resultsPanel.GetComponentsInChildren<Text>(true)[resultsPanelOffset];
            Text rightAndWrongPanelRightAndWrong = resultsPanel.GetComponentsInChildren<Text>(true)[resultsPanelOffset+1];
            Text rightAndWrongPanelAudienceRightAndWrong = resultsPanel.GetComponentsInChildren<Text>(true)[resultsPanelOffset+2];

            rightAndWrongPanelTitle.text = "";
            rightAndWrongPanelRightAndWrong.text = "";
            rightAndWrongPanelAudienceRightAndWrong.text = "";

            //first, display the questions and answers again
            int playerPanelTileOffset = 2;
            myText.text = "\n" + anonymousPlayerName + "'s answers\n\n";
            CameraZoom cz = resultsPanel.GetComponentsInChildren<Image>()[playerPanelTileOffset].gameObject.AddComponent<CameraZoom>();

            int zoomInTime = 1;
            float waitForContextSeconds = 1.5f;
            int waitForReadSeconds = 5;
            float waitForEachAnswer = .5f;

            float totalWaitTime = 3 * waitForContextSeconds + 2 * waitForReadSeconds + (3 + wrongVotesCount) * waitForEachAnswer;
            cz.Setup(zoomInTime, totalWaitTime, true, true, false);
            yield return new WaitForSeconds(zoomInTime);
            yield return new WaitForSeconds(waitForContextSeconds);
            for (int j = 0; j < answers.text.Length; j++)
            {
                yield return new WaitForSeconds(waitForEachAnswer);
                string answer = answers.text[j];

                myText.text += "<i>" + gameState.GetCurrentRound().questions[j] + "</i>\n<b>" + answer + "</b>";
                if (j < answers.text.Length - 1)
                {
                    myText.text += "<size=15>\n\n</size>";
                }
            }
            yield return new WaitForSeconds(waitForReadSeconds);

            rightAndWrongPanelAnimator.SetBool("Open", true);
            //titleAndGridContainerAnimator.SetBool("Open", true);
            yield return new WaitForSeconds(waitForContextSeconds);

            //myText.text = "Who did people guess " + anonymousPlayerName + " is\n";
            rightAndWrongPanelTitle.text = "Who did people guess " + anonymousPlayerName + " is\n";
            yield return new WaitForSeconds(waitForContextSeconds);

            foreach (string s in wrongVotesLines)
            {
                //myText.text += "<size=15>\n\n</size>" + s;
                rightAndWrongPanelRightAndWrong.text += "<size=15>\n\n</size>" + s;
                yield return new WaitForSeconds(waitForEachAnswer);
            }
            //myText.text += "<size=15>\n\n</size>" + correctVotesStringSb.ToString();
            rightAndWrongPanelRightAndWrong.text += "<size=15>\n\n</size>" + correctVotesStringSb.ToString();
            if(numberOfCorrectAudienceGuesses > 0)
            {
               // myText.text += "<size=15>\n\n</size><color=green>" + numberOfCorrectAudienceGuesses + "</color> Correct Audience Members";
                rightAndWrongPanelAudienceRightAndWrong.text += "<size=15>\n\n</size><color=green>" + numberOfCorrectAudienceGuesses + "</color> Correct Audience Members";
                gameState.totalAudienceCorrectGuesses += numberOfCorrectAudienceGuesses;
            }
            if(numberOfWrongAudienceGuesses > 0)
            {
                //myText.text += "<size=15>\n\n</size><color=red>" + numberOfWrongAudienceGuesses + "</color> Wrong Audience Members";
                rightAndWrongPanelAudienceRightAndWrong.text += "<size=15>\n\n</size><color=red>" + numberOfWrongAudienceGuesses + "</color> Wrong Audience Members";
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

            string tileTitle = anonymousPlayerName + " is " + targetPlayerName + "\n\n";
            myText.text = "\n<b><size=" + (increasedFontSize + 3) + ">" + tileTitle + "</size></b>" + correctVotesSB.ToString() + "\n\n" + wrongVotesSb.ToString()
                + audienceGuessesString;
            
            //reveal it on phones
            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendRevealNextPersonalRoundResult", new string[] { targetPlayerName })));

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
        for (int i = 0; i < 6 - gameState.GetNumberOfPlayers(); i++)
        {
            playerIconsList[i].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }
        resultsPanel.GetComponentsInChildren<Image>()[0].GetComponentInChildren<GridLayoutGroup>().enabled = true;
        autoResizeGrid.enabled = true;
        
        if (gameState.GetCurrentRoundNumber() == gameState.numRoundsPerGame - 1)
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

    public void SendEndScreen2()
    {
        resultsPanel.SetActive(false);
        endScreenPanel.SetActive(true);
        foreach (Player p in gameState.players.Values)
        {
            string currentBestFriend = CalculateBestFriend(p);

            AirConsole.instance.Message(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, currentBestFriend }));
        }

        foreach (Player p in gameState.audienceMembers.Values)
        {
            string currentBestFriend = CalculateBestFriend(p);

            AirConsole.instance.Message(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, currentBestFriend }));
        }

        int offset = 1;
        float correctPercent = ((float)gameState.totalCorrectGuesses) / ((float)(gameState.totalCorrectGuesses + gameState.totalWrongGuesses)) * 100.0f;
        string[] friendshipStatusArray = { "Archnemeses", "Enemies", "Strangers", "Acquaintances", "Just \"Ok\" Friends", "Friends", "Best Friends" };
        string[] resultsStatuses = { "The board is impressed and will grant you the funds to continue your research. Congratulations!" ,
        "The board is unimpressed, but will allow you to continue your research in hopes that you’ll better discover friendship and will grand you the funds to continue your research."};

        string friendshipStatus = CaluclateFriendshipStatus(correctPercent, friendshipStatusArray);
        string resultStatus = correctPercent >= 60.0f ? resultsStatuses[0] : resultsStatuses[1];
        
        //set percentCorrectText
        endScreenPanel.GetComponentsInChildren<Text>()[offset].text = "Collectively, your team identified\n<b><size=90> " + correctPercent + "%</size> </b>\nof your teammates correctly!";
        //set friendship status
        endScreenPanel.GetComponentsInChildren<Text>()[offset + 1].text = "You have achieved the status of\n<b><size=60> " + friendshipStatus + " </size></b> ";
        //set result text
        endScreenPanel.GetComponentsInChildren<Text>()[offset + 2].text = resultStatus;


        gameState.phoneViewGameState = PhoneViewGameState.SendEndScreen;
        gameState.tvViewGameState = TvViewGameState.EndGameScreen;
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
            endScreenPanel.GetComponentsInChildren<Text>()[tilesOffset + playerCounter].text = pointsSB.ToString();
            playerCounter++;
            string currentBestFriend = CalculateBestFriend(p);

            AirConsole.instance.Message(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, currentBestFriend }));

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
            endScreenPanel.GetComponentsInChildren<Text>()[0].text = audiencePointsSB.ToString();
            string currentBestFriend = CalculateBestFriend(p);

            AirConsole.instance.Message(p.deviceId, new JsonAction("sendEndScreen", new string[] { "" + p.points, currentBestFriend }));

        }
        
        gameState.phoneViewGameState = PhoneViewGameState.SendEndScreen;
    }

    private string CalculateBestFriend(Player p)
    {
        int currentBestFriendPoints = 0;
        string currentBestFriend = "No one!";
        //calculate best friends
        foreach (KeyValuePair<string, int> bestFriendValue in p.bestFriendPoints)
        {
            int myPoints = bestFriendValue.Value;
            Dictionary<string, int> theirBfp = gameState.GetPlayerByName(bestFriendValue.Key).Value.bestFriendPoints;
            int theirPoints = theirBfp.ContainsKey(p.nickname) ? theirBfp[p.nickname] : 0;
            if (myPoints + theirPoints > currentBestFriendPoints)
            {
                currentBestFriend = bestFriendValue.Key;
                currentBestFriendPoints = myPoints + theirPoints;
            }
        }

        return currentBestFriend;
    }

    private int anonymousNameCounter = 0;
    private int anonymousPlayerNumberCount = 0;
    private int[] anonymousPlayerNumbers = new int[] {1, 2, 3, 4, 5, 6 };
    public string GenerateAnonymousPlayerName()
    {   
        if(!options["anonymousNames"])
        {
            return "Answer Set " + anonymousPlayerNumbers[anonymousPlayerNumberCount++]; 
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
        QuestionCategory qc = questionCategories[selectedCategory % questionCategories.Count];
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
    public Animator myAnimator { get; set; }
    public Dictionary<string, int> bestFriendPoints { get; set; }
    public int alienNumber { get; set; }

    public Player(string n, int d, int pn, int a, Animator an, int selectedAlien)
    {
        nickname = n;
        deviceId = d;
        playerNumber = pn;
        avatarId = a;
        points = 0;
        myAnimator = an;
        bestFriendPoints = new Dictionary<string, int>();
        alienNumber = selectedAlien; //todo i dont think i need this
    }

    public Player(string n, int d, int pn, int a, int p, Animator an, Dictionary<string, int> bfp, int selectedAlien)
    {
        nickname = n;
        deviceId = d;
        playerNumber = pn;
        avatarId = a;
        points = p;
        myAnimator = an;
        bestFriendPoints = bfp;
        alienNumber = selectedAlien;
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
            sb.Append("\t" + question);
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
    public int numRoundsPerGame { get; set; }
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

    public void ResetGameState()
    {
        rounds = new List<Round>();
        ResetGuesses();
    }

    public int GetNumberOfPlayers()
    {
        return players.Count;
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

    private static string prependTextResourceFilepath(string filename)
    {
        return System.IO.Path.Combine(Application.streamingAssetsPath + "/", filename);
    }
}