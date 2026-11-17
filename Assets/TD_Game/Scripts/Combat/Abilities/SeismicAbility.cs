using System.Collections;
using UnityEngine;

namespace TD_Game
{
    /// <summary>
    /// Seismic Jump: the player crouches, leaps high spinning, hangs for a heartbeat, crashes down and
    /// the ground cracks. A shockwave travels along the floor and hits every enemy when it reaches it,
    /// throwing them in the air. The timing of each beat is editable in the asset.
    /// </summary>
    [CreateAssetMenu(menuName = "TD_Game/Abilities/Seismic Jump", fileName = "Ability_SeismicJump")]
    public class SeismicAbility : CombatAbility
    {
        [Min(0f)] public float damageMultiplier = 1.2f;

        [Header("Choreography")]
        [Tooltip("Crouch before jumping (anticipation).")]
        [Min(0.05f)] public float crouchTime = 0.22f;
        [Tooltip("Jump height in tiles (only the sprite rises; the shadow stays on the ground).")]
        [Min(0.5f)] public float jumpHeight = 1.8f;
        [Min(0.1f)] public float riseTime = 0.38f;
        [Tooltip("Moment frozen at the top before the slam.")]
        [Min(0f)] public float hangTime = 0.14f;
        [Min(0.05f)] public float slamTime = 0.1f;
        [Tooltip("Full turns while rising (0 = no spin).")]
        [Min(0)] public int spins = 1;

        [Header("Shockwave")]
        [Tooltip("Speed of the shockwave (tiles/s): farther enemies are hit later.")]
        [Min(1f)] public float waveSpeed = 10f;
        [Tooltip("How high the enemies are thrown by the wave (tiles).")]
        [Min(0f)] public float launchHeight = 0.7f;

