using Exiled.API.Interfaces;

namespace ArithFeather.AssistInfection {
	public class Config : IConfig {
		public const int DeadPlayerCacheSize = 30;

		public bool IsEnabled { get; set; } = true;

		public bool DisplayTimer { get; set; } = true;

		public string LanguageCultureInfo { get; set; } = "en-US";

		public string TimerFormat { get; set; } =
			"<size=50>{0} can be resurrected for <color=#44444>{1}</color> more seconds.</size>";
	}
}
