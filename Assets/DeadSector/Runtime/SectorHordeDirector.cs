using System;
using UnityEngine;

namespace DeadSector
{
    [Serializable]
    public sealed class SectorHordeSnapshot
    {
        public int lastTriggeredDay;
        public int lastClearedDay;
    }

    /// <summary>
    /// One nighttime horde for each configured survived-day interval.
    /// Hordes spawn near (not on top of) the player, pursue them, and grow
    /// by day. Server authority must replace this local scheduler in co-op.
    /// </summary>
    public sealed class SectorHordeDirector : MonoBehaviour
    {
        public SectorWorldClock clock;
        public SectorNavigation navigation;
        public SectorPlayer player;

        [Header("Horde Schedule")]
        [Min(1)] public int nightsBetweenAttacks = 1;
        [Min(1)] public int firstAttackDay = 1;
        [Range(16f, 23.5f)] public float warningHour = 20.5f;
        [Range(17f, 23.9f)] public float attackHour = 21f;

        [Header("Difficulty")]
        [Min(1)] public int initialZombies = 6;
        [Min(0)] public int extraZombiesPerDay = 3;
        [Min(1)] public int maxZombies = 30;

        public int LastTriggeredDay { get; private set; }
        public int LastClearedDay { get; private set; }
        public int ActiveCount { get; private set; }
        public int LastWaveSize { get; private set; }
        public bool Active => LastTriggeredDay > LastClearedDay && ActiveCount > 0;

        float nextCheck;
        float statusUntil;
        string statusText = "";

        public void Configure(
            SectorPlayer target,
            SectorWorldClock currentClock,
            SectorNavigation nav)
        {
            player = target;
            clock = currentClock;
            navigation = nav;
        }

        public bool HordeScheduledFor(int day)
        {
            return day >= firstAttackDay &&
                (day - firstAttackDay) % Mathf.Max(1, nightsBetweenAttacks) == 0;
        }

        public int ZombiesForDay(int day)
        {
            return Mathf.Clamp(
                initialZombies + Mathf.Max(0, day - firstAttackDay) *
                    extraZombiesPerDay,
                1, maxZombies);
        }

        public SectorHordeSnapshot Export() => new SectorHordeSnapshot
        {
            lastTriggeredDay = LastTriggeredDay,
            lastClearedDay = LastClearedDay
        };

        public void Import(SectorHordeSnapshot snapshot)
        {
            LastTriggeredDay = snapshot != null
                ? Mathf.Max(0, snapshot.lastTriggeredDay) : 0;
            LastClearedDay = snapshot != null
                ? Mathf.Clamp(snapshot.lastClearedDay, 0, LastTriggeredDay) : 0;

            // Current combat zombies are not yet serialized in Save V1.
            // Mark interrupted waves as concluded so loading cannot create
            // duplicate hordes; the following scheduled night still works.
            ActiveCount = 0;
            LastClearedDay = LastTriggeredDay;
            statusUntil = 0f;
        }

        void Update()
        {
            if (player == null || clock == null || navigation == null ||
                !player.Ready || player.Health <= 0 ||
                Time.time < nextCheck)
                return;

            nextCheck = Time.time + .9f;
            int today = clock.DayNumber;

            if (LastTriggeredDay > LastClearedDay)
            {
                ActiveCount = 0;
                foreach (SectorZombie zombie in
                    FindObjectsByType<SectorZombie>(FindObjectsSortMode.None))
                    if (zombie != null && !zombie.Dead && zombie.IsHorde)
                        ActiveCount++;

                if (ActiveCount == 0)
                {
                    LastClearedDay = LastTriggeredDay;
                    statusText = "HORDE REPELLED — DAY " + LastClearedDay;
                    statusUntil = Time.time + 8f;
                }
            }

            if (!HordeScheduledFor(today) ||
                LastTriggeredDay == today ||
                clock.hourOfDay < attackHour ||
                clock.hourOfDay >= 23.95f ||
                !navigation.Ready ||
                LastTriggeredDay > LastClearedDay)
                return;

            int count = navigation.SpawnHordeZombies(
                ZombiesForDay(today), 50f, today);

            if (count == 0)
            {
                // NavMesh may still be rebuilding following a tile stream.
                // Retry rather than recording a horde that never appeared.
                statusText = "HORDE WAITING FOR WALKABLE TERRAIN";
                statusUntil = Time.time + 3f;
                return;
            }

            LastTriggeredDay = today;
            ActiveCount = count;
            LastWaveSize = count;
            statusText = "HORDE INCOMING — " + count + " INFECTED";
            statusUntil = Time.time + 9f;
            Debug.Log("[Dead Sector] Night horde day " + today +
                " spawned " + count + " infected.");
        }

        public string Banner
        {
            get
            {
                if (Time.time < statusUntil)
                    return statusText;

                if (Active)
                    return "HORDE ATTACK — " + ActiveCount + " INFECTED REMAIN";

                if (clock != null && HordeScheduledFor(clock.DayNumber) &&
                    LastTriggeredDay != clock.DayNumber &&
                    clock.hourOfDay >= warningHour &&
                    clock.hourOfDay < attackHour)
                    return "NIGHT HORDE APPROACHING — PREPARE SHELTER";

                return "";
            }
        }

        void OnGUI()
        {
            if (player == null || !player.Ready)
                return;

            string message = Banner;
            if (string.IsNullOrEmpty(message))
                return;

            float width = Mathf.Min(Screen.width - 24f, 410f);
            Rect rect = new Rect(
                (Screen.width - width) * .5f, 93f, width, 36f);

            Color old = GUI.color;
            GUI.color = new Color(.75f, .21f, .18f, .94f);
            GUI.Box(rect, message);
            GUI.color = old;
        }
    }
}