        protected override IEnumerator Execute(CombatContext context)
        {
            var player = context.Player;
            var fx = ActorFx.Of(player);
            var sprite = context.PlayerSprite;
            Vector3 feet = player.transform.position;
            float damage = player.AttackDamage(damageMultiplier);
            CombatFeed.ShowBanner(displayName + "!", 1.2f);
            var weapon = MacuahuitlRig.Active(player);

            // 1. Crouch: gather strength.
            fx.Pose = new Vector2(1.35f, 0.62f);
            fx.Tremble = 0.015f;
            if (weapon != null) context.Run(weapon.RaiseTo(150f, crouchTime));        // weapon low behind, ready to swing up
            Vfx.Dust(feet, 4, MexicaPalette.Ash, 0.6f);
            yield return new WaitForSeconds(crouchTime);
            fx.Tremble = 0f;

            // 2. Launch: stretched upwards, spinning, leaving a trail.
            fx.Pose = Vector2.one;
            fx.Squash(new Vector2(0.65f, 1.45f));
            Vfx.Ring(feet, MexicaPalette.Ash, 0.2f, 0.8f, 0.3f, onGround: true, squashY: 0.45f);
            Vfx.Dust(feet, 6, MexicaPalette.Ash, 1f);
            CombatFeed.RequestShake(0.05f, 0.15f);
            if (weapon != null) context.Run(weapon.RaiseTo(15f, riseTime * 0.6f));     // swung overhead on the way up
            for (float t = 0f; t < riseTime; t += Time.deltaTime)
            {
                float k = t / riseTime;
                fx.Height = jumpHeight * (1f - (1f - k) * (1f - k));      // ease-out: slows down near the top
                fx.Lean = -360f * spins * k;
                if (Time.frameCount % 2 == 0) Vfx.Afterimage(sprite, MexicaPalette.AgedGold, 0.2f);
                yield return null;
            }
            fx.Lean = 0f;
            fx.Height = jumpHeight;

            // 3. Hang: a heartbeat in the air, the Macuahuitl gleams.
            fx.Squash(new Vector2(1.15f, 0.9f));
            if (weapon != null) { context.Run(weapon.RaiseTo(40f, hangTime)); context.Run(weapon.Glow(MexicaPalette.AgedGold, hangTime + slamTime)); }
            Vfx.Ring(sprite != null ? sprite.transform.position : feet + Vector3.up * jumpHeight,
                MexicaPalette.AgedGold, 0.1f, 0.9f, 0.25f);
            yield return new WaitForSeconds(hangTime);

            // 4. Slam: falls fast, stretched downwards.
            fx.Squash(new Vector2(0.7f, 1.4f));
            if (weapon != null)   // the Macuahuitl comes down first and is driven into the ground
                context.Run(weapon.Strike(new MacuahuitlTechnique("Seismic", 40f, -175f, slamTime, 0f, 0f), null));
            for (float t = 0f; t < slamTime; t += Time.deltaTime)
            {
                float k = t / slamTime;
                fx.Height = jumpHeight * (1f - k * k);                     // ease-in: accelerates into the floor
                Vfx.Afterimage(sprite, MexicaPalette.Bone, 0.12f);
                yield return null;
            }
            fx.Height = 0f;

            // 5. Impact.
            fx.Squash(new Vector2(1.6f, 0.5f));
            CombatFeed.RequestHitStop(0.12f);
            CombatFeed.RequestShake(0.32f, 0.55f);
            Vfx.Crack(feet, 2.4f);
            Vfx.Dust(feet, 10, MexicaPalette.Ash, 1.6f);
            Vfx.Burst(feet + Vector3.up * 0.1f, 22, MexicaPalette.DarkGold, new Vector2(2f, 6f), 16f, 1.8f, 0.9f);

            float reach = 1.5f;
            foreach (var enemy in context.Enemies)
                if (enemy != null && !enemy.Health.IsDead)
                    reach = Mathf.Max(reach, Vector2.Distance(feet, enemy.transform.position) + 1f);
            float waveTime = reach / waveSpeed;
            Vfx.Ring(feet, MexicaPalette.AgedGold, 0.3f, reach, waveTime, onGround: true, squashY: 0.45f);
            Vfx.Ring(feet, MexicaPalette.Bone, 0.2f, reach * 0.8f, waveTime * 1.2f, onGround: true, squashY: 0.45f);

            // 6. The wave reaches each enemy in turn.
            float longestDelay = 0f;
            foreach (var enemy in context.Enemies)
            {
                if (enemy == null || enemy.Health.IsDead) continue;
                float delay = Vector2.Distance(feet, enemy.transform.position) / waveSpeed;
                longestDelay = Mathf.Max(longestDelay, delay);
                context.Run(HitAfter(context, enemy, damage, delay, feet));
            }

            yield return new WaitForSeconds(0.25f);
            fx.Squash(new Vector2(0.9f, 1.1f));                        // stands up again
            yield return new WaitForSeconds(Mathf.Max(0f, longestDelay - 0.25f) + 0.45f);
        }

        IEnumerator HitAfter(CombatContext context, CombatEnemy enemy, float damage, float delay, Vector3 origin)
        {
            yield return new WaitForSeconds(delay);
            if (enemy == null || enemy.Health.IsDead) yield break;
            Vector3 at = enemy.transform.position;
            Vfx.Burst(at, 10, MexicaPalette.DarkGold, new Vector2(1.5f, 4f), 14f, 1.5f, 0.7f);
            Vfx.Dust(at, 3, MexicaPalette.Ash, 0.8f);
            context.DamageEnemy(enemy, damage);
            if (enemy.Health.IsDead) yield break;

            // Thrown in the air by the wave.
            var fx = ActorFx.Of(enemy);
            fx.Squash(new Vector2(0.75f, 1.3f));
            const float airTime = 0.45f;
            for (float t = 0f; t < airTime; t += Time.deltaTime)
            {
                float k = t / airTime;
                fx.Height = 4f * launchHeight * k * (1f - k);
                fx.Lean = Mathf.Sin(k * Mathf.PI) * 20f * Mathf.Sign(at.x - origin.x);
                yield return null;
            }
            fx.Height = 0f;
            fx.Lean = 0f;
            fx.Squash(new Vector2(1.3f, 0.75f));
            Vfx.Dust(at, 2, MexicaPalette.Ash, 0.5f);
        }
    }
}
