using System.Collections.Generic;
using Exiled.API.Features;
using MEC;
using PlayableScps;
using static MEC.Timing;

namespace ArithFeather.AssistInfection
{
	public class VictimOf049 {

		public void Initialize(Player player, Player killer, AssistInfection plugin) {
			_player = player;
			_killer = killer;
			_infectionTimer = Scp049.ReviveEligibilityDuration;
			_plugin = plugin;

			Coroutine = RunCoroutine(StartCountdown());
		}

		private Player _player;
		private Player _killer;
		private AssistInfection _plugin;
		private float _infectionTimer;

		/// <summary>
		/// When the timer reaches 0, spawn this player.
		/// </summary>
		public bool ShouldHaveSpawned { get; set; } = false;

		public RoleType SavedRole { get; set; }

		public CoroutineHandle Coroutine { get; private set; }

		private IEnumerator<float> StartCountdown()
		{
			do
			{
				if (_plugin.Config.DisplayTimer)
				{
					_killer.ClearBroadcasts();
					_killer.Broadcast(1,
						string.Format(_plugin.Config.TimerFormat, _player.Nickname, ((int) _infectionTimer)).ToString(_plugin.CachedCultureInfo));
				}

				yield return WaitForSeconds(1);
				_infectionTimer -= 1;

			} while (_infectionTimer >= 0);

			yield return WaitForSeconds(Scp049.TimeToRevive);

			_plugin.RemoveInfectedPlayerFromList((byte)_player.Id);

			if (ShouldHaveSpawned)
			{
				_player.SetRole(SavedRole);
			}
		}

		#region LazyPool

		private static readonly VictimOf049[] CachedClasses = new VictimOf049[Config.DeadPlayerCacheSize];

		public static void InitializePool() {
			for (int i = 0; i < Config.DeadPlayerCacheSize; i++) {
				CachedClasses[i] = new VictimOf049();
			}
		}

		public static VictimOf049 SetNewVictim(Player player, Player killer, AssistInfection plugin) {
			for (int i = 0; i < Config.DeadPlayerCacheSize; i++) {
				var c = CachedClasses[i];

				if (c.Coroutine.IsRunning) continue;

				c.Initialize(player, killer, plugin);
				return c;
			}

			var createNew = new VictimOf049();
			createNew.Initialize(player, killer, plugin);
			return createNew;
		}

		public static void Reset()
		{
			for (int i = 0; i < Config.DeadPlayerCacheSize; i++) {
				var c = CachedClasses[i];

				c._player = null;
				c._killer = null;

				if (c.Coroutine.IsRunning)
					KillCoroutines(c.Coroutine);
			}
		}

		#endregion
	}
}
