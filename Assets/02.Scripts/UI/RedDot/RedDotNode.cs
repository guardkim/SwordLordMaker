using System;
using System.Collections.Generic;

public class RedDotNode
{
    private readonly RedDotKey _key;
    private readonly List<RedDotNode> _children = new();
    private readonly List<IRedDotCondition> _conditions = new();

    private RedDotNode _parent;
    private bool _isActive;

    public RedDotKey Key => _key;
    public bool IsActive => _isActive;
    public IReadOnlyList<RedDotNode> Children => _children;

    public event Action<RedDotKey, bool> OnStateChanged;

    public RedDotNode(RedDotKey key)
    {
        _key = key;
    }

    public void AddChild(RedDotNode child)
    {
        if (child == null || _children.Contains(child))
        {
            return;
        }

        child._parent = this;
        _children.Add(child);
        child.OnStateChanged += HandleChildStateChanged;
    }

    public void RemoveChild(RedDotNode child)
    {
        if (child == null || !_children.Contains(child))
        {
            return;
        }

        child._parent = null;
        child.OnStateChanged -= HandleChildStateChanged;
        _children.Remove(child);
    }

    public void AddCondition(IRedDotCondition condition)
    {
        if (condition == null || _conditions.Contains(condition))
        {
            return;
        }

        _conditions.Add(condition);
        condition.OnConditionChanged += Evaluate;
    }

    public void RemoveCondition(IRedDotCondition condition)
    {
        if (condition == null || !_conditions.Contains(condition))
        {
            return;
        }

        condition.OnConditionChanged -= Evaluate;
        _conditions.Remove(condition);
    }

    public void Evaluate()
    {
        bool previousState = _isActive;
        _isActive = CheckSelfConditions() || CheckChildrenActive();

        if (previousState != _isActive)
        {
            OnStateChanged?.Invoke(_key, _isActive);
            _parent?.Evaluate();
        }
    }

    public void ForceEvaluate()
    {
        foreach (var child in _children)
        {
            child.ForceEvaluate();
        }
        Evaluate();
    }

    private bool CheckSelfConditions()
    {
        foreach (var condition in _conditions)
        {
            if (condition.CheckCondition())
            {
                return true;
            }
        }
        return false;
    }

    private bool CheckChildrenActive()
    {
        foreach (var child in _children)
        {
            if (child.IsActive)
            {
                return true;
            }
        }
        return false;
    }

    private void HandleChildStateChanged(RedDotKey key, bool isActive)
    {
        Evaluate();
    }

    public void Clear()
    {
        foreach (var condition in _conditions)
        {
            condition.OnConditionChanged -= Evaluate;
        }
        _conditions.Clear();

        foreach (var child in _children)
        {
            child.OnStateChanged -= HandleChildStateChanged;
            child.Clear();
        }
        _children.Clear();
    }
}
