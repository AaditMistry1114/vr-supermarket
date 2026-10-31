using UnityEngine;

/// <summary>
/// Raycasts from the camera centre to detect ProductItem and CheckoutTrigger objects.
/// Shows HUD prompts and handles the E key interaction.
/// Attach to the FPSPlayer GameObject.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast")]
    public float interactionRange = 3.5f;

    [Header("Camera")]
    [Tooltip("Drag the Camera child transform here.")]
    public Transform cameraTransform;

    // Current detected targets
    private ProductItem     currentProduct  = null;
    private CheckoutTrigger currentCheckout = null;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        // Don't interact while any UI panel is open
        if (StoreManager.Instance != null && StoreManager.Instance.IsAnyPanelOpen)
        {
            ClearAll();
            return;
        }

        // Don't interact until the game has started (welcome screen dismissed)
        var pc = GetComponent<PlayerController>();
        if (pc != null && !pc.IsInputEnabled)
        {
            ClearAll();
            return;
        }

        Scan();
    }

    // ─── Detection ────────────────────────────────────────────────────────────

    private void Scan()
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
        {
            // ── Product ────────────────────────────────────────────────────────
            var product = hit.collider.GetComponent<ProductItem>();
            if (product != null)
            {
                if (currentProduct != product)
                {
                    ClearAll();
                    currentProduct = product;
                    currentProduct.SetHighlight(true);
                }
                HUDController.Instance?.ShowPrompt(product.GetInteractionPrompt());

                if (Input.GetKeyDown(KeyCode.E))
                    PickUp(product);
                return;
            }

            // ── Checkout counter ───────────────────────────────────────────────
            var checkout = hit.collider.GetComponent<CheckoutTrigger>();
            if (checkout != null)
            {
                ClearProductHighlight();
                currentCheckout = checkout;
                HUDController.Instance?.ShowPrompt(checkout.GetPrompt());

                if (Input.GetKeyDown(KeyCode.E) && checkout.CanCheckout())
                    CheckoutManager.Instance?.OpenCheckout();
                return;
            }
        }

        // Nothing detected
        ClearAll();
        HUDController.Instance?.HidePrompt();
    }

    // ─── Actions ──────────────────────────────────────────────────────────────

    private void PickUp(ProductItem item)
    {
        if (item == null || item.productData == null) return;

        ShoppingCart.Instance?.AddItem(item.productData);
        UIAudioManager.Instance?.PlayCartAdd();
        HUDController.Instance?.HidePrompt();

        item.SetHighlight(false);
        item.gameObject.SetActive(false);    // "Remove" from shelf
        currentProduct = null;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void ClearProductHighlight()
    {
        if (currentProduct != null)
        {
            currentProduct.SetHighlight(false);
            currentProduct = null;
        }
    }

    private void ClearAll()
    {
        ClearProductHighlight();
        currentCheckout = null;
        HUDController.Instance?.HidePrompt();
    }
}
