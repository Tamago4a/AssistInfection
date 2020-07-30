using Exiled.API.Interfaces;

namespace ArithFeather.AssistInfection {
	public class Config : IConfig{
		public bool IsEnabled { get; set; } = true;
		public bool DisplayTimer { get; set; } = true;

		public string TimerFormat { get; set; } =
			"<size=50>{0} can be resurrected for <color=#44444>{1}</color> more seconds.</size>";
	}
}
