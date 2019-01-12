using System.Threading.Tasks;

namespace AzureServices {
	public interface IBingSpeechService
	{
		Task<SpeechResult> RecognizeSpeechAsync(string filename);
	}
}
