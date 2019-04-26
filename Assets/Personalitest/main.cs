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
    public GameObject canvas;
    public AudioSource introAudioSource;
    public AudioSource mainLoopAudioSource;
    public AudioSource blipAudioSource;
    //should make a different sound per person
    public AudioClip[] blips;
    private int currentQuestionIndex;
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
    }

    void OnReady(string code)
    {
        gameCode = code;
        welcomeInstructionsText.text = "Navigate to airconsole.com and enter " + code + " to join!";
    }

    void Update()
    {
        //gameStateText.text = gameState.ToString();
    }

    void OnMessage(int from, JToken data)
    {
        Debug.Log("message received");
        Debug.Log("message received from " + from + " data: " + data.ToString());
        string action = data["action"].ToString();
        if ("sendAnswers".Equals(action))
        {
            List<string> myAnswers = new List<string>();
            foreach (JProperty property in ((JObject)(data["info"])).Properties())
            {
                Debug.Log("property: " + property);
                myAnswers.Add(property.Value.ToString());
            }
            gameState.GetCurrentRound().answers.Add(
                new Answers(myAnswers.ToArray(), from, GenerateAnonymousPlayerName()));
            if(HasEveryoneSubmittedAnswers())
            {
                answerQuestionsPanel.SetActive(false);
                votingPanel.SetActive(true);

                SendVoting();
            }
        }
        else if ("sendWelcomeInfo".Equals(action))
        {
            if (gameState.GetNumberOfPlayers() < 6)
            {
                string name = data["info"]["name"].ToString();
                Player p = new Player(name, from, gameState.GetNumberOfPlayers(), 0);
                welcomePanels[gameState.GetNumberOfPlayers()].GetComponentInChildren<Text>().text = p.nickname;
                gameState.players.Add(from, p);
            }
        }
        else if ("sendStartGame".Equals(action))
        {
            Debug.Log("received sendStartGame");
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
                myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.3f, myTransform.position.y);
            } else
            {
                Transform myTransform = wouldYouRatherPanel.GetComponentsInChildren<Image>()[indexOfFirstIcon + playerNumber].transform;
                myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.70f, myTransform.position.y);
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
                CalculateVoting();
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

        //answers.Add(from, new Answers(myAnswers.ToArray(), from));
        //TODO SEND CONFIRMATION MESSAGE
        //AirConsole.instance.Message(from, data.ToString());
        //answers.Add(from, data.get);
    }

    /* Possible actions to send */

    public void SendWaitScreenToOnePlayer(int playerNumber)
    {
        AirConsole.instance.Message(AirConsole.instance.GetControllerDeviceIds()[0], new JsonAction("sendWaitScreen", new[] { "This is a funny quote" }));
    }

    public void SendWaitScreenToEveryone()
    {
        AirConsole.instance.Broadcast(new JsonAction("sendWaitScreen", new[] { "This is a funny quote" }));
    }

    //startRound sends one person a SendRetrieveQuestions message and sends the others would you rathers until the questions are complete
    public void StartRound()
    {
        resultsPanel.SetActive(false);
        gameState.rounds.Add(new Round());
        //controllers in the retrieve questions state will ignore would you rathers
        List<int> currentPlayerTurnDeviceId = new List<int>(gameState.players.Keys);
        SendRetrieveQuestions(currentPlayerTurnDeviceId[gameState.GetCurrentRoundNumber()]); 
        
        wouldYouRatherPanel.SetActive(true);
        InvokeRepeating("SendWouldYouRather", 0f, 8f);
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
            myTransform.position = new Vector2(canvas.GetComponent<RectTransform>().rect.width * 0.50f, myTransform.position.y);
        }
        string[] currentWouldYouRather = wouldYouRathers[Random.Range(0, wouldYouRathers.Length - 1)];
        wouldYouRatherPanel.GetComponentInChildren<Text>().text = currentWouldYouRather[0];
        //left answer
        wouldYouRatherPanel.GetComponentsInChildren<Text>()[1].text = currentWouldYouRather[1];
        //right answer
        wouldYouRatherPanel.GetComponentsInChildren<Text>()[2].text = currentWouldYouRather[2];
        //Maybe send the possible answers here
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendWouldYouRather", new[] { " " })));
    }

    public void SendQuestions() {
        string[] questionsToSend = gameState.GetCurrentRound().questions.ToArray();

        string actionData = JsonUtility.ToJson(new JsonAction("sendQuestions", questionsToSend));
        Debug.Log(actionData);

        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendQuestions", questionsToSend)));
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
            myText.text = answers.displayVotePanel();
        }
        votingPanel.GetComponentsInChildren<Text>()[2].text = gameState.GetCurrentRound().PrintQuestions();

        //send data to the phones
        List<string> listToSend = new List<string>();
        listToSend.AddRange(playerNames);
        listToSend.AddRange(anonymousPlayerNames);
        AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendVoting", listToSend.ToArray())));
    }
    
    public void CalculateVoting()
    {
        int increasedFontSize = 27;
        List<Answers> answerList = gameState.GetCurrentRound().answers;
        for (int i = 0; i < answerList.Count; i++)
        {
            List<string> namesOfCorrectPeople = new List<string>();
            Dictionary<string, List<string>> wrongGuessNameToPlayerNames = new Dictionary<string, List<string>>();

            Answers answers = answerList[i];
            string anonymousPlayerName = answers.anonymousPlayerName;
            string targetPlayerName = gameState.players[answers.deviceId].nickname;
            /*
            StringBuilder votesSB = new StringBuilder();
            //todo convert player names to player numbers in sendvoting

            
    votesSB.Append(anonymousPlayerName);
    votesSB.Append(" is ");
    votesSB.Append(targetPlayerName);
    votesSB.Append("\n");
    votesSB.Append("\n");
    */
            foreach (KeyValuePair<int, Dictionary<string, string>> playerVote in gameState.GetCurrentRound().votes)
            {
                Player p = gameState.players[playerVote.Key];
                if (playerVote.Value.ContainsKey(anonymousPlayerName))
                {
                    string currentGuess = playerVote.Value[anonymousPlayerName];
                    if (playerVote.Value.ContainsKey(anonymousPlayerName))
                    {

                        /*
                            votesSB.Append("\t");
                            votesSB.Append(p.nickname);
                            votesSB.Append(" guessed ");
                            votesSB.Append(anonymousPlayerName);
                            votesSB.Append(" was ");
                            votesSB.Append(currentGuess);
                            votesSB.Append("\n");
                            votesSB.Append("\n");
                            */
                        if (currentGuess == targetPlayerName)
                        {
                            namesOfCorrectPeople.Add("<b><size=" + increasedFontSize + "><color=green>" + p.nickname + "</color></size></b>");
                            p.points++;
                        }
                        else
                        {
                            if(!wrongGuessNameToPlayerNames.ContainsKey(currentGuess))
                            {
                                wrongGuessNameToPlayerNames.Add(currentGuess, new List<string>());
                            }
                            wrongGuessNameToPlayerNames[currentGuess].Add("<b><size=" + increasedFontSize + "><color=red>" + p.nickname + "</color></size></b>");
                        }
                    }
                } else
                {
                    /*
                    votesSB.Append("\t");
                    votesSB.Append(p.nickname);
                    votesSB.Append(" has no idea who ");
                    votesSB.Append(anonymousPlayerName);
                    votesSB.Append(" is!");
                    votesSB.Append("\n");
                    votesSB.Append("\n");
                    */
                    if (!wrongGuessNameToPlayerNames.ContainsKey("none"))
                    {
                        wrongGuessNameToPlayerNames.Add("none", new List<string>());
                    }
                    wrongGuessNameToPlayerNames["none"].Add("<b><size=" + increasedFontSize + "><color=red>" + p.nickname + "</color></size></b>");
                }
            }

            StringBuilder correctVotesSB = new StringBuilder();
            if (namesOfCorrectPeople.Count > 0)
            {
                correctVotesSB.Append(System.String.Join(", ", namesOfCorrectPeople.GetRange(0, namesOfCorrectPeople.Count - 1)));
                if (namesOfCorrectPeople.Count != 1)
                {
                    correctVotesSB.Append(" and ");
                }
                correctVotesSB.Append(namesOfCorrectPeople[namesOfCorrectPeople.Count - 1]);
            } else
            {
                correctVotesSB.Append("No one");
            }
            correctVotesSB.Append(" correctly guessed that ");
            correctVotesSB.Append(anonymousPlayerName);
            correctVotesSB.Append(" is ");
            correctVotesSB.Append(targetPlayerName);

            StringBuilder wrongVotesSb = new StringBuilder();
            foreach (KeyValuePair<string, List<string>> wrongVotes in wrongGuessNameToPlayerNames)
            {
                if("none".Equals(wrongVotes.Key))
                {
                    continue;
                }
                wrongVotesSb.Append(System.String.Join(", ", wrongVotes.Value.GetRange(0, wrongVotes.Value.Count - 1)));
                if(wrongVotes.Value.Count != 1) { 
                    wrongVotesSb.Append(" and ");
                }
                wrongVotesSb.Append(wrongVotes.Value[wrongVotes.Value.Count - 1]);
                wrongVotesSb.Append(" incorrectly guessed that ");
                wrongVotesSb.Append(anonymousPlayerName);
                wrongVotesSb.Append(" is ");
                wrongVotesSb.Append(wrongVotes.Key);
                wrongVotesSb.Append("\n");
                wrongVotesSb.Append("\n");
            }

            if (wrongGuessNameToPlayerNames.ContainsKey("none"))
            {
                List<string> noneVoters = wrongGuessNameToPlayerNames["none"];
                bool hasOneNoneVoter = noneVoters.Count == 1;
                wrongVotesSb.Append(System.String.Join(", ", noneVoters.GetRange(0, noneVoters.Count - 1)));
                if (!hasOneNoneVoter)
                {
                    wrongVotesSb.Append(" and ");
                }
                wrongVotesSb.Append(noneVoters[noneVoters.Count - 1]);
                if(hasOneNoneVoter)
                {
                    wrongVotesSb.Append(" has ");
                } else
                {
                    wrongVotesSb.Append(" have ");
                }
                wrongVotesSb.Append("no idea who ");
                wrongVotesSb.Append(anonymousPlayerName);
                wrongVotesSb.Append(" is!");
            }


            //an offset for the instructions tile and the questions tile
            int playerTileOffset = 1;
            Text myText = resultsPanel.GetComponentsInChildren<Text>()[playerTileOffset + i];
            //todo set the text size to the same size as the panel
            string tileTitle = anonymousPlayerName + " is " + targetPlayerName + "\n\n";
            myText.text = "<b><size=" + (increasedFontSize + 3) + ">" + tileTitle + "</size></b>" + correctVotesSB.ToString() + "\n\n" + wrongVotesSb.ToString();
        }
        
        if (gameState.GetCurrentRoundNumber() == gameState.GetNumberOfPlayers() - 1)
        {
            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendAdvanceToResultsScreen", new string[] { " " })));
        }
        else
        {
            AirConsole.instance.Broadcast(JsonUtility.ToJson(new JsonAction("sendNextRoundScreen", new string[] { " " })));
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
    }

    public void DisplayAnswers(Text answersDisplay)
    {
        StringBuilder sb = new StringBuilder("", 100);
        /*
        foreach (KeyValuePair<int, Answers> answer in answers)
        {

            sb.Append("Key = "+ answer.Key.ToString() + ", Value = \n");
            foreach(string a in answer.Value.text)
            {
                sb.Append(a);
                sb.Append("\n");
            }
        }
        answersDisplay.text = sb.ToString();
        */
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
        return questions[currentQuestionIndex++];
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
    private int deviceId { get; set; }
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
    public int deviceId { get; }
    public string anonymousPlayerName { get; }

    public Answers(string[] t, int di, string apn)
    {
        text = t;
        deviceId = di;
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
        sb.Append("Player Number: " + deviceId + "\n");
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
        foreach (string question in questions)
        {
            sb.Append("\t" + question);
            sb.Append("\n");
            sb.Append("\n");
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

    public GameState()
    {
        players = new Dictionary<int, Player>();
        rounds = new List<Round>();
    }

    public Player GetPlayerByPlayerNumber(int playerNumber)
    {
        foreach(Player p in players.Values)
        {
            if (playerNumber == p.playerNumber)
            {
                return p;
            }
        }
        return null;
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
What do you like to do on your phone?";
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
The Trolly Problem. Would you personally kill one person to save 5?|No|Yes
Would you be willing to sit in a room for 8 hours a day for $25 an hour?|No|Yes
Would you rather be slapped very hard, or slap someone very hard?|Be Slapped|Slap Someone
Do you believe in love at first sight?|No|Yes
Would you rather travel to the past or the future?|Past|Future";
    }
}