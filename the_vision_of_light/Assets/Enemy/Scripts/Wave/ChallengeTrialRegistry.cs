using System.Collections.Generic;

namespace VisionOfLight.Enemy
{
    /// <summary>
    /// Tracks cleared one-time trials and quest-gated challenge entries for <see cref="ChallengeStone"/>.
    /// </summary>
    public static class ChallengeTrialRegistry
    {
        #region Runtime State
        private static readonly HashSet<string> completedOneTime = new HashSet<string>();
        private static readonly HashSet<string> completedQuestChallenges = new HashSet<string>();
        #endregion

        #region Save / Load
        public static void ApplyFromSave(GameData data)
        {
            completedOneTime.Clear();
            completedQuestChallenges.Clear();

            if (data == null)
                return;

            if (data.completedOneTimeTrials != null)
            {
                foreach (string id in data.completedOneTimeTrials)
                {
                    if (!string.IsNullOrEmpty(id))
                        completedOneTime.Add(id);
                }
            }

            if (data.completedQuestChallenges != null)
            {
                foreach (string key in data.completedQuestChallenges)
                {
                    if (!string.IsNullOrEmpty(key))
                        completedQuestChallenges.Add(key);
                }
            }
        }

        public static void WriteToSave(GameData data)
        {
            if (data == null)
                return;

            data.completedOneTimeTrials = new List<string>(completedOneTime);
            data.completedQuestChallenges = new List<string>(completedQuestChallenges);
        }
        #endregion

        #region One-Time Trials
        public static bool IsOneTimeCompleted(string trialId)
        {
            return !string.IsNullOrEmpty(trialId) && completedOneTime.Contains(trialId);
        }

        public static void MarkOneTimeCompleted(string trialId)
        {
            if (string.IsNullOrEmpty(trialId))
                return;

            completedOneTime.Add(trialId);
        }
        #endregion

        #region Quest-Gated Challenges
        public static string BuildQuestChallengeKey(string trialId, int questState, int questStep)
        {
            return $"{trialId}:{questState}:{questStep}";
        }

        public static bool IsQuestChallengeCompleted(string trialId, int questState, int questStep)
        {
            if (string.IsNullOrEmpty(trialId))
                return false;

            return completedQuestChallenges.Contains(BuildQuestChallengeKey(trialId, questState, questStep));
        }

        public static void MarkQuestChallengeCompleted(string trialId, int questState, int questStep)
        {
            if (string.IsNullOrEmpty(trialId))
                return;

            completedQuestChallenges.Add(BuildQuestChallengeKey(trialId, questState, questStep));
        }

        /// <summary>Clears one quest-gated wave entry so the stone can be retried (testing).</summary>
        public static void ClearQuestChallenge(string trialId, int questState, int questStep)
        {
            if (string.IsNullOrEmpty(trialId))
                return;

            completedQuestChallenges.Remove(BuildQuestChallengeKey(trialId, questState, questStep));
        }

        /// <summary>Clears a one-time trial id so the stone can be retried (testing).</summary>
        public static void ClearOneTimeTrial(string trialId)
        {
            if (string.IsNullOrEmpty(trialId))
                return;

            completedOneTime.Remove(trialId);
        }

        /// <summary>Clears all remembered trial progress in this play session.</summary>
        public static void ClearAll()
        {
            completedOneTime.Clear();
            completedQuestChallenges.Clear();
        }
        #endregion
    }
}
