using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoResizeGrid : MonoBehaviour
{
    public GameObject container;
    public int numRows;
    public int numCols;
    public int panelsOffset;

    // Start is called before the first frame update
    void Awake()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float width = container.GetComponent<RectTransform>().rect.width;
        float height = container.GetComponent<RectTransform>().rect.height;
        Vector2 newSize = new Vector2(width / numCols, height / numRows);
        container.GetComponent<GridLayoutGroup>().cellSize = newSize;

        for (int i = 0; i < numCols; i++)
        {
            for (int j = 0; j < numRows; j++)
            {
                //j * numCols + i

                container.GetComponentsInChildren<Image>()[panelsOffset + i + j * numCols].GetComponent<GentleShake>()
                    .SetOriginalPosition(container.transform.position + new Vector3((i - 1f) * newSize.x, (.5f - j) * newSize.y, 0f));
            }
        }

    }

}