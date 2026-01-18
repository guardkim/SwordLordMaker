using System;
using System.Collections.Generic;
using UnityEngine;

public class RedDotManager : DontDestroySingleton<RedDotManager>
{
    private readonly Dictionary<RedDotKey, RedDotNode> _nodes = new();
    private readonly List<IRedDotCondition> _registeredConditions = new();

    public event Action<RedDotKey, bool> OnRedDotStateChanged;

    protected override void Initialize()
    {
        BuildTree();
        RegisterConditions();
        EvaluateAll();
    }

    private void BuildTree()
    {
        // 메인 메뉴
        var mainMenu = CreateNode(RedDotKey.MainMenu);

        // 강화 트리
        var upgrade = CreateNode(RedDotKey.Upgrade);
        mainMenu.AddChild(upgrade);

        var upgradePlayer = CreateNode(RedDotKey.UpgradePlayer);
        upgrade.AddChild(upgradePlayer);

        upgradePlayer.AddChild(CreateNode(RedDotKey.UpgradePlayerHealth));
        upgradePlayer.AddChild(CreateNode(RedDotKey.UpgradePlayerMoveSpeed));

        var upgradeSword = CreateNode(RedDotKey.UpgradeSword);
        upgrade.AddChild(upgradeSword);

        upgradeSword.AddChild(CreateNode(RedDotKey.UpgradeSwordAttackDamage));
        upgradeSword.AddChild(CreateNode(RedDotKey.UpgradeSwordCooldown));
        upgradeSword.AddChild(CreateNode(RedDotKey.UpgradeSwordMoveSpeed));
        upgradeSword.AddChild(CreateNode(RedDotKey.UpgradeSwordCritChance));
        upgradeSword.AddChild(CreateNode(RedDotKey.UpgradeSwordCritDamage));

        // 탭 트리
        var tabHero = CreateNode(RedDotKey.TabHero);
        mainMenu.AddChild(tabHero);
        tabHero.AddChild(upgradePlayer);

        var tabSword = CreateNode(RedDotKey.TabSword);
        mainMenu.AddChild(tabSword);
        tabSword.AddChild(upgradeSword);

        // 상점 트리
        var shop = CreateNode(RedDotKey.Shop);
        mainMenu.AddChild(shop);

        shop.AddChild(CreateNode(RedDotKey.ShopFreeReward));
        shop.AddChild(CreateNode(RedDotKey.ShopDailyDeal));

        // 인벤토리 트리
        var inventory = CreateNode(RedDotKey.Inventory);
        mainMenu.AddChild(inventory);

        inventory.AddChild(CreateNode(RedDotKey.InventoryNewItem));

        // 퀘스트 트리
        var quest = CreateNode(RedDotKey.Quest);
        mainMenu.AddChild(quest);

        quest.AddChild(CreateNode(RedDotKey.QuestDaily));
        quest.AddChild(CreateNode(RedDotKey.QuestWeekly));
        quest.AddChild(CreateNode(RedDotKey.QuestAchievement));

        // 우편함 트리
        var mail = CreateNode(RedDotKey.Mail);
        mainMenu.AddChild(mail);

        mail.AddChild(CreateNode(RedDotKey.MailReward));
    }

    private void RegisterConditions()
    {
        // 강화 조건 등록
        RegisterUpgradeCondition(RedDotKey.UpgradePlayerHealth, UpgradeId.PlayerHealth.ToKey());
        RegisterUpgradeCondition(RedDotKey.UpgradePlayerMoveSpeed, UpgradeId.PlayerMoveSpeed.ToKey());
        RegisterUpgradeCondition(RedDotKey.UpgradeSwordAttackDamage, UpgradeId.SwordAttackDamage.ToKey());
        RegisterUpgradeCondition(RedDotKey.UpgradeSwordCooldown, UpgradeId.SwordCooldown.ToKey());
        RegisterUpgradeCondition(RedDotKey.UpgradeSwordMoveSpeed, UpgradeId.SwordMoveSpeed.ToKey());
        RegisterUpgradeCondition(RedDotKey.UpgradeSwordCritChance, UpgradeId.SwordCritChance.ToKey());
        RegisterUpgradeCondition(RedDotKey.UpgradeSwordCritDamage, UpgradeId.SwordCritDamage.ToKey());
    }

    private void RegisterUpgradeCondition(RedDotKey key, string upgradeId)
    {
        var condition = new UpgradeRedDotCondition(key, upgradeId);
        RegisterCondition(key, condition);
    }

    private RedDotNode CreateNode(RedDotKey key)
    {
        if (_nodes.ContainsKey(key))
        {
            Debug.LogWarning($"[RedDotManager] 중복 키: {key}");
            return _nodes[key];
        }

        var node = new RedDotNode(key);
        node.OnStateChanged += HandleNodeStateChanged;
        _nodes[key] = node;
        return node;
    }

    public void RegisterCondition(RedDotKey key, IRedDotCondition condition)
    {
        if (!_nodes.TryGetValue(key, out RedDotNode node))
        {
            Debug.LogWarning($"[RedDotManager] 노드를 찾을 수 없습니다: {key}");
            return;
        }

        node.AddCondition(condition);
        _registeredConditions.Add(condition);
    }

    public void UnregisterCondition(RedDotKey key, IRedDotCondition condition)
    {
        if (!_nodes.TryGetValue(key, out RedDotNode node))
        {
            return;
        }

        node.RemoveCondition(condition);
        _registeredConditions.Remove(condition);
    }

    public bool IsActive(RedDotKey key)
    {
        if (_nodes.TryGetValue(key, out RedDotNode node))
        {
            return node.IsActive;
        }
        return false;
    }

    public void Evaluate(RedDotKey key)
    {
        if (_nodes.TryGetValue(key, out RedDotNode node))
        {
            node.Evaluate();
        }
    }

    public void EvaluateAll()
    {
        if (_nodes.TryGetValue(RedDotKey.MainMenu, out RedDotNode root))
        {
            root.ForceEvaluate();
        }
    }

    public void Subscribe(RedDotKey key, Action<RedDotKey, bool> callback)
    {
        if (_nodes.TryGetValue(key, out RedDotNode node))
        {
            node.OnStateChanged += callback;
            callback?.Invoke(key, node.IsActive);
        }
    }

    public void Unsubscribe(RedDotKey key, Action<RedDotKey, bool> callback)
    {
        if (_nodes.TryGetValue(key, out RedDotNode node))
        {
            node.OnStateChanged -= callback;
        }
    }

    private void HandleNodeStateChanged(RedDotKey key, bool isActive)
    {
        OnRedDotStateChanged?.Invoke(key, isActive);
    }

    private void OnDestroy()
    {
        foreach (var node in _nodes.Values)
        {
            node.Clear();
        }
        _nodes.Clear();
        _registeredConditions.Clear();
    }
}
