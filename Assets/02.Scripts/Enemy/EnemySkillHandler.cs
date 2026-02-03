using System;
using System.Collections;
using UnityEngine;

public class EnemySkillHandler : MonoBehaviour
{
    [Header("▼ 스킬 설정")]
    [SerializeField] private float _skillCooldown = 5f;
    [SerializeField] private float _skillRadius = 3f;
    [SerializeField] private float _skillChargeTime = 1f;
    [SerializeField] private float _skillDamageMultiplier = 2f;

    private EnemyAnimation _enemyAnimation;
    private float _lastSkillTime;
    private bool _isUsingSkill;

    public bool IsUsingSkill => _isUsingSkill;
    public float SkillRadius => _skillRadius;

    public void Initialize(EnemyAnimation animation, float attackRange)
    {
        _enemyAnimation = animation;
        _lastSkillTime = -_skillCooldown;
        _isUsingSkill = false;

        // 보스 스킬 범위 조정: 공격 범위의 5배
        _skillRadius = attackRange * 5f;
    }

    public void Reset()
    {
        _lastSkillTime = 0f;
        _isUsingSkill = false;
        StopAllCoroutines();
    }

    public bool CanUseSkill()
    {
        if (_isUsingSkill) return false;
        return Time.time - _lastSkillTime >= _skillCooldown;
    }

    public void TryUseSkill(EnemyStat stat, Action onSkillStart, Action onSkillEnd)
    {
        if (!CanUseSkill()) return;
        StartCoroutine(ExecuteSkillAttack(stat, onSkillStart, onSkillEnd));
    }

    private IEnumerator ExecuteSkillAttack(EnemyStat stat, Action onSkillStart, Action onSkillEnd)
    {
        _isUsingSkill = true;
        onSkillStart?.Invoke();

        _enemyAnimation?.TriggerSkill();

        yield return new WaitForSeconds(_skillChargeTime);

        ApplyAoEDamage(stat);

        _lastSkillTime = Time.time;
        _isUsingSkill = false;
        onSkillEnd?.Invoke();
    }

    private void ApplyAoEDamage(EnemyStat stat)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _skillRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                {
                    double skillDamage = stat.AttackDamage * _skillDamageMultiplier;
                    target.TakeDamage(skillDamage, false);
                }
            }
        }

        EffectManager.Instance?.PlaySkillVfx(transform.position);
    }
}
