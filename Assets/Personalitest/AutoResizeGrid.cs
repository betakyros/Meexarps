using UnityEngine;
using UnityEngine.UI;

public class AutoResizeGrid : MonoBehaviour
{
    public Canvas canvas;
    public GameObject container;
    public int panelsOffset;
    public int padding;
    public bool isWouldYouRather;
    public bool isAnswerQuestionsGrid;
    public bool isWelcomeScreen;
    public bool isResultsPanel;
    

    // Start is called before the first frame update
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(!container.activeInHierarchy)
        {
            return;
        }
        int numCols, numRows;

        int numPlayers = gameObject.GetComponent<main>().getNumPlayers() - (isWouldYouRather ? 1 : 0);
        if(isResultsPanel)
        {
            numPlayers = gameObject.GetComponent<main>().getCurrentRoundNumberOfAnswers();
        }
        int numGridCells = (numPlayers == 0 || isWelcomeScreen) ? 6 : numPlayers;


        //for the welcome screen keep all boxes
        if (isWelcomeScreen)
        {
            numCols = 3;
            numRows = 2;
        }
        else
        {
            switch (numPlayers)
            {
                case 1:
                    numCols = 1;
                    numRows = 1;
                    break;
                case 2:
                    numCols = 2;
                    numRows = 1;
                    break;
                case 3:
                    numCols = 2;
                    numRows = 2;
                    break;
                case 4:
                    numCols = 2;
                    numRows = 2;
                    break;
                case 5:
                    numCols = 3;
                    numRows = 2;
                    break;
                case 6:
                    numCols = 3;
                    numRows = 2;
                    break;
                default:
                    numCols = 3;
                    numRows = 2;
                    break;
            }
        }
        //transpose wouldyourather grid
        if(isWouldYouRather)
        {
            int temp = numCols;
            numCols = numRows;
            numRows = temp;
        }

        float width = container.GetComponent<RectTransform>().rect.width - padding * 2;
        float height = container.GetComponent<RectTransform>().rect.height - padding * 2;
        Vector2 newSize = new Vector2(width / numCols, height / numRows);
        container.GetComponent<GridLayoutGroup>().cellSize = newSize;
        bool isWouldYouRatherOrAnswerQuestions = isWouldYouRather || isAnswerQuestionsGrid;
        Image[] images = container.GetComponentsInChildren<Image>(!isWouldYouRather);
        //Image[] images = container.GetComponentsInChildren<Image>(!isWouldYouRatherOrAnswerQuestions);
        if(isWouldYouRatherOrAnswerQuestions)
        {
            images = main.getPlayerIconTags(images, "WouldYouRatherPlayerIcon").ToArray();
        }
        int maxNumRows = 2;
        int maxNumCols = 3;
        //activate/disable cells
        for (int j = 0; j < maxNumRows; j++)
        {
            for (int i = 0; i < maxNumCols; i++)
            {
                int nthGridElement = i + j * maxNumCols;

                int nthChild = (isWouldYouRatherOrAnswerQuestions ? 0 : panelsOffset) + nthGridElement;
                if(images.Length <= nthChild)
                {
                    //skip this round
                } else if (nthGridElement< numGridCells)
                {
                    images[nthChild].gameObject.SetActive(true);
                //} else if(!isWouldYouRatherOrAnswerQuestions)
                } else if (!isWouldYouRather)
                {
                    images[nthChild].gameObject.SetActive(false);
                }
            }
        }
        //set original position
        for (int j = 0; j < numRows; j++)
        {
            for (int i = 0; i < numCols; i++)
            {
                int nthGridElement = i + j * numCols;
                int nthChild = panelsOffset + nthGridElement;
                if (nthGridElement < numGridCells && !isWouldYouRatherOrAnswerQuestions)
                {
                    float newXPos = numCols == 2 ? (-.5f + i) * newSize.x : (i - 1f) * newSize.x;
                    float newYPos = numRows == 1 ? 0 : (.5f - j) * newSize.y;
                    Vector3 originalPos = (container.transform.position + canvas.scaleFactor * new Vector3(newXPos, newYPos, 0));
                    GentleShake gentleShake = images[nthChild].GetComponent<GentleShake>();
                    if (gentleShake != null)
                    {
                        gentleShake.SetOriginalPosition(originalPos);
                    }
                }
            }
        }

    }

}