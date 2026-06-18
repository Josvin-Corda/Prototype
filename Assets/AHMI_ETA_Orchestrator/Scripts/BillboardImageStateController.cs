using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Controls the billboard background display by swapping full-screen images 
/// based on simulation ETA events.
/// </summary>
public class BillboardImageStateController : MonoBehaviour
{
    private enum BillboardVisualState
    {
        Default,
        RecallActive,
        VehicleArrived
    }

    [Header("UI Renderers")]
    [SerializeField] private Image[] dashboardImages;

    [Header("State Sprites")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite recallActiveSprite;
    [SerializeField] private Sprite vehicleArrivedSprite;

    private BillboardVisualState currentState = BillboardVisualState.Default;

    private void Start()
    {
        ApplyState(BillboardVisualState.Default);
    }

    private void OnEnable()
    {
        SimulationEvents.OnETAUpdated += OnETAUpdatedReceived;
    }

    private void OnDisable()
    {
        SimulationEvents.OnETAUpdated -= OnETAUpdatedReceived;
    }

    private void OnETAUpdatedReceived(int secondsRemaining, string pickupTag, string statusMessage)
    {
        // Do not allow later stale positive ETA events to return the dashboard to RecallActive after VehicleArrived
        if (currentState == BillboardVisualState.VehicleArrived)
        {
            return;
        }

        if (secondsRemaining <= 0)
        {
            ApplyState(BillboardVisualState.VehicleArrived);
        }
        else if (secondsRemaining > 0 && currentState == BillboardVisualState.Default)
        {
            ApplyState(BillboardVisualState.RecallActive);
        }
    }

    /// <summary>
    /// Restores the billboard background to the default image and resets the internal state.
    /// </summary>
    public void ResetToDefault()
    {
        ApplyState(BillboardVisualState.Default);
    }

    private void ApplyState(BillboardVisualState state)
    {
        currentState = state;
        Sprite targetSprite = GetSpriteForState(state);

        if (dashboardImages != null)
        {
            foreach (var img in dashboardImages)
            {
                if (img != null)
                {
                    img.sprite = targetSprite;
                }
            }
        }
    }

    private Sprite GetSpriteForState(BillboardVisualState state)
    {
        switch (state)
        {
            case BillboardVisualState.Default:
                return defaultSprite;
            case BillboardVisualState.RecallActive:
                return recallActiveSprite;
            case BillboardVisualState.VehicleArrived:
                return vehicleArrivedSprite;
            default:
                return defaultSprite;
        }
    }
}
