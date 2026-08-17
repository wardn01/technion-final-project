using UnityEngine;
using TMPro;

namespace VisionOfLight.Chest
{
    /// <summary>Interact prompt UI for <see cref="WorldChest"/>.</summary>
    public partial class WorldChest
    {
        #region Interaction
        private void ShowOpenPrompt()
        {
            ResolveSharedInteractUi();
            SharedInteractPromptUtility.Show(
                this, promptRoot, promptContainer, interactKeyPrompt, promptTextUI, openPromptText);
            SetLetterBadgeVisible(true);
        }

        private void HidePrompt()
        {
            HideInteractPrompt();
        }

        private void HideInteractPrompt()
        {
            SharedInteractPromptUtility.Hide(this, promptContainer, interactKeyPrompt);
        }

        private void SetLetterBadgeVisible(bool visible)
        {
            Transform letter = FindLetterBadge();
            if (letter == null)
                return;

            if (letter.gameObject.activeSelf != visible)
                letter.gameObject.SetActive(visible);
        }

        private Transform FindLetterBadge()
        {
            if (promptContainer != null)
            {
                Transform letter = promptContainer.transform.Find("F");
                if (letter != null)
                    return letter;
            }

            if (promptRoot != null)
            {
                Transform letter = promptRoot.transform.Find("F");
                if (letter != null)
                    return letter;
            }

            return null;
        }

        private void ResolvePromptRoot()
        {
            if (promptRoot != null || promptContainer == null)
                return;

            Transform parent = promptContainer.transform.parent;
            if (parent != null)
                promptRoot = parent.gameObject;
        }

        private void ResolveSharedInteractUi()
        {
            FixSwappedPromptReferences();

            if (promptContainer == null && promptRoot != null)
            {
                foreach (Transform descendant in promptRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (descendant.name != "Interact_F")
                        continue;

                    if (descendant.Find("btn") == null)
                        continue;

                    promptContainer = descendant.gameObject;
                    break;
                }
            }

            if (promptRoot == null && promptContainer != null && promptContainer.transform.parent != null)
                promptRoot = promptContainer.transform.parent.gameObject;

            if (interactKeyPrompt == null && promptContainer != null)
            {
                Transform btn = promptContainer.transform.Find("btn");
                if (btn != null)
                    interactKeyPrompt = btn.gameObject;
            }

            if (promptTextUI == null && promptContainer != null)
                promptTextUI = promptContainer.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        /// <summary>
        /// Common inspector mistake: Prompt Root / Prompt Container swapped
        /// (Interact_F assigned as root, InteractPrompt as container).
        /// </summary>
        private void FixSwappedPromptReferences()
        {
            if (promptRoot == null || promptContainer == null)
                return;

            bool rootIsInteractF = promptRoot.name.IndexOf("Interact_F", System.StringComparison.OrdinalIgnoreCase) >= 0
                                   || promptRoot.transform.Find("btn") != null;
            bool containerIsPromptRoot = promptContainer.name.IndexOf("InteractPrompt", System.StringComparison.OrdinalIgnoreCase) >= 0
                                         || promptContainer.transform.Find("Interact_F") != null;

            if (!rootIsInteractF || !containerIsPromptRoot)
                return;

            GameObject swap = promptRoot;
            promptRoot = promptContainer;
            promptContainer = swap;
        }

        private static bool IsUiBlockingInteraction()
        {
            if (Time.timeScale == 0f)
                return true;

            if (ShopManager.Instance != null
                && ShopManager.Instance.shopPanel != null
                && ShopManager.Instance.shopPanel.activeSelf)
                return true;

            if (UIManager.Instance != null && UIManager.Instance.isDialogueOpen)
                return true;

            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.isPaused)
                return true;

            return false;
        }
        #endregion
    }
}
