using System;
using System.Collections.Generic;
using UnityEngine;

public class BackroomsTestGameManager : MonoBehaviour
{
    [Serializable]
    public class CeilingGroup
    {
        public string name = "Group";
        public Transform[] roots;
        public CeilingLightController[] directCeilings;

        public List<CeilingLightController> Resolve()
        {
            HashSet<CeilingLightController> set = new HashSet<CeilingLightController>();

            if (roots != null)
            {
                foreach (var root in roots)
                {
                    if (!root) continue;

                    var found = root.GetComponentsInChildren<CeilingLightController>(true);
                    foreach (var c in found)
                    {
                        if (c) set.Add(c);
                    }
                }
            }

            if (directCeilings != null)
            {
                foreach (var c in directCeilings)
                {
                    if (c) set.Add(c);
                }
            }

            return new List<CeilingLightController>(set);
        }
    }

    [Serializable]
    public class NamedFlag
    {
        public string name = "Flag";
        public bool value;
    }

    public enum TriggerMode
    {
        OnStart,
        DelayAfterStart,
        ManualInspectorBool,
        WhenFlagBecomesTrue,
        WhenFlagBecomesFalse,
        WhileFlagTrueEvery,
        WhileFlagFalseEvery
    }

    public enum ActionMode
    {
        SetGroupOn,
        SetGroupSecondary,
        SetGroupAllPairsFlicker,
        StartGroupSurge,
        StopGroupSurge,
        SetGroupSuppressed,
        SetGroupProximitySuppressed,
        SetGameObjectActive,
        SetDarknessActive
    }

    [Serializable]
    public class ActivationRule
    {
        public string label = "Rule";
        public bool enabled = true;

        [Header("Trigger")]
        public TriggerMode triggerMode = TriggerMode.OnStart;
        public int flagIndex = -1;
        public bool manualActivate;
        public float delaySeconds = 1f;
        public float repeatEvery = 1f;
        public bool runOnce = true;

        [Header("Action")]
        public ActionMode actionMode = ActionMode.SetGroupOn;
        public int groupIndex = -1;
        public bool boolValue = true;

        [Header("Surge settings")]
        public bool surgeUseSecondary = true;
        public bool surgeOverrideParams = true;
        public Vector2 surgeInterval = new Vector2(0.02f, 0.08f);
        public Vector2 surgeRange = new Vector2(0.05f, 1.35f);

        [Header("Optional direct targets")]
        public GameObject gameObjectTarget;
        public MovingDarknessController darknessTarget;

        [NonSerialized] public bool fired;
        [NonSerialized] public float startedAt;
        [NonSerialized] public float lastRepeatAt;
        [NonSerialized] public bool previousFlagValue;
    }

    public CeilingGroup[] ceilingGroups;
    public NamedFlag[] flags;
    public ActivationRule[] activations;

    void Start()
    {
        if (activations == null) return;

        for (int i = 0; i < activations.Length; i++)
        {
            activations[i].startedAt = Time.time;
            activations[i].lastRepeatAt = Time.time;
            activations[i].previousFlagValue = GetFlagValue(activations[i].flagIndex);
        }
    }

    void Update()
    {
        if (activations == null) return;

        for (int i = 0; i < activations.Length; i++)
        {
            var rule = activations[i];
            if (rule == null || !rule.enabled) continue;
            if (rule.runOnce && rule.fired) continue;

            bool currentFlag = GetFlagValue(rule.flagIndex);
            bool fireNow = false;

            switch (rule.triggerMode)
            {
                case TriggerMode.OnStart:
                    fireNow = !rule.fired;
                    break;

                case TriggerMode.DelayAfterStart:
                    fireNow = !rule.fired && (Time.time - rule.startedAt >= rule.delaySeconds);
                    break;

                case TriggerMode.ManualInspectorBool:
                    fireNow = rule.manualActivate;
                    break;

                case TriggerMode.WhenFlagBecomesTrue:
                    fireNow = currentFlag && !rule.previousFlagValue;
                    break;

                case TriggerMode.WhenFlagBecomesFalse:
                    fireNow = !currentFlag && rule.previousFlagValue;
                    break;

                case TriggerMode.WhileFlagTrueEvery:
                    fireNow = currentFlag && (Time.time - rule.lastRepeatAt >= rule.repeatEvery);
                    break;

                case TriggerMode.WhileFlagFalseEvery:
                    fireNow = !currentFlag && (Time.time - rule.lastRepeatAt >= rule.repeatEvery);
                    break;
            }

            if (fireNow)
            {
                FireRule(rule);

                if (rule.runOnce)
                    rule.fired = true;

                rule.lastRepeatAt = Time.time;

                if (rule.triggerMode == TriggerMode.ManualInspectorBool)
                    rule.manualActivate = false;
            }

            rule.previousFlagValue = currentFlag;
        }
    }

    bool GetFlagValue(int index)
    {
        if (flags == null || index < 0 || index >= flags.Length) return false;
        return flags[index].value;
    }

    void FireRule(ActivationRule rule)
    {
        switch (rule.actionMode)
        {
            case ActionMode.SetGroupOn:
                ApplyToGroup(rule.groupIndex, c => c.SetOn(rule.boolValue));
                break;

            case ActionMode.SetGroupSecondary:
                ApplyToGroup(rule.groupIndex, c => c.SetUseSecondary(rule.boolValue));
                break;

            case ActionMode.SetGroupAllPairsFlicker:
                ApplyToGroup(rule.groupIndex, c => c.SetFlicker(rule.boolValue));
                break;

            case ActionMode.StartGroupSurge:
                ApplyToGroup(rule.groupIndex, c =>
                    c.BeginSurge(rule.surgeUseSecondary, rule.surgeOverrideParams, rule.surgeInterval, rule.surgeRange));
                break;

            case ActionMode.StopGroupSurge:
                ApplyToGroup(rule.groupIndex, c => c.EndSurge());
                break;

            case ActionMode.SetGroupSuppressed:
                ApplyToGroup(rule.groupIndex, c => c.SetGroupSuppressed(rule.boolValue));
                break;

            case ActionMode.SetGroupProximitySuppressed:
                ApplyToGroup(rule.groupIndex, c => c.SetProximitySuppressed(rule.boolValue));
                break;

            case ActionMode.SetGameObjectActive:
                if (rule.gameObjectTarget) rule.gameObjectTarget.SetActive(rule.boolValue);
                break;

            case ActionMode.SetDarknessActive:
                if (rule.darknessTarget) rule.darknessTarget.SetControllerActive(rule.boolValue);
                break;
        }
    }

    void ApplyToGroup(int groupIndex, Action<CeilingLightController> action)
    {
        if (ceilingGroups == null || groupIndex < 0 || groupIndex >= ceilingGroups.Length) return;

        var resolved = ceilingGroups[groupIndex].Resolve();
        foreach (var ceiling in resolved)
        {
            if (!ceiling) continue;
            action(ceiling);
        }
    }

    public void SetFlag(int index, bool value)
    {
        if (flags == null || index < 0 || index >= flags.Length) return;
        flags[index].value = value;
    }

    public void SetFlag(string flagName, bool value)
    {
        if (flags == null) return;

        for (int i = 0; i < flags.Length; i++)
        {
            if (flags[i] != null && flags[i].name == flagName)
            {
                flags[i].value = value;
                return;
            }
        }
    }
}