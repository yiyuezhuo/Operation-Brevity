using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathLineController : MonoBehaviour
{
    public LineRenderer firstSegmentLineRenderer;
    public LineRenderer otherSegmentLineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Sync(Vector3[] positions, float firstSegmentProgress)
    {
        var show = positions.Length >= 2;
        firstSegmentLineRenderer.gameObject.SetActive(show);
        otherSegmentLineRenderer.gameObject.SetActive(show);

        if (show)
        {
            var p = firstSegmentProgress;
            var progressBreak = (1 - p) * positions[0] + p * positions[1];

            firstSegmentLineRenderer.positionCount = 2;
            firstSegmentLineRenderer.SetPositions(new[] { positions[0], progressBreak });

            positions[0] = progressBreak;

            otherSegmentLineRenderer.positionCount = positions.Length;
            otherSegmentLineRenderer.SetPositions(positions);
        }
    }
}
