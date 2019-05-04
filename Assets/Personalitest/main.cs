using System.Collections.Generic;
using UnityEngine;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using System.Text;
using System.Text.RegularExpressions;

public class main : MonoBehaviour
{
    public Text gameStateText;
    public Text welcomeInstructionsText;
    private static string[] questions;
    private static string[] anonymousNames;
    //(question, left answer, right answer)
    private static string[][] wouldYouRathers;
    private GameState gameState;
    public List<GameObject> welcomePanels;
    public GameObject wouldYouRatherPanel;
    public GameObject answerQuestionsPanel;
    public GameObject votingPanel;
    public GameObject resultsPanel;
    public GameObject endScreenPanel;
    public Canvas canvas;
    public AudioSource introAudioSource;
    public AudioSource mainLoopAudioSource;
    public AudioSource blipAudioSource;
    //should make a different sound per person
    public AudioClip[] blips;
    private int currentQuestionIndex;
    private int currentWouldYouRatherIndex;
    private string gameCode;

    // Start is called before the first frame update
    void Start()
    {
        //read the resources
        var newLinesRegex = new Regex(@"\r\n|\n|\r", RegexOptions.Singleline);
        questions = newLinesRegex.Split(Resources.GetQuestions());
        anonymousNames = newLinesRegex.Split(Resources.GetAnonymousNames());
        string[] wouldYouRathersLines = newLinesRegex.Split(Resources.GetWouldYouRathers());
        List<string[]> tempWouldYouRathers = new List<string[]>();
        foreach (string s in wouldYouRathersLines)
        {
            tempWouldYouRathers.Add(s.Split('|'));
        }
        //randomize the order of the resources
        questions.Shuffle();
        anonymousNames.Shuffle();
        tempWouldYouRathers.Shuffle();
        wouldYouRathers = tempWouldYouRathers.ToArray();

        blips.Shuffle();

        AirConsole.instance.onReady += OnReady;
        AirConsole.instance.onMessage += OnMessage;
        gameState = new GameState();
        currentQuestionIndex = 0;
        currentWouldYouRatherIndex = 0;
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

    void OnMessage(int from, JToken data)
    {
        string action = data["action"].ToString();
        if ("sendWelcomeInfo".Equals(action))
        {
            string name = data["info"]["name"].ToString();
            KeyValuePair<int, Player> currentPlayer = gameState.GetPlayerByName(name);

            //reconect case
            if (!currentPlayer.Equals(default(KeyValuePair<int, Player>)))
            {
                gameState.players.Remove(currentPlayer.Key);
                gameState.players.Add(from, new Player(name, from, currentPlayer.Value.playerNumber, 0, currentPlayer.Value.points));
                switch (gameState.phoneViewGameState)
                {
                    case PhoneViewGameState.SendStartGameScreen:
                        AirConsole.instance.Message(from, new JsonAction("startGameScreenView", new string[] { " " }));
                        break;
                    case PhoneViewGameState.SendWaitScreen:
                        AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
                        break;
                    case PhoneViewGameState.SendWouldYouRather:
                        if (currentPlayer.Value.playerNumber == gameState.GetCurrentRoundNumber())
                        {
                            SendRetrieveQuestions(from);
                        } else
                        {
                            AirConsole.instance.Message(from, new JsonAction("sendWouldYouRather", new string[] { " " }));
                        }
                        break;
                    case PhoneViewGameState.SendQuestions:
                        SendQuestions(from);
                        break;
                    case PhoneViewGameState.SendVoting:
                        if(gameState.GetCurrentRound().votes.ContainsKey(from))
                        {
                            AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
                        } else
                        {
                            SendVoting(from);
                        }
                        break;
                    case PhoneViewGameState.SendNextRoundScreen:
                        AirConsole.instance.Message(from, new JsonAction("sendNextRoundScreen", new string[] { " " }));
                        break;
                    case PhoneViewGameState.SendAdvanceToResultsScreen:
                        AirConsole.instance.Message(from, new JsonAction("sendAdvanceToResultsScreen", new string[] { " " }));
                        break;
                    case PhoneViewGameState.SendEndScreen:
                        AirConsole.instance.Message(from, new JsonAction("sendEndScreen", new string[] { " " }));
                        break;
                    default:
                        AirConsole.instance.Message(from, new JsonAction("sendWaitScreen", new string[] { " " }));
                        break;
                }
            }
            //new player
            else if (gameState.GetNumberOfPlayers() < 6)
            {
                //remove the nbsp
                name = Regex.Replace(name, @"\u00a0", " ");
                Player p = new Player(name, from, gameState.GetNumberOfPlayers(), 0);
                welcomePanels[gameState.GetNumberOfPlayers()].GetComponentInChildren<Text>().text = p.nickname;
                gameState.players.Add(from, p);
            }
        }
        else if ("sendStartGame".Equals(action))
        {
            GameObject.Find("WelcomeScreenPanel").SetActive(false);

            int playerIconOffset = 5;
            for (int i = 5; i >= gameState.GetNumberOfPlayers(); i--)
            {
                wouldYouRatherPanel.GetComponentsInChildren<Image>()[playerIconOffset + i].gameObject.SetActive(false);
            }
            int playerTextOffset = 3;
            for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
            {
                wouldYouRatherPanel.GetComponentsInChildren<Text>()[playerTextOffset + i].text = gameState.GetPlayerByPlayerNumber(i).nickname;
            }
            introAudioSource.Stop();
            mainLoopAudioSource.Play();
            StartRound();
        }
        else if ("sendWouldYouRatherAnswer".Equals(action))
        {
            int leftOrRight = data["info"].ToObject<int>();
            int indexOfFirstIcon = 5;
            int playerNumber = gameState.players[from].playerNumber;
            if (leftOrRight == 0)
            {
                Transform myTransform = wouldYouRatherPanel.GetComponentsInChildren<Image>()[indexOfFirstIcon + playerNumber].transform;
                myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.3f * canvas.scaleFactor, myTransform.position.y);
            } else
            {
                Transform myTransform = wouldYouRatherPanel.GetComponentsInChildren<Image>()[indexOfFirstIcon + playerNumber].transform;
                myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.70f * canvas.scaleFactor, myTransform.position.y);
            }

            Debug.Log("received sendWouldYouRatherAnswer from: " + from + " with answer: " + leftOrRight);
        }
        else if ("sendRequestAnotherQuestion".Equals(action))
        {
            string elementId = data["info"]["elementId"].ToString();
            string nextQuestion = GetNextQuestion();
            AirConsole.instance.Message(from, new JsonAction("sendAnotherQuestion", new string[] { elementId, nextQuestion }));
        }
        else if ("sendDecidedQuestions".Equals(action))
        {
            //Stop the would you rathers
            CancelInvoke();

            wouldYouRatherPanel.SetActive(false);
            answerQuestionsPanel.SetActive(true);

            //display the status of each player's submission
            for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
            {
                Player currentPlayer = gameState.GetPlayerByPlayerNumber(i);
                int tilesOffset = 1;
                answerQuestionsPanel.GetComponentsInChildren<Text>()[tilesOffset + i].text = currentPlayer.nickname + "\n\n<color=red>Has Not Submitted</color>";

            }

            List<string> myQuestions = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                Debug.Log("property: " + property);
                myQuestions.Add(property.Value.ToString());
            }
            Debug.Log("received sendDecidedQuestions from: " + from + " with questions: " + myQuestions);
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
            answerQuestionsPanel.GetComponentsInChildren<Text>()[tilesOffset + currentPlayer.playerNumber].text = currentPlayer.nickname + "\n\n<color=green>Has Submitted</color>";


