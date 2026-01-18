using System;

public interface IRedDotCondition
{
    RedDotKey Key { get; }
    bool CheckCondition();
    event Action OnConditionChanged;
}
