using System;
using System.Collections.Generic;

/// <summary>
/// Tracks cleared one-time trials and repeatable training cooldowns for <see cref="ChallengeStone"/>.
/// Synced with <see cref="GameData"/> on save/load.
/// </summary>
public static class ChallengeTrialRegistry
{
    #region Runtime State
    private static readonly HashSet<string> completedOneTime = new HashSet<string>();
    private static readonly Dictionary<string, RepeatableTrialSaveEntry> repeatable =
        new Dictionary<string, RepeatableTrialSaveEntry>();
    #endregion

    #region Save / Load
    public static void ApplyFromSave(GameData data)
    {
        completedOneTime.Clear();
        repeatable.Clear();

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

        if (data.repeatableTrials == null)
            return;

        foreach (RepeatableTrialSaveEntry entry in data.repeatableTrials)
        {
            if (entry == null || string.IsNullOrEmpty(entry.trialId))
                continue;

            repeatable[entry.trialId] = entry;
        }
    }

    public static void WriteToSave(GameData data)
    {
        if (data == null)
            return;

        data.completedOneTimeTrials = new List<string>(completedOneTime);
        data.repeatableTrials = new List<RepeatableTrialSaveEntry>();

        foreach (RepeatableTrialSaveEntry entry in repeatable.Values)
            data.repeatableTrials.Add(entry);
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

    #region Repeatable Training
    public static bool IsRepeatableReady(string trialId)
    {
        if (string.IsNullOrEmpty(trialId))
            return true;

        if (!repeatable.TryGetValue(trialId, out RepeatableTrialSaveEntry entry))
            return true;

        if (entry.nextAvailableUtcTicks <= 0)
            return true;

        return DateTime.UtcNow.Ticks >= entry.nextAvailableUtcTicks;
    }

    public static float GetRepeatableCooldownRemaining(string trialId)
    {
        if (string.IsNullOrEmpty(trialId)
            || !repeatable.TryGetValue(trialId, out RepeatableTrialSaveEntry entry))
            return 0f;

        if (entry.nextAvailableUtcTicks <= 0)
            return 0f;

        long remainingTicks = entry.nextAvailableUtcTicks - DateTime.UtcNow.Ticks;
        return remainingTicks <= 0 ? 0f : (float)TimeSpan.FromTicks(remainingTicks).TotalSeconds;
    }

    public static void StartRepeatableCooldown(string trialId, float seconds)
    {
        if (string.IsNullOrEmpty(trialId))
            return;

        if (!repeatable.TryGetValue(trialId, out RepeatableTrialSaveEntry entry))
        {
            entry = new RepeatableTrialSaveEntry { trialId = trialId };
            repeatable[trialId] = entry;
        }

        entry.nextAvailableUtcTicks = seconds > 0f
            ? DateTime.UtcNow.AddSeconds(seconds).Ticks
            : 0L;
    }

    public static bool IsQuestClearClaimed(string trialId)
    {
        return !string.IsNullOrEmpty(trialId)
               && repeatable.TryGetValue(trialId, out RepeatableTrialSaveEntry entry)
               && entry.questClearClaimed;
    }

    public static void MarkQuestClearClaimed(string trialId)
    {
        if (string.IsNullOrEmpty(trialId))
            return;

        if (!repeatable.TryGetValue(trialId, out RepeatableTrialSaveEntry entry))
        {
            entry = new RepeatableTrialSaveEntry { trialId = trialId };
            repeatable[trialId] = entry;
        }

        entry.questClearClaimed = true;
    }
    #endregion
}
