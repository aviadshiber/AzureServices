using System.Threading.Tasks;

namespace AzureServices {
	public interface IAuthenticationService
	{
		Task InitializeAsync();
		string GetAccessToken();
	}
}
