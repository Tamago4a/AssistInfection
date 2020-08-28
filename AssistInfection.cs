using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Exiled.API.Features;
using MEC;

namespace ArithFeather.AssistInfection {
	public class AssistInfection : Plugin<Config> {

		public AssistInfection() {
			VictimOf049.InitializePool();
		}

		private readonly Dictionary<byte, VictimOf049> _infectedPlayers =
			new Dictionary<byte, VictimOf049>(Config.DeadPlayerCacheSize);

		public override string Author => "Arith";
		public override Version Version => new Version("2.01");

		public CultureInfo CachedCultureInfo { get; private set; }

		public override void OnEnabled() {
			base.OnEnabled();

			CachedCultureInfo = CultureInfo.GetCultureInfo(Config.LanguageCultureInfo);

			Exiled.Events.Handlers.Server.RoundEnded += Server_RoundEnded;
			Exiled.Events.Handlers.Player.Died += Player_Died;
			Exiled.Events.Handlers.Scp049.FinishingRecall += Scp049_FinishingRecall;
			Exiled.Events.Handlers.Player.ChangingRole += Player_ChangingRole;
		}

		public override void OnDisabled() {
			Exiled.Events.Handlers.Server.RoundEnded -= Server_RoundEnded;
			Exiled.Events.Handlers.Player.Died -= Player_Died;
			Exiled.Events.Handlers.Scp049.FinishingRecall -= Scp049_FinishingRecall;
			Exiled.Events.Handlers.Player.ChangingRole -= Player_ChangingRole;

			base.OnDisabled();
		}

		private void Server_RoundEnded(Exiled.Events.EventArgs.RoundEndedEventArgs ev) {
			VictimOf049.Reset();
			_infectedPlayers.Clear();
		}

		/// <summary>
		/// Make sure an infected player doesn't respawn.
		/// </summary>
		private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev) {
			if (_infectedPlayers.Count == 0) return;
			var player = ev.Player;
			var playerId = (byte)player.Id;
			if (_infectedPlayers.TryGetValue(playerId, out var victim)) {
				victim.ShouldHaveSpawned = true;
				victim.SavedRole = ev.NewRole;
				ev.NewRole = RoleType.Spectator;
			}
		}

		/// <summary>
		/// Save a player killed by SCP 049.
		/// </summary>
		private void Player_Died(Exiled.Events.EventArgs.DiedEventArgs ev) {

			var killer = ev.Killer;

			if (killer.Role != RoleType.Scp049) return;

			var player = ev.Target;

			_infectedPlayers.Add((byte)player.Id, VictimOf049.SetNewVictim(player, killer, this));
		}

		/// <summary>
		/// Remove a player from the list if they were recalled
		/// </summary>
		private void Scp049_FinishingRecall(Exiled.Events.EventArgs.FinishingRecallEventArgs ev) {
			var playerId = (byte)ev.Target.Id;
			if (ev.IsAllowed && _infectedPlayers.ContainsKey(playerId)) {
				Timing.KillCoroutines(_infectedPlayers[playerId].Coroutine);
				RemoveInfectedPlayerFromList(playerId);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RemoveInfectedPlayerFromList(byte id) => _infectedPlayers.Remove(id);
	}
}