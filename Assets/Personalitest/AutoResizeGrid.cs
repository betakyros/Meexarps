using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AutoResizeGrid : MonoBehaviour
{
    public GameObject container;
    public int numRows;
    public int numCols;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float width = container.GetComponent<RectTransform>().rect.width;
        float height = container.GetComponent<RectTransform>().rect.height;
        Vector2 newSize = new Vector2(width / numCols, height / numRows);
        container.GetComponent<GridLayoutGroup>().cellSize = newSize;
    }
}
