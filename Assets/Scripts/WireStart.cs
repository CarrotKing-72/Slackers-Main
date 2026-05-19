using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class WireStart : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Wire References")]
    public RectTransform wireRect;
    public string wireID;
    public float thickness = 10f;
    public float retractSpeed = 600f;

    [Header("Particle Settings")]
    public GameObject wireParticlePrefab;
    public int particleCount = 5;
    private List<GameObject> particles = new List<GameObject>();

    private RectTransform parentRect; // Parent RectTransform
    private Vector2 startPos;
    private bool wireConnected = false;

    private void Start()
    {
        if (wireRect == null)
        {
            Debug.LogError("Wire RectTransform not assigned!");
            return;
        }

        parentRect = wireRect.parent.GetComponent<RectTransform>();
        wireRect.gameObject.SetActive(false);

        // Instantiate particle dots
        if (wireParticlePrefab != null)
        {
            for (int i = 0; i < particleCount; i++)
            {
                GameObject p = Instantiate(wireParticlePrefab, wireRect);
                p.transform.localPosition = Vector3.zero;
                p.SetActive(false);
                particles.Add(p);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        wireRect.gameObject.SetActive(true);

        // Convert screen point to local coordinates relative to wireRect's parent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out startPos);

        wireRect.anchoredPosition = startPos;
        wireRect.sizeDelta = new Vector2(0, thickness);

        foreach (var p in particles) p.SetActive(true);
        wireConnected = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (wireConnected) return;

        Vector2 currentPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, eventData.pressEventCamera, out currentPos);

        Vector2 diff = currentPos - startPos;
        float dist = diff.magnitude;

        wireRect.sizeDelta = new Vector2(dist, thickness);
        wireRect.anchoredPosition = startPos + diff * 0.5f;

        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        wireRect.rotation = Quaternion.Euler(0, 0, angle);

        // Position particles along the line
        for (int i = 0; i < particles.Count; i++)
        {
            float t = (float)(i + 1) / (particles.Count + 1);
            particles[i].transform.localPosition = new Vector3(diff.x * t, diff.y * t, 0f);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (wireConnected) return;

        PointerEventData p = new PointerEventData(EventSystem.current) { position = eventData.position };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(p, results);

        foreach (var r in results)
        {
            var connector = r.gameObject.GetComponent<WireConnector>();
            if (connector != null && connector.connectorID == wireID)
            {
                SnapWire(connector);
                return;
            }
        }

        StartCoroutine(RetractWire());
    }

    private void SnapWire(WireConnector connector)
    {
        if (connector == null) return;

        wireConnected = true;

        Vector2 endPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, connector.GetComponent<RectTransform>().position, null, out endPos);

        Vector2 diff = endPos - startPos;
        float dist = diff.magnitude;

        wireRect.sizeDelta = new Vector2(dist, thickness);
        wireRect.anchoredPosition = startPos + diff * 0.5f;
        wireRect.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

        connector.connectedWire = wireRect;

        // Notify minigame
        WireMinigame minigame = FindFirstObjectByType<WireMinigame>();
        if (minigame != null) minigame.WireConnected(connector);

        foreach (var p in particles) p.SetActive(true);
    }

    private IEnumerator RetractWire()
    {
        Vector2 mid = wireRect.anchoredPosition;
        float length = wireRect.sizeDelta.x;

        while (length > 1f)
        {
            length -= retractSpeed * Time.deltaTime;
            if (length < 0) length = 0;

            Vector2 dir = (mid - startPos).normalized;
            Vector2 newMid = startPos + dir * (length / 2f);

            wireRect.sizeDelta = new Vector2(length, thickness);
            wireRect.anchoredPosition = newMid;

            yield return null;
        }

        if (!wireConnected)
        {
            wireRect.gameObject.SetActive(false);
            foreach (var p in particles) p.SetActive(false);
        }
    }

    public void ResetWire()
    {
        wireConnected = false;
        wireRect.gameObject.SetActive(false);
        foreach (var p in particles) p.SetActive(false);
    }
}