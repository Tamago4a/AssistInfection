using ServerMod2.API;
using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.EventHandlers;
using Smod2.Events;
using Smod2.EventSystem.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ArithFeather.AssistInfection
{
	[PluginDetails(
		author = "Arith",
		name = "Assist Infection",
		description = "Delays your spawn if there's a chance for SCP049 to turn you into a zombie.",
		id = "ArithFeather.AssistInfection",
		configPrefix = "afai",
		version = "1.1",
		SmodMajor = 3,
		SmodMinor = 4,
		SmodRevision = 0
		)]
	public class AssistInfection : Plugin, IEventHandlerUpdate, IEventHandlerWaitingForPlayers, IEventHandlerSetRole, IEventHandlerInfected, IEventHandlerRecallZombie
	{
		[ConfigOption] private readonly bool disablePlugin = false;
		[ConfigOption] private readonly bool displayTimer = true;
		[ConfigOption] private readonly string timerFormat = "<size=50>{0} can be resurrected for <color=#44444>{1}</color> more seconds.</size>";

		public override void Register() =>AddEventHandlers(this);
		public override void OnEnable() => Info("Zombie Spawn Delay Enabled");
		public override void OnDisable() => Info("Zombie Spawn Delay Disabled");

		private List<Infected> infectedPlayers;
		private List<Infected> InfectedPlayers => infectedPlayers ?? (infectedPlayers = new List<Infected>(20));
		private float infectedTimer;

		private class Infected
		{
			public Infected(Player player, Player killer, float infectionTimer)
			{
				Player = player;
				Killer = killer;
				InfectionTimer = infectionTimer;
			}

			public Player Player { get; }
			public Player Killer { get; }
			public float InfectionTimer { set; get; }

			/// <summary>
			/// When the timer reaches 0, spawn this player.
			/// </summary>
			public bool ShouldHaveSpawned { get; set; } = false;

			public Role SavedRole { get; set; }
		}

		private bool isAPlayerDead;

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			if (disablePlugin)
			{
				PluginManager.DisablePlugin(this);
				return;
			}

			cachedBroadcast = GameObject.Find("Host").GetComponent<Broadcast>();
			InfectedPlayers.Clear();
			isAPlayerDead = false;
		}



		/// <summary>
		/// Ticks down the zboy timers, if they reach 0, remove them, if they reach 0 and are "active", 
		/// meaning they were supposed to spawn already, spawn them to what they were suppoed to be.
		/// </summary>
		public void OnUpdate(UpdateEvent ev)
		{
			if (!isAPlayerDead) return;

			var deltaTime = Time.deltaTime;
			var showTime = false;

			if (displayTimer)
			{
				infectedTimer -= deltaTime;
				if (infectedTimer <= 0)
				{
					infectedTimer = 1;
					showTime = true;
				}
			}

			for (var i = InfectedPlayers.Count - 1; i >= 0; i--)
			{
				try
				{
					var zboy = InfectedPlayers[i];
					var player = zboy.Player;

					zboy.InfectionTimer -= deltaTime;

					if (showTime && zboy.InfectionTimer >= 0)
					{
						PersonalBroadcast(zboy.Killer, 1, string.Format(timerFormat, zboy.Player.Name, (int)zboy.InfectionTimer));
					}

					var go = (zboy.Killer.GetGameObject() as GameObject);
					var hpPerc = go.GetComponent<PlayerStats>().GetHealthPercent();
					var recallSpeed = (zboy.Killer.GetGameObject() as GameObject).GetComponent<Scp049PlayerScript>().boost_recallTime.Evaluate(hpPerc);

					if (zboy.InfectionTimer <= -recallSpeed)
					{
						InfectedPlayers.RemoveAt(i);

						if (InfectedPlayers.Count == 0)
						{
							isAPlayerDead = false;
						}

						if (zboy.ShouldHaveSpawned)
						{
							zboy.Player.ChangeRole(zboy.SavedRole);
						}
					}
				}
				catch // Catch disconnected players
				{
					InfectedPlayers.RemoveAt(i);

					if (InfectedPlayers.Count == 0)
					{
						isAPlayerDead = false;
					}
				}
			}
		}

		/// <summary>
		/// If the player 
		/// </summary>
		/// <param name="ev"></param>
		public void OnSetRole(PlayerSetRoleEvent ev)
		{
			if (!isAPlayerDead || ev.Role == Role.SPECTATOR) return;

			for (int i = InfectedPlayers.Count - 1; i >= 0; i--)
			{
				var zBoy = InfectedPlayers[i];

				if (zBoy.Player.PlayerId == ev.Player.PlayerId)
				{
					zBoy.ShouldHaveSpawned = true;
					zBoy.SavedRole = ev.Role;

					ev.Role = Role.SPECTATOR;

					return;
				}
			}
		}

		/// <summary>
		/// Called when player dies to 049. Saves that player to make sure they can't spawn before the infection runs out.
		/// </summary>
		/// <param name="ev"></param>
		public void OnPlayerInfected(PlayerInfectedEvent ev)
		{
			var scp = ev.Attacker;

			if (isAPlayerDead)
			{
				// Try to find
				var infectedCount = InfectedPlayers.Count;
				for (int i = 0; i < infectedCount; i++)
				{
					var infectedPlayer = InfectedPlayers[i];
					if (infectedPlayer.Killer.PlayerId == scp.PlayerId)
					{
						InfectedPlayers[i] = new Infected(ev.Player, scp, ev.InfectTime);

						if (infectedPlayer.ShouldHaveSpawned)
						{
							infectedPlayer.Player.ChangeRole(infectedPlayer.SavedRole);
						}
						return;
					}
				}
			}

			InfectedPlayers.Add(new Infected(ev.Player, scp, ev.InfectTime));
			isAPlayerDead = true;
		}

		private readonly FieldInfo cachedPlayerConnFieldInfo = typeof(SmodPlayer).GetField("conn", BindingFlags.NonPublic | BindingFlags.Instance);
		private Broadcast cachedBroadcast;
		private void PersonalBroadcast(Player player, uint duration, string message)
		{
			var connection = cachedPlayerConnFieldInfo.GetValue(player) as NetworkConnection;

			if (connection == null)
			{
				return;
			}

			cachedBroadcast.CallTargetAddElement(connection, message, duration, false);
		}

		public void OnRecallZombie(PlayerRecallZombieEvent ev)
		{
			for (var i = InfectedPlayers.Count - 1; i >= 0; i--)
			{
				var infected = infectedPlayers[i];

				if (infected.Player.PlayerId == ev.Target.PlayerId)
				{
					InfectedPlayers.RemoveAt(i);

					if (InfectedPlayers.Count == 0)
					{
						isAPlayerDead = false;
					}

					return;
				}
			}
		}
	}
}