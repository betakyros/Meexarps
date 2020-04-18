using UnityEngine;
using UnityEngine.UI;

public class AutoResizeGrid : MonoBehaviour
{
    public Canvas canvas;
    public GameObject container;
    public int numRows;
    public int numCols;
    public int panelsOffset;
    public int padding;
    

    // Start is called before the first frame update
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float width = container.GetComponent<RectTransform>().rect.width - padding * 2;
        float height = container.GetComponent<RectTransform>().rect.height - padding * 2;
        Vector2 newSize = new Vector2(width / numCols, height / numRows);
        container.GetComponent<GridLayoutGroup>().cellSize = newSize;

        for (int j = 0; j < numRows; j++)
        {
                for (int i = 0; i < numCols; i++)
            {
                int nthChild = panelsOffset + i + j * numCols;
                if (nthChild < container.GetComponentsInChildren<Image>().Length)
                {
                    Vector3 originalPos = (container.transform.position + canvas.scaleFactor * new Vector3(((i - 1f) * newSize.x), (.5f - j) * newSize.y, 0));
                    container.GetComponentsInChildren<Image>()[panelsOffset + i + j * numCols].GetComponent<GentleShake>()
                        .SetOriginalPosition(originalPos);
                }
            }
        }

    }

}