            if (HasEveryoneSubmittedAnswers())
            {
                answerQuestionsPanel.SetActive(false);
                votingPanel.SetActive(true);

                SendVoting();
                gameState.phoneViewGameState = PhoneViewGameState.SendVoting;
            }
        }
        else if ("sendVoting".Equals(action))
        {
            Debug.Log("received sendVoting from: " + from);
            Dictionary<string, string> myVotes = new Dictionary<string, string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                Debug.Log("property: " + property);
                string anonymousName = property.Value.ToString();
                string playerName = property.Name.ToString();
                myVotes.Add(anonymousName, playerName);
            }

            gameState.GetCurrentRound().votes.Add(from, myVotes);
            if(HasEveryoneVoted())
            {
                votingPanel.SetActive(false);
                resultsPanel.SetActive(true);
                StartCoroutine(CalculateVoting(2));
                Debug.Log("All Votes collected");
            }
        }
        else if ("sendNextRound".Equals(action))
        {
            StartRound();
        }
        else if ("sendShowEndScreen".Equals(action))
        {
            resultsPanel.SetActive(false);
            endScreenPanel.SetActive(true);
            SendEndScreen();
            Debug.Log("received sendPlayAgain");
        }
        else if ("sendPlayAgain".Equals(action))
        {
            gameState.ResetGameState();
            endScreenPanel.SetActive(false);
            StartRound();
            Debug.Log("received sendPlayAgain");
        }
        //play a sound to confirm the input
        blipAudioSource.PlayOneShot(blips[gameState.players[from].playerNumber], Random.Range(.5f, 1f));
    }

    /* Possible actions to send */

    public void SendWaitScreenToOnePlayer(int playerNumber)
    {
        AirConsole.instance.Message(AirConsole.instance.GetControllerDeviceIds()[0], new JsonAction("sendWaitScreen", new[] { "This is a funny quote" }));
    }

    public void SendWaitScreenToEveryone()
    {
        AirConsole.instance.Broadcast(new JsonAction("sendWaitScreen", new[] { "This is a funny quote" }));
        gameState.phoneViewGameState = PhoneViewGameState.SendWaitScreen;
    }

    //startRound sends one person a SendRetrieveQuestions message and sends the others would you rathers until the questions are complete
    public void StartRound()
    {
        //if starting from previous round
        resultsPanel.SetActive(false);

        gameState.rounds.Add(new Round());
        //controllers in the retrieve questions state will ignore would you rathers
        int currentPlayerTurnDeviceId = gameState.GetPlayerByPlayerNumber(gameState.GetCurrentRoundNumber()).deviceId;
        SendRetrieveQuestions(currentPlayerTurnDeviceId); 
        
        wouldYouRatherPanel.SetActive(true);
        gameState.phoneViewGameState = PhoneViewGameState.SendWouldYouRather;
        InvokeRepeating("SendWouldYouRather", 0f, 12f);
    }

    public void SendRetrieveQuestions(int deviceId)
    {
        string[] questionsToSend = new string[] {
            GetNextQuestion(),
            GetNextQuestion(),
            GetNextQuestion()
        };
        AirConsole.instance.Message(deviceId, new JsonAction("sendRetrieveQuestions", questionsToSend));
    }

    public void SendWouldYouRather()
    {
        //reset the icon
        int playerIconOffset = 5;
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {
            Transform myTransform = wouldYouRatherPanel.GetComponentsInChildren<Image>()[playerIconOffset + i].transform;
            myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.50f * canvas.scaleFactor, myTransform.position.y);
        }
        string[] currentWouldYouRather = wouldYouRathers[currentWouldYouRatherIndex++ % wouldYouRathers.Length];
        wouldYouRatherPanel.GetComponentInChildren<Text>().text = currentWouldYouRather[0];
        //left answer
        wouldYouRatherPanel.GetComponentsInChildren<Text>()[1].text = currentWouldYouRather[1];
        //right answer
        wouldYouRatherPanel.GetComponentsInChildren<Text>()[2].text = currentWouldYouRather[2];
        //Maybe send the possible answers here
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendWouldYouRather", new[] { " " })));
    }

    public void SendQuestions()
    {
        SendQuestions(-1);
    }

    public void SendQuestions(int playerDeviceId)
    {
        string[] questionsToSend = gameState.GetCurrentRound().questions.ToArray();

        string actionData = JsonUtility.ToJson(new JsonAction("sendQuestions", questionsToSend));

        if (playerDeviceId < 0)
        {
            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendQuestions", questionsToSend)));
            gameState.phoneViewGameState = PhoneViewGameState.SendQuestions;
        } else
        {
            AirConsole.instance.Message(playerDeviceId, JsonUtility.ToJson(new JsonAction("sendQuestions", questionsToSend)));
        }
    }

    public void SendVoting()
    {
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
            int answerPanelOffset = 3;
            Text myText = votingPanel.GetComponentsInChildren<Text>()[answerPanelOffset + i];
            //todo set the text size to the same size as the panel
            myText.text = answers.anonymousPlayerName + "\n\n";
        }
        votingPanel.GetComponentsInChildren<Text>()[2].text = gameState.GetCurrentRound().PrintQuestions();
        StartCoroutine(DoZoom(2));

        //send data to the phones
        List<string> listToSend = new List<string>();
        listToSend.AddRange(playerNames);
        listToSend.AddRange(anonymousPlayerNames);
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendVoting", listToSend.ToArray())));
        gameState.phoneViewGameState = PhoneViewGameState.SendVoting;
        //todo display instructions

    }

    public void SendVoting(int playerDeviceId)
    {
        List<string> playerNames = new List<string>();
        List<string> anonymousPlayerNames = new List<string>();

        foreach (Player p in gameState.players.Values)
        {
            playerNames.Add(p.nickname);
        }

        List<Answers> answersList = gameState.GetCurrentRound().answers;
        for (int i = 0; i < answersList.Count; i++)
        {
            Answers answers = answersList[i];
            anonymousPlayerNames.Add(answers.anonymousPlayerName);
        }

        //send data to the phones
        List<string> listToSend = new List<string>();
        listToSend.AddRange(playerNames);
        listToSend.AddRange(anonymousPlayerNames);
        AirConsole.instance.Message(playerDeviceId, JsonUtility.ToJson(new JsonAction("sendVoting", listToSend.ToArray())));
    }

    //todo remove parameter
    private IEnumerator<WaitForSeconds> DoZoom(float seconds)
    {
        yield return new WaitForSeconds(1);

        //flash the instructions
        if (gameState.GetCurrentRoundNumber() == 0)
        {
            Image[] images = votingPanel.GetComponentsInChildren<Image>();
            CameraZoom instructionsCz = images[0].gameObject.AddComponent<CameraZoom>();
            instructionsCz.Setup(1f, 10f, true, false, true);
            yield return new WaitForSeconds(12);

            Destroy(instructionsCz);
            //reset the position of the child panels
            //questions panel
            images[1].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
            //votingpanelgrid
            images[2].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }


        votingPanel.GetComponentsInChildren<Image>()[2].GetComponentInChildren<GridLayoutGroup>().enabled = false;
        AutoResizeGrid autoResizeGrid = FindObjectsOfType(typeof(AutoResizeGrid))[2] as AutoResizeGrid;
        autoResizeGrid.enabled = false;
        int panelOffset = 3;
        for (int i = 0; i < gameState.GetNumberOfPlayers(); i++)
        {

            int answerPanelOffset = 3;
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

                myText.text += gameState.GetCurrentRound().questions[j] + "\n" + answer;
                shortTextSb.Append(answer);
                if (j < answers.text.Length - 1 )
                {
                    myText.text += "<size=15>\n\n</size>";
                    shortTextSb.Append("<size=15>\n\n</size>");
                }
                //wait four seconds between each answer to give it a punch
                yield return new WaitForSeconds(4);
            }
            myText.text = shortTextSb.ToString();
            yield return new WaitForSeconds(2);
            Destroy(cz);
        }
        for (int i = gameState.GetNumberOfPlayers(); i < 6; i++)
        {
            votingPanel.GetComponentsInChildren<Image>()[panelOffset].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }
        votingPanel.GetComponentsInChildren<Image>()[2].GetComponentInChildren<GridLayoutGroup>().enabled = true;
        autoResizeGrid.enabled = true;
    }


    //todo remove parameter
    public IEnumerator<WaitForSeconds> CalculateVoting(int count)
    {

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

        //give some time for the context switch
        yield return new WaitForSeconds(3);


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

            //answers is stooring device ids it should be playeriids
            Answers answers = answerList[i];
            string anonymousPlayerName = answers.anonymousPlayerName;
            string targetPlayerName = gameState.GetPlayerByPlayerNumber(answers.playerNumber).nickname;
            foreach (KeyValuePair<int, Dictionary<string, string>> playerVote in gameState.GetCurrentRound().votes)
            {
                Player p = gameState.players[playerVote.Key];
                if (playerVote.Value.ContainsKey(anonymousPlayerName))
                {
                    string currentGuess = playerVote.Value[anonymousPlayerName];
                    if (playerVote.Value.ContainsKey(anonymousPlayerName))
                    {
                        if (currentGuess == targetPlayerName)
                        {
                            namesOfCorrectPeopleForZoomInView.Add("<color=green>" + p.nickname + "</color>");
                            namesOfCorrectPeople.Add("<b><size=" + increasedFontSize + "><color=green>" + p.nickname + "</color></size></b>");
                            p.points++;
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
                    wrongVotesSb.Append(" and ");
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


            //display all results people

            //first, display the questions and answers again
            int playerPanelTileOffset = 2;
            myText.text = anonymousPlayerName + "'s answers\n\n";
            CameraZoom cz = resultsPanel.GetComponentsInChildren<Image>()[playerPanelTileOffset].gameObject.AddComponent<CameraZoom>();
            cz.Setup(1f, 20f + wrongVotesCount * 4f, true, true, false);

            for (int j = 0; j < answers.text.Length; j++)
            {
                yield return new WaitForSeconds(3);
                string answer = answers.text[j];

                myText.text += gameState.GetCurrentRound().questions[j] + "\n" + answer;
                if (j < answers.text.Length - 1)
                {
                    myText.text += "<size=15>\n\n</size>";
                }
            }
            yield return new WaitForSeconds(3);

            myText.text = "Who did people guess " + anonymousPlayerName + " is\n";
            yield return new WaitForSeconds(3);

            foreach (string s in wrongVotesLines)
            {
                myText.text += "<size=15>\n\n</size>" + s;
                yield return new WaitForSeconds(4);
            }
            myText.text += "<size=15>\n\n</size>" + correctVotesStringSb.ToString();

            yield return new WaitForSeconds(4);
            yield return new WaitForSeconds(3);

            string tileTitle = anonymousPlayerName + " is " + targetPlayerName + "\n\n";
            myText.text = "<b><size=" + (increasedFontSize + 3) + ">" + tileTitle + "</size></b>" + correctVotesSB.ToString() + "\n\n" + wrongVotesSb.ToString();

            //wait for the shrink
            yield return new WaitForSeconds(1);
            Destroy(cz);
        }
        //reset the position of the other panels
        for (int i = gameState.GetNumberOfPlayers(); i < 6; i++)
        {
            int playerPanelTileOffset = 2;
            resultsPanel.GetComponentsInChildren<Image>()[playerPanelTileOffset].gameObject.GetComponent<RectTransform>().SetAsLastSibling();
        }
        resultsPanel.GetComponentsInChildren<Image>()[0].GetComponentInChildren<GridLayoutGroup>().enabled = true;
        autoResizeGrid.enabled = true;
        if (gameState.GetCurrentRoundNumber() == gameState.GetNumberOfPlayers() - 1)
        {
            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendAdvanceToResultsScreen", new string[] { " " })));
            gameState.phoneViewGameState = PhoneViewGameState.SendAdvanceToResultsScreen;
        }
        else
        {
            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendNextRoundScreen", new string[] { " " })));
            gameState.phoneViewGameState = PhoneViewGameState.SendNextRoundScreen;
        }
    }

    public void SendEndScreen()
    {

        //display points
        int playerCounter = 0;
        foreach (Player p in gameState.players.Values)
        {
            StringBuilder pointsSB = new StringBuilder(100);

            pointsSB.Append(p.nickname);
            pointsSB.Append(" has ");
            pointsSB.Append(p.points);
            pointsSB.Append(" points");
            pointsSB.Append("\n");
            int tilesOffset = 1;
            endScreenPanel.GetComponentsInChildren<Text>()[tilesOffset + playerCounter].text = pointsSB.ToString();
            playerCounter++;

        }
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendEndScreen", new string[] { " "})));
        gameState.phoneViewGameState = PhoneViewGameState.SendEndScreen;
    }

    private int anonymousNameCounter = 0;
    public string GenerateAnonymousPlayerName()
    {   
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
        return questions[currentQuestionIndex++ % questions.Length];
    }
}

public static class IListExtensions
{
    /// <summary>
    /// Shuffles the element order of the specified list.
    /// </summary>
    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
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

    public Player(string n, int d, int pn, int a)
    {
        nickname = n;
        deviceId = d;
        playerNumber = pn;
        avatarId = a;
        points = 0;
    }

    public Player(string n, int d, int pn, int a, int p)
    {
        nickname = n;
        deviceId = d;
        playerNumber = pn;
        avatarId = a;
        points = p;
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

class Round
{
    public List<string> questions { get; set; }
    public List<Answers> answers { get; set; }
    
    //<deviceId, <anonymousName, playerName>>
    public Dictionary<int, Dictionary<string, string>> votes { get; set; }
    public Round()
    {
        questions = new List<string>();
        answers = new List<Answers>();
        votes = new Dictionary<int, Dictionary<string, string>>();
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
    public List<Round> rounds { get; set; }
    public PhoneViewGameState phoneViewGameState;

    public GameState()
    {
        players = new Dictionary<int, Player>();
        rounds = new List<Round>();
        phoneViewGameState = PhoneViewGameState.SendStartGameScreen;
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

    public void ResetGameState()
    {
        rounds = new List<Round>();
    }

    public int GetNumberOfPlayers()
    {
        return players.Count;
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
    SendRetrieveQuestions = 2,
    SendWouldYouRather = 3,
    SendVoting = 4,
    SendNextRoundScreen = 5,
    SendAdvanceToResultsScreen = 6,
    SendEndScreen = 7,
    SendStartGameScreen
}

static class Resources
{

    public static string GetAnonymousNames()
    {
        return @"Katharine Briggs
Isabel Myers
Gary Chapman
William Moulton Marston
Gary Gygax
Walter H. Holtzman
Paul Costa
Robert McCrae
Derpy McDerpface
Hugh Jass
Anita Bath
Personali Tea
Seymour Butz
Al Coholic
Ivana Tinkle
Miss Chiff
Amanda Hugankiss
Sir Render
Ann Tickwittee
Dee Plomasy
Kim Yoonity
Polly Tix
Justin Case
Sarah Nade";
    }

    public static string GetQuestions()
    {
        return @"What is your DnD alignment (Lawful Evil, Chaotic Neutral, Neutral Neutral)?
What is your favorite color?
What is your favorite genre of movie?
What is your favorite genre of video game?
What is your favorite food?
What is your favorite animal?
What is your ideal group size?
What is your preferred indoor temperature?
What is your favorite outdoor activity?
What is your favorite body part?
What is something you look for in a romantic partner?
What is one of your hobbies?
What do you enjoy about traveling?
What is one of your values?
How do you support a sad friend?
What do you do when something doesn't go as planned?
How do you like to show affection?
What kind of dreams do you have?
What kind of nightmares do you have?
What are your life goals?
What kind of conversations do you like to have?
Where would you go for a second date?
What kinds of presents do you like to give?
What is your favorite simple ingredient (for example, corn)?
What is your favorite dessert?
What is one thing you like about your parents?
What is a trait of someone you admire?
What is something you look forward to when you retire?
How long would you wait in line for $50?
What kind of super power would you want for your daily life?
Which Harry Potter house would you be sorted into?
You have just become the president. What is the newest top priority?
What is something you are thankful for?
What is your opinion of children?
How important is physical appearance to you?
What is one of your 'dealbreakers'?
What are your thoughts about white lies?
If you had to get a tatoo, what kind of tatoo would you get?
What kind of music do you like?
What do you like to do on your phone?
What is the minimum amount of money you need to completely change your lifestyle?";
    }

    public static string GetWouldYouRathers()
    {
        return @"Would you take the last slice of pizza?|Never|Absolutely
Would you rather not need to eat, or not need to sleep?|Never Eat|Never Sleep
On an average weekend, would you rather stay in or go out?|Stay In|Go Out
When you are home alone, do you close the bathroom door?|Nope|There might be a killer!
Would you rather be able to stop time for an hour or rewind 15 minutes?|Stop Time|Rewind Time
Do you like pineapple on pizza?|Never|As God Intended
Would you rather have someone cook for you or have someone take you out to a restaurant?|Cook|Go out
Do you easily get upset by what other people say?|No|Yes
Do you prefer to start a conversation or have someone else start one with you?|I like to start|I like to be approached
At a party, would you rather meet someone new or stick to your crowd?|Meet new people!|Hang out with your friends!
Do you relate to people who let their emotions guide them?|No|Yes
Do you stay calm under pressure?|AHHHHHHH|Yes
Does it take you a long time to decide what to watch on tv?|No|Yes
When in a group of people you do not know, do you have any problems jumping into their conversation?|No|Yes
Would you be willing to step on others to get ahead in life?|No|Yes
When focusing on a task, are you likely to get distracted?|No|Yes
Do you cry in front of others?|Never|Yes
When making life choices, which do you listen to more, your heart or your head?|Heart|Head
Do you prefer to understand practical things or theoretical things?|Practical|Theoretical
Would you talk about politics with your parents?|Never|Yep
Would you rather get revenge or forgive?|Revenge!|Forgive!
Can you make decisions on a whim?|Let me think about that...|Yes!
Do you focus on present realities or future possibilities?|Present|Future
Are you good at understanding other people's feelings?|No|Yes
Do you prefer to plan ahead or go with the flow?|Plan ahead|Go with the flow
Would you rather explore space or the ocean?|Spaaaaace|Ocean
Would you rather sit next to someone who smells or someone who is loud?|Give me the smells|I can handle the loudness
Would you rather be rich with average intelligence or Intelligent with average wealth?|Rich!|Intelligent!
Do you like Anime?|What is anime?|Yes
Would you rather be able to professionaly play 3 instruments, or fluently speak 3 languages|Instruments|Language
Would you date someone with a creepy mustache?|Probably|Very Yes
Do you prefer zombies or aliens?|Zombies|Aliens
Would you rather be able to fall asleep immediately or wake up at a specific time?|Sleep|Wake Up
The Trolley Problem. Would you personally kill one person to save 5?|No|Yes
Would you be willing to sit in a room for 8 hours a day for $25 an hour?|No|Yes
Would you rather be slapped very hard, or slap someone very hard?|Be Slapped|Slap Someone
Do you believe in love at first sight?|No|Yes
Would you rather travel to the past or the future?|Past|Future
Would you rather get eaten by a shark or get run over by a car? |Eaten by a shark|Get run over by a car
Do you believe in life after love? | I can feel something inside me say... | I really don't think I'm strong enough
Would you rather fight 10 horse-sized flies or 100 fly-sized pigs? | Flies | Pigs
Hit or miss? | I guess you never miss, huh? | You got a boyfriend...I bet he doesn't kiss ya!!!
Would you allow twerking at your funeral? | Absolutely not!!! | It should be REQUIRED
Is a Hot Pocket a big pizza roll or is a pizza roll a mini Hot Pocket? | big pizza roll | mini Hot Pocket
If you were stranded on an island with Toad, would you eat him | Nah bruh | Ye duh 
Do you do your part to save the planet? | I suck |I'm an environmentalist :)
Would you rather live in an 80s teen movie or early 2000s teen movie?| 80s| 2000s
Would you rather swim in jello or nacho cheese? | jello| nacho cheese
Would you have a servant? | no, that's weird| SERVE ME PEASANT
If you farted an something solid came out, would you own up to it?| [Shaggy voice] It wasn't me!| I'm poopy and I'm PROUD
Shrek or Shrek 2? | Shrek | Shrek 2
Bee Movie or Bratz the Movie | Bee Movie | Bratz the Movie
Broccoli|gross|hawt
No pants during the winter or fleece pants (flants) during the summer?|I wanna freeze|Sweat baby sweat
Would you rather receive a lump sum of money or a steady amount daily?| Gimme LUMPS | Stream it 2 me
Doughnuts or cake?|DOUGHNUTS|CAKE
Would you rather rave or mosh?|RAVE|MOSH
Would you rather be a wizard or a superhero?|I'm Harry Potter!!!|I'm Spider Man!!!
Starburst jelly beans or regular Starburst?| jelly beans | Starburst
Would you rather have an elephant-sized cat or a cat-sized elephant?|I WANNA HOLD IT|I WANNA RIDE IT
Livin' on a Prayer or Don't Stop Believin'|WOAAAAAAAAH LIVIN ON A PRAYER|JUST A SMALL TOWN GUUURRRLLLLL";

    }
}