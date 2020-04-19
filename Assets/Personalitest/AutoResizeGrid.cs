using UnityEngine;
using UnityEngine.UI;

public class AutoResizeGrid : MonoBehaviour
{
    public Canvas canvas;
    public GameObject container;
    public int panelsOffset;
    public int padding;
    

    // Start is called before the first frame update
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        int numCols, numRows;

        int numPlayers = gameObject.GetComponent<main>().getNumPlayers();
        int numGridCells = numPlayers == 0 ? 6 : numPlayers;

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
        float width = container.GetComponent<RectTransform>().rect.width - padding * 2;
        float height = container.GetComponent<RectTransform>().rect.height - padding * 2;
        Vector2 newSize = new Vector2(width / numCols, height / numRows);
        container.GetComponent<GridLayoutGroup>().cellSize = newSize;
        Image[] images = container.GetComponentsInChildren<Image>(true);

        int maxNumRows = 2;
        int maxNumCols = 3;
        //activate/disable cells
        for (int j = 0; j < maxNumRows; j++)
        {
            for (int i = 0; i < maxNumCols; i++)
            {
                int nthGridElement = i + j * maxNumCols;
                int nthChild = panelsOffset + nthGridElement;
                if(nthGridElement< numGridCells)
                {
                    images[nthChild].gameObject.SetActive(true);
                } else
                {
                    images[nthChild].gameObject.SetActive(false);
                }
            }
        }
        //set position
        for (int j = 0; j < numRows; j++)
        {
            for (int i = 0; i < numCols; i++)
            {
                int nthGridElement = i + j * numCols;
                int nthChild = panelsOffset + nthGridElement;
                if (nthGridElement < numGridCells)
                {
                    float newXPos = numCols == 2 ? (-.5f + i) * newSize.x : (i - 1f) * newSize.x;
                    float newYPos = numRows == 1 ? 0 : (.5f - j) * newSize.y;
                    Vector3 originalPos = (container.transform.position + canvas.scaleFactor * new Vector3(newXPos, newYPos, 0));
                    images[nthChild].GetComponent<GentleShake>()
                        .SetOriginalPosition(originalPos);
                }
            }
        }

    }

}